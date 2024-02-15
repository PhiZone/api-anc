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
using PhiZoneApi.Services;

// ReSharper disable RouteTemplates.ActionRoutePrefixCanBeExtractedToControllerRoute

namespace PhiZoneApi.Controllers;

[Route("me")]
[ApiVersion("2.0")]
[ApiController]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class UserInfoController(
    UserManager<User> userManager,
    IDtoMapper mapper,
    IResourceService resourceService,
    IUserRepository userRepository,
    ITapTapService tapTapService,
    IApplicationRepository applicationRepository,
    INotificationRepository notificationRepository,
    AuthProviderFactory factory,
    IApplicationUserRepository applicationUserRepository) : Controller
{
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
        var user = await userRepository.GetUserByIdAsync(int.Parse(User.GetClaim(OpenIddictConstants.Claims.Subject)!));
        if (user == null) return Unauthorized();
        if (!resourceService.HasPermission(user, UserRole.Member))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        var dto = mapper.MapUser<UserDetailedDto>(user);
        dto.Notifications =
            await notificationRepository.CountNotificationsAsync(e =>
                e.OwnerId == user.Id && e.DateRead == null);
        return Ok(new ResponseDto<UserDetailedDto> { Status = ResponseStatus.Ok, Code = ResponseCodes.Ok, Data = dto });
    }

    /// <summary>
    ///     Binds a user to an account of the specified authentication provider.
    /// </summary>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified application is not found.</response>
    [HttpPost("bindings/{provider}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> BindAuthProvider([FromRoute] string provider, [FromQuery] string code,
        [FromQuery] string state)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;

        if (!resourceService.HasPermission(currentUser, UserRole.Member))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (!Enum.TryParse(typeof(AuthProvider), provider, true, out var providerEnum))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var authProvider = factory.GetAuthProvider((providerEnum as AuthProvider?)!.Value);
        if (authProvider == null)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        await authProvider.RequestTokenAsync(code, state, currentUser);
        if (!await authProvider.BindIdentityAsync(currentUser))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidData
            });

        return NoContent();
    }

    /// <summary>
    ///     Binds a user to a TapTap account.
    /// </summary>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified application is not found.</response>
    [HttpPost("bindings/tapTap")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> BindTapTap([FromBody] TapTapRequestDto dto)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;

        if (!resourceService.HasPermission(currentUser, UserRole.Member))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (!await applicationRepository.ApplicationExistsAsync(dto.ApplicationId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ApplicationNotFound
            });

        if ((await applicationRepository.GetApplicationAsync(dto.ApplicationId)).TapClientId == null)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidOperation
            });

        if (await applicationUserRepository.RelationExistsAsync(dto.ApplicationId, currentUser.Id))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.AlreadyDone
            });

        var unionId = dto.UnionId;
        if (unionId == null)
        {
            var response = await tapTapService.Login(dto);

            if (!response!.IsSuccessStatusCode)
                return BadRequest(new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorWithData,
                    Code = ResponseCodes.RemoteFailure,
                    Data = JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync())
                });

            var responseDto =
                JsonConvert.DeserializeObject<TapTapDelivererDto>(await response.Content.ReadAsStringAsync())!;

            unionId = responseDto.Data.Unionid;
        }

        if (await userRepository.GetUserByTapUnionIdAsync(dto.ApplicationId, unionId) != null)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief,
                Code = ResponseCodes.BindingOccupied
            });

        var tapUserRelation = new ApplicationUser
        {
            ApplicationId = dto.ApplicationId, UserId = currentUser.Id, TapUnionId = unionId
        };

        if (!await applicationUserRepository.CreateRelationAsync(tapUserRelation))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return NoContent();
    }

    /// <summary>
    ///     Unbinds a user from an account of the specified authentication provider.
    /// </summary>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified application is not found.</response>
    [HttpDelete("bindings/{provider}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> UnbindAuthProvider([FromRoute] string provider)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;

        if (!resourceService.HasPermission(currentUser, UserRole.Member))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (!Enum.TryParse(typeof(AuthProvider), provider, true, out var providerEnum))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var authProvider = factory.GetAuthProvider((providerEnum as AuthProvider?)!.Value);
        if (authProvider == null)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        if (!await applicationUserRepository.RelationExistsAsync(authProvider.GetApplicationId(), currentUser.Id))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.AlreadyDone
            });

        if (!await applicationUserRepository.RemoveRelationAsync(authProvider.GetApplicationId(), currentUser.Id))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });
        return NoContent();
    }

    /// <summary>
    ///     Unbinds a user from a TapTap account.
    /// </summary>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified application is not found.</response>
    [HttpDelete("bindings/tapTap")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> UnbindTapTap([FromQuery] Guid applicationId)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;

        if (!resourceService.HasPermission(currentUser, UserRole.Member))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (!await applicationRepository.ApplicationExistsAsync(applicationId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ApplicationNotFound
            });

        if (!await applicationUserRepository.RelationExistsAsync(applicationId, currentUser.Id))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.AlreadyDone
            });

        if (!await applicationUserRepository.RemoveRelationAsync(applicationId, currentUser.Id))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });
        return NoContent();
    }
}