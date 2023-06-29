using System.Collections.Immutable;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using PhiZoneApi.Constants;
using PhiZoneApi.Dtos;
using PhiZoneApi.Enums;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using RabbitMQ.Client;
using StackExchange.Redis;
using static OpenIddict.Abstractions.OpenIddictConstants;
using Role = PhiZoneApi.Models.Role;

namespace PhiZoneApi.Controllers;

/// <summary>
///     Provides authentication services, namely login, registration, and email confirmation.
/// </summary>
[ApiController]
[ApiVersion("2.0")]
[Route("auth")]
[Produces("application/json")]
public class AuthenticationController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly IFileStorageService _fileStorageService;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IRabbitMqService _rabbitMqService;
    private readonly IConnectionMultiplexer _redis;
    private readonly RoleManager<Role> _roleManager;
    private readonly ITemplateService _templateService;
    private readonly UserManager<User> _userManager;

    public AuthenticationController(UserManager<User> userManager, RoleManager<Role> roleManager,
        IConfiguration configuration, ITemplateService templateService, IFileStorageService fileStorageService,
        IRabbitMqService rabbitMqService, IPasswordHasher<User> passwordHasher, IConnectionMultiplexer redis)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
        _templateService = templateService;
        _fileStorageService = fileStorageService;
        _rabbitMqService = rabbitMqService;
        _passwordHasher = passwordHasher;
        _redis = redis;
    }

    /// <summary>
    ///     Retrieves authentication credentials using either email + password or <c>refresh_token</c>.
    /// </summary>
    /// <param name="client_id">The client's identifier, e.g. "regular".</param>
    /// <param name="client_secret">The client's secret, e.g. "c29b1587-80f9-475f-b97b-dca1884eb0e3".</param>
    /// <param name="grant_type">The grant type desired, either <c>password</c> or <c>refresh_token</c>.</param>
    /// <param name="username">The user's email address, e.g. "contact@phi.zone", when the grant type is <c>password</c>.</param>
    /// <param name="password">The user's password, when the grant type is <c>password</c>.</param>
    /// <param name="refresh_token">The user's refresh token, when the grant type is <c>refresh_token</c>.</param>
    /// <returns>Authentication credentials, e.g. <c>access_token</c>, <c>refresh_token</c>, etc.</returns>
    /// <remarks>
    ///     This is the only endpoint where all the fields are named in the underscore case, both in the request and in the
    ///     response.
    ///     It's also the only one that responds without following the <see cref="ResponseDto{T}" /> structure.
    /// </remarks>
    /// <response code="200">Returns authentication credentials.</response>
    /// <response code="403">
    ///     When the user
    ///     1. has input an incorrect password (<c>invalid_grant</c>);
    ///     2. is temporarily locked out (<c>temporarily_unavailable</c>);
    ///     3. is permanently locked out (<c>access_denied</c>);
    ///     4. has not yet confirmed their email address (<c>interaction_required</c>), in which case the client should direct
    ///     the user to request a confirmation mail.
    /// </response>
    /// <response code="404">When the user has input an email address that does not match any existing user.</response>
    [HttpPost("token")]
    [IgnoreAntiforgeryToken]
    [Consumes("application/x-www-form-urlencoded")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(OpenIddictTokenDto))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(OpenIddictErrorDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest()!;

        if (request.IsPasswordGrantType())
        {
            var user = await _userManager.FindByEmailAsync(request.Username!);
            if (user == null) return NotFound();

            var actionResult = CheckUserLockoutState(user);
            if (actionResult != null) return actionResult;

            if (!await _userManager.CheckPasswordAsync(user, request.Password!))
            {
                user.AccessFailedCount++;
                await _userManager.UpdateAsync(user);

                var properties = new AuthenticationProperties(new Dictionary<string, string>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The password is incorrect."
                }!);

                return Forbid(properties, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
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
                return Unauthorized(new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCode.RefreshTokenOutdated
                });

            var actionResult = CheckUserLockoutState(user);
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

        throw new NotImplementedException("The specified grant type is not implemented.");
    }

    /// <summary>
    ///     Creates a new user.
    /// </summary>
    /// <param name="dto">
    ///     User Name, Email, Password, Language, Gender (Optional), Avatar (Optional), Biography (Optional),
    ///     Date of Birth (Optional)
    /// </param>
    /// <returns>An empty body.</returns>
    /// <response code="201">Returns an empty body. Sends a confirmation email to the user.</response>
    /// <response code="400">
    ///     When
    ///     1. the input email address / user name has been occupied;
    ///     2. one of the input fields has failed on data validation.
    /// </response>
    /// <response code="500">When a Redis / Mail Service error has occurred.</response>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> Register([FromForm] UserRegistrationDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user != null)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCode.EmailOccupied
            });

        user = await _userManager.FindByNameAsync(dto.UserName);
        if (user != null)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCode.UserNameOccupied
            });

        string? avatarUrl = null;
        if (dto.Avatar != null) avatarUrl = await _fileStorageService.Upload(dto.UserName, dto.Avatar);

        user = new User
        {
            SecurityStamp = Guid.NewGuid().ToString(),
            UserName = dto.UserName,
            Email = dto.Email,
            Avatar = avatarUrl,
            Language = dto.Language,
            Gender = (int)dto.Gender,
            Biography = dto.Biography,
            DateOfBirth = dto.DateOfBirth,
            DateJoined = DateTimeOffset.UtcNow
        };

        var errorCode = await SendConfirmationEmail(user);
        if (!errorCode.Equals(string.Empty))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = errorCode });

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded) // TODO figure out in what circumstances can this statement be fired off.
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorDetailed,
                Code = ResponseCode.DataInvalid,
                Errors = result.Errors.ToArray()
            });

        await _userManager.AddToRoleAsync(user, "Member");

        return StatusCode(StatusCodes.Status201Created);
    }

    /// <summary>
    ///     Activates user's account.
    /// </summary>
    /// <param name="dto">Code from the confirmation Email</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">
    ///     When
    ///     1. the input code is invalid;
    ///     2. the user has already been activated.
    /// </response>
    [HttpPost("activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> Activate([FromBody] UserActivationDto dto)
    {
        var db = _redis.GetDatabase();
        var key = $"ACTIVATION{dto.Code}";
        if (!await db.KeyExistsAsync(key))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCode.InvalidActivationCode
            });

        var id = await db.StringGetAsync(key);
        db.KeyDelete(key);
        var user = (await _userManager.FindByIdAsync(id!))!;
        if (user is { EmailConfirmed: true, LockoutEnabled: false })
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCode.AlreadyActivated
            });
        user.EmailConfirmed = true;
        user.LockoutEnabled = false;
        await _userManager.UpdateAsync(user);
        return NoContent();
    }

    private async Task<MailDto> GenerateMail(User user)
    {
        if (user.Email == null || user.UserName == null) throw new ArgumentNullException(nameof(user));

        string code;
        var random = new Random();
        var db = _redis.GetDatabase();
        do
        {
            code = random.Next(1000000, 2000000).ToString()[1..];
        } while (await db.KeyExistsAsync($"ACTIVATION{code}"));

        if (!await db.StringSetAsync($"ACTIVATION{code}", user.Id, TimeSpan.FromMinutes(5)))
            throw new RedisException("An error occurred whilst saving activation code.");

        var template = _templateService.GetConfirmationEmailTemplate(user.Language);

        return new MailDto
        {
            RecipientAddress = user.Email,
            RecipientName = user.UserName,
            EmailSubject = template["Subject"],
            EmailBody = _templateService.ReplacePlaceholders(template["Body"],
                new Dictionary<string, string> { { "UserName", user.UserName }, { "Code", code } })
        };
    }

    private async Task<string> SendConfirmationEmail(User user)
    {
        var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(await GenerateMail(user)));

        try
        {
            using var channel = _rabbitMqService.GetConnection().CreateModel();
            channel.BasicPublish("", "email", null, body);
        }
        catch (Exception)
        {
            return ResponseCode.MailError;
        }

        return string.Empty;
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

    private IActionResult? CheckUserLockoutState(User user)
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