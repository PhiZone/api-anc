using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using PhiZoneApi.Constants;
using PhiZoneApi.Dtos.Deliverers;
using PhiZoneApi.Dtos.Responses;
using PhiZoneApi.Enums;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using StackExchange.Redis;

namespace PhiZoneApi.Controllers;

[Route("")]
[ApiVersion("2.0")]
[ApiController]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
    Policy = "AllowAnonymous")]
public class RootController : Controller
{
    private readonly IChartRepository _chartRepository;
    private readonly IPlayConfigurationRepository _playConfigurationRepository;
    private readonly IApplicationRepository _applicationRepository;
    private readonly IConnectionMultiplexer _redis;
    private readonly UserManager<User> _userManager;

    public RootController(IChartRepository chartRepository, IApplicationRepository applicationRepository,
        IConnectionMultiplexer redis, UserManager<User> userManager,
        IPlayConfigurationRepository playConfigurationRepository)
    {
        _chartRepository = chartRepository;
        _playConfigurationRepository = playConfigurationRepository;
        _applicationRepository = applicationRepository;
        _redis = redis;
        _userManager = userManager;
    }

    /// <summary>
    ///     Obtains a token to play a chart.
    /// </summary>
    /// <returns>A <see cref="PlayResponseDto"/>.</returns>
    /// <response code="200">Returns a <see cref="PlayResponseDto"/>.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When either the chart, the configuration, or the application is not found.</response>
    [HttpGet("play")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<PlayResponseDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> Play([FromQuery] Guid chartId, Guid configurationId, Guid applicationId)
    {
        var currentUser = await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        if (!await _userManager.IsInRoleAsync(currentUser!, Roles.Member))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        if (!await _chartRepository.ChartExistsAsync(chartId))
        {
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        }

        if (!await _playConfigurationRepository.PlayConfigurationExistsAsync(configurationId))
        {
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ConfigurationNotFound
            });
        }

        if (!await _applicationRepository.ApplicationExistsAsync(applicationId))
        {
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ApplicationNotFound
            });
        }

        var db = _redis.GetDatabase();
        Guid token;
        do
        {
            token = Guid.NewGuid();
        } while (await db.KeyExistsAsync($"PLAY:{token}"));

        var chart = await _chartRepository.GetChartAsync(chartId);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        
        var info = new PlayInfoDto
        {
            ChartId = chartId,
            ConfigurationId = configurationId,
            ApplicationId = applicationId,
            PlayerId = currentUser!.Id,
            EarliestEndTime = DateTimeOffset.UtcNow.Add(chart.Song.Duration!.Value),
            Timestamp = timestamp
        };
        await db.StringSetAsync($"PLAY:{token}", JsonConvert.SerializeObject(info), TimeSpan.FromDays(14));
        return Ok(new ResponseDto<PlayResponseDto>
        {
            Status = ResponseStatus.Ok,
            Data = new PlayResponseDto
            {
                Token = token, Timestamp = timestamp
            }
        });
    }
}