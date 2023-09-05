using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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

// ReSharper disable InvertIf

namespace PhiZoneApi.Controllers;

[Route("notifications")]
[ApiVersion("2.0")]
[ApiController]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class NotificationController : Controller
{
    private readonly IOptions<DataSettings> _dataSettings;
    private readonly IDtoMapper _dtoMapper;
    private readonly IFilterService _filterService;
    private readonly INotificationRepository _notificationRepository;
    private readonly IResourceService _resourceService;
    private readonly UserManager<User> _userManager;

    public NotificationController(IOptions<DataSettings> dataSettings, INotificationRepository notificationRepository,
        UserManager<User> userManager, IResourceService resourceService, IFilterService filterService,
        IDtoMapper dtoMapper)
    {
        _dataSettings = dataSettings;
        _notificationRepository = notificationRepository;
        _userManager = userManager;
        _resourceService = resourceService;
        _filterService = filterService;
        _dtoMapper = dtoMapper;
    }

    /// <summary>
    ///     Retrieves notifications.
    /// </summary>
    /// <returns>An array of notifications.</returns>
    /// <response code="200">Returns an array of notifications.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<NotificationDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    public async Task<IActionResult> GetNotifications([FromQuery] ArrayWithTimeRequestDto dto,
        [FromQuery] NotificationRequestDto notificationDto, [FromQuery] NotificationFilterDto? filterDto = null)
    {
        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        dto.PerPage = dto.PerPage > 0 && dto.PerPage < _dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? _dataSettings.Value.PaginationPerPage : _dataSettings.Value.PaginationMaxPerPage;
        var position = dto.PerPage * (dto.Page - 1);
        var predicateExpr = await _filterService.Parse(filterDto, dto.Predicate, currentUser,
            e => e.OwnerId == currentUser.Id && (notificationDto.GetRead ? e.DateRead != null : e.DateRead == null));
        var notifications = await _notificationRepository.GetNotificationsAsync(dto.Order, dto.Desc, position,
            dto.PerPage, dto.Search, predicateExpr);
        var total = await _notificationRepository.CountNotificationsAsync(dto.Search, predicateExpr);
        var list = new List<NotificationDto>();
        foreach (var notification in notifications)
            list.Add(await _dtoMapper.MapNotificationAsync<NotificationDto>(notification, currentUser));

        if (notificationDto is { MarkAsRead: true, GetRead: false })
        {
            var now = DateTimeOffset.UtcNow;
            foreach (var notification in notifications.Where(e => e.DateRead == null)) notification.DateRead = now;

#pragma warning disable CS4014
            _notificationRepository.UpdateNotificationsAsync(notifications);
#pragma warning restore CS4014
        }

        return Ok(new ResponseDto<IEnumerable<NotificationDto>>
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
    ///     Retrieves a specific notification.
    /// </summary>
    /// <param name="id">A notification's ID.</param>
    /// <returns>A notification.</returns>
    /// <response code="200">Returns a notification.</response>
    /// <response code="304">
    ///     When the resource has not been updated since last retrieval. Requires <c>If-None-Match</c>.
    /// </response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified notification is not found.</response>
    [HttpGet("{id:guid}")]
    [ServiceFilter(typeof(ETagFilter))]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<NotificationDto>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status304NotModified, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetNotification([FromRoute] Guid id,
        [FromQuery] NotificationRequestDto notificationDto)
    {
        if (!await _notificationRepository.NotificationExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var notification = await _notificationRepository.GetNotificationAsync(id);
        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (notification.OwnerId != currentUser.Id &&
            !await _resourceService.HasPermission(currentUser, Roles.Administrator))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        var dto = await _dtoMapper.MapNotificationAsync<NotificationDto>(notification);

        if (notificationDto.MarkAsRead)
        {
            notification.DateRead = DateTimeOffset.UtcNow;
            await _notificationRepository.UpdateNotificationAsync(notification);
        }

        return Ok(new ResponseDto<NotificationDto> { Status = ResponseStatus.Ok, Code = ResponseCodes.Ok, Data = dto });
    }

    /// <summary>
    ///     Removes a notification.
    /// </summary>
    /// <param name="id">A notification's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified notification is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpDelete("{id:guid}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> RemoveNotification([FromRoute] Guid id)
    {
        if (!await _notificationRepository.NotificationExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var notification = await _notificationRepository.GetNotificationAsync(id);
        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (notification.OwnerId != currentUser.Id &&
            !await _resourceService.HasPermission(currentUser, Roles.Administrator))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (!await _notificationRepository.RemoveNotificationAsync(id))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return NoContent();
    }
}