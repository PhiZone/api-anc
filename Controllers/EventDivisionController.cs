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

// ReSharper disable RouteTemplates.ActionRoutePrefixCanBeExtractedToControllerRoute

namespace PhiZoneApi.Controllers;

[Route("events/divisions")]
[ApiVersion("2.0")]
[ApiController]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
    Policy = "AllowAnonymous")]
public class EventDivisionController(
    IEventDivisionRepository eventDivisionRepository,
    IEventTaskRepository eventTaskRepository,
    IOptions<DataSettings> dataSettings,
    IDtoMapper dtoMapper,
    IFilterService filterService,
    UserManager<User> userManager,
    ILikeRepository likeRepository,
    ILikeService likeService,
    IMapper mapper,
    ICommentRepository commentRepository,
    IFileStorageService fileStorageService,
    IResourceService resourceService,
    INotificationService notificationService,
    IMeilisearchService meilisearchService) : Controller
{
    /// <summary>
    ///     Retrieves event divisions.
    /// </summary>
    /// <returns>An array of event divisions.</returns>
    /// <response code="200">Returns an array of event divisions.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<EventDivisionDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetEventDivisions([FromQuery] ArrayRequestDto dto,
        [FromQuery] EventDivisionFilterDto? filterDto = null)
    {
        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        dto.PerPage = dto.PerPage > 0 && dto.PerPage < dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? dataSettings.Value.PaginationPerPage : dataSettings.Value.PaginationMaxPerPage;
        dto.Page = dto.Page > 1 ? dto.Page : 1;
        var position = dto.PerPage * (dto.Page - 1);
        var predicateExpr = await filterService.Parse(filterDto, dto.Predicate, currentUser,
            e => e.DateUnveiled <= DateTimeOffset.UtcNow || (currentUser != null &&
                                                             (resourceService.HasPermission(currentUser,
                                                                  UserRole.Administrator) ||
                                                              e.Administrators.Contains(currentUser))));
        IEnumerable<EventDivision> eventDivisions;
        int total;
        if (dto.Search != null)
        {
            var result = await meilisearchService.SearchAsync<EventDivision>(dto.Search, dto.PerPage, dto.Page);
            var idList = result.Hits.Select(item => item.Id).ToList();
            eventDivisions =
                (await eventDivisionRepository.GetEventDivisionsAsync(["DateCreated"], [false], 0, -1,
                    e => idList.Contains(e.Id), currentUser?.Id)).OrderBy(e => idList.IndexOf(e.Id));
            total = result.TotalHits;
        }
        else
        {
            eventDivisions = await eventDivisionRepository.GetEventDivisionsAsync(dto.Order, dto.Desc, position,
                dto.PerPage, predicateExpr, currentUser?.Id);
            total = await eventDivisionRepository.CountEventDivisionsAsync(predicateExpr);
        }

        var list = eventDivisions.Select(dtoMapper.MapEventDivision<EventDivisionDto>).ToList();

        return Ok(new ResponseDto<IEnumerable<EventDivisionDto>>
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
    ///     Retrieves a specific event division.
    /// </summary>
    /// <param name="id">An event division's ID.</param>
    /// <returns>An event division.</returns>
    /// <response code="200">Returns an event division.</response>
    /// <response code="304">
    ///     When the resource has not been updated since last retrieval. Requires <c>If-None-Match</c>.
    /// </response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified event division is not found.</response>
    [HttpGet("{id:guid}")]
    [ServiceFilter(typeof(ETagFilter))]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<EventDivisionDto>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status304NotModified, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetEventDivision([FromRoute] Guid id)
    {
        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        if (!await eventDivisionRepository.EventDivisionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var eventDivision = await eventDivisionRepository.GetEventDivisionAsync(id, currentUser?.Id);
        if ((currentUser == null || !eventDivision.Administrators.Contains(currentUser) ||
             !resourceService.HasPermission(currentUser, UserRole.Administrator)) &&
            eventDivision.DateUnveiled <= DateTimeOffset.UtcNow)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var dto = dtoMapper.MapEventDivision<EventDivisionDto>(eventDivision);

        return Ok(new ResponseDto<EventDivisionDto>
        {
            Status = ResponseStatus.Ok, Code = ResponseCodes.Ok, Data = dto
        });
    }

    /// <summary>
    ///     Retrieves tasks of a specific event division.
    /// </summary>
    /// <returns>An array of event tasks.</returns>
    /// <response code="200">Returns an array of event tasks.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    [HttpGet("{id:guid}/tasks")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<EventDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetEventTasks([FromRoute] Guid id, [FromQuery] ArrayRequestDto dto,
        [FromQuery] EventTaskFilterDto? filterDto = null)
    {
        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        if (!await eventDivisionRepository.EventDivisionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        dto.PerPage = dto.PerPage > 0 && dto.PerPage < dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? dataSettings.Value.PaginationPerPage : dataSettings.Value.PaginationMaxPerPage;
        dto.Page = dto.Page > 1 ? dto.Page : 1;
        var position = dto.PerPage * (dto.Page - 1);
        var predicateExpr = await filterService.Parse(filterDto, dto.Predicate, currentUser);
        IEnumerable<EventTask> eventTasks;
        int total;
        if (dto.Search != null)
        {
            var result = await meilisearchService.SearchAsync<EventTask>(dto.Search, dto.PerPage, dto.Page);
            var idList = result.Hits.Select(item => item.Id).ToList();
            eventTasks =
                (await eventTaskRepository.GetEventTasksAsync(["DateCreated"], [false], 0, -1,
                    e => idList.Contains(e.Id))).OrderBy(e => idList.IndexOf(e.Id));
            total = result.TotalHits;
        }
        else
        {
            eventTasks = await eventTaskRepository.GetEventTasksAsync(dto.Order, dto.Desc, position, dto.PerPage,
                predicateExpr);
            total = await eventTaskRepository.CountEventTasksAsync(predicateExpr);
        }

        var list = eventTasks.Select(mapper.Map<EventTaskDto>).ToList();

        return Ok(new ResponseDto<IEnumerable<EventTaskDto>>
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
    ///     Creates a new event division.
    /// </summary>
    /// <returns>An empty body.</returns>
    /// <response code="201">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(void), StatusCodes.Status201Created, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> CreateEventDivision([FromBody] EventDivisionCreationDto dto)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!resourceService.HasPermission(currentUser, UserRole.Administrator))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        var illustrationUrl =
            (await fileStorageService.UploadImage<EventDivision>(dto.Title, dto.Illustration, (16, 9))).Item1;
        await fileStorageService.SendUserInput(illustrationUrl, "Illustration", Request, currentUser);

        var administratorTasks = dto.Administrators.Select(id => userManager.FindByIdAsync(id.ToString())).ToList();
        await Task.WhenAll(administratorTasks);

        var eventDivision = new EventDivision
        {
            Title = dto.Title,
            Subtitle = dto.Subtitle,
            Type = dto.Type,
            Status = dto.Status,
            Illustration = illustrationUrl,
            Illustrator = dto.Illustrator,
            Description = dto.Description,
            TagId = dto.TagId,
            MinTeamCount = dto.MinTeamCount,
            MaxTeamCount = dto.MaxTeamCount,
            MinParticipantPerTeamCount = dto.MinParticipantPerTeamCount,
            MaxParticipantPerTeamCount = dto.MaxParticipantPerTeamCount,
            MinSubmissionCount = dto.MinSubmissionCount,
            MaxSubmissionCount = dto.MaxSubmissionCount,
            Accessibility = dto.Accessibility,
            IsHidden = dto.IsHidden,
            IsLocked = dto.IsLocked,
            EventId = dto.EventId,
            OwnerId = dto.OwnerId,
            DateUnveiled = dto.DateUnveiled,
            Administrators = administratorTasks.Where(t => t.Result != null).Select(t => t.Result!).ToList(),
            DateCreated = DateTimeOffset.UtcNow,
            DateUpdated = DateTimeOffset.UtcNow
        };
        if (!await eventDivisionRepository.CreateEventDivisionAsync(eventDivision))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return StatusCode(StatusCodes.Status201Created);
    }

    /// <summary>
    ///     Updates an event division.
    /// </summary>
    /// <param name="id">An event division's ID.</param>
    /// <param name="patchDocument">A JSON Patch Document.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified event division is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPatch("{id:guid}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> UpdateEventDivision([FromRoute] Guid id,
        [FromBody] JsonPatchDocument<EventDivisionUpdateDto> patchDocument)
    {
        if (!await eventDivisionRepository.EventDivisionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var eventDivision = await eventDivisionRepository.GetEventDivisionAsync(id);

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!(resourceService.HasPermission(currentUser, UserRole.Administrator) ||
              currentUser.Id == eventDivision.OwnerId))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        var dto = mapper.Map<EventDivisionUpdateDto>(eventDivision);
        patchDocument.ApplyTo(dto, ModelState);

        if (!TryValidateModel(dto))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorDetailed,
                Code = ResponseCodes.InvalidData,
                Errors = ModelErrorTranslator.Translate(ModelState)
            });

        var administratorTasks =
            dto.Administrators.Select(userId => userManager.FindByIdAsync(userId.ToString())).ToList();
        await Task.WhenAll(administratorTasks);

        eventDivision.Title = dto.Title;
        eventDivision.Subtitle = dto.Subtitle;
        eventDivision.Type = dto.Type;
        eventDivision.Status = dto.Status;
        eventDivision.Illustrator = dto.Illustrator;
        eventDivision.Description = dto.Description;
        eventDivision.TagId = dto.TagId;
        eventDivision.MinTeamCount = dto.MinTeamCount;
        eventDivision.MaxTeamCount = dto.MaxTeamCount;
        eventDivision.MinParticipantPerTeamCount = dto.MinParticipantPerTeamCount;
        eventDivision.MaxParticipantPerTeamCount = dto.MaxParticipantPerTeamCount;
        eventDivision.MinSubmissionCount = dto.MinSubmissionCount;
        eventDivision.MaxSubmissionCount = dto.MaxSubmissionCount;
        eventDivision.Accessibility = dto.Accessibility;
        eventDivision.IsHidden = dto.IsHidden;
        eventDivision.IsLocked = dto.IsLocked;
        eventDivision.EventId = dto.EventId;
        eventDivision.OwnerId = dto.OwnerId;
        eventDivision.DateUnveiled = dto.DateUnveiled;
        eventDivision.Administrators = administratorTasks.Where(t => t.Result != null).Select(t => t.Result!).ToList();
        eventDivision.DateUpdated = DateTimeOffset.UtcNow;

        if (!await eventDivisionRepository.UpdateEventDivisionAsync(eventDivision))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return NoContent();
    }

    /// <summary>
    ///     Updates an event division's illustration.
    /// </summary>
    /// <param name="id">EventDivision's ID.</param>
    /// <param name="dto">The new illustration.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified event division is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPatch("{id:guid}/illustration")]
    [Consumes("multipart/form-data")]
    [Produces("event/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> UpdateEventDivisionIllustration([FromRoute] Guid id, [FromForm] FileDto dto)
    {
        if (!await eventDivisionRepository.EventDivisionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var eventDivision = await eventDivisionRepository.GetEventDivisionAsync(id);

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!resourceService.HasPermission(currentUser, UserRole.Administrator))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        if (dto.File != null)
        {
            eventDivision.Illustration =
                (await fileStorageService.UploadImage<EventDivision>(eventDivision.Title, dto.File, (16, 9))).Item1;
            await fileStorageService.SendUserInput(eventDivision.Illustration, "Illustration", Request, currentUser);
            eventDivision.DateUpdated = DateTimeOffset.UtcNow;
        }

        if (!await eventDivisionRepository.UpdateEventDivisionAsync(eventDivision))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });
        return NoContent();
    }

    /// <summary>
    ///     Removes an event division's illustration.
    /// </summary>
    /// <param name="id">An event division's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified event division is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpDelete("{id:guid}/illustration")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> RemoveEventDivisionIllustration([FromRoute] Guid id)
    {
        if (!await eventDivisionRepository.EventDivisionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var eventDivision = await eventDivisionRepository.GetEventDivisionAsync(id);

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!resourceService.HasPermission(currentUser, UserRole.Administrator))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        eventDivision.Illustration = null;
        eventDivision.DateUpdated = DateTimeOffset.UtcNow;

        if (!await eventDivisionRepository.UpdateEventDivisionAsync(eventDivision))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return NoContent();
    }

    /// <summary>
    ///     Removes an event division.
    /// </summary>
    /// <param name="id">An event division's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified event division is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpDelete("{id:guid}")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> RemoveEventDivision([FromRoute] Guid id)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;

        if (!await eventDivisionRepository.EventDivisionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var eventDivision = await eventDivisionRepository.GetEventDivisionAsync(id);

        if (!(resourceService.HasPermission(currentUser, UserRole.Administrator) ||
              currentUser.Id == eventDivision.OwnerId))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (!await eventDivisionRepository.RemoveEventDivisionAsync(id))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return NoContent();
    }

    /// <summary>
    ///     Retrieves likes from a specific event division.
    /// </summary>
    /// <param name="id">An event division's ID.</param>
    /// <returns>An array of likes.</returns>
    /// <response code="200">Returns an array of likes.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified event division is not found.</response>
    [HttpGet("{id:guid}/likes")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<LikeDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetEventDivisionLikes([FromRoute] Guid id, [FromQuery] ArrayRequestDto dto)
    {
        dto.PerPage = dto.PerPage > 0 && dto.PerPage < dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? dataSettings.Value.PaginationPerPage : dataSettings.Value.PaginationMaxPerPage;
        dto.Page = dto.Page > 1 ? dto.Page : 1;
        var position = dto.PerPage * (dto.Page - 1);
        if (!await eventDivisionRepository.EventDivisionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var likes = await likeRepository.GetLikesAsync(dto.Order, dto.Desc, position, dto.PerPage,
            e => e.ResourceId == id);
        var list = mapper.Map<List<LikeDto>>(likes);
        var total = await likeRepository.CountLikesAsync(e => e.ResourceId == id);

        return Ok(new ResponseDto<IEnumerable<LikeDto>>
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
    ///     Likes a specific event division.
    /// </summary>
    /// <param name="id">An event division's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="201">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="404">When the specified event division is not found.</response>
    [HttpPost("{id:guid}/likes")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(void), StatusCodes.Status201Created, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> CreateLike([FromRoute] Guid id)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!await eventDivisionRepository.EventDivisionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var eventDivision = await eventDivisionRepository.GetEventDivisionAsync(id);
        if (await resourceService.IsBlacklisted(eventDivision.OwnerId, currentUser.Id))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.Blacklisted
            });
        if (!await likeService.CreateLikeAsync(eventDivision, currentUser.Id))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.AlreadyDone
            });

        return StatusCode(StatusCodes.Status201Created);
    }

    /// <summary>
    ///     Removes the like from a specific event division.
    /// </summary>
    /// <param name="id">An event division's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="404">When the specified event division is not found.</response>
    [HttpDelete("{id:guid}/likes")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> RemoveLike([FromRoute] Guid id)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!await eventDivisionRepository.EventDivisionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var eventDivision = await eventDivisionRepository.GetEventDivisionAsync(id);
        if (!await likeService.RemoveLikeAsync(eventDivision, currentUser.Id))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.AlreadyDone
            });

        return NoContent();
    }

    /// <summary>
    ///     Retrieves comments from a specific event division.
    /// </summary>
    /// <param name="id">An event division's ID.</param>
    /// <returns>An array of comments.</returns>
    /// <response code="200">Returns an array of comments.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified event division is not found.</response>
    [HttpGet("{id:guid}/comments")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<CommentDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetEventDivisionComments([FromRoute] Guid id, [FromQuery] ArrayRequestDto dto)
    {
        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        dto.PerPage = dto.PerPage > 0 && dto.PerPage < dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? dataSettings.Value.PaginationPerPage : dataSettings.Value.PaginationMaxPerPage;
        dto.Page = dto.Page > 1 ? dto.Page : 1;
        var position = dto.PerPage * (dto.Page - 1);
        if (!await eventDivisionRepository.EventDivisionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var comments = await commentRepository.GetCommentsAsync(dto.Order, dto.Desc, position, dto.PerPage,
            e => e.ResourceId == id, currentUser?.Id);
        var total = await commentRepository.CountCommentsAsync(e => e.ResourceId == id);
        var list = comments.Select(dtoMapper.MapComment<CommentDto>).ToList();

        return Ok(new ResponseDto<IEnumerable<CommentDto>>
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
    ///     Comments on a specific event division.
    /// </summary>
    /// <param name="id">An event division's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="201">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified event division is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPost("{id:guid}/comments")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(void), StatusCodes.Status201Created, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> CreateComment([FromRoute] Guid id, [FromBody] CommentCreationDto dto)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!resourceService.HasPermission(currentUser, UserRole.Member))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        if (!await eventDivisionRepository.EventDivisionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var eventDivision = await eventDivisionRepository.GetEventDivisionAsync(id);
        if (await resourceService.IsBlacklisted(eventDivision.OwnerId, currentUser.Id))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.Blacklisted
            });

        var result = await resourceService.ParseUserContent(dto.Content);
        var comment = new Comment
        {
            ResourceId = eventDivision.Id,
            Content = result.Item1,
            Language = dto.Language,
            OwnerId = currentUser.Id,
            DateCreated = DateTimeOffset.UtcNow
        };
        if (!await commentRepository.CreateCommentAsync(comment))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        await notificationService.NotifyComment(comment, eventDivision, eventDivision.GetDisplay(), dto.Content);
        await notificationService.NotifyMentions(result.Item2, currentUser,
            resourceService.GetRichText<Comment>(comment.Id.ToString(), dto.Content));

        return StatusCode(StatusCodes.Status201Created);
    }
}