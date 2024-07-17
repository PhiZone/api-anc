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
using StackExchange.Redis;

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
    IConnectionMultiplexer redis,
    AuthProviderFactory factory,
    IFileStorageService fileStorageService,
    IRecordRepository recordRepository,
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
        UserDetailedDto dto;
        var user = await userRepository.GetUserByIdAsync(int.Parse(User.GetClaim(OpenIddictConstants.Claims.Subject)!));
        if (user != null)
        {
            if (!resourceService.HasPermission(user, UserRole.Member))
                return StatusCode(StatusCodes.Status403Forbidden,
                    new ResponseDto<object>
                    {
                        Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                    });

            dto = mapper.MapUser<UserDetailedDto>(user);
            dto.Notifications =
                await notificationRepository.CountNotificationsAsync(e => e.OwnerId == user.Id && e.DateRead == null);
        }
        else
        {
            var db = redis.GetDatabase();
            var key = $"phizone:tapghost:{User.GetClaim("appId")}:{User.GetClaim("unionId")}";
            if (!await db.KeyExistsAsync(key)) return Unauthorized();
            dto = JsonConvert.DeserializeObject<UserDetailedDto>((await db.StringGetAsync(key))!)!;
        }

        return Ok(new ResponseDto<UserDetailedDto> { Status = ResponseStatus.Ok, Code = ResponseCodes.Ok, Data = dto });
    }

    /// <summary>
    ///     Binds a user to an account on a specified authentication provider.
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
        [FromQuery] string state, [FromQuery] string? redirectUri = null)
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

        await authProvider.RequestTokenAsync(code, state, currentUser, redirectUri);
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
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPost("bindings/tapTap")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
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

        var unionId = responseDto.Data.Unionid;

        if (await userRepository.GetUserByTapUnionIdAsync(dto.ApplicationId, unionId) != null)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.BindingOccupied
            });

        var tapUserRelation = new ApplicationUser
        {
            ApplicationId = dto.ApplicationId,
            UserId = currentUser.Id,
            TapUnionId = unionId,
            DateCreated = DateTimeOffset.UtcNow,
            DateUpdated = DateTimeOffset.UtcNow
        };

        if (!await applicationUserRepository.CreateRelationAsync(tapUserRelation))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return NoContent();
    }

    /// <summary>
    ///     Unbinds a user from an account on a specified authentication provider.
    /// </summary>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified application is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpDelete("bindings/{provider}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
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
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpDelete("bindings/tapTap")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
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

    /// <summary>
    ///     Requests to migrate data on a TapTap ghost account to a formal user account.
    /// </summary>
    /// <returns>The code required for inheritance.</returns>
    /// <response code="201">Returns the code required for inheritance.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPost("bindings/tapTap/inherit/request")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(ResponseDto<CodeDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> RequestTapTapGhostInheritance()
    {
        var db = redis.GetDatabase();
        var key =
            $"phizone:tapghost:{User.GetClaim("appId")}:{User.GetClaim("unionId")}";
        if (!await db.KeyExistsAsync(key)) return Unauthorized();
        string code;
        do
        {
            code = resourceService.GenerateCode(4);
        } while (await db.KeyExistsAsync($"phizone:tapghost:inherit:{code}"));

        await db.StringSetAsync($"phizone:tapghost:inherit:{code}",
            $"{User.GetClaim(OpenIddictConstants.Claims.ClientId)}:{User.GetClaim(OpenIddictConstants.Claims.KeyId)}",
            TimeSpan.FromMinutes(10));
        return Ok(new ResponseDto<CodeDto>
        {
            Status = ResponseStatus.Ok, Code = ResponseCodes.Ok, Data = new CodeDto { Code = code }
        });
    }

    /// <summary>
    ///     Migrates data on a TapTap ghost account to a formal user account.
    /// </summary>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPost("bindings/tapTap/inherit")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> InheritTapTapGhost([FromBody] TapTapGhostInheritanceDto dto)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;

        if (!resourceService.HasPermission(currentUser, UserRole.Member))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        var db = redis.GetDatabase();
        var key = $"phizone:tapghost:inherit:{dto.Code.ToUpper()}";
        if (!await db.KeyExistsAsync(key))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidCode
            });
        key = (await db.StringGetAsync(key))!;
        if (!await db.KeyExistsAsync($"phizone:tapghost:{key}")) return NotFound();
        var ghost = JsonConvert.DeserializeObject<UserDetailedDto>(
            (await db.StringGetAsync($"phizone:tapghost:{key}"))!)!;
        if (currentUser.Avatar == null)
        {
            var client = new HttpClient();
            var avatar = await client.GetByteArrayAsync(ghost.Avatar);
            var avatarUrl = (await fileStorageService.UploadImage<User>(ghost.UserName, avatar, (1, 1))).Item1;
            await fileStorageService.SendUserInput(avatarUrl, "Avatar", Request, currentUser);
            currentUser.Avatar = avatarUrl;
        }

        if (await db.KeyExistsAsync($"phizone:tapghost:{key}:records"))
        {
            var records =
                JsonConvert.DeserializeObject<List<Record>>(
                    (await db.StringGetAsync($"phizone:tapghost:{key}:records"))!)!;
            foreach (var record in records)
            {
                record.OwnerId = currentUser.Id;
                await recordRepository.CreateRecordAsync(record);
            }

            var phiRks = (await recordRepository.GetRecordsAsync(["Rks"], [true], 0, 1,
                    r => r.OwnerId == currentUser.Id && r.Score == 1000000 && r.Chart.IsRanked)).FirstOrDefault()
                ?.Rks ?? 0d;
            var best19Rks = (await recordRepository.GetPersonalBests(currentUser.Id)).Sum(r => r.Rks);
            currentUser.Rks = (phiRks + best19Rks) / 20;
            currentUser.Experience += ghost.Experience;
        }

        currentUser.Experience += 1000;
        await userManager.UpdateAsync(currentUser);

        var applicationId = Guid.Parse(key.Split(':')[0]);
        var unionId = key.Split(':')[1];
        ApplicationUser tapUserRelation;
        if (await applicationUserRepository.RelationExistsAsync(applicationId, currentUser.Id))
        {
            tapUserRelation = await applicationUserRepository.GetRelationAsync(applicationId, currentUser.Id);
            // ReSharper disable once InvertIf
            if (tapUserRelation.TapUnionId != unionId)
            {
                tapUserRelation.TapUnionId = unionId;
                tapUserRelation.DateUpdated = DateTimeOffset.UtcNow;
                await applicationUserRepository.UpdateRelationAsync(tapUserRelation);
            }
        }
        else
        {
            tapUserRelation = new ApplicationUser
            {
                ApplicationId = applicationId,
                UserId = currentUser.Id,
                TapUnionId = unionId,
                DateCreated = DateTimeOffset.UtcNow,
                DateUpdated = DateTimeOffset.UtcNow
            };

            if (!await applicationUserRepository.CreateRelationAsync(tapUserRelation))
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });
        }

        await db.KeyDeleteAsync($"phizone:tapghost:{key}");
        await db.KeyDeleteAsync($"phizone:tapghost:{key}:records");
        return NoContent();
    }
}