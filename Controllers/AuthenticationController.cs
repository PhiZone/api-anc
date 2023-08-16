using System.Collections.Immutable;
using System.Security.Claims;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using PhiZoneApi.Constants;
using PhiZoneApi.Dtos.Deliverers;
using PhiZoneApi.Dtos.Requests;
using PhiZoneApi.Dtos.Responses;
using PhiZoneApi.Enums;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;
using StackExchange.Redis;
using static OpenIddict.Abstractions.OpenIddictConstants;

// ReSharper disable InvertIf

namespace PhiZoneApi.Controllers;

[ApiController]
[ApiVersion("2.0")]
[Route("auth")]
[Produces("application/json")]
public class AuthenticationController : Controller
{
    private readonly IDtoMapper _dtoMapper;
    private readonly IMailService _mailService;
    private readonly IConnectionMultiplexer _redis;
    private readonly IResourceService _resourceService;
    private readonly ITapTapService _tapTapService;
    private readonly UserManager<User> _userManager;
    private readonly IUserRepository _userRepository;

    public AuthenticationController(UserManager<User> userManager, IConnectionMultiplexer redis,
        IMailService mailService, IResourceService resourceService, ITapTapService tapTapService,
        IUserRepository userRepository, IDtoMapper dtoMapper)
    {
        _userManager = userManager;
        _mailService = mailService;
        _resourceService = resourceService;
        _tapTapService = tapTapService;
        _userRepository = userRepository;
        _dtoMapper = dtoMapper;
        _redis = redis;
    }

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
    ///     2. using TapTap mode and the union ID does not match any existing user.
    /// </response>
    [HttpPost("token")]
    [IgnoreAntiforgeryToken]
    [Consumes("application/x-www-form-urlencoded")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(OpenIddictTokenResponseDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(OpenIddictErrorDto))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(OpenIddictErrorDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(OpenIddictErrorDto))]
    // ReSharper disable once UnusedParameter.Global
    public async Task<IActionResult> Exchange([FromForm] OpenIddictTokenRequestDto dto,
        [FromQuery] LoginMode mode = LoginMode.Direct)
    {
        var request = HttpContext.GetOpenIddictServerRequest()!;

        if (mode == LoginMode.TapTap)
        {
            var response = await _tapTapService.Login(new TapTapRequestDto
            {
                MacKey = dto.username!, AccessToken = dto.password!
            });

            if (response == null)
            {
                return BadRequest(new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidData
                });
            }

            if (!response.IsSuccessStatusCode)
                return Forbid(new AuthenticationProperties(new Dictionary<string, string>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidRequest,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                        "Unable to log into TapTap with provided credentials."
                }!));

            var responseDto =
                JsonConvert.DeserializeObject<TapTapDelivererDto>(await response.Content.ReadAsStringAsync())!;
            var user = await _userRepository.GetUserByTapUnionId(responseDto.Data.Unionid);
            if (user == null)
                return NotFound(new AuthenticationProperties(new Dictionary<string, string>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidToken,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                        "Unable to find a user with provided credentials."
                }!));

            var actionResult = await CheckUserLockoutState(user);
            if (actionResult != null) return actionResult;

            var identity = new ClaimsIdentity(TokenValidationParameters.DefaultAuthenticationType, Claims.Name,
                Claims.Role);

            identity.AddClaim(Claims.Subject, user.Id.ToString(), Destinations.AccessToken);
            identity.AddClaim(Claims.Username, user.UserName!, Destinations.AccessToken);

            foreach (var role in await _userManager.GetRolesAsync(user))
                identity.AddClaim(Claims.Role, role, Destinations.AccessToken);

            var claimsPrincipal = new ClaimsPrincipal(identity);
            claimsPrincipal.SetScopes(Scopes.Roles, Scopes.OfflineAccess, Scopes.Email, Scopes.Profile);

            user.DateLastLoggedIn = DateTimeOffset.UtcNow;
            user.AccessFailedCount = 0;
            await _userManager.UpdateAsync(user);

            return SignIn(claimsPrincipal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        if (request.IsPasswordGrantType())
        {
            var user = await _userManager.FindByEmailAsync(request.Username!);
            if (user == null)
                return NotFound(new AuthenticationProperties(new Dictionary<string, string>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidToken,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                        "Unable to find a user with provided credentials."
                }!));

            var actionResult = await CheckUserLockoutState(user);
            if (actionResult != null) return actionResult;

            var isPasswordCorrect = true;
            try
            {
                if (!await _userManager.CheckPasswordAsync(user, request.Password!)) isPasswordCorrect = false;
            }
            catch (FormatException)
            {
                if (!ObsoletePasswordUtil.Check(request.Password!, user.PasswordHash!))
                    isPasswordCorrect = false;
                else
                    user.PasswordHash = _userManager.PasswordHasher.HashPassword(user, request.Password!);
            }

            if (!isPasswordCorrect)
            {
                user.AccessFailedCount++;
                await _userManager.UpdateAsync(user);

                return Forbid(
                    new AuthenticationProperties(new Dictionary<string, string>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "The password is incorrect."
                    }!), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            var identity = new ClaimsIdentity(TokenValidationParameters.DefaultAuthenticationType, Claims.Name,
                Claims.Role);

            identity.AddClaim(Claims.Subject, user.Id.ToString(), Destinations.AccessToken);
            identity.AddClaim(Claims.Username, user.UserName!, Destinations.AccessToken);

            foreach (var role in await _userManager.GetRolesAsync(user))
                identity.AddClaim(Claims.Role, role, Destinations.AccessToken);

            var claimsPrincipal = new ClaimsPrincipal(identity);
            claimsPrincipal.SetScopes(Scopes.Roles, Scopes.OfflineAccess, Scopes.Email, Scopes.Profile);

            user.DateLastLoggedIn = DateTimeOffset.UtcNow;
            user.AccessFailedCount = 0;
            await _userManager.UpdateAsync(user);

            return SignIn(claimsPrincipal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        if (request.IsRefreshTokenGrantType())
        {
            var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            var user = await _userManager.FindByIdAsync(result.Principal!.GetClaim(Claims.Subject)!);
            if (user == null)
                return Forbid(
                    new AuthenticationProperties(new Dictionary<string, string>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.ExpiredToken,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "The refresh token has expired."
                    }!), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            var actionResult = await CheckUserLockoutState(user);
            if (actionResult != null) return actionResult;

            var identity = new ClaimsIdentity(result.Principal!.Claims,
                TokenValidationParameters.DefaultAuthenticationType, Claims.Name, Claims.Role);

            identity.SetClaim(Claims.Subject, await _userManager.GetUserIdAsync(user))
                .SetClaim(Claims.Email, await _userManager.GetEmailAsync(user))
                .SetClaim(Claims.Name, await _userManager.GetUserNameAsync(user))
                .SetClaims(Claims.Role, (await _userManager.GetRolesAsync(user)).ToImmutableArray());

            identity.SetDestinations(GetDestinations);

            user.DateLastLoggedIn = DateTimeOffset.UtcNow;
            user.AccessFailedCount = 0;
            await _userManager.UpdateAsync(user);

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
    public async Task<IActionResult> SendEmail([FromBody] UserEmailRequestDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.UserNotFound
            });

        if (dto.Mode != EmailRequestMode.EmailConfirmation && !await _resourceService.HasPermission(user, Roles.Member))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        var db = _redis.GetDatabase();
        if (await db.KeyExistsAsync($"COOLDOWN:{dto.Mode}:{user.Email}"))
        {
            var dateAvailable = DateTimeOffset.Parse((await db.StringGetAsync($"COOLDOWN:{dto.Mode}:{user.Email}"))!);
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorTemporarilyUnavailable,
                Code = ResponseCodes.EmailCooldown,
                DateAvailable = dateAvailable
            });
        }

        if (dto.Mode == EmailRequestMode.EmailConfirmation && user.EmailConfirmed)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.AlreadyDone
            });

        var mailDto = await _mailService.GenerateEmailAsync(user, dto.Mode, null);
        if (mailDto == null)
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.RedisError });

        try
        {
            await _mailService.SendMailAsync(mailDto);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorWithMessage, Code = ResponseCodes.MailError, Message = ex.Message
                });
        }

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

        user.PasswordHash = _userManager.PasswordHasher.HashPassword(user, dto.Password);
        await _userManager.UpdateAsync(user);
        return NoContent();
    }

    /// <summary>
    ///     Activates user's account.
    /// </summary>
    /// <param name="dto">Code from the confirmation email.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">
    ///     When
    ///     1. the input code is invalid;
    ///     2. the user has already been activated.
    /// </response>
    [HttpPost("activate")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> Activate([FromBody] ConfirmationCodeDto dto)
    {
        var user = await RedeemCode(dto.Code, EmailRequestMode.EmailConfirmation);
        switch (user)
        {
            case null:
                return BadRequest(new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidCode
                });
            case { EmailConfirmed: true, LockoutEnabled: false }:
                return BadRequest(new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.AlreadyDone
                });
        }

        user.EmailConfirmed = true;
        user.LockoutEnabled = false;
        await _userManager.UpdateAsync(user);
        await _userManager.AddToRoleAsync(user, Roles.Member.Name);
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
        var response = await _tapTapService.Login(dto);

        if (response == null)
        {
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidData
            });
        }

        if (!response.IsSuccessStatusCode)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorWithData,
                Code = ResponseCodes.RemoteFailure,
                Data = JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync())
            });

        var responseDto =
            JsonConvert.DeserializeObject<TapTapDelivererDto>(await response.Content.ReadAsStringAsync())!;
        var user = await _userRepository.GetUserByTapUnionId(responseDto.Data.Unionid);

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
                User = user != null ? await _dtoMapper.MapUserAsync<UserDetailedDto>(user) : null
            }
        });
    }

    /// <summary>
    ///     Revokes user's account.
    /// </summary>
    /// <param name="dto">Code from the confirmation email.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When the input code is invalid.</response>
    [HttpPost("revokeAccount")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> RevokeAccount([FromBody] ConfirmationCodeDto dto)
    {
        var user = await RedeemCode(dto.Code, EmailRequestMode.AccountRevocation);
        if (user == null)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidCode
            });

        await _userManager.DeleteAsync(user);
        return NoContent();
    }

    private async Task<User?> RedeemCode(string code, EmailRequestMode mode)
    {
        var db = _redis.GetDatabase();
        var key = $"EMAIL:{mode}:{code}";
        if (!await db.KeyExistsAsync(key)) return null;
        var email = await db.StringGetAsync(key);
        db.KeyDelete(key);
        var user = (await _userManager.FindByEmailAsync(email!))!;
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
            await _userManager.UpdateAsync(user);
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