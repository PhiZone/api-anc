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
using PhiZoneApi.Services;
using PhiZoneApi.Utils;

// ReSharper disable RouteTemplates.ActionRoutePrefixCanBeExtractedToControllerRoute

namespace PhiZoneApi.Controllers;

[Route("events/tasks")]
[ApiVersion("2.0")]
[ApiController]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
    Policy = "AllowAnonymous")]
public class EventTaskController(
    IEventTaskRepository eventTaskRepository,
    IEventDivisionRepository eventDivisionRepository,
    IOptions<DataSettings> dataSettings,
    IDtoMapper dtoMapper,
    IFilterService filterService,
    UserManager<User> userManager,
    EventTaskScheduler eventTaskScheduler,
    IMapper mapper,
    IResourceService resourceService,
    IMeilisearchService meilisearchService) : Controller
{
    /// <summary>
    ///     Retrieves event tasks.
    /// </summary>
    /// <returns>An array of event tasks.</returns>
    /// <response code="200">Returns an array of event tasks.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<EventTaskDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetEventTasks([FromQuery] ArrayRequestDto dto,
        [FromQuery] EventTaskFilterDto? filterDto = null)
    {
        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        dto.PerPage = dto.PerPage > 0 && dto.PerPage < dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? dataSettings.Value.PaginationPerPage : dataSettings.Value.PaginationMaxPerPage;
        dto.Page = dto.Page > 1 ? dto.Page : 1;
        var position = dto.PerPage * (dto.Page - 1);
        var predicateExpr = await filterService.Parse(filterDto, dto.Predicate, currentUser,
            e => e.Division.DateUnveiled <= DateTimeOffset.UtcNow || (currentUser != null &&
                                                             (resourceService.HasPermission(currentUser,
                                                                  UserRole.Administrator) ||
                                                              e.Division.Administrators.Contains(currentUser))));
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
            eventTasks = await eventTaskRepository.GetEventTasksAsync(dto.Order, dto.Desc, position,
                dto.PerPage, predicateExpr);
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
    ///     Retrieves a specific event task.
    /// </summary>
    /// <param name="id">An event task's ID.</param>
    /// <returns>An event task.</returns>
    /// <response code="200">Returns an event task.</response>
    /// <response code="304">
    ///     When the resource has not been updated since last retrieval. Requires <c>If-None-Match</c>.
    /// </response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified event task is not found.</response>
    [HttpGet("{id:guid}")]
    [ServiceFilter(typeof(ETagFilter))]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<EventTaskDto>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status304NotModified, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetEventTask([FromRoute] Guid id)
    {
        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        if (!await eventTaskRepository.EventTaskExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var eventTask = await eventTaskRepository.GetEventTaskAsync(id);
        if ((currentUser == null || !eventTask.Division.Administrators.Contains(currentUser) ||
             !resourceService.HasPermission(currentUser, UserRole.Administrator)) &&
            eventTask.Division.DateUnveiled <= DateTimeOffset.UtcNow)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var dto = mapper.Map<EventTaskDto>(eventTask);

        return Ok(new ResponseDto<EventTaskDto>
        {
            Status = ResponseStatus.Ok, Code = ResponseCodes.Ok, Data = dto
        });
    }

    /// <summary>
    ///     Retrieves tasks of a specific event task.
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
        if (!await eventTaskRepository.EventTaskExistsAsync(id))
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
    ///     Creates a new event task.
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
    public async Task<IActionResult> CreateEventTask([FromBody] EventTaskRequestDto dto)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;

        if (!await eventDivisionRepository.EventDivisionExistsAsync(dto.DivisionId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentNotFound
            });
        
        var eventDivision = await eventDivisionRepository.GetEventDivisionAsync(dto.DivisionId);
        if (!(resourceService.HasPermission(currentUser, UserRole.Administrator) ||
              eventDivision.Administrators.Contains(currentUser)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        var eventTask = new EventTask
        {
            Name = dto.Name,
            Type = dto.Type,
            Code = dto.Code,
            IsHidden = dto.IsHidden,
            Description = dto.Description,
            DivisionId = dto.DivisionId,
            DateExecuted = dto.DateExecuted,
            DateCreated = DateTimeOffset.UtcNow,
            DateUpdated = DateTimeOffset.UtcNow
        };
        
        if (!await eventTaskRepository.CreateEventTaskAsync(eventTask))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        if (eventTask.Type == TaskType.Scheduled)
        {
            eventTaskScheduler.Schedule(eventTask);
        }

        return StatusCode(StatusCodes.Status201Created);
    }

    /// <summary>
    ///     Updates an event task.
    /// </summary>
    /// <param name="id">An event task's ID.</param>
    /// <param name="patchDocument">A JSON Patch Document.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified event task is not found.</response>
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
    public async Task<IActionResult> UpdateEventTask([FromRoute] Guid id,
        [FromBody] JsonPatchDocument<EventTaskRequestDto> patchDocument)
    {
        if (!await eventTaskRepository.EventTaskExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var eventTask = await eventTaskRepository.GetEventTaskAsync(id);
        var eventDivision = await eventDivisionRepository.GetEventDivisionAsync(eventTask.DivisionId);

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!(resourceService.HasPermission(currentUser, UserRole.Administrator) ||
              eventDivision.Administrators.Contains(currentUser)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        var dto = mapper.Map<EventTaskRequestDto>(eventTask);
        patchDocument.ApplyTo(dto, ModelState);

        if (!TryValidateModel(dto))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorDetailed,
                Code = ResponseCodes.InvalidData,
                Errors = ModelErrorTranslator.Translate(ModelState)
            });

        if (!await eventDivisionRepository.EventDivisionExistsAsync(dto.DivisionId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentNotFound
            });

        eventTask.Name = dto.Name;
        eventTask.Type = dto.Type;
        eventTask.Code = dto.Code;
        eventTask.IsHidden = dto.IsHidden;
        eventTask.Description = dto.Description;
        eventTask.DivisionId = dto.DivisionId;
        eventTask.DateExecuted = dto.DateExecuted;
        eventTask.DateUpdated = DateTimeOffset.UtcNow;

        if (!await eventTaskRepository.UpdateEventTaskAsync(eventTask))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        if (eventTask.Type == TaskType.Scheduled)
        {
            eventTaskScheduler.Schedule(eventTask, true);
        }

        return NoContent();
    }

    /// <summary>
    ///     Removes an event task.
    /// </summary>
    /// <param name="id">An event task's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified event task is not found.</response>
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
    public async Task<IActionResult> RemoveEventTask([FromRoute] Guid id)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;

        if (!await eventTaskRepository.EventTaskExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var eventTask = await eventTaskRepository.GetEventTaskAsync(id);
        var eventDivision = await eventDivisionRepository.GetEventDivisionAsync(eventTask.DivisionId);

        if (!(resourceService.HasPermission(currentUser, UserRole.Administrator) ||
              eventDivision.Administrators.Contains(currentUser)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (!await eventTaskRepository.RemoveEventTaskAsync(id))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return NoContent();
    }
}