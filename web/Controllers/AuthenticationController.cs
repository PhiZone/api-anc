using System.Security.Claims;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;
using PhiZoneApi.Constants;
using PhiZoneApi.Dtos.Deliverers;
using PhiZoneApi.Dtos.Requests;
using PhiZoneApi.Dtos.Responses;
using PhiZoneApi.Enums;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Services;
using PhiZoneApi.Utils;
using StackExchange.Redis;
using static OpenIddict.Abstractions.OpenIddictConstants;

// ReSharper disable InvertIf

namespace PhiZoneApi.Controllers;

[ApiController]
[ApiVersion("2.0")]
[Route("auth")]
[Produces("application/json")]
public class AuthenticationController(
    UserManager<User> userManager,
    IConnectionMultiplexer redis,
    IMailService mailService,
    IResourceService resourceService,
    ITapTapService tapTapService,
    ITapGhostService tapGhostService,
    IUserRepository userRepository,
    IApplicationRepository applicationRepository,
    INotificationRepository notificationRepository,
    IChartRepository chartRepository,
    ISongRepository songRepository,
    IChartSubmissionRepository chartSubmissionRepository,
    ISongSubmissionRepository songSubmissionRepository,
    IPetAnswerRepository petAnswerRepository,
    IDtoMapper dtoMapper,
    AuthProviderFactory factory,
    ILogger<AuthenticationController> logger) : Controller
{
    /// <summary>
    ///     Retrieves authentication credentials.
    /// </summary>
    /// <returns>Authentication credentials, e.g. <c>access_token</c>, <c>refresh_token</c>, etc.</returns>
    /// <remarks>
    ///     This is one of the only two endpoints where fields are named in snake case, both in the request and in the
    ///     response.
    ///     It's also one that responds without following the <see cref="ResponseDto{T}" /> structure.
    ///     Refer to RFC 6749 for further information.
    /// </remarks>
    /// <response code="200">Returns authentication credentials.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When any of the client credentials is invalid.</response>
    /// <response code="403">
    ///     When the user
    ///     1. has input an incorrect password (<c>invalid_grant</c>);
    ///     2. is temporarily locked out (<c>temporarily_unavailable</c>);
    ///     3. is permanently locked out (<c>access_denied</c>);
    ///     4. has not yet confirmed their email address (<c>interaction_required</c>), in which case the client should direct
    ///     the user to request a confirmation mail.
    /// </response>
    /// <response code="404">
    ///     When
    ///     1. using Direct mode and the user has input an email address that does not match any existing user;
    ///     2. using any other mode and the union ID / remote user ID does not match any existing user.
    /// </response>
    [HttpPost("token")]
    [IgnoreAntiforgeryToken]
    [Consumes("application/x-www-form-urlencoded")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(OpenIddictTokenResponseDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(OpenIddictErrorDto))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(OpenIddictErrorDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(OpenIddictErrorDto))]
    public async Task<IActionResult> Exchange([FromForm] OpenIddictTokenRequestDto dto,
        [FromQuery] string? provider = null, [FromQuery] string? redirectUri = null,
        [FromQuery] Guid? tapApplicationId = null, [FromQuery] string? token = null)
    {
        var request = HttpContext.GetOpenIddictServerRequest()!;
        if (token != null)
        {
            var db = redis.GetDatabase();
            var key = $"phizone:login:{token}";
            if (!await db.KeyExistsAsync(key))
                return Forbid(
                    new AuthenticationProperties(new Dictionary<string, string>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidToken,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "The specified token is invalid."
                    }!), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            var userId = await db.StringGetAsync($"phizone:login:{token}");
            var user = await userManager.FindByIdAsync(userId!);
            if (user == null)
                return Forbid(
                    new AuthenticationProperties(new Dictionary<string, string>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidToken,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "The specified token is invalid."
                    }!), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            await db.KeyDeleteAsync(key);

            var actionResult = await CheckUserLockoutState(user);
            if (actionResult != null) return actionResult;
            return await Login(user, "Token");
        }

        if (provider != null)
        {
            if (!Enum.TryParse(typeof(AuthProvider), provider, true, out var providerEnum))
                return Forbid(
                    new AuthenticationProperties(new Dictionary<string, string>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidClient,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "The specified authentication provider does not exist."
                    }!), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            var authProvider = factory.GetAuthProvider(providerEnum as AuthProvider?);
            if (authProvider == null)
                return Forbid(
                    new AuthenticationProperties(new Dictionary<string, string>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidClient,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "The specified authentication provider does not exist."
                    }!), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            var result = await authProvider.RequestTokenAsync(dto.username!, dto.password!, redirectUri: redirectUri);
            if (result == null)
                return Forbid(
                    new AuthenticationProperties(new Dictionary<string, string>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidRequest,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "Unable to log into the specified provider with provided credentials."
                    }!), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            var user = await authProvider.GetIdentityAsync(result.Value.Item1);
            if (user == null)
                return Forbid(
                    new AuthenticationProperties(new Dictionary<string, string>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidToken,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "Unable to find a user with provided credentials."
                    }!), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            var actionResult = await CheckUserLockoutState(user);
            if (actionResult != null) return actionResult;
            return await Login(user, providerEnum.ToString()!);
        }

        if (tapApplicationId != null)
        {
            if (!await applicationRepository.ApplicationExistsAsync(tapApplicationId.Value))
                return Forbid(
                    new AuthenticationProperties(new Dictionary<string, string>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidClient,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "The specified application does not exist."
                    }!), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            if ((await applicationRepository.GetApplicationAsync(tapApplicationId.Value)).TapClientId == null)
                return Forbid(
                    new AuthenticationProperties(new Dictionary<string, string>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidClient,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "The specified application does not have a client ID for TapTap."
                    }!), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            var response = await tapTapService.Login(new TapTapRequestDto
            {
                MacKey = dto.username!, AccessToken = dto.password!, ApplicationId = tapApplicationId.Value
            });

            if (response == null)
                return Forbid(
                    new AuthenticationProperties(new Dictionary<string, string>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidRequest,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "Insufficient data to contact TapTap."
                    }!), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            if (!response.IsSuccessStatusCode)
                return Forbid(
                    new AuthenticationProperties(new Dictionary<string, string>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.AccessDenied,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "Unable to log into TapTap with provided credentials."
                    }!), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            var responseDto =
                JsonConvert.DeserializeObject<TapTapDelivererDto>(await response.Content.ReadAsStringAsync())!;
            var user = await userRepository.GetUserByTapUnionIdAsync(tapApplicationId.Value, responseDto.Data.Unionid);
            if (user != null)
            {
                var actionResult = await CheckUserLockoutState(user);
                if (actionResult != null) return actionResult;
                return await Login(user, "TapTap");
            }

            var ghost = await tapGhostService.GetGhost(tapApplicationId.Value, responseDto.Data.Unionid);
            if (ghost == null)
                ghost = new TapGhost
                {
                    ApplicationId = tapApplicationId.Value,
                    UnionId = responseDto.Data.Unionid,
                    UserName = responseDto.Data.Name,
                    Avatar = responseDto.Data.Avatar,
                    Experience = 0,
                    Rks = 0,
                    DateLastLoggedIn = DateTimeOffset.UtcNow,
                    DateJoined = DateTimeOffset.UtcNow
                };
            else
                ghost.DateLastLoggedIn = DateTimeOffset.UtcNow;

            await tapGhostService.ModifyGhost(ghost);

            var identity = new ClaimsIdentity(TokenValidationParameters.DefaultAuthenticationType, Claims.Name,
                Claims.Role);

            identity.AddClaim(Claims.Subject, CriticalValues.TapTapGhostUserId.ToString());
            identity.AddClaim(Claims.Name, ghost.UserName);
            identity.AddClaim("appId", tapApplicationId.Value.ToString());
            identity.AddClaim("unionId", responseDto.Data.Unionid);
            identity.SetDestinations(GetDestinations);

            var claimsPrincipal = new ClaimsPrincipal(identity);
            claimsPrincipal.SetScopes(Scopes.OfflineAccess);

            logger.LogInformation(LogEvents.UserInfo, "New login (TapTap Ghost): {UserName}", ghost.UserName);

            return SignIn(claimsPrincipal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        if (request.IsPasswordGrantType())
        {
            var user = await userManager.FindByEmailAsync(request.Username!);
            if (user == null)
                return Forbid(
                    new AuthenticationProperties(new Dictionary<string, string>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidToken,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "Unable to find a user with provided credentials."
                    }!), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            if (!resourceService.HasPermission(user, UserRole.Member))
                return Forbid(
                    new AuthenticationProperties(new Dictionary<string, string>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InsufficientAccess,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "You do not have enough permission to perform the action."
                    }!), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            var actionResult = await CheckUserLockoutState(user);
            if (actionResult != null) return actionResult;

            var isPasswordCorrect = true;
            try
            {
                if (!await userManager.CheckPasswordAsync(user, request.Password!)) isPasswordCorrect = false;
            }
            catch (FormatException)
            {
                if (!ObsoletePasswordUtil.Check(request.Password!, user.PasswordHash!))
                    isPasswordCorrect = false;
                else
                    user.PasswordHash = userManager.PasswordHasher.HashPassword(user, request.Password!);
            }

            if (!isPasswordCorrect)
            {
                user.AccessFailedCount++;
                await userManager.UpdateAsync(user);

                return Forbid(
                    new AuthenticationProperties(new Dictionary<string, string>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "The password is incorrect."
                    }!), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            return await Login(user, "Password");
        }

        if (request.IsRefreshTokenGrantType())
        {
            var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            var user = await userManager.FindByIdAsync(result.Principal!.GetClaim(Claims.Subject)!);
            if (user == null)
            {
                var appId = Guid.Parse(result.Principal!.GetClaim("appId")!);
                var unionId = result.Principal!.GetClaim("unionId")!;
                var ghost = await tapGhostService.GetGhost(appId, unionId);
                if (ghost == null)
                    return Forbid(
                        new AuthenticationProperties(new Dictionary<string, string>
                        {
                            [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.ExpiredToken,
                            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                                "The refresh token has expired."
                        }!), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

                ghost.DateLastLoggedIn = DateTimeOffset.UtcNow;

                await tapGhostService.ModifyGhost(ghost);
                var ghostIdentity = new ClaimsIdentity(result.Principal!.Claims,
                    TokenValidationParameters.DefaultAuthenticationType, Claims.Name, Claims.Role);

                ghostIdentity.SetDestinations(GetDestinations);

                return SignIn(new ClaimsPrincipal(ghostIdentity),
                    OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            var actionResult = await CheckUserLockoutState(user);
            if (actionResult != null) return actionResult;

            var identity = new ClaimsIdentity(result.Principal!.Claims,
                TokenValidationParameters.DefaultAuthenticationType, Claims.Name, Claims.Role);

            identity.SetClaim(Claims.Subject, user.Id.ToString());
            identity.SetClaim(Claims.Name, user.UserName!);
            identity.SetDestinations(GetDestinations);

            user.DateLastLoggedIn = DateTimeOffset.UtcNow;
            await userManager.UpdateAsync(user);

            return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        return Forbid(
            new AuthenticationProperties(new Dictionary<string, string>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidRequest,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                    "The specified 'grant_type' is not supported."
            }!), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    /// <summary>
    ///     Revokes a refresh token.
    /// </summary>
    /// <returns>An empty json object.</returns>
    /// <remarks>
    ///     This is one of the only two endpoints where fields are named in snake case, both in the request and in the
    ///     response.
    ///     It's also one that responds without following the <see cref="ResponseDto{T}" /> structure.
    ///     Refer to RFC 6749 for further information.
    /// </remarks>
    /// <response code="200">Returns an empty json object.</response>
    /// <response code="400">When any of the parameters is missing.</response>
    /// <response code="401">When any of the client credentials is invalid.</response>
    [HttpPost("revoke")]
    [IgnoreAntiforgeryToken]
    [Consumes("application/x-www-form-urlencoded")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(OpenIddictErrorDto))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(OpenIddictErrorDto))]
// ReSharper disable once UnusedParameter.Global
    public IActionResult Revoke([FromForm] OpenIddictRevocationRequestDto dto)
    {
        return Ok();
    }

    /// <summary>
    ///     Requests identity from an authentication provider.
    /// </summary>
    /// <returns>A service response.</returns>
    /// <response code="200">Returns a service response.</response>
    /// <response code="404">When the specified authentication provider is not found.</response>
    [HttpPost("provider/{provider}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
        Policy = "AllowAnonymous")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<ServiceResponseDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> UseAuthProvider([FromRoute] string provider, [FromBody] ProviderRequestDto dto)
    {
        var currentUser = await userManager.FindByIdAsync(User.GetClaim(Claims.Subject)!);
        if (!Enum.TryParse(typeof(AuthProvider), provider, true, out var providerEnum))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var authProvider = factory.GetAuthProvider(providerEnum as AuthProvider?);
        if (authProvider == null)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        return Ok(new ResponseDto<ServiceResponseDto>
        {
            Status = ResponseStatus.Ok,
            Code = ResponseCodes.Ok,
            Data = await authProvider.RequestIdentityAsync(dto.State, dto.RedirectUri, currentUser)
        });
    }

    /// <summary>
    ///     Sends an email to user's email address.
    /// </summary>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body. Sends an email to the email address.</response>
    /// <response code="400">
    ///     When
    ///     1. the user's email address is in cooldown;
    ///     2. the mode is email confirmation and the user has already been activated.
    /// </response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the user has input an email address that does not match any existing user.</response>
    /// <response code="500">When a Redis / Mail Service error has occurred.</response>
    [HttpPost("sendEmail")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> SendEmail([FromBody] UserEmailRequestDto dto, [FromQuery] bool wait = false)
    {
        var user = await userManager.FindByEmailAsync(dto.Email);
        if (dto.Mode != EmailRequestMode.EmailConfirmation && dto.Mode != EmailRequestMode.EmailAddressUpdate)
        {
            if (user == null)
                return NotFound(new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.UserNotFound
                });
            if (!resourceService.HasPermission(user, UserRole.Member))
                return StatusCode(StatusCodes.Status403Forbidden,
                    new ResponseDto<object>
                    {
                        Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                    });
        }

        var db = redis.GetDatabase();
        if (await db.KeyExistsAsync($"phizone:cooldown:{dto.Mode}:{dto.Email}"))
        {
            var dateAvailable =
                DateTimeOffset.Parse((await db.StringGetAsync($"phizone:cooldown:{dto.Mode}:{dto.Email}"))!);
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorTemporarilyUnavailable,
                Code = ResponseCodes.EmailCooldown,
                DateAvailable = dateAvailable
            });
        }

        if (dto.Mode is EmailRequestMode.EmailConfirmation or EmailRequestMode.EmailAddressUpdate)
        {
            if (user != null)
                return BadRequest(new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief,
                    Code = dto.Mode == EmailRequestMode.EmailConfirmation && string.Equals(user.UserName,
                        dto.UserName, StringComparison.InvariantCultureIgnoreCase)
                        ? ResponseCodes.AlreadyDone
                        : ResponseCodes.EmailOccupied
                });

            if (dto.Mode == EmailRequestMode.EmailConfirmation)
            {
                if (dto.UserName == null)
                    return BadRequest(new ResponseDto<object>
                    {
                        Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidUserName
                    });

                if (dto.Language == null)
                    return BadRequest(new ResponseDto<object>
                    {
                        Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidLanguageCode
                    });

                user = await userManager.FindByNameAsync(dto.UserName);
                if (user != null)
                    return BadRequest(new ResponseDto<object>
                    {
                        Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.UserNameOccupied
                    });
            }
        }

        if (wait)
            try
            {
                var mailDto = await mailService.GenerateEmailAsync(dto.Email, dto.UserName ?? user!.UserName!,
                    dto.Language ?? user!.Language, dto.Mode);
                if (mailDto == null)
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        new ResponseDto<object>
                        {
                            Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.RedisError
                        });
                await mailService.SendMailAsync(mailDto);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ResponseDto<object>
                    {
                        Status = ResponseStatus.ErrorWithMessage,
                        Code = ResponseCodes.MailError,
                        Message = ex.Message
                    });
            }
        else
            await mailService.PublishEmailAsync(dto.Email, dto.UserName ?? user!.UserName!,
                dto.Language ?? user!.Language, dto.Mode);

        return NoContent();
    }

    /// <summary>
    ///     Updates user's email address.
    /// </summary>
    /// <param name="dto">Code from the address update email and the new email address.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When the input code is invalid.</response>
    [HttpPost("updateEmailAddress")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> UpdateEmailAddress([FromBody] UserEmailAddressUpdateDto dto)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(Claims.Subject)!))!;
        var db = redis.GetDatabase();
        var key = $"phizone:email:{EmailRequestMode.EmailAddressUpdate}:{dto.Code}";
        if (!await db.KeyExistsAsync(key))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidCode
            });
        var email = await db.StringGetAsync(key);
        if (email != dto.NewEmailAddress)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidCode
            });
        db.KeyDelete(key);

        var user = await userManager.FindByEmailAsync(dto.NewEmailAddress);
        if (user != null)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.EmailOccupied
            });
        currentUser.Email = dto.NewEmailAddress;

        await userManager.UpdateAsync(currentUser);
        return NoContent();
    }

    /// <summary>
    ///     Resets user's password.
    /// </summary>
    /// <param name="dto">Code from the password reset email and the new password.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When the input code is invalid.</response>
    [HttpPost("resetPassword")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> ResetPassword([FromBody] UserPasswordResetDto dto)
    {
        var user = await RedeemCode(dto.Code, EmailRequestMode.PasswordReset);
        if (user == null)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidCode
            });

        user.PasswordHash = userManager.PasswordHasher.HashPassword(user, dto.Password);
        await userManager.UpdateAsync(user);
        return NoContent();
    }

    /// <summary>
    ///     Retrieves TapTap user info.
    /// </summary>
    /// <returns>TapTap user info.</returns>
    /// <response code="200">Returns TapTap user info.</response>
    /// <response code="400">When errors have occurred whilst contacting TapTap.</response>
    [HttpPost("tapTap")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<TapTapResponseDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> RetrieveTapTapInfo([FromBody] TapTapRequestDto dto)
    {
        if (!await applicationRepository.ApplicationExistsAsync(dto.ApplicationId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ApplicationNotFound
            });

        if ((await applicationRepository.GetApplicationAsync(dto.ApplicationId)).TapClientId == null)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidOperation
            });

        var response = await tapTapService.Login(dto);

        if (response == null)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidData
            });

        if (!response.IsSuccessStatusCode)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorWithData,
                Code = ResponseCodes.RemoteFailure,
                Data = JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync())
            });

        var responseDto =
            JsonConvert.DeserializeObject<TapTapDelivererDto>(await response.Content.ReadAsStringAsync())!;
        var user = await userRepository.GetUserByTapUnionIdAsync(dto.ApplicationId, responseDto.Data.Unionid);

        return Ok(new ResponseDto<TapTapResponseDto>
        {
            Status = ResponseStatus.Ok,
            Code = ResponseCodes.Ok,
            Data = new TapTapResponseDto
            {
                UserName = responseDto.Data.Name,
                Avatar = responseDto.Data.Avatar,
                OpenId = responseDto.Data.Openid,
                UnionId = responseDto.Data.Unionid,
                User = user != null ? dtoMapper.MapUser<UserDetailedDto>(user) : null
            }
        });
    }

    /// <summary>
    ///     Revokes user's account.
    /// </summary>
    /// <param name="codeDto">Code from the confirmation email.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When the input code is invalid.</response>
    [HttpPost("revokeAccount")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> RevokeAccount([FromBody] CodeRequestDto codeDto)
    {
        var user = await RedeemCode(codeDto.Code, EmailRequestMode.AccountRevocation);
        if (user == null)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidCode
            });

        await notificationRepository.RemoveNotificationsAsync(
            await notificationRepository.GetNotificationsAsync(predicate: e =>
                e.OwnerId == user.Id || e.OperatorId == user.Id));
        var rankedCharts = await chartRepository.GetChartsAsync(predicate: e => e.OwnerId == user.Id && e.IsRanked);
        foreach (var chart in rankedCharts) chart.OwnerId = CriticalValues.PhiZoneOfficialUserId;

        await chartRepository.UpdateChartsAsync(rankedCharts);
        var rankedChartIds = rankedCharts.Select(f => f.Id);
        var rankedChartSubmissions = await chartSubmissionRepository.GetChartSubmissionsAsync(predicate: e =>
            e.OwnerId == user.Id && e.RepresentationId != null && rankedChartIds.Contains(e.RepresentationId.Value));
        foreach (var chart in rankedChartSubmissions) chart.OwnerId = CriticalValues.PhiZoneOfficialUserId;

        await chartSubmissionRepository.UpdateChartSubmissionsAsync(rankedChartSubmissions);
        await chartSubmissionRepository.RemoveChartSubmissionsAsync(
            await chartSubmissionRepository.GetChartSubmissionsAsync(predicate: e => e.OwnerId == user.Id));
        await chartRepository.RemoveChartsAsync(
            await chartRepository.GetChartsAsync(predicate: e => e.OwnerId == user.Id));
        var preservedSongs =
            await songRepository.GetSongsAsync(predicate: e => e.OwnerId == user.Id && e.Charts.Count > 0);
        foreach (var song in preservedSongs) song.OwnerId = CriticalValues.PhiZoneOfficialUserId;

        await songRepository.UpdateSongsAsync(preservedSongs);
        var preservedSongIds = preservedSongs.Select(e => e.Id);
        var preservedSongSubmissions = await songSubmissionRepository.GetSongSubmissionsAsync(predicate: e =>
            e.OwnerId == user.Id && e.RepresentationId != null && preservedSongIds.Contains(e.RepresentationId.Value));
        foreach (var song in preservedSongSubmissions) song.OwnerId = CriticalValues.PhiZoneOfficialUserId;

        await songSubmissionRepository.UpdateSongSubmissionsAsync(preservedSongSubmissions);
        await songSubmissionRepository.RemoveSongSubmissionsAsync(
            await songSubmissionRepository.GetSongSubmissionsAsync(predicate: e => e.OwnerId == user.Id));
        await songRepository.RemoveSongsAsync(await songRepository.GetSongsAsync(predicate: e => e.OwnerId == user.Id));

        var reviewedSongSubmissions =
            await songSubmissionRepository.GetSongSubmissionsAsync(predicate: e => e.ReviewerId == user.Id);
        foreach (var song in reviewedSongSubmissions) song.ReviewerId = CriticalValues.PhiZoneOfficialUserId;

        await songSubmissionRepository.UpdateSongSubmissionsAsync(reviewedSongSubmissions);
        var petAnswers = await petAnswerRepository.GetPetAnswersAsync(predicate: e => e.AssessorId == user.Id);
        foreach (var answer in petAnswers) answer.AssessorId = CriticalValues.PhiZoneOfficialUserId;

        await petAnswerRepository.UpdatePetAnswersAsync(petAnswers);
        await userManager.DeleteAsync(user);
        return NoContent();
    }

    private async Task<User?> RedeemCode(string code, EmailRequestMode mode)
    {
        var db = redis.GetDatabase();
        var key = $"phizone:email:{mode}:{code}";
        if (!await db.KeyExistsAsync(key)) return null;
        var email = await db.StringGetAsync(key);
        db.KeyDelete(key);
        var user = (await userManager.FindByEmailAsync(email!))!;
        return user;
    }

    private static IEnumerable<string> GetDestinations(Claim claim)
    {
        switch (claim.Type)
        {
            case Claims.Name:
                yield return Destinations.AccessToken;

                if (claim.Subject!.HasScope(Scopes.Profile)) yield return Destinations.IdentityToken;

                yield break;

            case Claims.Email:
                yield return Destinations.AccessToken;

                if (claim.Subject!.HasScope(Scopes.Email)) yield return Destinations.IdentityToken;

                yield break;

            case Claims.Role:
                yield return Destinations.AccessToken;

                if (claim.Subject!.HasScope(Scopes.Roles)) yield return Destinations.IdentityToken;

                yield break;

            case "AspNet.Identity.SecurityStamp":
                yield break;

            default:
                yield return Destinations.AccessToken;
                yield break;
        }
    }

    private async Task<IActionResult> Login(User user, string method)
    {
        var identity = new ClaimsIdentity(TokenValidationParameters.DefaultAuthenticationType, Claims.Name,
            Claims.Role);

        identity.AddClaim(Claims.Subject, user.Id.ToString());
        identity.AddClaim(Claims.Name, user.UserName!);

        identity.SetDestinations(GetDestinations);

        var claimsPrincipal = new ClaimsPrincipal(identity);
        claimsPrincipal.SetScopes(Scopes.OfflineAccess);

        user.DateLastLoggedIn = DateTimeOffset.UtcNow;
        user.AccessFailedCount = 0;
        await userRepository.SaveAsync();

        logger.LogInformation(LogEvents.UserInfo, "New login ({Method}): #{Id} {UserName}", method, user.Id,
            user.UserName);

        return SignIn(claimsPrincipal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private async Task<IActionResult?> CheckUserLockoutState(User user)
    {
        if (!user.LockoutEnabled) return null;

        if (user.LockoutEnd != null)
        {
            if (user.LockoutEnd > DateTimeOffset.UtcNow) // temporary
            {
                var properties = new AuthenticationProperties(new Dictionary<string, string>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.TemporarilyUnavailable,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                        $"Temporarily locked out until {user.LockoutEnd}."
                }!);

                return Forbid(properties, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            user.LockoutEnabled = false;
            await userManager.UpdateAsync(user);
        }
        else // permanent
        {
            if (user.EmailConfirmed)
            {
                var properties = new AuthenticationProperties(new Dictionary<string, string>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.AccessDenied,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "Permanently locked out."
                }!);

                return Forbid(properties, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }
            else
            {
                var properties = new AuthenticationProperties(new Dictionary<string, string>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InteractionRequired,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                        "Email confirmation is required."
                }!);

                return Forbid(properties, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }
        }

        return null;
    }
}