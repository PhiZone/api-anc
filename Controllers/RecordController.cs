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

// ReSharper disable RouteTemplates.ActionRoutePrefixCanBeExtractedToControllerRoute

// ReSharper disable RouteTemplates.ParameterConstraintCanBeSpecified

namespace PhiZoneApi.Controllers;

[Route("records")]
[ApiVersion("2.0")]
[ApiController]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
    Policy = "AllowAnonymous")]
public class RecordController(IRecordRepository recordRepository, IOptions<DataSettings> dataSettings,
        UserManager<User> userManager, IFilterService filterService, IDtoMapper dtoMapper, IMapper mapper,
        IChartRepository chartRepository, ILikeRepository likeRepository,
        ILikeService likeService, ICommentRepository commentRepository, IConnectionMultiplexer redis,
        IApplicationRepository applicationRepository, IPlayConfigurationRepository playConfigurationRepository,
        IRecordService recordService, IResourceService resourceService, ILogger<RecordController> logger,
        INotificationService notificationService)
    : Controller
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
        var records = await recordRepository.GetRecordsAsync(dto.Order, dto.Desc, position,
            dto.PerPage, predicateExpr);
        var total = await recordRepository.CountRecordsAsync(predicateExpr);
        var list = new List<RecordDto>();

        foreach (var record in records) list.Add(await dtoMapper.MapRecordAsync<RecordDto>(record, currentUser));

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
        var record = await recordRepository.GetRecordAsync(id);
        var dto = await dtoMapper.MapRecordAsync<RecordDto>(record, currentUser);

        return Ok(new ResponseDto<RecordDto> { Status = ResponseStatus.Ok, Code = ResponseCodes.Ok, Data = dto });
    }

    /// <summary>
    ///     Creates a new record.
    /// </summary>
    /// <returns>An object containing calculated results related to the play.</returns>
    /// <response code="201">Returns an object containing calculated results related to the play.</response>
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

        var player = await userManager.FindByIdAsync(info.PlayerId.ToString());
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
        if (chart.FileChecksum != null && !chart.FileChecksum.Equals(dto.Checksum))
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
        var experienceDelta = 0;
        var highestAccuracy = 0d;
        if (await recordRepository.CountRecordsAsync(record =>
                record.ChartId == info.ChartId && record.OwnerId == player.Id) > 0)
            highestAccuracy = (await recordRepository.GetRecordsAsync(new List<string> { "Accuracy" },
                new List<bool> { true }, 0, 1,
                record => record.ChartId == info.ChartId && record.OwnerId == player.Id)).FirstOrDefault()!.Accuracy;

        logger.LogInformation(LogEvents.RecordInfo,
            "New record: {User} - {Chart} {Score} {Accuracy} {Rks} {StdDeviation}ms",
            player.UserName, await resourceService.GetDisplayName(chart), score, accuracy.ToString("P2"),
            rks.ToString("N3"), dto.StdDeviation.ToString("N3"));

        var record = new Record
        {
            ChartId = info.ChartId,
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

        if (!await recordRepository.CreateRecordAsync(record))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        experienceDelta += (int)(rksFactor * score switch
        {
            1000000 => 20,
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
                experienceDelta += (int)Math.Pow(chart.Difficulty + 2, temp * (Math.Pow(pos, 4d / 7) - temp) / -1152);
            }
        }

        var phiRks =
            (await recordRepository.GetRecordsAsync(new List<string> { "Rks" }, new List<bool> { true }, 0, 1,
                r => r.OwnerId == player.Id && r.Score == 1000000 && r.Chart.IsRanked)).FirstOrDefault()
            ?.Rks ?? 0d;
        var best19Rks = (await recordService.GetBest19(player.Id)).Sum(r => r.Rks);
        var rksAfter = (phiRks + best19Rks) / 20;

        if (!chart.IsRanked) experienceDelta = (int)(experienceDelta * 0.1);

        player.Experience += experienceDelta;
        player.Rks = rksAfter;
        await userManager.UpdateAsync(player);

        chart.PlayCount = await chartRepository.CountChartRecordsAsync(chart.Id);
        await chartRepository.UpdateChartAsync(chart);

        return StatusCode(StatusCodes.Status201Created,
            new ResponseDto<RecordResponseDto>
            {
                Status = ResponseStatus.Ok,
                Data = new RecordResponseDto
                {
                    Id = record.Id,
                    Score = score,
                    Accuracy = accuracy,
                    IsFullCombo = record.IsFullCombo,
                    Player = await dtoMapper.MapUserAsync<UserDto>(player),
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
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Produces("application/json")]
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
    ///     Retrieves likes from a specific record.
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
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Produces("application/json")]
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
    ///     Removes the like from a specific record.
    /// </summary>
    /// <param name="id">A record's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="404">When the specified record is not found.</response>
    [HttpDelete("{id:guid}/likes")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Produces("application/json")]
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
    ///     Retrieves comments from a specific record.
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
            e => e.ResourceId == id);
        var list = new List<CommentDto>();
        var total = await commentRepository.CountCommentsAsync(e => e.ResourceId == id);

        foreach (var comment in comments) list.Add(await dtoMapper.MapCommentAsync<CommentDto>(comment, currentUser));

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
    /// <returns>An empty body.</returns>
    /// <response code="201">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified record is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPost("{id:guid}/comments")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status201Created, "text/plain")]
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

        return StatusCode(StatusCodes.Status201Created);
    }
}