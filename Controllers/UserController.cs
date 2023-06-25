using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PhiZoneApi.Configurations;
using PhiZoneApi.Data;
using PhiZoneApi.Dtos;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

namespace PhiZoneApi.Controllers;

/// <summary>
///     Provides user-related services.
/// </summary>
[Route("users")]
[ApiVersion("2.0")]
[ApiController]
public class UserController : Controller
{
    private readonly IFileStorageService _fileStorageService;
    private readonly IOptions<DataSettings> _dataSettings;
    private readonly IMapper _mapper;
    private readonly UserManager<User> _userManager;
    private readonly IUserRepository _userRepository;

    public UserController(
        IUserRepository userRepository,
        UserManager<User> userManager,
        IFileStorageService fileStorageService,
        IOptions<DataSettings> dataSettings,
        IMapper mapper)
    {
        _userRepository = userRepository;
        _userManager = userManager;
        _fileStorageService = fileStorageService;
        _dataSettings = dataSettings;
        _mapper = mapper;
    }

    /// <summary>
    /// Gets users.
    /// </summary>
    /// <param name="order">The field by which the result is sorted. Defaults to <c>id</c>.</param>
    /// <param name="desc">Whether or not the result is sorted in descending order. Defaults to <c>false</c>.</param>
    /// <param name="page">The page number. Defaults to 1.</param>
    /// <param name="perPage">How many entries are present in one page. Defaults to DataSettings.PaginationPerPage.</param>
    /// <returns>An array containing users.</returns>
    [HttpGet("")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<UserDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    public IActionResult GetUsers(string order = "id", bool desc = false, int page = 1, int perPage = 0)
    {
        perPage = perPage > 0 ? perPage : _dataSettings.Value.PaginationPerPage;
        var position = perPage * (page - 1);
        var users = _mapper.Map<List<UserDto>>(_userRepository.GetUsers(order, desc, position, perPage));

        return Ok(new ResponseDto<IEnumerable<UserDto>>
        {
            Status = ResponseStatus.Ok,
            Code = ResponseCode.Ok,
            PerPage = perPage,

            Data = users
        });
    }

    /// <summary>
    /// Gets a specific user by ID.
    /// </summary>
    /// <param name="id">A user's ID.</param>
    /// <returns>A user.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<UserDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    public IActionResult GetUser(int id)
    {
        if (!_userRepository.UserExists(id)) return NotFound();
        var user = _mapper.Map<UserDto>(_userRepository.GetUser(id));

        return Ok(new ResponseDto<UserDto>
        {
            Status = ResponseStatus.Ok,
            Code = ResponseCode.Ok,
            Data = user
        });
    }

    /// <summary>
    /// Updates a user.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="dto"></param>
    /// <returns>An empty body.</returns>
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