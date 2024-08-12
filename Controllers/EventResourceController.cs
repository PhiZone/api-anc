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
using HP = PhiZoneApi.Constants.HostshipPermissions;

// ReSharper disable RouteTemplates.ActionRoutePrefixCanBeExtractedToControllerRoute

namespace PhiZoneApi.Controllers;

[Route("events/resources")]
[ApiVersion("2.0")]
[ApiController]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
    Policy = "AllowAnonymous")]
public class EventResourceController(
    IEventResourceRepository eventResourceRepository,
    IEventDivisionRepository eventDivisionRepository,
    IEventTeamRepository eventTeamRepository,
    IEventRepository eventRepository,
    IOptions<DataSettings> dataSettings,
    IScriptService scriptService,
    IFilterService filterService,
    UserManager<User> userManager,
    IMapper mapper,
    IResourceService resourceService) : Controller
{
    /// <summary>
    ///     Retrieves event resources.
    /// </summary>
    /// <returns>An array of event resources.</returns>
    /// <response code="200">Returns an array of event resources.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<EventResourceDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetEventResources([FromQuery] ArrayRequestDto dto,
        [FromQuery] EventResourceFilterDto? filterDto = null)
    {
        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        var permission = HP.Gen(HP.Retrieve, HP.Resource);
        dto.PerPage = dto.PerPage > 0 && dto.PerPage < dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? dataSettings.Value.PaginationPerPage : dataSettings.Value.PaginationMaxPerPage;
        dto.Page = dto.Page > 1 ? dto.Page : 1;
        var position = dto.PerPage * (dto.Page - 1);
        var predicateExpr = await filterService.Parse(filterDto, dto.Predicate, currentUser,
            e => currentUser != null && (resourceService.HasPermission(currentUser, UserRole.Administrator) ||
                                         e.Division.Event.Hostships.Any(f =>
                                             f.UserId == currentUser.Id &&
                                             (f.IsAdmin || f.Permissions.Contains(permission))) ||
                                         (e.Type == EventResourceType.Entry &&
                                          ((e.IsAnonymous != null && !e.IsAnonymous.Value) ||
                                           e.Team!.Participants.Any(f => f.Id == currentUser.Id)))));
        var eventResources = await eventResourceRepository.GetEventResourcesAsync(dto.Order, dto.Desc, position,
            dto.PerPage, predicateExpr);
        var total = await eventResourceRepository.CountEventResourcesAsync(predicateExpr);

        var list = eventResources.Select(mapper.Map<EventResourceDto>).ToList();

        return Ok(new ResponseDto<IEnumerable<EventResourceDto>>
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
    ///     Retrieves a specific event resource.
    /// </summary>
    /// <param name="divisionId">An event division's ID.</param>
    /// <param name="resourceId">A resource's ID.</param>
    /// <returns>An event resource.</returns>
    /// <response code="200">Returns an event resource.</response>
    /// <response code="304">
    ///     When the resource has not been updated since last retrieval. Requires <c>If-None-Match</c>.
    /// </response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified event resource is not found.</response>
    [HttpGet("{divisionId:guid}/{resourceId:guid}")]
    [ServiceFilter(typeof(ETagFilter))]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<EventResourceDto>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status304NotModified, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetEventResource([FromRoute] Guid divisionId, [FromRoute] Guid resourceId)
    {
        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        if (!await eventResourceRepository.EventResourceExistsAsync(divisionId, resourceId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var eventResource = await eventResourceRepository.GetEventResourceAsync(divisionId, resourceId);
        var eventDivision = await eventDivisionRepository.GetEventDivisionAsync(eventResource.DivisionId);
        var eventEntity = await eventRepository.GetEventAsync(eventDivision.EventId);
        EventTeam? eventTeam = null;
        if (eventResource.TeamId != null)
            eventTeam = await eventTeamRepository.GetEventTeamAsync(eventResource.TeamId.Value);

        var permission = HP.Gen(HP.Retrieve, HP.Resource);
        if (currentUser == null || !(resourceService.HasPermission(currentUser, UserRole.Administrator) ||
                                     eventEntity.Hostships.Any(f =>
                                         f.UserId == currentUser.Id &&
                                         (f.IsAdmin || f.Permissions.Contains(permission))) ||
                                     (eventResource.Type == EventResourceType.Entry &&
                                      ((eventResource.IsAnonymous != null && !eventResource.IsAnonymous.Value) ||
                                       (eventTeam != null &&
                                        eventTeam.Participants.Any(f => f.Id == currentUser.Id))))))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var dto = mapper.Map<EventResourceDto>(eventResource);

        return Ok(new ResponseDto<EventResourceDto>
        {
            Status = ResponseStatus.Ok, Code = ResponseCodes.Ok, Data = dto
        });
    }

    /// <summary>
    ///     Retrieves reserved fields of event resources.
    /// </summary>
    /// <returns>A matrix of reserved fields.</returns>
    /// <response code="200">Returns an array of reserved fields.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    [HttpGet("reservedFields")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK,
        Type = typeof(ResponseDto<IEnumerable<IEnumerable<ReservedFieldDto?>>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetReservedFields([FromQuery] ArrayRequestDto dto,
        [FromQuery] EventResourceFilterDto? filterDto = null)
    {
        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        var permission = HP.Gen(HP.Retrieve, HP.Resource);
        dto.PerPage = dto.PerPage > 0 && dto.PerPage < dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? dataSettings.Value.PaginationPerPage : dataSettings.Value.PaginationMaxPerPage;
        dto.Page = dto.Page > 1 ? dto.Page : 1;
        var position = dto.PerPage * (dto.Page - 1);
        var predicateExpr = await filterService.Parse(filterDto, dto.Predicate, currentUser,
            e => currentUser != null && (resourceService.HasPermission(currentUser, UserRole.Administrator) ||
                                         e.Division.Event.Hostships.Any(f =>
                                             f.UserId == currentUser.Id &&
                                             (f.IsAdmin || f.Permissions.Contains(permission)))));
        var eventResources = await eventResourceRepository.GetEventResourcesAsync(dto.Order, dto.Desc, position,
            dto.PerPage, predicateExpr);
        var total = await eventResourceRepository.CountEventResourcesAsync(predicateExpr);

        List<IEnumerable<ReservedFieldDto?>> matrix = [];
        Dictionary<Guid, Hostship?> cache = [];
        permission = HP.Gen(HP.Retrieve, HP.ReservedField);

        // ReSharper disable once InvertIf
        if (currentUser == null || !resourceService.HasPermission(currentUser, UserRole.Administrator))
            foreach (var eventResource in eventResources)
            {
                Hostship? hostship;
                if (cache.TryGetValue(eventResource.DivisionId, out var value))
                {
                    hostship = value;
                }
                else
                {
                    var eventDivision = await eventDivisionRepository.GetEventDivisionAsync(eventResource.DivisionId);
                    var eventEntity = await eventRepository.GetEventAsync(eventDivision.EventId);
                    hostship = eventEntity.Hostships.FirstOrDefault(f =>
                        currentUser != null && f.UserId == currentUser.Id &&
                        (f.IsAdmin || f.Permissions.Any(e => e.SameAs(permission))));
                    cache.Add(eventResource.DivisionId, hostship);
                }

                if (hostship == null)
                {
                    matrix.Add([]);
                }
                else
                {
                    IEnumerable<ReservedFieldDto?> list =
                        eventResource.Reserved.Select((e, i) => new ReservedFieldDto { Index = i + 1, Content = e });
                    if (hostship.Permissions.All(e => e != permission))
                        matrix.Add(hostship.Permissions.Where(e => e.SameAs(permission))
                            .Select(HP.GetIndex)
                            .Select(index => list.ElementAtOrDefault(index - 1))
                            .ToList());
                    else
                        matrix.Add(list);
                }
            }
        else
            matrix = eventResources.Select(e =>
                    e.Reserved.Select((f, i) => new ReservedFieldDto { Index = i + 1, Content = f }))
                .ToList()!;

        return Ok(new ResponseDto<IEnumerable<IEnumerable<ReservedFieldDto?>>>
        {
            Status = ResponseStatus.Ok,
            Code = ResponseCodes.Ok,
            Total = total,
            PerPage = dto.PerPage,
            HasPrevious = position > 0,
            HasNext = dto.PerPage > 0 && dto.PerPage * dto.Page < total,
            Data = matrix
        });
    }

    /// <summary>
    ///     Retrieves reserved fields of a specified event resource.
    /// </summary>
    /// <param name="divisionId">An event division's ID.</param>
    /// <param name="resourceId">A resource's ID.</param>
    /// <returns>An array of reserved fields.</returns>
    /// <response code="200">Returns an array of reserved fields.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    [HttpGet("{divisionId:guid}/{resourceId:guid}/reservedFields")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<ReservedFieldDto?>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetReservedFields([FromRoute] Guid divisionId, [FromRoute] Guid resourceId)
    {
        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        if (!await eventResourceRepository.EventResourceExistsAsync(divisionId, resourceId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var eventResource = await eventResourceRepository.GetEventResourceAsync(divisionId, resourceId);
        var eventDivision = await eventDivisionRepository.GetEventDivisionAsync(eventResource.DivisionId);
        var eventEntity = await eventRepository.GetEventAsync(eventDivision.EventId);
        var permission = HP.Gen(HP.Retrieve, HP.Resource);
        if (currentUser == null ||
            (!(eventEntity.Hostships.Any(f =>
                   f.UserId == currentUser.Id && (f.IsAdmin || f.Permissions.Contains(permission))) ||
               resourceService.HasPermission(currentUser, UserRole.Administrator)) &&
             eventDivision.DateUnveiled >= DateTimeOffset.UtcNow))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        IEnumerable<ReservedFieldDto?> list =
            eventResource.Reserved.Select((e, i) => new ReservedFieldDto { Index = i + 1, Content = e });

        // ReSharper disable once InvertIf
        if (!(eventEntity.Hostships.Any(f => f.UserId == currentUser.Id && f.IsAdmin) ||
              resourceService.HasPermission(currentUser, UserRole.Administrator)))
        {
            permission = HP.Gen(HP.Retrieve, HP.ReservedField);
            var hostship = eventEntity.Hostships.FirstOrDefault(f =>
                f.UserId == currentUser.Id && (f.IsAdmin || f.Permissions.Any(e => e.SameAs(permission))));
            if (hostship == null)
                list = [];
            else if (hostship.Permissions.All(e => e != permission))
                list = hostship.Permissions.Where(e => e.SameAs(permission))
                    .Select(HP.GetIndex)
                    .Select(index => list.ElementAtOrDefault(index - 1))
                    .ToList();
        }

        return Ok(new ResponseDto<IEnumerable<ReservedFieldDto?>>
        {
            Status = ResponseStatus.Ok, Code = ResponseCodes.Ok, Data = list
        });
    }

    /// <summary>
    ///     Updates a specific reserved field of a specified event resource.
    /// </summary>
    /// <param name="divisionId">An event division's ID.</param>
    /// <param name="resourceId">A resource's ID.</param>
    /// <param name="index">A 1-based index.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified event resource is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPost("{divisionId:guid}/{resourceId:guid}/reservedFields/{index:int}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> UpdateReservedField([FromRoute] Guid divisionId, [FromRoute] Guid resourceId,
        [FromRoute] int index, [FromBody] StringDto dto)
    {
        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        if (!await eventResourceRepository.EventResourceExistsAsync(divisionId, resourceId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var eventResource = await eventResourceRepository.GetEventResourceAsync(divisionId, resourceId);
        if (eventResource.TeamId == null)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidOperation
            });
        var eventDivision = await eventDivisionRepository.GetEventDivisionAsync(divisionId);
        var eventEntity = await eventRepository.GetEventAsync(eventDivision.EventId);
        var permission = HP.Gen(HP.Update, HP.Resource);
        if (currentUser == null ||
            (!(eventEntity.Hostships.Any(f =>
                   f.UserId == currentUser.Id && (f.IsAdmin || f.Permissions.Contains(permission))) ||
               resourceService.HasPermission(currentUser, UserRole.Administrator)) &&
             eventDivision.DateUnveiled >= DateTimeOffset.UtcNow))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        // ReSharper disable once InvertIf
        if (!(eventEntity.Hostships.Any(f => f.UserId == currentUser.Id && f.IsAdmin) ||
              resourceService.HasPermission(currentUser, UserRole.Administrator)))
        {
            var hostship = eventEntity.Hostships.FirstOrDefault(f =>
                f.UserId == currentUser.Id && (f.IsAdmin || f.Permissions.Contains(permission)));
            permission = HP.Gen(HP.Update, HP.ReservedField);
            var permissionWithIndex = HP.Gen(HP.Update, HP.ReservedField, index);
            if (hostship == null || (hostship.Permissions.All(e => e != permission) &&
                                     !hostship.HasPermission(permissionWithIndex)))
                return StatusCode(StatusCodes.Status403Forbidden,
                    new ResponseDto<object>
                    {
                        Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                    });
        }

        while (index > eventResource.Reserved.Count) eventResource.Reserved.Add(null);

        eventResource.Reserved[index - 1] = dto.Content;

        if (!await eventResourceRepository.UpdateEventResourceAsync(eventResource))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        await scriptService.RunEventTaskAsync(eventResource.DivisionId, (eventResource, index),
            eventResource.TeamId.Value, currentUser, [EventTaskType.OnEntryEvaluation]);

        return NoContent();
    }

    /// <summary>
    ///     Updates an event resource.
    /// </summary>
    /// <param name="id">An event resource's ID.</param>
    /// <param name="patchDocument">A JSON Patch Document.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified event resource is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPatch("{divisionId:guid}/{resourceId:guid}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> UpdateEventResource([FromRoute] Guid divisionId, [FromRoute] Guid resourceId,
        [FromBody] JsonPatchDocument<EventResourceRequestDto> patchDocument)
    {
        if (!await eventResourceRepository.EventResourceExistsAsync(divisionId, resourceId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var eventResource = await eventResourceRepository.GetEventResourceAsync(divisionId, resourceId);
        var eventDivision = await eventDivisionRepository.GetEventDivisionAsync(eventResource.DivisionId);
        var eventEntity = await eventRepository.GetEventAsync(eventDivision.EventId);

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        var permission = HP.Gen(HP.Update, HP.Resource);
        if (!(resourceService.HasPermission(currentUser, UserRole.Administrator) || eventEntity.Hostships.Any(f =>
                f.UserId == currentUser.Id && (f.IsAdmin || f.Permissions.Contains(permission)))))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        var dto = mapper.Map<EventResourceRequestDto>(eventResource);
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

        eventResource.DivisionId = dto.DivisionId;
        eventResource.Type = dto.Type;
        eventResource.Label = dto.Label;
        eventResource.Description = dto.Description;
        eventResource.IsAnonymous = dto.IsAnonymous;
        eventResource.TeamId = dto.TeamId;

        if (!await eventResourceRepository.UpdateEventResourceAsync(eventResource))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return NoContent();
    }

    /// <summary>
    ///     Removes an event resource.
    /// </summary>
    /// <param name="id">An event resource's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified event resource is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpDelete("{divisionId:guid}/{resourceId:guid}")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> RemoveEventResource([FromRoute] Guid divisionId, [FromRoute] Guid resourceId)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;

        if (!await eventResourceRepository.EventResourceExistsAsync(divisionId, resourceId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var eventResource = await eventResourceRepository.GetEventResourceAsync(divisionId, resourceId);
        var eventDivision = await eventDivisionRepository.GetEventDivisionAsync(eventResource.DivisionId);
        var eventEntity = await eventRepository.GetEventAsync(eventDivision.EventId);

        var permission = HP.Gen(HP.Remove, HP.Resource);
        if (!(resourceService.HasPermission(currentUser, UserRole.Administrator) || eventEntity.Hostships.Any(f =>
                f.UserId == currentUser.Id && (f.IsAdmin || f.Permissions.Contains(permission)))))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (!await eventResourceRepository.RemoveEventResourceAsync(divisionId, resourceId))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return NoContent();
    }
}