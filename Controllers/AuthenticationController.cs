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

[Route("auth")]
[ApiVersion("2.0")]
[ApiController]
public class AuthenticationController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly IConnectionMultiplexer _redis;
    private readonly IFileStorageService _fileStorageService;
    private readonly IMailService _mailService;
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

    [HttpPost]
    [Route("login")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<TokenDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> Login([FromBody] UserLoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null) return Unauthorized();
        if (user.LockoutEnabled)
        {
            if (user.LockoutEnd != null)
            {
                if (user.LockoutEnd > DateTime.Now) // temporary
                    return BadRequest(new ResponseDto<object>
                    {
                        Status = ResponseStatus.ErrorNotYetAvailable,
                        Code = ResponseCode.AccountLocked,
                        DateAvailable = user.LockoutEnd.Value.UtcDateTime
                    });
                user.LockoutEnabled = false;
            }
            else // permanent
            {
                return BadRequest(new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief,
                    Code = ResponseCode.AccountLocked
                });
            }
        }

        if (!await _userManager.CheckPasswordAsync(user, dto.Password))
        {
            user.AccessFailedCount++;
            await _userManager.UpdateAsync(user);
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief,
                Code = ResponseCode.PasswordIncorrect
            });
        }

        var userRoles = await _userManager.GetRolesAsync(user);

        if (user.Email != null)
        {
            var authClaims = new List<Claim>
            {
                new(ClaimTypes.Email, user.Email!),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            foreach (var userRole in userRoles) authClaims.Add(new Claim(ClaimTypes.Role, userRole));

            var token = GetToken(authClaims);
            user.DateLastLoggedIn = DateTime.Now;
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

        return Unauthorized();
    }

    [HttpPost]
    [Route("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
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

        if (!ModelState.IsValid)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorDetailed,
                Code = ResponseCode.DataInvalid,
                Errors = ModelErrorTranslator.Translate(ModelState)
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

        var code = "";
        var random = new Random();
        var db = _redis.GetDatabase();
        do
        {
            code = random.Next(1000000, 2000000).ToString().Substring(1);
        } while (await db.KeyExistsAsync($"ACTIVATION{code}"));

        if (!await db.StringSetAsync($"ACTIVATION{code}", user.Id, TimeSpan.FromMinutes(5)))
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief,
                Code = ResponseCode.RedisError
            });
        }

        var template = _templateService.GetRegistrationEmailTemplate(user.Language);

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
            return StatusCode(StatusCodes.Status500InternalServerError, new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief,
                Code = ResponseCode.MailError
            });
        }

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
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

    private JwtSecurityToken GetToken(List<Claim> authClaims)
    {
        var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]!));

        var token = new JwtSecurityToken(
            _configuration["JWT:ValidIssuer"],
            _configuration["JWT:ValidAudience"],
            expires: DateTime.UtcNow.AddHours(3),
            claims: authClaims,
            signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
        );

        return token;
    }
}