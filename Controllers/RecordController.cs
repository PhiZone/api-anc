using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
using StackExchange.Redis;
using HP = PhiZoneApi.Constants.HostshipPermissions;

// ReSharper disable RouteTemplates.ActionRoutePrefixCanBeExtractedToControllerRoute

// ReSharper disable RouteTemplates.ParameterConstraintCanBeSpecified

namespace PhiZoneApi.Controllers;

[Route("records")]
[ApiVersion("2.0")]
[ApiController]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
    Policy = "AllowAnonymous")]
public class RecordController(
    IRecordRepository recordRepository,
    IOptions<DataSettings> dataSettings,
    UserManager<User> userManager,
    IUserRepository userRepository,
    IFilterService filterService,
    IDtoMapper dtoMapper,
    IMapper mapper,
    IChartRepository chartRepository,
    ILikeRepository likeRepository,
    ILikeService likeService,
    ICommentRepository commentRepository,
    IConnectionMultiplexer redis,
    IApplicationRepository applicationRepository,
    IPlayConfigurationRepository playConfigurationRepository,
    IEventDivisionRepository eventDivisionRepository,
    IEventTeamRepository eventTeamRepository,
    IEventResourceRepository eventResourceRepository,
    IEventRepository eventRepository,
    IRecordService recordService,
    ILeaderboardService leaderboardService,
    IScriptService scriptService,
    IResourceService resourceService,
    ILogger<RecordController> logger,
    INotificationService notificationService) : Controller
{
    /// <summary>
    ///     Retrieves records.
    /// </summary>
    /// <returns>An array of records.</returns>
    /// <response code="200">Returns an array of records.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<RecordDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetRecords([FromQuery] ArrayRequestDto dto,
        [FromQuery] RecordFilterDto? filterDto = null)
    {
        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        dto.PerPage = dto.PerPage > 0 && dto.PerPage < dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? dataSettings.Value.PaginationPerPage : dataSettings.Value.PaginationMaxPerPage;
        dto.Page = dto.Page > 1 ? dto.Page : 1;
        var position = dto.PerPage * (dto.Page - 1);
        var predicateExpr = await filterService.Parse(filterDto, dto.Predicate, currentUser);
        var showAnonymous = filterDto is { RangeId: not null, RangeOwnerId: null, MinOwnerId: null, MaxOwnerId: null };
        var records = await recordRepository.GetRecordsAsync(dto.Order, dto.Desc, position, dto.PerPage, predicateExpr,
            true, currentUser?.Id, showAnonymous);
        var total = await recordRepository.CountRecordsAsync(predicateExpr, showAnonymous);
        var list = records.Select(e => dtoMapper.MapRecord<RecordDto>(e)).ToList();

        return Ok(new ResponseDto<IEnumerable<RecordDto>>
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
    ///     Retrieves a specific record.
    /// </summary>
    /// <param name="id">A record's ID.</param>
    /// <returns>A record.</returns>
    /// <response code="200">Returns a record.</response>
    /// <response code="304">
    ///     When the resource has not been updated since last retrieval. Requires <c>If-None-Match</c>.
    /// </response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified record is not found.</response>
    [HttpGet("{id:guid}")]
    [ServiceFilter(typeof(ETagFilter))]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<RecordDto>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status304NotModified, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetRecord([FromRoute] Guid id)
    {
        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        if (!await recordRepository.RecordExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var record = await recordRepository.GetRecordAsync(id, true, currentUser?.Id);
        var dto = dtoMapper.MapRecord<RecordDto>(record);

        return Ok(new ResponseDto<RecordDto> { Status = ResponseStatus.Ok, Code = ResponseCodes.Ok, Data = dto });
    }

    /// <summary>
    ///     Creates a new record.
    /// </summary>
    /// <returns>Calculated results related to the play.</returns>
    /// <response code="201">Returns calculated results related to the play.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When either the token, the application, the chart, the user, or the configuration is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(ResponseDto<RecordResponseDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> CreateRecord([FromBody] RecordCreationDto dto)
    {
        var db = redis.GetDatabase();
        if (!await db.KeyExistsAsync($"phizone:play:{dto.Token}"))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidToken
            });

        var info = JsonConvert.DeserializeObject<PlayInfoDto>((await db.StringGetAsync($"phizone:play:{dto.Token}"))!)!;
        if (DateTimeOffset.UtcNow < info.EarliestEndTime)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorWithMessage,
                Code = ResponseCodes.InvalidData,
                Message = "Your request is made earlier than expected."
            });

        if (!await applicationRepository.ApplicationExistsAsync(info.ApplicationId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ApplicationNotFound
            });

        var player = await userRepository.GetUserByIdAsync(info.PlayerId);
        if (player == null)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.UserNotFound
            });

        if (!await chartRepository.ChartExistsAsync(info.ChartId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var chart = await chartRepository.GetChartAsync(info.ChartId);
        if (chart.Accessibility == Accessibility.RefuseAny)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentIsPrivate
            });
        if (chart.FileChecksum != null && !chart.FileChecksum.Equals(dto.Checksum, StringComparison.OrdinalIgnoreCase))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorWithMessage,
                Code = ResponseCodes.InvalidData,
                Message = "File checksum validation has failed."
            });

        var secret = (await applicationRepository.GetApplicationAsync(info.ApplicationId)).Secret!;
        var digest =
            $"{info.ChartId}:{info.ConfigurationId}:{info.PlayerId}:{dto.MaxCombo}:{dto.Perfect}:{dto.GoodEarly}:{dto.GoodLate}:{dto.Bad}:{dto.Miss}:{info.Timestamp}";
        using var hasher = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hmac = Convert.ToBase64String(
            await hasher.ComputeHashAsync(new MemoryStream(Encoding.UTF8.GetBytes(digest))));
        logger.LogDebug(LogEvents.RecordDebug, "{Digest} - {Hmac}", digest, hmac);
        if (!dto.Hmac.Equals(hmac, StringComparison.OrdinalIgnoreCase))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorWithMessage,
                Code = ResponseCodes.InvalidData,
                Message = "HMAC validation has failed."
            });

        var judgmentCount = dto.Perfect + dto.GoodEarly + dto.GoodLate + dto.Bad + dto.Miss;
        if (judgmentCount != chart.NoteCount)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorWithMessage,
                Code = ResponseCodes.InvalidData,
                Message =
                    $"The number of judgments ({judgmentCount}) is not equal to the number of notes ({chart.NoteCount})."
            });

        var minExpectation = judgmentCount / (dto.Bad + dto.Miss + 1);
        var maxExpectation = judgmentCount - dto.Bad - dto.Miss;
        if (dto.MaxCombo < minExpectation || dto.MaxCombo > maxExpectation)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorWithMessage,
                Code = ResponseCodes.InvalidData,
                Message =
                    $"Max combo ({dto.MaxCombo}) is not within the expected range [{minExpectation}, {maxExpectation}]."
            });

        db.KeyDelete($"phizone:play:{dto.Token}");

        if (!await playConfigurationRepository.PlayConfigurationExistsAsync(info.ConfigurationId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ConfigurationNotFound
            });

        var configuration = await playConfigurationRepository.GetPlayConfigurationAsync(info.ConfigurationId);

        var score = recordService.CalculateScore(dto.Perfect, dto.GoodEarly + dto.GoodLate, dto.Bad, dto.Miss,
            dto.MaxCombo);
        var accuracy = recordService.CalculateAccuracy(dto.Perfect, dto.GoodEarly + dto.GoodLate, dto.Bad, dto.Miss);
        var rksFactor = recordService.CalculateRksFactor(configuration.PerfectJudgment, configuration.GoodJudgment);
        var rks = recordService.CalculateRks(dto.Perfect, dto.GoodEarly + dto.GoodLate, dto.Bad, dto.Miss,
            chart.Difficulty, dto.StdDeviation) * rksFactor;
        var rksBefore = player.Rks;
        var experienceDelta = 0ul;
        var highestAccuracy = 0d;
        if (await recordRepository.CountRecordsAsync(record =>
                record.ChartId == info.ChartId && record.OwnerId == player.Id) > 0)
            highestAccuracy = (await recordRepository.GetRecordsAsync(["Accuracy"], [true], 0, 1,
                record => record.ChartId == info.ChartId && record.OwnerId == player.Id)).FirstOrDefault()!.Accuracy;

        var culture = (CultureInfo)CultureInfo.CurrentCulture.Clone();
        culture.NumberFormat.PercentPositivePattern = 1;
        logger.LogInformation(LogEvents.RecordInfo,
            "[{Now}] New record: {User} - {Chart} {Score} {Accuracy} {Rks} {StdDeviation}ms",
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), player.UserName, await resourceService.GetDisplayName(chart),
            score, accuracy.ToString("P2", culture), rks.ToString("N3"), dto.StdDeviation.ToString("N3"));

        var record = new Record
        {
            ChartId = info.ChartId,
            Chart = chart,
            OwnerId = info.PlayerId,
            ApplicationId = info.ApplicationId,
            Score = score,
            Accuracy = accuracy,
            IsFullCombo = dto.MaxCombo == judgmentCount,
            MaxCombo = dto.MaxCombo,
            Perfect = dto.Perfect,
            GoodEarly = dto.GoodEarly,
            GoodLate = dto.GoodLate,
            Bad = dto.Bad,
            Miss = dto.Miss,
            StdDeviation = dto.StdDeviation,
            Rks = rks,
            PerfectJudgment = configuration.PerfectJudgment,
            GoodJudgment = configuration.GoodJudgment,
            DateCreated = DateTimeOffset.UtcNow
        };

        EventDivision? eventDivision = null;
        EventTeam? eventTeam = null;
        if (info is { DivisionId: not null, TeamId: not null })
        {
            eventDivision = await eventDivisionRepository.GetEventDivisionAsync(info.DivisionId.Value);
            eventTeam = await eventTeamRepository.GetEventTeamAsync(info.TeamId.Value);

            var firstFailure = await scriptService.RunEventTaskAsync(eventTeam.DivisionId, record, eventTeam.Id, player,
                [EventTaskType.PreSubmission]);

            if (firstFailure != null)
                return BadRequest(new ResponseDto<EventTaskResponseDto>
                {
                    Status = ResponseStatus.ErrorWithData, Code = ResponseCodes.InvalidData, Data = firstFailure
                });
        }

        if (!await recordRepository.CreateRecordAsync(record))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        experienceDelta += (ulong)(rksFactor * score switch
        {
            >= 1000000 => 20,
            >= 960000 => 14,
            >= 920000 => 9,
            >= 880000 => 5,
            _ => 1
        });

        if ((accuracy >= 0.98 && (highestAccuracy is >= 0.97 and < 0.98 || accuracy - highestAccuracy >= 0.01)) ||
            (Math.Abs(accuracy - 1d) < 1e-7 && highestAccuracy < 1))
        {
            var pos = await recordRepository.CountRecordsAsync(r => r.ChartId == info.ChartId && r.Rks > rks) + 1;
            if (pos <= 1000)
            {
                var temp = Math.Pow(chart.Difficulty, 7d / 5);
                experienceDelta += (ulong)Math.Pow(chart.Difficulty + 2, temp * (Math.Pow(pos, 4d / 7) - temp) / -1152);
            }
        }

        var phiRks = (await recordRepository.GetRecordsAsync(["Rks"], [true], 0, 1,
                r => r.OwnerId == player.Id && r.Score == 1000000 && r.Chart.IsRanked)).FirstOrDefault()
            ?.Rks ?? 0d;
        var best19Rks = (await recordRepository.GetPersonalBests(player.Id)).Sum(r => r.Rks);
        var rksAfter = (phiRks + best19Rks) / 20;

        if (!chart.IsRanked) experienceDelta = (ulong)(experienceDelta * 0.5);

        player.Experience += experienceDelta;
        player.Rks = rksAfter;
        await userManager.UpdateAsync(player);

        leaderboardService.Add(record);

        // ReSharper disable once InvertIf
        if (eventDivision != null && eventTeam != null)
        {
            var eventResource = new EventResource
            {
                DivisionId = eventDivision.Id,
                ResourceId = record.Id,
                RecordId = record.Id,
                Type = EventResourceType.Entry,
                IsAnonymous = eventDivision.Anonymization,
                TeamId = eventTeam.Id,
                DateCreated = DateTimeOffset.UtcNow
            };
            await eventResourceRepository.CreateEventResourceAsync(eventResource);
            if (await eventResourceRepository.CountResourcesAsync(eventDivision.Id,
                    e => e.Type == EventResourceType.Entry && e.TeamId == eventTeam.Id) ==
                eventTeam.ClaimedSubmissionCount)
            {
                eventTeam.Status = ParticipationStatus.Finished;
                await eventTeamRepository.UpdateEventTeamAsync(eventTeam);
            }

            await scriptService.RunEventTaskAsync(eventTeam.DivisionId, record, eventTeam.Id, player,
                [EventTaskType.PostSubmission]);
            if (eventTeam.IsUnveiled) leaderboardService.Add(eventTeam);
        }

        return StatusCode(StatusCodes.Status201Created,
            new ResponseDto<RecordResponseDto>
            {
                Status = ResponseStatus.Ok,
                Code = ResponseCodes.Ok,
                Data = new RecordResponseDto
                {
                    Id = record.Id,
                    Score = score,
                    Accuracy = accuracy,
                    IsFullCombo = record.IsFullCombo,
                    Player = dtoMapper.MapUser<UserDto>(player),
                    ExperienceDelta = experienceDelta,
                    RksBefore = rksBefore,
                    DateCreated = record.DateCreated
                }
            });
    }

    /// <summary>
    ///     Creates a new record using a TapTap ghost account.
    /// </summary>
    /// <returns>Calculated results related to the play.</returns>
    /// <response code="201">Returns calculated results related to the play.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When either the token, the application, the chart, the user, or the configuration is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPost("tapTap")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(ResponseDto<RecordResponseDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> CreateRecordTapTapGhost([FromBody] RecordCreationDto dto)
    {
        var db = redis.GetDatabase();
        if (!await db.KeyExistsAsync($"phizone:play:tapghost:{dto.Token}"))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidToken
            });

        var info = JsonConvert.DeserializeObject<PlayInfoTapTapDto>(
            (await db.StringGetAsync($"phizone:play:tapghost:{dto.Token}"))!)!;
        if (DateTimeOffset.UtcNow < info.EarliestEndTime)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorWithMessage,
                Code = ResponseCodes.InvalidData,
                Message = "Your request is made earlier than expected."
            });

        if (!await applicationRepository.ApplicationExistsAsync(info.ApplicationId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ApplicationNotFound
            });
        var key = $"phizone:tapghost:{info.ApplicationId}:{info.PlayerId}";
        if (!await db.KeyExistsAsync(key))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.UserNotFound
            });
        var player = JsonConvert.DeserializeObject<UserDetailedDto>((await db.StringGetAsync(key))!)!;

        if (!await chartRepository.ChartExistsAsync(info.ChartId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var chart = await chartRepository.GetChartAsync(info.ChartId);
        if (chart.Accessibility == Accessibility.RefuseAny)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentIsPrivate
            });
        if (chart.FileChecksum != null && !chart.FileChecksum.Equals(dto.Checksum, StringComparison.OrdinalIgnoreCase))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorWithMessage,
                Code = ResponseCodes.InvalidData,
                Message = "File checksum validation has failed."
            });

        var secret = (await applicationRepository.GetApplicationAsync(info.ApplicationId)).Secret!;
        var digest =
            $"{info.ChartId}:{info.PlayerId}:{dto.MaxCombo}:{dto.Perfect}:{dto.GoodEarly}:{dto.GoodLate}:{dto.Bad}:{dto.Miss}:{info.Timestamp}";
        using var hasher = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hmac = Convert.ToBase64String(
            await hasher.ComputeHashAsync(new MemoryStream(Encoding.UTF8.GetBytes(digest))));
        logger.LogDebug(LogEvents.RecordDebug, "{Digest} - {Hmac}", digest, hmac);
        if (!dto.Hmac.Equals(hmac, StringComparison.OrdinalIgnoreCase))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorWithMessage,
                Code = ResponseCodes.InvalidData,
                Message = "HMAC validation has failed."
            });

        var judgmentCount = dto.Perfect + dto.GoodEarly + dto.GoodLate + dto.Bad + dto.Miss;
        if (judgmentCount != chart.NoteCount)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorWithMessage,
                Code = ResponseCodes.InvalidData,
                Message =
                    $"The number of judgments ({judgmentCount}) is not equal to the number of notes ({chart.NoteCount})."
            });

        var minExpectation = judgmentCount / (dto.Bad + dto.Miss + 1);
        var maxExpectation = judgmentCount - dto.Bad - dto.Miss;
        if (dto.MaxCombo < minExpectation || dto.MaxCombo > maxExpectation)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorWithMessage,
                Code = ResponseCodes.InvalidData,
                Message =
                    $"Max combo ({dto.MaxCombo}) is not within the expected range [{minExpectation}, {maxExpectation}]."
            });

        db.KeyDelete($"phizone:play:tapghost:{dto.Token}");

        var score = recordService.CalculateScore(dto.Perfect, dto.GoodEarly + dto.GoodLate, dto.Bad, dto.Miss,
            dto.MaxCombo);
        var accuracy = recordService.CalculateAccuracy(dto.Perfect, dto.GoodEarly + dto.GoodLate, dto.Bad, dto.Miss);
        var rksFactor = recordService.CalculateRksFactor(80, 160);
        var rks = recordService.CalculateRks(dto.Perfect, dto.GoodEarly + dto.GoodLate, dto.Bad, dto.Miss,
            chart.Difficulty, dto.StdDeviation) * rksFactor;
        var rksBefore = player.Rks;
        var experienceDelta = 0ul;

        var culture = (CultureInfo)CultureInfo.CurrentCulture.Clone();
        culture.NumberFormat.PercentPositivePattern = 1;
        logger.LogInformation(LogEvents.RecordInfo,
            "[{Now}] New record: {User} - {Chart} {Score} {Accuracy} {Rks} {StdDeviation}ms",
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), player.UserName, await resourceService.GetDisplayName(chart),
            score, accuracy.ToString("P2", culture), rks.ToString("N3"), dto.StdDeviation.ToString("N3"));

        var record = new Record
        {
            ChartId = info.ChartId,
            Chart = chart,
            OwnerId = -1,
            ApplicationId = info.ApplicationId,
            Score = score,
            Accuracy = accuracy,
            IsFullCombo = dto.MaxCombo == judgmentCount,
            MaxCombo = dto.MaxCombo,
            Perfect = dto.Perfect,
            GoodEarly = dto.GoodEarly,
            GoodLate = dto.GoodLate,
            Bad = dto.Bad,
            Miss = dto.Miss,
            StdDeviation = dto.StdDeviation,
            Rks = rks,
            PerfectJudgment = info.PerfectJudgment,
            GoodJudgment = info.GoodJudgment,
            DateCreated = DateTimeOffset.UtcNow
        };

        var records = await db.KeyExistsAsync($"{key}:records")
            ? JsonConvert.DeserializeObject<List<Record>>((await db.StringGetAsync($"{key}:records"))!)!
            : [];
        records.Add(record);
        await db.StringSetAsync($"{key}:records", (RedisValue)JsonConvert.SerializeObject(records),
            TimeSpan.FromDays(180));

        experienceDelta += (ulong)(rksFactor * score switch
        {
            >= 1000000 => 20,
            >= 960000 => 14,
            >= 920000 => 9,
            >= 880000 => 5,
            _ => 1
        });

        var phiRks = records.OrderByDescending(e => e.Rks)
            .FirstOrDefault(r => r.OwnerId == player.Id && r is { Score: 1000000, Chart.IsRanked: true })
            ?.Rks ?? 0d;
        var best19Rks = records.Where(e => e.Chart.IsRanked && e.OwnerId == player.Id)
            .GroupBy(e => e.ChartId)
            .Select(g => g.OrderByDescending(e => e.Rks).ThenBy(e => e.DateCreated).First())
            .Sum(r => r.Rks);
        var rksAfter = (phiRks + best19Rks) / 20;

        if (!chart.IsRanked) experienceDelta = (ulong)(experienceDelta * 0.5);

        player.Experience += experienceDelta;
        player.Rks = rksAfter;
        await db.StringSetAsync(key, JsonConvert.SerializeObject(player), TimeSpan.FromDays(180));

        return StatusCode(StatusCodes.Status201Created,
            new ResponseDto<RecordResponseDto>
            {
                Status = ResponseStatus.Ok,
                Code = ResponseCodes.Ok,
                Data = new RecordResponseDto
                {
                    Id = record.Id,
                    Score = score,
                    Accuracy = accuracy,
                    IsFullCombo = record.IsFullCombo,
                    Player = mapper.Map<UserDto>(player),
                    ExperienceDelta = experienceDelta,
                    RksBefore = rksBefore,
                    DateCreated = record.DateCreated
                }
            });
    }

    /// <summary>
    ///     Removes a record.
    /// </summary>
    /// <param name="id">A record's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified record is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpDelete("{id:guid}")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> RemoveRecord([FromRoute] Guid id)
    {
        if (!await recordRepository.RecordExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!resourceService.HasPermission(currentUser, UserRole.Administrator))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (!await recordRepository.RemoveRecordAsync(id))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return NoContent();
    }

    /// <summary>
    ///     Retrieves likes from a specified record.
    /// </summary>
    /// <param name="id">A record's ID.</param>
    /// <returns>An array of likes.</returns>
    /// <response code="200">Returns an array of likes.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified record is not found.</response>
    [HttpGet("{id:guid}/likes")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<LikeDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetRecordLikes([FromRoute] Guid id, [FromQuery] ArrayRequestDto dto)
    {
        dto.PerPage = dto.PerPage > 0 && dto.PerPage < dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? dataSettings.Value.PaginationPerPage : dataSettings.Value.PaginationMaxPerPage;
        dto.Page = dto.Page > 1 ? dto.Page : 1;
        var position = dto.PerPage * (dto.Page - 1);
        if (!await recordRepository.RecordExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var likes = await likeRepository.GetLikesAsync(dto.Order, dto.Desc, position, dto.PerPage,
            e => e.ResourceId == id);
        var list = mapper.Map<List<LikeDto>>(likes);
        var total = await likeRepository.CountLikesAsync(e => e.ResourceId == id);

        return Ok(new ResponseDto<IEnumerable<LikeDto>>
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
    ///     Likes a specific record.
    /// </summary>
    /// <param name="id">A record's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="201">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="404">When the specified record is not found.</response>
    [HttpPost("{id:guid}/likes")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(void), StatusCodes.Status201Created, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> CreateLike([FromRoute] Guid id)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!resourceService.HasPermission(currentUser, UserRole.Member))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        if (!await recordRepository.RecordExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var record = await recordRepository.GetRecordAsync(id);
        if (await resourceService.IsBlacklisted(record.OwnerId, currentUser.Id))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.Blacklisted
            });
        if (!await likeService.CreateLikeAsync(record, currentUser.Id))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.AlreadyDone
            });

        return StatusCode(StatusCodes.Status201Created);
    }

    /// <summary>
    ///     Removes the like from a specified record.
    /// </summary>
    /// <param name="id">A record's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="404">When the specified record is not found.</response>
    [HttpDelete("{id:guid}/likes")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> RemoveLike([FromRoute] Guid id)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!resourceService.HasPermission(currentUser, UserRole.Member))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        if (!await recordRepository.RecordExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var record = await recordRepository.GetRecordAsync(id);
        if (!await likeService.RemoveLikeAsync(record, currentUser.Id))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.AlreadyDone
            });

        return NoContent();
    }

    /// <summary>
    ///     Retrieves comments from a specified record.
    /// </summary>
    /// <param name="id">A record's ID.</param>
    /// <returns>An array of comments.</returns>
    /// <response code="200">Returns an array of comments.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified record is not found.</response>
    [HttpGet("{id:guid}/comments")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<CommentDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetRecordComments([FromRoute] Guid id, [FromQuery] ArrayRequestDto dto)
    {
        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        dto.PerPage = dto.PerPage > 0 && dto.PerPage < dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? dataSettings.Value.PaginationPerPage : dataSettings.Value.PaginationMaxPerPage;
        dto.Page = dto.Page > 1 ? dto.Page : 1;
        var position = dto.PerPage * (dto.Page - 1);
        if (!await recordRepository.RecordExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var comments = await commentRepository.GetCommentsAsync(dto.Order, dto.Desc, position, dto.PerPage,
            e => e.ResourceId == id, currentUser?.Id);
        var total = await commentRepository.CountCommentsAsync(e => e.ResourceId == id);
        var list = comments.Select(dtoMapper.MapComment<CommentDto>).ToList();

        return Ok(new ResponseDto<IEnumerable<CommentDto>>
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
    ///     Comments on a specific record.
    /// </summary>
    /// <param name="id">A record's ID.</param>
    /// <returns>The ID of the comment.</returns>
    /// <response code="201">Returns the ID of the comment.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified record is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPost("{id:guid}/comments")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(ResponseDto<CreatedResponseDto<Guid>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> CreateComment([FromRoute] Guid id, [FromBody] CommentCreationDto dto)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!resourceService.HasPermission(currentUser, UserRole.Member))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        if (!await recordRepository.RecordExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var record = await recordRepository.GetRecordAsync(id);
        if (await resourceService.IsBlacklisted(record.OwnerId, currentUser.Id))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.Blacklisted
            });
        var result = await resourceService.ParseUserContent(dto.Content);
        var comment = new Comment
        {
            ResourceId = record.Id,
            Content = result.Item1,
            Language = dto.Language,
            OwnerId = currentUser.Id,
            DateCreated = DateTimeOffset.UtcNow
        };
        if (!await commentRepository.CreateCommentAsync(comment))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        await notificationService.NotifyComment(comment, record, await resourceService.GetDisplayName(record),
            dto.Content);
        await notificationService.NotifyMentions(result.Item2, currentUser,
            resourceService.GetRichText<Comment>(comment.Id.ToString(), dto.Content));

        return StatusCode(StatusCodes.Status201Created,
            new ResponseDto<CreatedResponseDto<Guid>>
            {
                Status = ResponseStatus.Ok,
                Code = ResponseCodes.Ok,
                Data = new CreatedResponseDto<Guid> { Id = comment.Id }
            });
    }

    /// <summary>
    ///     Links a record to a specific event division.
    /// </summary>
    /// <returns>An empty body.</returns>
    /// <response code="201">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPost("{id:guid}/event")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(void), StatusCodes.Status201Created, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> CreateEventResource([FromRoute] Guid id, [FromBody] EventResourceRequestDto dto)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!await recordRepository.RecordExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var record = await recordRepository.GetRecordAsync(id);

        if (!await eventDivisionRepository.EventDivisionExistsAsync(dto.DivisionId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentNotFound
            });

        var eventDivision = await eventDivisionRepository.GetEventDivisionAsync(dto.DivisionId);
        var eventEntity = await eventRepository.GetEventAsync(eventDivision.EventId);
        var permission = HP.Gen(HP.Create, HP.Resource);

        if (!(resourceService.HasPermission(currentUser, UserRole.Administrator) || eventEntity.Hostships.Any(f =>
                f.UserId == currentUser.Id && (f.IsAdmin || f.Permissions.Contains(permission)))))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        var eventResource = new EventResource
        {
            DivisionId = dto.DivisionId,
            ResourceId = record.Id,
            RecordId = record.Id,
            Type = EventResourceType.Entry,
            Label = dto.Label,
            Description = dto.Description,
            IsAnonymous = dto.IsAnonymous,
            TeamId = dto.TeamId,
            DateCreated = DateTimeOffset.UtcNow
        };

        if (!await eventResourceRepository.CreateEventResourceAsync(eventResource))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return StatusCode(StatusCodes.Status201Created);
    }
}