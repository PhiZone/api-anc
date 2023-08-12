using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using PhiZoneApi.Constants;
using PhiZoneApi.Dtos.Deliverers;
using PhiZoneApi.Dtos.Requests;
using PhiZoneApi.Dtos.Responses;
using PhiZoneApi.Enums;
using PhiZoneApi.Filters;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;

// ReSharper disable RouteTemplates.ActionRoutePrefixCanBeExtractedToControllerRoute

namespace PhiZoneApi.Controllers;

[Route("me")]
[ApiVersion("2.0")]
[ApiController]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class UserInfoController : Controller
{
    private readonly IDtoMapper _mapper;
    private readonly IResourceService _resourceService;
    private readonly UserManager<User> _userManager;
    private readonly IUserRepository _userRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly ITapTapService _tapTapService;

    public UserInfoController(UserManager<User> userManager, IDtoMapper mapper, IResourceService resourceService,
        IUserRepository userRepository, ITapTapService tapTapService, INotificationRepository notificationRepository)
    {
        _userManager = userManager;
        _mapper = mapper;
        _resourceService = resourceService;
        _userRepository = userRepository;
        _tapTapService = tapTapService;
        _notificationRepository = notificationRepository;
    }

    /// <summary>
    ///     Retrieves user's information.
    /// </summary>
    /// <returns>User's information.</returns>
    /// <response code="200">Returns user's information.</response>
    /// <response code="304">
    ///     When the resource has not been updated since last retrieval. Requires <c>If-None-Match</c>.
    /// </response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    [HttpGet]
    [ServiceFilter(typeof(ETagFilter))]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<UserDetailedDto>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status304NotModified, "text/plain")]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUserInfo()
    {
        var user = await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        if (user == null) return Unauthorized();
        if (!await _resourceService.HasPermission(user, Roles.Member))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        var dto = await _mapper.MapUserAsync<UserDetailedDto>(user);
        dto.Notifications =
            await _notificationRepository.CountNotificationsAsync(predicate: e =>
                e.OwnerId == user.Id && e.DateRead == null);
        return Ok(new ResponseDto<UserDetailedDto> { Status = ResponseStatus.Ok, Code = ResponseCodes.Ok, Data = dto });
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
    [HttpPost("bindings/tapTap")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> BindTapTap([FromBody] TapTapRequestDto dto)
    {
        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;

        if (!await _resourceService.HasPermission(currentUser, Roles.Member))
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

        currentUser.TapUnionId = responseDto.Data.Unionid;
        await _userManager.UpdateAsync(currentUser);
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
    [HttpDelete("bindings/tapTap")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> UnbindTapTap()
    {
        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;

        if (!await _resourceService.HasPermission(currentUser, Roles.Member))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (currentUser.TapUnionId == null)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.AlreadyDone
            });

        currentUser.TapUnionId = null;
        await _userManager.UpdateAsync(currentUser);
        return NoContent();
    }
}