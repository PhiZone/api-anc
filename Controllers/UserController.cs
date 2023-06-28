using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using PhiZoneApi.Configurations;
using PhiZoneApi.Constants;
using PhiZoneApi.Dtos;
using PhiZoneApi.Enums;
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
    private readonly IOptions<DataSettings> _dataSettings;
    private readonly IFileStorageService _fileStorageService;
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
    ///     Gets users.
    /// </summary>
    /// <param name="order">The field by which the result is sorted. Defaults to <c>id</c>.</param>
    /// <param name="desc">Whether or not the result is sorted in descending order. Defaults to <c>false</c>.</param>
    /// <param name="page">The page number. Defaults to 1.</param>
    /// <param name="perPage">How many entries are present in one page. Defaults to DataSettings:PaginationPerPage.</param>
    /// <returns>An array containing users.</returns>
    [HttpGet("")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<UserDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    public IActionResult GetUsers(string order = "id", bool desc = false, int page = 1, int perPage = 0)
    {
        perPage = perPage > 0 ? perPage : _dataSettings.Value.PaginationPerPage;
        var position = perPage * (page - 1);
        var data = _mapper.Map<List<UserDto>>(_userRepository.GetUsers(order, desc, position, perPage));

        return Ok(new ResponseDto<IEnumerable<UserDto>>
        {
            Status = ResponseStatus.Ok,
            Code = ResponseCode.Ok,
            PerPage = perPage,
            Data = data
        });
    }

    /// <summary>
    ///     Gets a specific user by ID.
    /// </summary>
    /// <param name="id">A user's ID.</param>
    /// <returns>A user.</returns>
    /// <response code="200">Returns a user.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified user is not found.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<UserDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetUser([FromRoute] int id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound();
        var data = _mapper.Map<UserDto>(user);

        return Ok(new ResponseDto<UserDto>
        {
            Status = ResponseStatus.Ok,
            Code = ResponseCode.Ok,
            Data = data
        });
    }

    /// <summary>
    ///     Updates a user.
    /// </summary>
    /// <param name="id">User's ID.</param>
    /// <param name="patchDocument">A JSON Patch Document.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified user is not found.</response>
    /// <response code="500">When an internal server error occurs.</response>
    [HttpPatch("{id:int}")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> UpdateUser([FromRoute] int id,
        [FromBody] JsonPatchDocument<UserUpdateDto> patchDocument)
    {
        // Obtain user by id
        var user = await _userManager.FindByIdAsync(id.ToString());

        // Check user's existence
        if (user == null)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief,
                Code = ResponseCode.UserNotFound
            });

        // Check permission
        var currentUser = await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        if (currentUser == null || (currentUser.Id != id && !await _userManager.IsInRoleAsync(currentUser, "Admin")))
            return StatusCode(StatusCodes.Status403Forbidden, new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief,
                Code = ResponseCode.InsufficientPermission
            });

        var dto = _mapper.Map<UserUpdateDto>(user);

        patchDocument.ApplyTo(dto, ModelState);

        if (!ModelState.IsValid)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorDetailed,
                Code = ResponseCode.DataInvalid,
                Errors = ModelErrorTranslator.Translate(ModelState)
            });

        // Update user name
        if (patchDocument.Operations.FirstOrDefault(operation =>
                operation.path == "/userName" && operation.op is "add" or "replace") != null)
        {
            if (user.DateLastModifiedUserName != null
                && DateTimeOffset.UtcNow - user.DateLastModifiedUserName < TimeSpan.FromDays(15))
                return BadRequest(new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorTemporarilyUnavailable,
                    Code = ResponseCode.UserNameCoolDown,
                    DateAvailable = (DateTimeOffset)(user.DateLastModifiedUserName + TimeSpan.FromDays(15))
                });
            user.UserName = dto.UserName;
            user.DateLastModifiedUserName = DateTimeOffset.UtcNow;
        }

        // Update date of birth
        if (patchDocument.Operations.FirstOrDefault(operation =>
                operation.path == "/dateOfBirth" && operation.op is "add" or "replace") != null)
        {
            var dateOfBirth = dto.DateOfBirth.GetValueOrDefault();
            user.DateOfBirth = new DateTimeOffset(dateOfBirth.DateTime, TimeSpan.Zero);
        }

        user.Gender = dto.Gender;
        user.Biography = dto.Biography;
        user.Language = dto.Language;

        // Save
        await _userManager.UpdateAsync(user);

        return NoContent();
    }
}