using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using PhiZoneApi.Configurations;
using PhiZoneApi.Constants;
using PhiZoneApi.Dtos.Deliverers;
using PhiZoneApi.Dtos.Filters;
using PhiZoneApi.Dtos.Requests;
using PhiZoneApi.Dtos.Responses;
using PhiZoneApi.Enums;
using PhiZoneApi.Filters;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

// ReSharper disable RouteTemplates.ActionRoutePrefixCanBeExtractedToControllerRoute

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
    private readonly IFilterService _filterService;
    private readonly IMailService _mailService;
    private readonly IMapper _mapper;
    private readonly IRecordRepository _recordRepository;
    private readonly IRecordService _recordService;
    private readonly IRegionRepository _regionRepository;
    private readonly IResourceService _resourceService;
    private readonly ITapTapService _tapTapService;
    private readonly UserManager<User> _userManager;
    private readonly IUserRelationRepository _userRelationRepository;
    private readonly IUserRepository _userRepository;

    public UserController(IUserRepository userRepository, IUserRelationRepository userRelationRepository,
        UserManager<User> userManager, IMailService mailService, IFilterService filterService,
        IFileStorageService fileStorageService, IOptions<DataSettings> dataSettings, IMapper mapper,
        IDtoMapper dtoMapper, IRegionRepository regionRepository, IRecordRepository recordRepository,
        IRecordService recordService, IResourceService resourceService, ITapTapService tapTapService)
    {
        _userRepository = userRepository;
        _userRelationRepository = userRelationRepository;
        _userManager = userManager;
        _mailService = mailService;
        _filterService = filterService;
        _fileStorageService = fileStorageService;
        _dataSettings = dataSettings;
        _mapper = mapper;
        _dtoMapper = dtoMapper;
        _regionRepository = regionRepository;
        _recordRepository = recordRepository;
        _recordService = recordService;
        _resourceService = resourceService;
        _tapTapService = tapTapService;
    }

    /// <summary>
    ///     Retrieves users.
    /// </summary>
    /// <returns>An array of users.</returns>
    /// <response code="200">Returns an array of users.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<UserDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetUsers([FromQuery] ArrayRequestDto dto,
        [FromQuery] UserFilterDto? filterDto = null)
    {
        var currentUser = await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        dto.PerPage = dto.PerPage > 0
            ? dto.PerPage <= _dataSettings.Value.PaginationMaxPerPage
                ? dto.PerPage
                : _dataSettings.Value.PaginationMaxPerPage
            : _dataSettings.Value.PaginationPerPage;
        var position = dto.PerPage * (dto.Page - 1);
        var predicateExpr = await _filterService.Parse(filterDto, dto.Predicate, currentUser);
        var users = await _userRepository.GetUsersAsync(dto.Order, dto.Desc, position, dto.PerPage, dto.Search,
            predicateExpr);
        var total = await _userRepository.CountUsersAsync(dto.Search, predicateExpr);
        var list = new List<UserDto>();

        foreach (var user in users) list.Add(await _dtoMapper.MapUserAsync<UserDto>(user, currentUser));

        return Ok(new ResponseDto<IEnumerable<UserDto>>
        {
            Status = ResponseStatus.Ok,
            Code = ResponseCodes.Ok,
            Total = total,
            PerPage = dto.PerPage,
            HasPrevious = position > 0,
            HasNext = position < total - total % dto.PerPage,
            Data = list
        });
    }

    /// <summary>
    ///     Retrieves a specific user.
    /// </summary>
    /// <param name="id">A user's ID.</param>
    /// <returns>A user.</returns>
    /// <response code="200">Returns a user.</response>
    /// <response code="304">
    ///     When the resource has not been updated since last retrieval. Requires <c>If-None-Match</c>.
    /// </response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified user is not found.</response>
    [HttpGet("{id:int}")]
    [ServiceFilter(typeof(ETagFilter))]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<UserDto>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status304NotModified, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetUser([FromRoute] int id)
    {
        var currentUser = await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.UserNotFound
            });
        var dto = await _dtoMapper.MapUserAsync<UserDto>(user, currentUser);

        return Ok(new ResponseDto<UserDto> { Status = ResponseStatus.Ok, Code = ResponseCodes.Ok, Data = dto });
    }

    /// <summary>
    ///     Creates a new user.
    /// </summary>
    /// <param name="dto">
    ///     User Name, Email, Password, Language, Gender (optional), Avatar (optional), Biography (optional),
    ///     Date of Birth (optional)
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
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status201Created, "text/plain")]
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
            avatarUrl = (await _fileStorageService.UploadImage<User>(dto.UserName, dto.Avatar, (1, 1))).Item1;

        user = new User
        {
            SecurityStamp = Guid.NewGuid().ToString(),
            UserName = dto.UserName,
            Email = dto.Email,
            Avatar = avatarUrl,
            Language = dto.Language,
            Gender = (int)dto.Gender,
            Biography = dto.Biography,
            RegionId = (await _regionRepository.GetRegionAsync(dto.RegionCode)).Id,
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
    /// <param name="id">A user's ID.</param>
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
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> UpdateUser([FromRoute] int id,
        [FromBody] JsonPatchDocument<UserUpdateDto> patchDocument)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());

        if (user == null)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.UserNotFound
            });

        var currentUser = await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        if (currentUser == null) return Unauthorized();

        if ((currentUser.Id == id && !await _resourceService.HasPermission(currentUser, Roles.Member)) ||
            (currentUser.Id != id && !await _resourceService.HasPermission(currentUser, Roles.Administrator)))
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
                Code = ResponseCodes.InvalidData,
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
        user.RegionId = (await _regionRepository.GetRegionAsync(dto.RegionCode)).Id;

        // Save
        await _userManager.UpdateAsync(user);

        return NoContent();
    }

    /// <summary>
    ///     Updates a user's avatar.
    /// </summary>
    /// <param name="id">A user's ID.</param>
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
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> UpdateUserAvatar([FromRoute] int id, [FromForm] UserAvatarDto dto)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());

        if (user == null)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.UserNotFound
            });

        var currentUser = await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        if (currentUser == null) return Unauthorized();

        if ((currentUser.Id == id && !await _resourceService.HasPermission(currentUser, Roles.Member)) ||
            (currentUser.Id != id && !await _resourceService.HasPermission(currentUser, Roles.Administrator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        if (dto.Avatar != null)
            user.Avatar = (await _fileStorageService.UploadImage<User>(user.UserName ?? "Avatar", dto.Avatar, (1, 1)))
                .Item1;

        await _userManager.UpdateAsync(user);
        return NoContent();
    }

    /// <summary>
    ///     Removes a user's avatar.
    /// </summary>
    /// <param name="id">A user's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified user is not found.</response>
    [HttpDelete("{id:int}/avatar")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> RemoveUserAvatar([FromRoute] int id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());

        if (user == null)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.UserNotFound
            });

        var currentUser = await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        if (currentUser == null) return Unauthorized();

        if ((currentUser.Id == id && !await _resourceService.HasPermission(currentUser, Roles.Member)) ||
            (currentUser.Id != id && !await _resourceService.HasPermission(currentUser, Roles.Administrator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        user.Avatar = null;

        await _userManager.UpdateAsync(user);
        return NoContent();
    }

    /// <summary>
    ///     Binds a user to a TapTap account.
    /// </summary>
    /// <param name="id">A user's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified user is not found.</response>
    [HttpPost("{id:int}/bindings/tapTap")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> BindTapTap([FromRoute] int id, [FromBody] TapTapRequestDto dto)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());

        if (user == null)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.UserNotFound
            });

        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;

        if ((currentUser.Id == id && !await _resourceService.HasPermission(currentUser, Roles.Member)) ||
            (currentUser.Id != id && !await _resourceService.HasPermission(currentUser, Roles.Administrator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        var response = await _tapTapService.Login(dto);

        if (!response.IsSuccessStatusCode)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorWithData,
                Code = ResponseCodes.RemoteFailure,
                Data = JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync())
            });

        var responseDto =
            JsonConvert.DeserializeObject<TapTapDelivererDto>(await response.Content.ReadAsStringAsync())!;
        var targetUser = await _userRepository.GetUserByTapUnionId(responseDto.Data.Unionid);
        if (targetUser != null)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief,
                Code = targetUser.Id == currentUser.Id ? ResponseCodes.AlreadyDone : ResponseCodes.BindingOccupied
            });

        user.TapUnionId = responseDto.Data.Unionid;
        await _userManager.UpdateAsync(user);
        return NoContent();
    }

    /// <summary>
    ///     Unbinds a user from a TapTap account.
    /// </summary>
    /// <param name="id">A user's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified user is not found.</response>
    [HttpDelete("{id:int}/bindings/tapTap")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> UnbindTapTap([FromRoute] int id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());

        if (user == null)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.UserNotFound
            });

        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;

        if ((currentUser.Id == id && !await _resourceService.HasPermission(currentUser, Roles.Member)) ||
            (currentUser.Id != id && !await _resourceService.HasPermission(currentUser, Roles.Administrator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (user.TapUnionId == null)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief,
                Code = ResponseCodes.AlreadyDone
            });

        user.TapUnionId = null;
        await _userManager.UpdateAsync(user);
        return NoContent();
    }

    /// <summary>
    ///     Retrieves followers of user.
    /// </summary>
    /// <param name="id">A user's ID.</param>
    /// <returns>An array of followers of user.</returns>
    /// <response code="200">Returns an array of followers of user.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified user is not found.</response>
    [HttpGet("{id:int}/followers")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<UserDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetFollowers([FromRoute] int id, [FromQuery] ArrayWithTimeRequestDto dto,
        [FromQuery] UserRelationFilterDto? filterDto = null)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());

        if (user == null)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.UserNotFound
            });

        var currentUser = await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        dto.PerPage = dto.PerPage > 0
            ? dto.PerPage <= _dataSettings.Value.PaginationMaxPerPage
                ? dto.PerPage
                : _dataSettings.Value.PaginationMaxPerPage
            : _dataSettings.Value.PaginationPerPage;
        var position = dto.PerPage * (dto.Page - 1);
        var predicateExpr = await _filterService.Parse(filterDto, dto.Predicate, currentUser);
        var followers =
            await _userRelationRepository.GetFollowersAsync(user.Id, dto.Order, dto.Desc, position, dto.PerPage,
                predicateExpr);
        var total = await _userRelationRepository.CountFollowersAsync(user.Id, predicateExpr);
        var list = new List<UserDto>();

        foreach (var follower in followers) list.Add(await _dtoMapper.MapFollowerAsync<UserDto>(follower, currentUser));

        return Ok(new ResponseDto<IEnumerable<UserDto>>
        {
            Status = ResponseStatus.Ok,
            Code = ResponseCodes.Ok,
            Total = total,
            PerPage = dto.PerPage,
            HasPrevious = position > 0,
            HasNext = position < total - total % dto.PerPage,
            Data = list
        });
    }

    /// <summary>
    ///     Retrieves followees of user.
    /// </summary>
    /// <param name="id">A user's ID.</param>
    /// <returns>An array of followees of user.</returns>
    /// <response code="200">Returns an array of followees of user.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified user is not found.</response>
    [HttpGet("{id:int}/followees")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<UserDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetFollowees([FromRoute] int id, [FromQuery] ArrayWithTimeRequestDto dto,
        [FromQuery] UserRelationFilterDto? filterDto = null)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());

        if (user == null)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.UserNotFound
            });

        var currentUser = await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        dto.PerPage = dto.PerPage > 0
            ? dto.PerPage <= _dataSettings.Value.PaginationMaxPerPage
                ? dto.PerPage
                : _dataSettings.Value.PaginationMaxPerPage
            : _dataSettings.Value.PaginationPerPage;
        var position = dto.PerPage * (dto.Page - 1);
        var predicateExpr = await _filterService.Parse(filterDto, dto.Predicate, currentUser);
        var followees =
            await _userRelationRepository.GetFolloweesAsync(user.Id, dto.Order, dto.Desc, position, dto.PerPage,
                predicateExpr);
        var total = await _userRelationRepository.CountFolloweesAsync(user.Id, predicateExpr);
        var list = new List<UserDto>();

        foreach (var followee in followees) list.Add(await _dtoMapper.MapFolloweeAsync<UserDto>(followee, currentUser));

        return Ok(new ResponseDto<IEnumerable<UserDto>>
        {
            Status = ResponseStatus.Ok,
            Code = ResponseCodes.Ok,
            Total = total,
            PerPage = dto.PerPage,
            HasPrevious = position > 0,
            HasNext = position < total - total % dto.PerPage,
            Data = list
        });
    }

    /// <summary>
    ///     Follows a user.
    /// </summary>
    /// <param name="id">A user's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="201">Returns an empty body.</response>
    /// <response code="400">When the user follows themselves or is blacklisted by the specified user.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="404">When the specified user is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPost("{id:int}/follow")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status201Created, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> Follow([FromRoute] int id,
        [FromQuery] UserRelationType type = UserRelationType.Following)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());

        if (user == null)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.UserNotFound
            });

        var currentUser = await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        if (currentUser == null) return Unauthorized();

        if (currentUser.Id == id)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidOperation
            });

        if (await _userRelationRepository.RelationExistsAsync(user.Id, currentUser.Id))
        {
            var opposite = await _userRelationRepository.GetRelationAsync(user.Id, currentUser.Id);
            if (opposite.Type == UserRelationType.Blacklisted)
                return BadRequest(new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.Blacklisted
                });

            if (type == UserRelationType.Blacklisted)
                await _userRelationRepository.RemoveRelationAsync(user.Id, currentUser.Id);
        }

        var relation = new UserRelation
        {
            Followee = user, Follower = currentUser, Type = type, DateCreated = DateTimeOffset.UtcNow
        };

        if (await _userRelationRepository.RelationExistsAsync(currentUser.Id, user.Id))
        {
            if (!await _userRelationRepository.UpdateRelationAsync(relation))
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });
        }
        else
        {
            if (!await _userRelationRepository.CreateRelationAsync(relation))
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });
        }

        return StatusCode(StatusCodes.Status201Created);
    }

    /// <summary>
    ///     Unfollows a user.
    /// </summary>
    /// <param name="id">A user's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">
    ///     When the user
    ///     1. unfollows themselves;
    ///     2. has not yet followed (already unfollowed) the specified user.
    /// </response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="404">When the specified user is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPost("{id:int}/unfollow")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> Unfollow([FromRoute] int id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());

        if (user == null)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.UserNotFound
            });

        var currentUser = await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        if (currentUser == null) return Unauthorized();

        if (currentUser.Id == id)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidOperation
            });

        if (!await _userRelationRepository.RelationExistsAsync(currentUser.Id, user.Id))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.AlreadyDone
            });

        if (!await _userRelationRepository.RemoveRelationAsync(currentUser.Id, user.Id))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return NoContent();
    }

    /// <summary>
    ///     Gets a user's best play records.
    /// </summary>
    /// <param name="id">A user's ID.</param>
    /// <returns>Phi1 and Best19.</returns>
    /// <response code="200">Returns Phi1 and Best19.</response>
    [HttpGet("{id:int}/bestRecords")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<UserBestRecordsDto>))]
    public async Task<IActionResult> GetBestRecords([FromRoute] int id)
    {
        var currentUser = await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        var phi1 = (await _recordRepository.GetRecordsAsync("Rks", true, 0, 1,
            r => r.OwnerId == id && r.Score == 1000000 && r.Chart.IsRanked)).FirstOrDefault();
        var phi1Dto = phi1 != null ? await _dtoMapper.MapRecordAsync<RecordDto>(phi1) : null;
        var b19 = await _recordService.GetBest19(id);
        var b19Dto = new List<RecordDto>();
        foreach (var record in b19) b19Dto.Add(await _dtoMapper.MapRecordAsync<RecordDto>(record, currentUser));
        return Ok(new ResponseDto<UserBestRecordsDto>
        {
            Status = ResponseStatus.Ok,
            Code = ResponseCodes.Ok,
            Data = new UserBestRecordsDto { Phi1 = phi1Dto, Best19 = b19Dto }
        });
    }
}