using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using PhiZoneApi.Data;
using PhiZoneApi.Dtos;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;
using StackExchange.Redis;
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
    private readonly IMailService _mailService;
    private readonly IConnectionMultiplexer _redis;
    private readonly RoleManager<Role> _roleManager;
    private readonly ITemplateService _templateService;
    private readonly UserManager<User> _userManager;

    public AuthenticationController(
        UserManager<User> userManager,
        RoleManager<Role> roleManager,
        IConfiguration configuration,
        IMailService mailService,
        ITemplateService templateService,
        IFileStorageService fileStorageService,
        IConnectionMultiplexer redis)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
        _mailService = mailService;
        _templateService = templateService;
        _fileStorageService = fileStorageService;
        _redis = redis;
    }

    /// <summary>
    ///     Authenticates the user's identity.
    /// </summary>
    /// <param name="dto">Email and Password.</param>
    /// <returns>Authentication credentials, namely a token that expires in 18 hours and the expiry time.</returns>
    /// <response code="200">Returns authentication credentials</response>
    /// <response code="204">When the user has not yet confirmed their email address. Sends a confirmation email to the user.</response>
    /// <response code="401">When the user has entered a wrong password.</response>
    /// <response code="403">When the user was temporarily (in which case DateAvailable is present) / permanently locked out.</response>
    /// <response code="404">When user with the specified email address is not found.</response>
    /// <response code="500">When a Redis / Mail Service error has occurred.</response>
    [HttpPost]
    [Route("login")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<TokenDto>))]
    [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> Login([FromBody] UserLoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief,
                Code = ResponseCode.UserNotFound
            });
        if (user.LockoutEnabled)
        {
            if (user.LockoutEnd != null)
            {
                if (user.LockoutEnd > DateTimeOffset.UtcNow) // temporary
                    return StatusCode(StatusCodes.Status403Forbidden, new ResponseDto<object>
                    {
                        Status = ResponseStatus.ErrorNotYetAvailable,
                        Code = ResponseCode.AccountLocked,
                        DateAvailable = user.LockoutEnd.Value.UtcDateTime
                    });
                user.LockoutEnabled = false;
            }
            else // permanent
            {
                if (user.EmailConfirmed)
                    return StatusCode(StatusCodes.Status403Forbidden, new ResponseDto<object>
                    {
                        Status = ResponseStatus.ErrorBrief,
                        Code = ResponseCode.AccountLocked
                    });

                var errorCode = await SendConfirmationEmail(user);
                if (errorCode.Equals(string.Empty)) return NoContent();
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief,
                    Code = errorCode
                });
            }
        }

        if (!await _userManager.CheckPasswordAsync(user, dto.Password))
        {
            user.AccessFailedCount++;
            await _userManager.UpdateAsync(user);
            return Unauthorized(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief,
                Code = ResponseCode.PasswordIncorrect
            });
        }

        var userRoles = await _userManager.GetRolesAsync(user);

        var authClaims = new List<Claim>
        {
            new(ClaimTypes.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        foreach (var userRole in userRoles)
            authClaims.Add(new Claim(ClaimTypes.Role, userRole));

        var token = GetToken(authClaims);
        user.DateLastLoggedIn = DateTimeOffset.UtcNow;
        user.AccessFailedCount = 0;
        await _userManager.UpdateAsync(user);

        return Ok(new ResponseDto<TokenDto>
        {
            Status = 0,
            Code = ResponseCode.Ok,
            Data = new TokenDto
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Expiration = token.ValidTo
            }
        });
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
    [HttpPost]
    [Route("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> Register([FromForm] UserRegistrationDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user != null)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief,
                Code = ResponseCode.EmailOccupied
            });

        user = await _userManager.FindByNameAsync(dto.UserName);
        if (user != null)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief,
                Code = ResponseCode.UserNameOccupied
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
            Gender = dto.Gender,
            Biography = dto.Biography,
            DateOfBirth = dto.DateOfBirth,
            DateJoined = DateTimeOffset.UtcNow
        };

        var passwordHasher = new PasswordHasher<User>();
        user.PasswordHash = passwordHasher.HashPassword(user, dto.Password);

        var errorCode = await SendConfirmationEmail(user);
        if (!errorCode.Equals(string.Empty))
            return StatusCode(StatusCodes.Status500InternalServerError, new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief,
                Code = errorCode
            });

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded) // TODO figure out in what circumstances can this statement be fired off.
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorDetailed,
                Code = ResponseCode.DataInvalid,
                Errors = result.Errors.ToArray()
            });

        var roles = new List<string>
        {
            "Member", "Qualified", "Volunteer", "Moderator", "Administrator"
        };
        foreach (var role in roles)
            if (!await _roleManager.RoleExistsAsync(role))
                await _roleManager.CreateAsync(new Role
                {
                    Name = role
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
    [HttpPost]
    [Route("activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> Activate([FromBody] UserActivationDto dto)
    {
        var db = _redis.GetDatabase();
        if (!await db.KeyExistsAsync($"ACTIVATION{dto.Code}"))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief,
                Code = ResponseCode.InvalidActivationCode
            });

        var id = await db.StringGetAsync($"ACTIVATION{dto.Code}");
        var user = (await _userManager.FindByIdAsync(id!))!;
        if (user is { EmailConfirmed: true, LockoutEnabled: false })
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief,
                Code = ResponseCode.AlreadyActivated
            });
        user.EmailConfirmed = true;
        user.LockoutEnabled = false;
        await _userManager.UpdateAsync(user);
        return NoContent();
    }

    private JwtSecurityToken GetToken(IEnumerable<Claim> authClaims)
    {
        var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]!));

        var token = new JwtSecurityToken(
            _configuration["JWT:ValidIssuer"],
            _configuration["JWT:ValidAudience"],
            expires: DateTime.UtcNow.AddHours(18),
            claims: authClaims,
            signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
        );

        return token;
    }

    private async Task<string> SendConfirmationEmail(User user)
    {
        if (user.Email == null || user.UserName == null)
            throw new ArgumentNullException(nameof(user));

        string code;
        var random = new Random();
        var db = _redis.GetDatabase();
        do
        {
            code = random.Next(1000000, 2000000).ToString()[1..];
        } while (await db.KeyExistsAsync($"ACTIVATION{code}"));

        if (!await db.StringSetAsync($"ACTIVATION{code}", user.Id, TimeSpan.FromMinutes(5)))
            return ResponseCode.RedisError;

        var template = _templateService.GetConfirmationEmailTemplate(user.Language);

        var mailDto = new MailDto
        {
            RecipientAddress = user.Email,
            RecipientName = user.UserName,
            EmailSubject = template["Subject"],
            EmailBody = _templateService.ReplacePlaceholders(template["Body"], new Dictionary<string, string>
            {
                { "UserName", user.UserName },
                { "Code", code }
            })
        };

        try
        {
            _mailService.SendMail(mailDto);
        }
        catch (Exception)
        {
            return ResponseCode.MailError;
        }

        return string.Empty;
    }
}