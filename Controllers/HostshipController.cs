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

[Route("events/hostships")]
[ApiVersion("2.0")]
[ApiController]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
    Policy = "AllowAnonymous")]
public class HostshipController(
    IHostshipRepository hostshipRepository,
    IEventRepository eventRepository,
    IOptions<DataSettings> dataSettings,
    IFilterService filterService,
    UserManager<User> userManager,
    IMapper mapper,
    IResourceService resourceService) : Controller
{
    /// <summary>
    ///     Retrieves hostships.
    /// </summary>
    /// <returns>An array of hostships.</returns>
    /// <response code="200">Returns an array of hostships.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<HostshipDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetHostships([FromQuery] ArrayRequestDto dto,
        [FromQuery] HostshipFilterDto? filterDto = null)
    {
        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        var permission = HP.Gen(HP.Retrieve, HP.Hostship);
        dto.PerPage = dto.PerPage > 0 && dto.PerPage < dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? dataSettings.Value.PaginationPerPage : dataSettings.Value.PaginationMaxPerPage;
        dto.Page = dto.Page > 1 ? dto.Page : 1;
        var position = dto.PerPage * (dto.Page - 1);
        var predicateExpr = await filterService.Parse(filterDto, dto.Predicate, currentUser,
            e => e.IsUnveiled || (currentUser != null &&
                                  (resourceService.HasPermission(currentUser, UserRole.Administrator) ||
                                   e.Event.Hostships.Any(f =>
                                       f.UserId == currentUser.Id &&
                                       (f.IsAdmin || f.Permissions.Contains(permission))))));
        var hostships = await hostshipRepository.GetHostshipsAsync(dto.Order, dto.Desc, position,
            dto.PerPage, predicateExpr);
        var total = await hostshipRepository.CountHostshipsAsync(predicateExpr);

        var list = hostships.Select(mapper.Map<HostshipDto>).ToList();

        return Ok(new ResponseDto<IEnumerable<HostshipDto>>
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
    ///     Retrieves a specific hostship.
    /// </summary>
    /// <param name="id">A hostship's ID.</param>
    /// <returns>A hostship.</returns>
    /// <response code="200">Returns a hostship.</response>
    /// <response code="304">
    ///     When the resource has not been updated since last retrieval. Requires <c>If-None-Match</c>.
    /// </response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified hostship is not found.</response>
    [HttpGet("{eventId:guid}/{userId:int}")]
    [ServiceFilter(typeof(ETagFilter))]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<HostshipDto>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status304NotModified, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetHostship([FromRoute] Guid eventId, [FromRoute] int userId)
    {
        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        if (!await hostshipRepository.HostshipExistsAsync(eventId, userId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var hostship = await hostshipRepository.GetHostshipAsync(eventId, userId);
        var eventEntity = await eventRepository.GetEventAsync(eventId);
        var permission = HP.Gen(HP.Retrieve, HP.Hostship);
        if ((currentUser == null || !(resourceService.HasPermission(currentUser, UserRole.Administrator) ||
                                      eventEntity.Hostships.Any(f =>
                                          f.UserId == currentUser.Id &&
                                          (f.IsAdmin || f.Permissions.Contains(permission))))) && !hostship.IsUnveiled)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var dto = mapper.Map<HostshipDto>(hostship);

        return Ok(new ResponseDto<HostshipDto> { Status = ResponseStatus.Ok, Code = ResponseCodes.Ok, Data = dto });
    }

    /// <summary>
    ///     Creates a new hostship.
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
    public async Task<IActionResult> CreateHostship([FromBody] HostshipRequestDto dto)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;

        if (!await eventRepository.EventExistsAsync(dto.EventId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentNotFound
            });

        var eventEntity = await eventRepository.GetEventAsync(dto.EventId);
        var permission = HP.Gen(HP.Create, HP.Hostship);

        if (!(resourceService.HasPermission(currentUser, UserRole.Administrator) || eventEntity.Hostships.Any(f =>
                f.UserId == currentUser.Id && (f.IsAdmin || f.Permissions.Contains(permission)))))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        var isAdmin = resourceService.HasPermission(currentUser, UserRole.Administrator) ||
                      eventEntity.Hostships.Any(f => f.UserId == currentUser.Id && f.IsAdmin);

        var hostship = new Hostship
        {
            EventId = dto.EventId,
            UserId = dto.UserId,
            Position = dto.Position,
            IsUnveiled = dto.IsUnveiled,
            IsAdmin = eventEntity.OwnerId == currentUser.Id && dto.IsAdmin,
            Permissions = isAdmin ? dto.Permissions : []
        };

        if (!await hostshipRepository.CreateHostshipAsync(hostship))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return StatusCode(StatusCodes.Status201Created);
    }

    /// <summary>
    ///     Updates a hostship.
    /// </summary>
    /// <param name="id">A hostship's ID.</param>
    /// <param name="patchDocument">A JSON Patch Document.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified hostship is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPatch("{eventId:guid}/{userId:int}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> UpdateHostship([FromRoute] Guid eventId, [FromRoute] int userId,
        [FromBody] JsonPatchDocument<HostshipRequestDto> patchDocument)
    {
        if (!await hostshipRepository.HostshipExistsAsync(eventId, userId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var hostship = await hostshipRepository.GetHostshipAsync(eventId, userId);
        var eventEntity = await eventRepository.GetEventAsync(eventId);

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        var permission = HP.Gen(HP.Update, HP.Hostship);
        if (!(resourceService.HasPermission(currentUser, UserRole.Administrator) || eventEntity.Hostships.Any(f =>
                f.UserId == currentUser.Id && (f.IsAdmin || f.Permissions.Contains(permission)))))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        var dto = mapper.Map<HostshipRequestDto>(hostship);
        patchDocument.ApplyTo(dto, ModelState);

        if (!TryValidateModel(dto))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorDetailed,
                Code = ResponseCodes.InvalidData,
                Errors = ModelErrorTranslator.Translate(ModelState)
            });

        var isAdmin = resourceService.HasPermission(currentUser, UserRole.Administrator) ||
                      eventEntity.Hostships.Any(f => f.UserId == currentUser.Id && f.IsAdmin);

        hostship.EventId = dto.EventId;
        hostship.UserId = dto.UserId;
        hostship.Position = dto.Position;
        hostship.IsUnveiled = dto.IsUnveiled;
        hostship.IsAdmin = eventEntity.OwnerId == currentUser.Id && dto.IsAdmin;
        hostship.Permissions = isAdmin ? dto.Permissions : [];

        if (!await hostshipRepository.UpdateHostshipAsync(hostship))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return NoContent();
    }

    /// <summary>
    ///     Removes a hostship.
    /// </summary>
    /// <param name="id">A hostship's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified hostship is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpDelete("{eventId:guid}/{userId:int}")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> RemoveHostship([FromRoute] Guid eventId, [FromRoute] int userId)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;

        if (!await hostshipRepository.HostshipExistsAsync(eventId, userId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var hostship = await hostshipRepository.GetHostshipAsync(eventId, userId);
        var eventEntity = await eventRepository.GetEventAsync(eventId);

        var permission = HP.Gen(HP.Remove, HP.Hostship);
        if (!(resourceService.HasPermission(currentUser, UserRole.Administrator) || eventEntity.Hostships.Any(f =>
                f.UserId == currentUser.Id && (f.IsAdmin || f.Permissions.Contains(permission)))) ||
            (hostship.IsAdmin && eventEntity.OwnerId != currentUser.Id &&
             !resourceService.HasPermission(currentUser, UserRole.Administrator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (!await hostshipRepository.RemoveHostshipAsync(eventId, userId))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return NoContent();
    }
}