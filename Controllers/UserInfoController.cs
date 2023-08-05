using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using PhiZoneApi.Constants;
using PhiZoneApi.Dtos.Responses;
using PhiZoneApi.Enums;
using PhiZoneApi.Filters;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;

namespace PhiZoneApi.Controllers;

[Route("userInfo")]
[ApiVersion("2.0")]
[ApiController]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class UserInfoController : Controller
{
    private readonly IDtoMapper _mapper;
    private readonly IResourceService _resourceService;
    private readonly UserManager<User> _userManager;

    public UserInfoController(UserManager<User> userManager, IDtoMapper mapper, IResourceService resourceService)
    {
        _userManager = userManager;
        _mapper = mapper;
        _resourceService = resourceService;
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
    [Produces("application/json", "text/plain")]
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
        return Ok(new ResponseDto<UserDetailedDto> { Status = ResponseStatus.Ok, Code = ResponseCodes.Ok, Data = dto });
    }
}