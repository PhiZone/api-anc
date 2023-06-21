using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PhiZoneApi.Data;
using PhiZoneApi.Dtos;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

namespace PhiZoneApi.Controllers;

[Route("users")]
[ApiVersion("2.0")]
[ApiController]
public class UserController : Controller
{
    private readonly IFileStorageService _fileStorageService;
    private readonly IMapper _mapper;
    private readonly UserManager<User> _userManager;
    private readonly IUserRepository _userRepository;

    public UserController(
        IUserRepository userRepository,
        UserManager<User> userManager,
        IFileStorageService fileStorageService,
        IMapper mapper)
    {
        _userRepository = userRepository;
        _userManager = userManager;
        _fileStorageService = fileStorageService;
        _mapper = mapper;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<UserDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    public IActionResult GetUsers()
    {
        var users = _mapper.Map<List<UserDto>>(_userRepository.GetUsers());
        if (!ModelState.IsValid)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorDetailed,
                Code = ResponseCode.DataInvalid,
                Errors = ModelErrorTranslator.Translate(ModelState)
            });
        return Ok(new ResponseDto<IEnumerable<UserDto>>
        {
            Status = ResponseStatus.Ok,
            Code = ResponseCode.Ok,
            Data = users
        });
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<UserDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    public IActionResult GetUser(int id)
    {
        if (!_userRepository.UserExists(id)) return NotFound();
        var user = _mapper.Map<UserDto>(_userRepository.GetUser(id));
        if (!ModelState.IsValid)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorDetailed,
                Code = ResponseCode.DataInvalid,
                Errors = ModelErrorTranslator.Translate(ModelState)
            });
        return Ok(new ResponseDto<UserDto>
        {
            Status = ResponseStatus.Ok,
            Code = ResponseCode.Ok,
            Data = user
        });
    }

    [HttpPut("{id:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> UpdateUser([FromRoute] int id, [FromForm] UserUpdateDto dto)
    {
        // Check user's existence
        if (!_userRepository.UserExists(id)) return NotFound();

        // Check permission
        var currentUser = (User?)HttpContext.Items["User"];
        if (currentUser != null && currentUser.Id != id && !await _userManager.IsInRoleAsync(currentUser, "Admin"))
            return Forbid();

        // Obtain user by id
        var user = _userRepository.GetUser(id);

        // Check the validity of ModelState
        if (!ModelState.IsValid)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorDetailed,
                Code = ResponseCode.DataInvalid,
                Errors = ModelErrorTranslator.Translate(ModelState)
            });

        // Update user name
        if (dto.UserName != null)
        {
            if (user.DateLastModifiedUserName != null
                && DateTimeOffset.UtcNow - user.DateLastModifiedUserName < TimeSpan.FromDays(15))
                return BadRequest(new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorNotYetAvailable,
                    Code = ResponseCode.UserNameCoolDown,
                    DateAvailable = (DateTimeOffset)(user.DateLastModifiedUserName + TimeSpan.FromDays(15))
                });
            user.UserName = dto.UserName;
            user.DateLastModifiedUserName = DateTimeOffset.UtcNow;
        }

        // Update avatar
        if (dto.Avatar != null) user.Avatar = await _fileStorageService.Upload(user.UserName ?? "Avatar", dto.Avatar);

        // Save
        if (!_userRepository.UpdateUser(user))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief,
                    Code = ResponseCode.InternalError
                });

        return NoContent();
    }
}