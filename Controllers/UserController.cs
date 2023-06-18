using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using PhiZoneApi.Data;
using PhiZoneApi.Dtos;
using PhiZoneApi.Helpers;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;

namespace PhiZoneApi.Controllers
{
    [Route("users")]
    [ApiVersion("2.0")]
    [ApiController]
    public class UserController : Controller
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;

        public UserController(IUserRepository userRepository, IMapper mapper)
        {
            this._userRepository = userRepository;
            this._mapper = mapper;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<UserDto>>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
        public IActionResult GetUsers()
        {
            var users = _mapper.Map<List<UserDto>>(_userRepository.GetUsers());
            if (!ModelState.IsValid)
            {
                return BadRequest(new ResponseDto<object>()
                {
                    Status = 2,
                    Code = ResponseCodes.DataInvalid,
                    Errors = ModelErrorTranslator.Translate(ModelState)
                });
            }
            return Ok(new ResponseDto<IEnumerable<UserDto>>()
            {
                Status = 0,
                Code = ResponseCodes.Ok,
                Data = users
            });
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<UserDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
        public IActionResult GetUser(int id)
        {
            if (!_userRepository.UserExists(id))
            {
                return NotFound();
            }
            var user = _mapper.Map<UserDto>(_userRepository.GetUser(id));
            if (!ModelState.IsValid)
            {
                return BadRequest(new ResponseDto<object>()
                {
                    Status = 2,
                    Code = ResponseCodes.DataInvalid,
                    Errors = ModelErrorTranslator.Translate(ModelState)
                });
            }
            return Ok(new ResponseDto<UserDto>()
            {
                Status = 0,
                Code = ResponseCodes.Ok,
                Data = user
            });
        }

        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
        public async Task<IActionResult> UpdateUser([FromRoute] int id, [FromForm] UserUpdateDto dto)
        {
            if (!_userRepository.UserExists(id))
            {
                return NotFound();
            }
            var user = _userRepository.GetUser(id);
            if (user == null)
            {
                return NotFound();
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
            if (dto.UserName != null)
            {
                if (user.DateLastModifiedUserName != null
                    && DateTime.Now - user.DateLastModifiedUserName < TimeSpan.FromDays(15))
                {
                    return BadRequest(new CoolDownResponseDto()
                    {
                        Code = ResponseCodes.UserNameCoolDown,
                        DateAvailable = (DateTime)(user.DateLastModifiedUserName + TimeSpan.FromDays(15))
                    });
                }
                user.UserName = dto.UserName;
                user.DateLastModifiedUserName = DateTime.Now;
            }
            if (dto.Avatar != null)
            {
                user.Avatar = await FileUploader.Upload(user.UserName ?? "Avatar", dto.Avatar);
            }

            if (!_userRepository.UpdateUser(user))
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ResponseDto<object>()
                    {
                        Status = 1,
                        Code = ResponseCodes.InternalError
                    });
            }

            return NoContent();
        }
    }
}
