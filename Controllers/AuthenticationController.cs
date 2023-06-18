using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using PhiZoneApi.Data;
using PhiZoneApi.Dtos;
using PhiZoneApi.Helpers;
using PhiZoneApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PhiZoneApi.Controllers
{
    [Route("auth")]
    [ApiVersion("2.0")]
    [ApiController]
    public class AuthenticationController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly IConfiguration _configuration;

        public AuthenticationController(
            UserManager<User> userManager,
            RoleManager<Role> roleManager,
            IConfiguration configuration)
        {
            this._userManager = userManager;
            this._roleManager = roleManager;
            this._configuration = configuration;
        }

        [HttpPost]
        [Route("login")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<TokenDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
        public async Task<IActionResult> Login([FromBody] UserLoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user != null)
            {
                if (user.LockoutEnabled)
                {
                    if (user.LockoutEnd != null)
                    {
                        if (user.LockoutEnd > DateTime.Now) // temporary
                            return BadRequest(new CoolDownResponseDto()
                            {
                                Status = 1,
                                Code = ResponseCodes.AccountLocked,
                                DateAvailable = user.LockoutEnd.Value.UtcDateTime
                            });
                        else
                            user.LockoutEnabled = false;
                    }
                    else // permanent
                    {
                        return BadRequest(new ResponseDto<object>()
                        {
                            Status = 1,
                            Code = ResponseCodes.AccountLocked
                        });
                    }
                }

                if (!await _userManager.CheckPasswordAsync(user, dto.Password))
                {
                    user.AccessFailedCount++;
                    await _userManager.UpdateAsync(user);
                    return BadRequest(new ResponseDto<object>()
                    {
                        Status = 1,
                        Code = ResponseCodes.PasswordIncorrect
                    });
                }
                var userRoles = await _userManager.GetRolesAsync(user);

                if (user.Email != null)
                {
                    var authClaims = new List<Claim>
                    {
                        new(ClaimTypes.Email, user.Email!),
                        new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    };

                    foreach (var userRole in userRoles)
                    {
                        authClaims.Add(new Claim(ClaimTypes.Role, userRole));
                    }

                    var token = GetToken(authClaims);
                    user.DateLastLoggedIn = DateTime.Now;
                    user.AccessFailedCount = 0;
                    await _userManager.UpdateAsync(user);

                    return Ok(new ResponseDto<TokenDto>()
                    {
                        Status = 0,
                        Code = ResponseCodes.Ok,
                        Data = new TokenDto()
                        {
                            Token = new JwtSecurityTokenHandler().WriteToken(token),
                            Expiration = token.ValidTo
                        }
                    });
                }
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
            {
                return BadRequest(new ResponseDto<object>()
                {
                    Status = 1,
                    Code = ResponseCodes.EmailOccupied
                });
            }
            user = await _userManager.FindByNameAsync(dto.UserName);
            if (user != null)
            {
                return BadRequest(new ResponseDto<object>()
                {
                    Status = 1,
                    Code = ResponseCodes.UserNameOccupied
                });
            }
            if (!ModelState.IsValid)
            {
                return BadRequest(new ResponseDto<object>()
                {
                    Status = 2,
                    Code = ResponseCodes.DataInvalid,
                    Errors = ModelErrorTranslator.Translate(ModelState)
                });
            }

            string? avatarUrl = null;
            if (dto.Avatar != null)
            {
                avatarUrl = await FileUploader.Upload(dto.UserName, dto.Avatar);
            }

            var passwordHasher = new PasswordHasher<User>();

            user = new()
            {
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = dto.UserName,
                Email = dto.Email,
                Avatar = avatarUrl,
                Language = dto.Language,
                Gender = dto.Gender,
                Biography = dto.Biography,
                DateOfBirth = dto.DateOfBirth,
                DateJoined = DateTime.Now
            };

            user.PasswordHash = passwordHasher.HashPassword(user, dto.Password);

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
            {
                return BadRequest(new ResponseDto<object>()
                {
                    Status = 2,
                    Code = ResponseCodes.DataInvalid,
                    Errors = result.Errors.ToArray()
                });
            }

            await _userManager.AddToRoleAsync(user, UserRoles.Member);

            return StatusCode(StatusCodes.Status201Created);
        }

        private JwtSecurityToken GetToken(List<Claim> authClaims)
        {
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]!));

            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:ValidIssuer"],
                audience: _configuration["JWT:ValidAudience"],
                expires: DateTime.Now.AddHours(3),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                );

            return token;
        }
    }
}
