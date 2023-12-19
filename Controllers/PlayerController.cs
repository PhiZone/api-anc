using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using PhiZoneApi.Configurations;
using PhiZoneApi.Constants;
using PhiZoneApi.Dtos.Deliverers;
using PhiZoneApi.Dtos.Filters;
using PhiZoneApi.Dtos.Requests;
using PhiZoneApi.Dtos.Responses;
using PhiZoneApi.Enums;
using PhiZoneApi.Filters;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;
using StackExchange.Redis;

// ReSharper disable RouteTemplates.ParameterConstraintCanBeSpecified

namespace PhiZoneApi.Controllers;

[Route("player")]
[ApiVersion("2.0")]
[ApiController]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
    Policy = "AllowAnonymous")]
public class PlayerController(IPlayConfigurationRepository configurationRepository, IOptions<DataSettings> dataSettings,
    UserManager<User> userManager, IFilterService filterService, IMapper mapper, IChartRepository chartRepository,
    IApplicationRepository applicationRepository, IConnectionMultiplexer redis, ISongRepository songRepository,
    IResourceService resourceService) : Controller
{
    /// <summary>
    ///     Retrieves configurations.
    /// </summary>
    /// <returns>An array of configurations.</returns>
    /// <response code="200">Returns an array of configurations.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    [HttpGet("configurations")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK,
        Type = typeof(ResponseDto<IEnumerable<PlayConfigurationResponseDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetPlayConfigurations([FromQuery] ArrayRequestDto dto,
        [FromQuery] PlayConfigurationFilterDto? filterDto = null)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (filterDto != null && !resourceService.HasPermission(currentUser, UserRole.Administrator))
            filterDto.RangeOwnerId = new List<int> { currentUser.Id };

        dto.PerPage = dto.PerPage > 0 && dto.PerPage < dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? dataSettings.Value.PaginationPerPage : dataSettings.Value.PaginationMaxPerPage;
        dto.Page = dto.Page > 1 ? dto.Page : 1;
        var position = dto.PerPage * (dto.Page - 1);
        var predicateExpr = await filterService.Parse(filterDto, dto.Predicate, currentUser);
        var list = mapper.Map<List<PlayConfigurationResponseDto>>(
            await configurationRepository.GetPlayConfigurationsAsync(dto.Order, dto.Desc, position, dto.PerPage,
                predicateExpr));
        var total = await configurationRepository.CountPlayConfigurationsAsync(predicateExpr);

        return Ok(new ResponseDto<IEnumerable<PlayConfigurationResponseDto>>
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
    ///     Retrieves a specific configuration.
    /// </summary>
    /// <param name="id">A configuration's ID.</param>
    /// <returns>A configuration.</returns>
    /// <response code="200">Returns a configuration.</response>
    /// <response code="304">
    ///     When the resource has not been updated since last retrieval. Requires <c>If-None-Match</c>.
    /// </response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified configuration is not found.</response>
    [HttpGet("configurations/{id:guid}")]
    [ServiceFilter(typeof(ETagFilter))]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<PlayConfigurationResponseDto>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status304NotModified, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetPlayConfiguration([FromRoute] Guid id)
    {
        if (!await configurationRepository.PlayConfigurationExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var configuration = await configurationRepository.GetPlayConfigurationAsync(id);
        var dto = mapper.Map<PlayConfigurationResponseDto>(configuration);

        return Ok(new ResponseDto<PlayConfigurationResponseDto>
        {
            Status = ResponseStatus.Ok, Code = ResponseCodes.Ok, Data = dto
        });
    }

    /// <summary>
    ///     Creates a new configuration.
    /// </summary>
    /// <returns>An empty body.</returns>
    /// <response code="201">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="418">When the user attempts to swap the perfect and good judgments.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPost("configurations")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(void), StatusCodes.Status201Created, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status418ImATeapot, "text/plain")]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> CreatePlayConfiguration([FromBody] PlayConfigurationRequestDto dto)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!resourceService.HasPermission(currentUser, UserRole.Member))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        if (dto.PerfectJudgment > dto.GoodJudgment) return StatusCode(StatusCodes.Status418ImATeapot);

        var configuration = new PlayConfiguration
        {
            Name = dto.Name,
            PerfectJudgment = dto.PerfectJudgment,
            GoodJudgment = dto.GoodJudgment,
            AspectRatio = ReduceFraction(dto.AspectRatio),
            NoteSize = dto.NoteSize,
            ChartMirroring = dto.ChartMirroring,
            BackgroundLuminance = dto.BackgroundLuminance,
            BackgroundBlur = dto.BackgroundBlur,
            SimultaneousNoteHint = dto.SimultaneousNoteHint,
            FcApIndicator = dto.FcApIndicator,
            ChartOffset = dto.ChartOffset,
            HitSoundVolume = dto.HitSoundVolume,
            MusicVolume = dto.MusicVolume,
            OwnerId = currentUser.Id,
            DateCreated = DateTimeOffset.UtcNow
        };
        if (!await configurationRepository.CreatePlayConfigurationAsync(configuration))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return StatusCode(StatusCodes.Status201Created);
    }

    /// <summary>
    ///     Updates a configuration.
    /// </summary>
    /// <param name="id">A configuration's ID.</param>
    /// <param name="patchDocument">A JSON Patch Document.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified configuration is not found.</response>
    /// <response code="418">When the user attempts to swap the perfect and good judgments.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPatch("configurations/{id:guid}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status418ImATeapot, "text/plain")]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> UpdatePlayConfiguration([FromRoute] Guid id,
        [FromBody] JsonPatchDocument<PlayConfigurationRequestDto> patchDocument)
    {
        if (!await configurationRepository.PlayConfigurationExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var configuration = await configurationRepository.GetPlayConfigurationAsync(id);

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if ((currentUser.Id == configuration.OwnerId && !resourceService.HasPermission(currentUser, UserRole.Member)) ||
            (currentUser.Id != configuration.OwnerId &&
             !resourceService.HasPermission(currentUser, UserRole.Administrator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        var dto = mapper.Map<PlayConfigurationRequestDto>(configuration);
        patchDocument.ApplyTo(dto, ModelState);

        if (!TryValidateModel(dto))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorDetailed,
                Code = ResponseCodes.InvalidData,
                Errors = ModelErrorTranslator.Translate(ModelState)
            });

        if (dto.PerfectJudgment > dto.GoodJudgment) return StatusCode(StatusCodes.Status418ImATeapot);

        configuration.Name = dto.Name;
        configuration.PerfectJudgment = dto.PerfectJudgment;
        configuration.GoodJudgment = dto.GoodJudgment;
        configuration.AspectRatio = ReduceFraction(dto.AspectRatio);
        configuration.NoteSize = dto.NoteSize;
        configuration.BackgroundLuminance = dto.BackgroundLuminance;
        configuration.BackgroundBlur = dto.BackgroundBlur;
        configuration.SimultaneousNoteHint = dto.SimultaneousNoteHint;
        configuration.FcApIndicator = dto.FcApIndicator;
        configuration.ChartOffset = dto.ChartOffset;
        configuration.HitSoundVolume = dto.HitSoundVolume;
        configuration.MusicVolume = dto.MusicVolume;

        if (!await configurationRepository.UpdatePlayConfigurationAsync(configuration))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return NoContent();
    }

    /// <summary>
    ///     Removes a configuration.
    /// </summary>
    /// <param name="id">A configuration's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified configuration is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpDelete("configurations/{id:guid}")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> RemoveConfiguration([FromRoute] Guid id)
    {
        if (!await configurationRepository.PlayConfigurationExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var configuration = await configurationRepository.GetPlayConfigurationAsync(id);

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if ((currentUser.Id == configuration.OwnerId && !resourceService.HasPermission(currentUser, UserRole.Member)) ||
            (currentUser.Id != configuration.OwnerId &&
             !resourceService.HasPermission(currentUser, UserRole.Administrator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (!await configurationRepository.RemovePlayConfigurationAsync(configuration.Id))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return NoContent();
    }

    /// <summary>
    ///     Obtains a play token.
    /// </summary>
    /// <returns>An object containing a play token and a timestamp of the current time in UTC.</returns>
    /// <response code="200">Returns an object containing a play token and a timestamp of the current time in UTC.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When either the chart, the configuration, or the application is not found.</response>
    [HttpGet("play")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<PlayResponseDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> Play([FromQuery] Guid chartId, Guid configurationId, Guid applicationId)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!resourceService.HasPermission(currentUser, UserRole.Member))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        if (!await chartRepository.ChartExistsAsync(chartId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        if (!await configurationRepository.PlayConfigurationExistsAsync(configurationId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ConfigurationNotFound
            });

        if (!await applicationRepository.ApplicationExistsAsync(applicationId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ApplicationNotFound
            });

        var db = redis.GetDatabase();
        Guid token;
        do
        {
            token = Guid.NewGuid();
        } while (await db.KeyExistsAsync($"phizone:play:{token}"));

        var chart = await chartRepository.GetChartAsync(chartId);
        if (await resourceService.IsBlacklisted(chart.OwnerId, currentUser.Id))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.Blacklisted
            });
        if (chart.IsLocked)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.Locked
            });

        var song = await songRepository.GetSongAsync(chart.SongId);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var info = new PlayInfoDto
        {
            ChartId = chartId,
            ConfigurationId = configurationId,
            ApplicationId = applicationId,
            PlayerId = currentUser.Id,
            EarliestEndTime = DateTimeOffset.UtcNow.Add(song.Duration!.Value),
            Timestamp = timestamp
        };
        await db.StringSetAsync($"phizone:play:{token}", JsonConvert.SerializeObject(info), TimeSpan.FromDays(14));
        return Ok(new ResponseDto<PlayResponseDto>
        {
            Status = ResponseStatus.Ok, Data = new PlayResponseDto { Token = token, Timestamp = timestamp }
        });
    }

    private static List<int>? ReduceFraction(IReadOnlyList<int>? fraction)
    {
        if (fraction == null || fraction.Count < 2 || fraction[0] <= 0 || fraction[1] <= 0) return null;
        var gcd = CalculateGcd(fraction[0], fraction[1]);
        return new List<int> { fraction[0] / gcd, fraction[1] / gcd };
    }

    private static int CalculateGcd(int a, int b)
    {
        while (b != 0)
        {
            var temp = b;
            b = a % b;
            a = temp;
        }

        return a;
    }
}