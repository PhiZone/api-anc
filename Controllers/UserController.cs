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
using PhiZoneApi.Dtos.Filters;
using PhiZoneApi.Dtos.Requests;
using PhiZoneApi.Dtos.Responses;
using PhiZoneApi.Enums;
using PhiZoneApi.Filters;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;
using StackExchange.Redis;

// ReSharper disable SuggestBaseTypeForParameterInConstructor

// ReSharper disable RouteTemplates.ActionRoutePrefixCanBeExtractedToControllerRoute

namespace PhiZoneApi.Controllers;

[Route("users")]
[ApiVersion("2.0")]
[ApiController]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
    Policy = "AllowAnonymous")]
public class UserController(
    IUserRepository userRepository,
    IUserRelationRepository userRelationRepository,
    UserManager<User> userManager,
    IFilterService filterService,
    IFileStorageService fileStorageService,
    IOptions<DataSettings> dataSettings,
    IMapper mapper,
    IDtoMapper dtoMapper,
    IRegionRepository regionRepository,
    IRecordRepository recordRepository,
    IResourceService resourceService,
    IConnectionMultiplexer redis,
    ITemplateService templateService,
    IPlayConfigurationRepository playConfigurationRepository,
    IMeilisearchService meilisearchService) : Controller
{
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
        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        dto.PerPage = dto.PerPage > 0 && dto.PerPage < dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? dataSettings.Value.PaginationPerPage : dataSettings.Value.PaginationMaxPerPage;
        dto.Page = dto.Page > 1 ? dto.Page : 1;
        var position = dto.PerPage * (dto.Page - 1);
        var predicateExpr = await filterService.Parse(filterDto, dto.Predicate, currentUser);
        IEnumerable<User> users;
        int total;
        if (dto.Search != null)
        {
            var result = await meilisearchService.SearchAsync<User>(dto.Search, dto.PerPage, dto.Page);
            var idList = result.Hits.Select(item => item.Id).ToList();
            users = (await userRepository.GetUsersAsync(["Id"], [false], position, dto.PerPage,
                e => idList.Contains(e.Id), currentUser?.Id)).OrderBy(e => idList.IndexOf(e.Id));
            total = result.TotalHits;
        }
        else
        {
            users = await userRepository.GetUsersAsync(dto.Order, dto.Desc, position, dto.PerPage, predicateExpr,
                currentUser?.Id);
            total = await userRepository.CountUsersAsync(predicateExpr);
        }

        var list = users.Select(dtoMapper.MapUser<UserDto>).ToList();

        return Ok(new ResponseDto<IEnumerable<UserDto>>
        {
            Status = ResponseStatus.Ok,
            Code = ResponseCodes.Ok,
            Total = total,
            PerPage = dto.PerPage,
            HasPrevious = position > 0,
            HasNext = dto.PerPage > 0 && dto.PerPage * dto.Page < total,
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
        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        var user = await userRepository.GetUserByIdAsync(id, currentUser?.Id);
        if (user == null)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.UserNotFound
            });
        var dto = dtoMapper.MapUser<UserDto>(user);

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
        var db = redis.GetDatabase();
        var key = $"phizone:email:{EmailRequestMode.EmailConfirmation}:{dto.EmailConfirmationCode}";
        if (!await db.KeyExistsAsync(key))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidCode
            });
        var email = await db.StringGetAsync(key);
        if (email != dto.Email)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidCode
            });
        db.KeyDelete(key);

        var user = await userManager.FindByEmailAsync(dto.Email);
        if (user != null)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.EmailOccupied
            });

        user = await userManager.FindByNameAsync(dto.UserName);
        if (user != null)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.UserNameOccupied
            });

        string? avatarUrl = null;
        if (dto.Avatar != null)
            avatarUrl = (await fileStorageService.UploadImage<User>(dto.UserName, dto.Avatar, (1, 1))).Item1;

        user = new User
        {
            SecurityStamp = Guid.NewGuid().ToString(),
            UserName = dto.UserName,
            Email = dto.Email,
            Avatar = avatarUrl,
            Language = dto.Language,
            Gender = dto.Gender,
            Biography = dto.Biography,
            RegionId = (await regionRepository.GetRegionAsync(dto.RegionCode)).Id,
            DateOfBirth =
                dto.DateOfBirth != null
                    ? new DateTimeOffset(dto.DateOfBirth.GetValueOrDefault().DateTime, TimeSpan.Zero)
                    : null,
            DateJoined = DateTimeOffset.UtcNow
        };
        await userManager.CreateAsync(user);
        user.PasswordHash = userManager.PasswordHasher.HashPassword(user, dto.Password);
        user.EmailConfirmed = true;
        user.LockoutEnabled = false;
        user.Role = UserRole.Member;
        await userManager.UpdateAsync(user);
        await meilisearchService.AddAsync(user);
        if (avatarUrl != null) await fileStorageService.SendUserInput(avatarUrl, "Avatar", Request, user);
        var configuration = new PlayConfiguration
        {
            Name = templateService.GetMessage("default", user.Language),
            PerfectJudgment = 80,
            GoodJudgment = 160,
            ChartMirroring = ChartMirroringMode.Off,
            AspectRatio = null,
            NoteSize = 1,
            BackgroundLuminance = 0.5,
            BackgroundBlur = 1,
            SimultaneousNoteHint = true,
            FcApIndicator = true,
            ChartOffset = 0,
            HitSoundVolume = 1,
            MusicVolume = 1,
            OwnerId = user.Id,
            DateCreated = DateTimeOffset.UtcNow
        };
        await playConfigurationRepository.CreatePlayConfigurationAsync(configuration);
        return StatusCode(StatusCodes.Status201Created);
    }

    /// <summary>
    ///     Creates a new user.
    /// </summary>
    /// <param name="dto">
    ///     User Name, Email, Password, Language, Gender (optional), Biography (optional),
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
    [HttpPost("brief")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status201Created, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> Register([FromBody] UserRegistrationBriefDto dto)
    {
        var db = redis.GetDatabase();
        var key = $"phizone:email:{EmailRequestMode.EmailConfirmation}:{dto.EmailConfirmationCode}";
        if (!await db.KeyExistsAsync(key))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidCode
            });
        var email = await db.StringGetAsync(key);
        if (email != dto.Email)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidCode
            });
        db.KeyDelete(key);

        var user = await userManager.FindByEmailAsync(dto.Email);
        if (user != null)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.EmailOccupied
            });

        user = await userManager.FindByNameAsync(dto.UserName);
        if (user != null)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.UserNameOccupied
            });

        user = new User
        {
            SecurityStamp = Guid.NewGuid().ToString(),
            UserName = dto.UserName,
            Email = dto.Email,
            Language = dto.Language,
            Gender = dto.Gender,
            Biography = dto.Biography,
            RegionId = (await regionRepository.GetRegionAsync(dto.RegionCode)).Id,
            DateOfBirth =
                dto.DateOfBirth != null
                    ? new DateTimeOffset(dto.DateOfBirth.GetValueOrDefault().DateTime, TimeSpan.Zero)
                    : null,
            DateJoined = DateTimeOffset.UtcNow
        };
        await userManager.CreateAsync(user);
        user.PasswordHash = userManager.PasswordHasher.HashPassword(user, dto.Password);
        user.EmailConfirmed = true;
        user.LockoutEnabled = false;
        user.Role = UserRole.Member;
        await userManager.UpdateAsync(user);
        await meilisearchService.UpdateAsync(user);
        var configuration = new PlayConfiguration
        {
            Name = templateService.GetMessage("default", user.Language),
            PerfectJudgment = 80,
            GoodJudgment = 160,
            ChartMirroring = ChartMirroringMode.Off,
            AspectRatio = null,
            NoteSize = 1,
            BackgroundLuminance = 0.5,
            BackgroundBlur = 1,
            SimultaneousNoteHint = true,
            FcApIndicator = true,
            ChartOffset = 0,
            HitSoundVolume = 1,
            MusicVolume = 1,
            OwnerId = user.Id,
            DateCreated = DateTimeOffset.UtcNow
        };
        await playConfigurationRepository.CreatePlayConfigurationAsync(configuration);

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
    [Consumes("application/json")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> UpdateUser([FromRoute] int id,
        [FromBody] JsonPatchDocument<UserUpdateDto> patchDocument)
    {
        var user = await userManager.FindByIdAsync(id.ToString());

        if (user == null)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.UserNotFound
            });

        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        if (currentUser == null) return Unauthorized();

        if ((currentUser.Id == id && !resourceService.HasPermission(currentUser, UserRole.Member)) ||
            (currentUser.Id != id && !resourceService.HasPermission(currentUser, UserRole.Administrator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        var dto = mapper.Map<UserUpdateDto>(user);
        dto.RegionCode = (await regionRepository.GetRegionAsync(user.RegionId)).Code;
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
            var otherUser = await userManager.FindByNameAsync(dto.UserName);
            if (otherUser != null && otherUser.Id != user.Id)
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
        user.RegionId = (await regionRepository.GetRegionAsync(dto.RegionCode)).Id;

        // Save
        await userManager.UpdateAsync(user);
        await meilisearchService.UpdateAsync(user);
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
    [Consumes("multipart/form-data")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> UpdateUserAvatar([FromRoute] int id, [FromForm] FileDto dto)
    {
        var user = await userManager.FindByIdAsync(id.ToString());

        if (user == null)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.UserNotFound
            });

        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        if (currentUser == null) return Unauthorized();

        if ((currentUser.Id == id && !resourceService.HasPermission(currentUser, UserRole.Member)) ||
            (currentUser.Id != id && !resourceService.HasPermission(currentUser, UserRole.Administrator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        if (dto.File != null)
        {
            user.Avatar = (await fileStorageService.UploadImage<User>(user.UserName ?? "Avatar", dto.File, (1, 1)))
                .Item1;
            await fileStorageService.SendUserInput(user.Avatar, "Avatar", Request, currentUser);
        }

        await userManager.UpdateAsync(user);
        await meilisearchService.UpdateAsync(user);
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
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> RemoveUserAvatar([FromRoute] int id)
    {
        var user = await userManager.FindByIdAsync(id.ToString());

        if (user == null)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.UserNotFound
            });

        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        if (currentUser == null) return Unauthorized();

        if ((currentUser.Id == id && !resourceService.HasPermission(currentUser, UserRole.Member)) ||
            (currentUser.Id != id && !resourceService.HasPermission(currentUser, UserRole.Administrator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        user.Avatar = null;

        await userManager.UpdateAsync(user);
        await meilisearchService.UpdateAsync(user);
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
    public async Task<IActionResult> GetFollowers([FromRoute] int id, [FromQuery] ArrayRequestDto dto,
        [FromQuery] UserRelationFilterDto? filterDto = null)
    {
        var user = await userManager.FindByIdAsync(id.ToString());

        if (user == null)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.UserNotFound
            });

        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        dto.PerPage = dto.PerPage > 0 && dto.PerPage < dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? dataSettings.Value.PaginationPerPage : dataSettings.Value.PaginationMaxPerPage;
        dto.Page = dto.Page > 1 ? dto.Page : 1;
        var position = dto.PerPage * (dto.Page - 1);
        var predicateExpr = await filterService.Parse(filterDto, dto.Predicate, currentUser);
        var relations = await userRelationRepository.GetFollowersAsync(user.Id, dto.Order, dto.Desc, position,
            dto.PerPage, predicateExpr, currentUser?.Id);
        var total = await userRelationRepository.CountFollowersAsync(user.Id, predicateExpr);
        var list = relations.Select(relation => dtoMapper.MapUser<UserDto>(relation.Follower)).ToList();

        return Ok(new ResponseDto<IEnumerable<UserDto>>
        {
            Status = ResponseStatus.Ok,
            Code = ResponseCodes.Ok,
            Total = total,
            PerPage = dto.PerPage,
            HasPrevious = position > 0,
            HasNext = dto.PerPage > 0 && dto.PerPage * dto.Page < total,
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
    public async Task<IActionResult> GetFollowees([FromRoute] int id, [FromQuery] ArrayRequestDto dto,
        [FromQuery] UserRelationFilterDto? filterDto = null)
    {
        var user = await userManager.FindByIdAsync(id.ToString());

        if (user == null)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.UserNotFound
            });

        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        dto.PerPage = dto.PerPage > 0 && dto.PerPage < dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? dataSettings.Value.PaginationPerPage : dataSettings.Value.PaginationMaxPerPage;
        dto.Page = dto.Page > 1 ? dto.Page : 1;
        var position = dto.PerPage * (dto.Page - 1);
        var predicateExpr = await filterService.Parse(filterDto, dto.Predicate, currentUser);
        var relations = await userRelationRepository.GetFolloweesAsync(user.Id, dto.Order, dto.Desc, position,
            dto.PerPage, predicateExpr, currentUser?.Id);
        var total = await userRelationRepository.CountFolloweesAsync(user.Id, predicateExpr);
        var list = relations.Select(relation => dtoMapper.MapFollowee<UserDto>(relation.Followee, currentUser?.Id))
            .ToList();

        return Ok(new ResponseDto<IEnumerable<UserDto>>
        {
            Status = ResponseStatus.Ok,
            Code = ResponseCodes.Ok,
            Total = total,
            PerPage = dto.PerPage,
            HasPrevious = position > 0,
            HasNext = dto.PerPage > 0 && dto.PerPage * dto.Page < total,
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
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(void), StatusCodes.Status201Created, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> Follow([FromRoute] int id,
        [FromQuery] UserRelationType type = UserRelationType.Following)
    {
        var user = await userManager.FindByIdAsync(id.ToString());

        if (user == null)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.UserNotFound
            });

        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        if (currentUser == null) return Unauthorized();

        if (currentUser.Id == id)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidOperation
            });

        if (await userRelationRepository.RelationExistsAsync(user.Id, currentUser.Id))
        {
            var opposite = await userRelationRepository.GetRelationAsync(user.Id, currentUser.Id);
            if (opposite.Type == UserRelationType.Blacklisted)
                return BadRequest(new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.Blacklisted
                });

            if (type == UserRelationType.Blacklisted)
                await userRelationRepository.RemoveRelationAsync(user.Id, currentUser.Id);
        }

        var relation = new UserRelation
        {
            FolloweeId = user.Id, FollowerId = currentUser.Id, Type = type, DateCreated = DateTimeOffset.UtcNow
        };

        if (await userRelationRepository.RelationExistsAsync(currentUser.Id, user.Id))
        {
            if (!await userRelationRepository.UpdateRelationAsync(relation))
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });
        }
        else
        {
            if (!await userRelationRepository.CreateRelationAsync(relation))
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
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> Unfollow([FromRoute] int id)
    {
        var user = await userManager.FindByIdAsync(id.ToString());

        if (user == null)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.UserNotFound
            });

        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        if (currentUser == null) return Unauthorized();

        if (currentUser.Id == id)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidOperation
            });

        if (!await userRelationRepository.RelationExistsAsync(currentUser.Id, user.Id))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.AlreadyDone
            });

        if (!await userRelationRepository.RemoveRelationAsync(currentUser.Id, user.Id))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return NoContent();
    }

    /// <summary>
    ///     Retrieves a user's personal bests.
    /// </summary>
    /// <param name="id">A user's ID.</param>
    /// <returns>Phi1 and Best19.</returns>
    /// <response code="200">Returns Phi1 and Best19.</response>
    [HttpGet("{id:int}/personalBests")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<UserPersonalBestsDto>))]
    public async Task<IActionResult> GetPersonalBests([FromRoute] int id)
    {
        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        var phi1 = (await recordRepository.GetRecordsAsync(["Rks"], [true], 0, 1,
            r => r.OwnerId == id && r.Score == 1000000 && r.Chart.IsRanked, true, currentUser?.Id)).FirstOrDefault();
        var phi1Dto = phi1 != null ? dtoMapper.MapRecord<RecordDto>(phi1) : null;
        var b19 = await recordRepository.GetPersonalBests(id, queryChart: true, currentUserId: currentUser?.Id);
        var b19Dto = b19.Select(dtoMapper.MapRecord<RecordDto>).ToList();
        return Ok(new ResponseDto<UserPersonalBestsDto>
        {
            Status = ResponseStatus.Ok,
            Code = ResponseCodes.Ok,
            Data = new UserPersonalBestsDto { Phi1 = phi1Dto, Best19 = b19Dto }
        });
    }
}