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
        private readonly UserManager<User> userManager;
        private readonly RoleManager<Role> roleManager;
        private readonly IConfiguration configuration;

        public AuthenticationController(
            UserManager<User> userManager,
            RoleManager<Role> roleManager,
            IConfiguration configuration)
        {
            this.userManager = userManager;
            this.roleManager = roleManager;
            this.configuration = configuration;
        }

        [HttpPost]
        [Route("login")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<TokenDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
        public async Task<IActionResult> Login([FromBody] UserLoginDto dto)
        {
            var user = await userManager.FindByEmailAsync(dto.Email);
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

                if (!await userManager.CheckPasswordAsync(user, dto.Password))
                {
                    user.AccessFailedCount++;
                    await userManager.UpdateAsync(user);
                    return BadRequest(new ResponseDto<object>()
                    {
                        Status = 1,
                        Code = ResponseCodes.PasswordIncorrect
                    });
                }
                var userRoles = await userManager.GetRolesAsync(user);

                var authClaims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    };

                foreach (var userRole in userRoles)
                {
                    authClaims.Add(new Claim(ClaimTypes.Role, userRole));
                }

                var token = GetToken(authClaims);
                user.DateLastLoggedIn = DateTime.Now;
                user.AccessFailedCount = 0;
                await userManager.UpdateAsync(user);

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
            return Unauthorized();
        }

        [HttpPost]
        [Route("register")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
        public async Task<IActionResult> Register([FromForm] UserRegistrationDto dto)
        {
            var user = await userManager.FindByEmailAsync(dto.Email);
            if (user != null)
            {
                return BadRequest(new ResponseDto<object>()
                {
                    Status = 1,
                    Code = ResponseCodes.EmailOccupied
                });
            }
            user = await userManager.FindByNameAsync(dto.UserName);
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

            var result = await userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
            {
                return BadRequest(new ResponseDto<object>()
                {
                    Status = 2,
                    Code = ResponseCodes.DataInvalid,
                    Errors = result.Errors.ToArray()
                });
            }

            await userManager.AddToRoleAsync(user, UserRoles.Member);

            return StatusCode(StatusCodes.Status201Created);
        }

        private JwtSecurityToken GetToken(List<Claim> authClaims)
        {
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:Secret"]));

            var token = new JwtSecurityToken(
                issuer: configuration["JWT:ValidIssuer"],
                audience: configuration["JWT:ValidAudience"],
                expires: DateTime.Now.AddHours(3),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                );

            return token;
        }
    }
}
