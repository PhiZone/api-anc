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

[Route("users")]
[ApiVersion("2.0")]
[ApiController]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
    Policy = "AllowAnonymous")]
public class UserController : Controller
{
    private readonly IOptions<DataSettings> _dataSettings;
    private readonly IDtoMapper _dtoMapper;
    private readonly IFileStorageService _fileStorageService;
    private readonly IMailService _mailService;
    private readonly IMapper _mapper;
    private readonly UserManager<User> _userManager;
    private readonly IUserRelationRepository _userRelationRepository;
    private readonly IUserRepository _userRepository;

    public UserController(IUserRepository userRepository, IUserRelationRepository userRelationRepository,
        UserManager<User> userManager, IMailService mailService, IFileStorageService fileStorageService,
        IOptions<DataSettings> dataSettings, IMapper mapper, IDtoMapper dtoMapper)
    {
        _userRepository = userRepository;
        _userRelationRepository = userRelationRepository;
        _userManager = userManager;
        _mailService = mailService;
        _fileStorageService = fileStorageService;
        _dataSettings = dataSettings;
        _mapper = mapper;
        _dtoMapper = dtoMapper;
    }

    /// <summary>
    ///     Retrieves users.
    /// </summary>
    /// <param name="order">The field by which the result is sorted. Defaults to <c>id</c>.</param>
    /// <param name="desc">Whether or not the result is sorted in descending order. Defaults to <c>false</c>.</param>
    /// <param name="page">The page number. Defaults to 1.</param>
    /// <param name="perPage">How many entries are present in one page. Defaults to DataSettings:PaginationPerPage.</param>
    /// <returns>An array of users.</returns>
    /// <response code="200">Returns an array of users.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<UserDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetUsers(string order = "id", bool desc = false, int page = 1, int perPage = 0)
    {
        var currentUser = await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        perPage = perPage > 0 ? perPage : _dataSettings.Value.PaginationPerPage;
        var position = perPage * (page - 1);
        var users = await _userRepository.GetUsersAsync(order, desc, position, perPage);
        var total = await _userRepository.CountAsync();
        var list = new List<UserDto>();

        foreach (var user in users) list.Add(await _dtoMapper.MapUserAsync<UserDto>(user, currentUser: currentUser));

        return Ok(new ResponseDto<IEnumerable<UserDto>>
        {
            Status = ResponseStatus.Ok,
            Code = ResponseCodes.Ok,
            Total = total,
            PerPage = perPage,
            HasPrevious = position > 0,
            HasNext = position < total - total % perPage,
            Data = list
        });
    }

    /// <summary>
    ///     Retrieves a specific user.
    /// </summary>
    /// <param name="id">A user's ID.</param>
    /// <returns>A user.</returns>
    /// <response code="200">Returns a user.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified user is not found.</response>
    [HttpGet("{id:int}")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<UserDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetUser([FromRoute] int id)
    {
        var currentUser = await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound();
        var dto = await _dtoMapper.MapUserAsync<UserDto>(user, currentUser: currentUser);

        return Ok(new ResponseDto<UserDto> { Status = ResponseStatus.Ok, Code = ResponseCodes.Ok, Data = dto });
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
    [Consumes("multipart/form-data")]
    [Produces("text/plain", "application/json")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> Register([FromForm] UserRegistrationDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user != null)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.EmailOccupied
            });

        user = await _userManager.FindByNameAsync(dto.UserName);
        if (user != null)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.UserNameOccupied
            });

        string? avatarUrl = null;
        if (dto.Avatar != null)
        {
            var stream = ImageUtil.CropImage(dto.Avatar, (1, 1));
            avatarUrl = await _fileStorageService.Upload(dto.UserName, stream, "webp");
        }

        user = new User
        {
            SecurityStamp = Guid.NewGuid().ToString(),
            UserName = dto.UserName,
            Email = dto.Email,
            Avatar = avatarUrl,
            Language = dto.Language,
            Gender = (int)dto.Gender,
            Biography = dto.Biography,
            DateOfBirth =
                dto.DateOfBirth != null
                    ? new DateTimeOffset(dto.DateOfBirth.GetValueOrDefault().DateTime, TimeSpan.Zero)
                    : null,
            DateJoined = DateTimeOffset.UtcNow
        };

        user.PasswordHash = _userManager.PasswordHasher.HashPassword(user, dto.Password);

        var errorCode =
            await _mailService.PublishEmailAsync(user, EmailRequestMode.EmailConfirmation, SucceedingAction.Create);
        if (!errorCode.Equals(string.Empty))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = errorCode });

        return StatusCode(StatusCodes.Status201Created);
    }

    /// <summary>
    ///     Updates a user.
    /// </summary>
    /// <param name="id">User's ID.</param>
    /// <param name="patchDocument">A JSON Patch Document.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified user is not found.</response>
    [HttpPatch("{id:int}")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Consumes("application/json")]
    [Produces("text/plain", "application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> UpdateUser([FromRoute] int id,
        [FromBody] JsonPatchDocument<UserUpdateDto> patchDocument)
    {
        // Obtain user by id
        var user = await _userManager.FindByIdAsync(id.ToString());

        // Check user's existence
        if (user == null)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.UserNotFound
            });

        // Check permission
        var currentUser = await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        if (currentUser == null) return Unauthorized();

        if ((currentUser.Id == id && !await _userManager.IsInRoleAsync(currentUser, "Member")) ||
            (currentUser.Id != id && !await _userManager.IsInRoleAsync(currentUser, "Admin")))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        var dto = _mapper.Map<UserUpdateDto>(user);

        patchDocument.ApplyTo(dto, ModelState);

        if (!TryValidateModel(dto))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorDetailed,
                Code = ResponseCodes.DataInvalid,
                Errors = ModelErrorTranslator.Translate(ModelState)
            });

        // Update user name
        if (patchDocument.Operations.FirstOrDefault(operation =>
                operation.path == "/userName" && operation.op is "add" or "replace") != null)
        {
            var otherUser = await _userManager.FindByNameAsync(dto.UserName);
            if (otherUser != null)
                return BadRequest(new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.UserNameOccupied
                });
            if (user.DateLastModifiedUserName != null &&
                DateTimeOffset.UtcNow - user.DateLastModifiedUserName < TimeSpan.FromDays(15))
                return BadRequest(new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorTemporarilyUnavailable,
                    Code = ResponseCodes.UserNameCooldown,
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

    /// <summary>
    ///     Updates a user's avatar.
    /// </summary>
    /// <param name="id">User's ID.</param>
    /// <param name="dto">The new avatar.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified user is not found.</response>
    [HttpPatch("{id:int}/avatar")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Consumes("multipart/form-data")]
    [Produces("text/plain", "application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> UpdateUserAvatar([FromRoute] int id, [FromForm] UserAvatarDto dto)
    {
        // Obtain user by id
        var user = await _userManager.FindByIdAsync(id.ToString());

        // Check user's existence
        if (user == null)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.UserNotFound
            });

        // Check permission
        var currentUser = await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        if (currentUser == null) return Unauthorized();

        if ((currentUser.Id == id && !await _userManager.IsInRoleAsync(currentUser, "Member")) ||
            (currentUser.Id != id && !await _userManager.IsInRoleAsync(currentUser, "Admin")))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        if (dto.Avatar != null)
        {
            var image = ImageUtil.CropImage(dto.Avatar, (1, 1));
            user.Avatar = await _fileStorageService.Upload(user.UserName ?? "Avatar", image, "webp");
        }
        else
        {
            user.Avatar = null;
        }

        await _userManager.UpdateAsync(user);
        return NoContent();
    }

    /// <summary>
    ///     Retrieves followers of user.
    /// </summary>
    /// <param name="id">User's ID.</param>
    /// <param name="order">The field by which the result is sorted. Defaults to <c>time</c>.</param>
    /// <param name="desc">Whether or not the result is sorted in descending order. Defaults to <c>false</c>.</param>
    /// <param name="page">The page number. Defaults to 1.</param>
    /// <param name="perPage">How many entries are present in one page. Defaults to DataSettings:PaginationPerPage.</param>
    /// <returns>An array of followers of user.</returns>
    /// <response code="200">Returns an array of followers of user.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified user is not found.</response>
    [HttpGet("{id:int}/followers")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<UserDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetFollowers([FromRoute] int id, string order = "time", bool desc = false,
        int page = 1, int perPage = 0)
    {
        // Obtain user by id
        var user = await _userManager.FindByIdAsync(id.ToString());

        // Check user's existence
        if (user == null)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.UserNotFound
            });

        var currentUser = await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        perPage = perPage > 0 ? perPage : _dataSettings.Value.PaginationPerPage;
        var position = perPage * (page - 1);
        var followers = await _userRelationRepository.GetFollowersAsync(user.Id, order, desc, position, perPage);
        var total = await _userRelationRepository.CountFollowersAsync(user);
        var list = new List<UserDto>();

        foreach (var follower in followers) list.Add(await _dtoMapper.MapFollowerAsync<UserDto>(follower, currentUser));

        return Ok(new ResponseDto<IEnumerable<UserDto>>
        {
            Status = ResponseStatus.Ok,
            Code = ResponseCodes.Ok,
            Total = total,
            PerPage = perPage,
            HasPrevious = position > 0,
            HasNext = position < total - total % perPage,
            Data = list
        });
    }

    /// <summary>
    ///     Retrieves followees of user.
    /// </summary>
    /// <param name="id">User's ID.</param>
    /// <param name="order">The field by which the result is sorted. Defaults to <c>time</c>.</param>
    /// <param name="desc">Whether or not the result is sorted in descending order. Defaults to <c>false</c>.</param>
    /// <param name="page">The page number. Defaults to 1.</param>
    /// <param name="perPage">How many entries are present in one page. Defaults to DataSettings:PaginationPerPage.</param>
    /// <returns>An array of followees of user.</returns>
    /// <response code="200">Returns an array of followees of user.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified user is not found.</response>
    [HttpGet("{id:int}/followees")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<UserDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetFollowees([FromRoute] int id, string order = "time", bool desc = false,
        int page = 1, int perPage = 0)
    {
        // Obtain user by id
        var user = await _userManager.FindByIdAsync(id.ToString());

        // Check user's existence
        if (user == null)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.UserNotFound
            });

        var currentUser = await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        perPage = perPage > 0 ? perPage : _dataSettings.Value.PaginationPerPage;
        var position = perPage * (page - 1);
        var followees = await _userRelationRepository.GetFolloweesAsync(user.Id, order, desc, position, perPage);
        var total = await _userRelationRepository.CountFolloweesAsync(user);
        var list = new List<UserDto>();

        foreach (var followee in followees) list.Add(await _dtoMapper.MapFolloweeAsync<UserDto>(followee, currentUser));

        return Ok(new ResponseDto<IEnumerable<UserDto>>
        {
            Status = ResponseStatus.Ok,
            Code = ResponseCodes.Ok,
            Total = total,
            PerPage = perPage,
            HasPrevious = position > 0,
            HasNext = position < total - total % perPage,
            Data = list
        });
    }

    /// <summary>
    ///     Follows a user.
    /// </summary>
    /// <param name="id">Target's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="201">Returns an empty body.</response>
    /// <response code="400">
    ///     When the user
    ///     1. follows themselves;
    ///     2. has already followed the specified user.
    /// </response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="404">When the specified user is not found.</response>
    [HttpPost("{id:int}/follow")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Produces("text/plain", "application/json")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> Follow([FromRoute] int id)
    {
        // Obtain user by id
        var user = await _userManager.FindByIdAsync(id.ToString());

        // Check user's existence
        if (user == null)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.UserNotFound
            });

        // Check permission
        var currentUser = await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        if (currentUser == null) return Unauthorized();

        if (currentUser.Id == id)
            return BadRequest(
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidOperation
                });

        if (await _userRelationRepository.RelationExistsAsync(currentUser.Id, user.Id))
            return BadRequest(
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.AlreadyDone
                });

        var relation = new UserRelation
        {
            Followee = user,
            Follower = currentUser,
            Time = DateTimeOffset.UtcNow
        };

        if (!await _userRelationRepository.CreateRelationAsync(relation))
            return StatusCode(StatusCodes.Status500InternalServerError, new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief,
                Code = ResponseCodes.InternalError
            });

        return StatusCode(StatusCodes.Status201Created);
    }

    /// <summary>
    ///     Unfollows a user.
    /// </summary>
    /// <param name="id">Target's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">
    ///     When the user
    ///     1. unfollows themselves;
    ///     2. has not yet followed (already unfollowed) the specified user.
    /// </response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="404">When the specified user is not found.</response>
    [HttpPost("{id:int}/unfollow")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Produces("text/plain", "application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> Unfollow([FromRoute] int id)
    {
        // Obtain user by id
        var user = await _userManager.FindByIdAsync(id.ToString());

        // Check user's existence
        if (user == null)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.UserNotFound
            });

        // Check permission
        var currentUser = await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        if (currentUser == null) return Unauthorized();

        if (currentUser.Id == id)
            return BadRequest(
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidOperation
                });

        if (!await _userRelationRepository.RelationExistsAsync(currentUser.Id, user.Id))
            return BadRequest(
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.AlreadyDone
                });

        var relation = await _userRelationRepository.GetRelationAsync(currentUser.Id, user.Id);

        if (!await _userRelationRepository.RemoveRelationAsync(relation))
            return StatusCode(StatusCodes.Status500InternalServerError, new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief,
                Code = ResponseCodes.InternalError
            });

        return NoContent();
    }
}