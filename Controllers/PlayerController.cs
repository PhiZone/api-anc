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
public class PlayerController : Controller
{
    private readonly IApplicationRepository _applicationRepository;
    private readonly IChartRepository _chartRepository;
    private readonly IPlayConfigurationRepository _configurationRepository;
    private readonly IOptions<DataSettings> _dataSettings;
    private readonly IFilterService _filterService;
    private readonly IMapper _mapper;
    private readonly IConnectionMultiplexer _redis;
    private readonly ISongRepository _songRepository;
    private readonly UserManager<User> _userManager;

    public PlayerController(IPlayConfigurationRepository configurationRepository, IOptions<DataSettings> dataSettings,
        UserManager<User> userManager, IFilterService filterService, IMapper mapper, IChartRepository chartRepository,
        IApplicationRepository applicationRepository, IConnectionMultiplexer redis, ISongRepository songRepository)
    {
        _configurationRepository = configurationRepository;
        _dataSettings = dataSettings;
        _userManager = userManager;
        _filterService = filterService;
        _mapper = mapper;
        _chartRepository = chartRepository;
        _applicationRepository = applicationRepository;
        _redis = redis;
        _songRepository = songRepository;
    }

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
    public async Task<IActionResult> GetPlayConfigurations([FromQuery] ArrayWithTimeRequestDto dto,
        [FromQuery] PlayConfigurationFilterDto? filterDto = null)
    {
        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (filterDto != null && !await _userManager.IsInRoleAsync(currentUser, Roles.Administrator))
            filterDto.RangeOwnerId = new List<int> { currentUser.Id };

        dto.PerPage = dto.PerPage > 0
            ? dto.PerPage <= _dataSettings.Value.PaginationMaxPerPage
                ? dto.PerPage
                : _dataSettings.Value.PaginationMaxPerPage
            : _dataSettings.Value.PaginationPerPage;
        var position = dto.PerPage * (dto.Page - 1);
        var predicateExpr = await _filterService.Parse(filterDto, dto.Predicate, currentUser);
        var list = _mapper.Map<List<PlayConfigurationResponseDto>>(
            await _configurationRepository.GetPlayConfigurationsAsync(dto.Order, dto.Desc, position, dto.PerPage,
                dto.Search, predicateExpr));
        var total = await _configurationRepository.CountPlayConfigurationsAsync(dto.Search, predicateExpr);

        return Ok(new ResponseDto<IEnumerable<PlayConfigurationResponseDto>>
        {
            Status = ResponseStatus.Ok,
            Code = ResponseCodes.Ok,
            Total = total,
            PerPage = dto.PerPage,
            HasPrevious = position > 0,
            HasNext = position < total - total % dto.PerPage,
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
    [Produces("application/json", "text/plain")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<PlayConfigurationResponseDto>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status304NotModified, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetPlayConfiguration([FromRoute] Guid id)
    {
        if (!await _configurationRepository.PlayConfigurationExistsAsync(id)) return NotFound();
        var configuration = await _configurationRepository.GetPlayConfigurationAsync(id);
        var dto = _mapper.Map<PlayConfigurationResponseDto>(configuration);

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
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Consumes("multipart/form-data")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status201Created, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status418ImATeapot, "text/plain")]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> CreatePlayConfiguration([FromForm] PlayConfigurationRequestDto dto)
    {
        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!await _userManager.IsInRoleAsync(currentUser, Roles.Member))
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
            AspectRatio = dto.AspectRatio,
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
        if (!await _configurationRepository.CreatePlayConfigurationAsync(configuration))
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
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Consumes("application/json")]
    [Produces("text/plain", "application/json")]
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
        if (!await _configurationRepository.PlayConfigurationExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var configuration = await _configurationRepository.GetPlayConfigurationAsync(id);

        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if ((currentUser.Id == configuration.OwnerId && !await _userManager.IsInRoleAsync(currentUser, Roles.Member)) ||
            (currentUser.Id != configuration.OwnerId &&
             !await _userManager.IsInRoleAsync(currentUser, Roles.Administrator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        var dto = _mapper.Map<PlayConfigurationRequestDto>(configuration);
        patchDocument.ApplyTo(dto, ModelState);

        if (!TryValidateModel(dto))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorDetailed,
                Code = ResponseCodes.InvalidData,
                Errors = ModelErrorTranslator.Translate(ModelState)
            });

        if (dto.PerfectJudgment > dto.GoodJudgment) return StatusCode(StatusCodes.Status418ImATeapot);

        configuration = _mapper.Map<PlayConfiguration>(dto);

        if (!await _configurationRepository.UpdatePlayConfigurationAsync(configuration))
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
        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!await _userManager.IsInRoleAsync(currentUser, Roles.Member))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        if (!await _chartRepository.ChartExistsAsync(chartId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        if (!await _configurationRepository.PlayConfigurationExistsAsync(configurationId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ConfigurationNotFound
            });

        if (!await _applicationRepository.ApplicationExistsAsync(applicationId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ApplicationNotFound
            });

        var db = _redis.GetDatabase();
        Guid token;
        do
        {
            token = Guid.NewGuid();
        } while (await db.KeyExistsAsync($"PLAY:{token}"));

        var chart = await _chartRepository.GetChartAsync(chartId);
        var song = await _songRepository.GetSongAsync(chart.SongId);
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
        await db.StringSetAsync($"PLAY:{token}", JsonConvert.SerializeObject(info), TimeSpan.FromDays(14));
        return Ok(new ResponseDto<PlayResponseDto>
        {
            Status = ResponseStatus.Ok, Data = new PlayResponseDto { Token = token, Timestamp = timestamp }
        });
    }
}