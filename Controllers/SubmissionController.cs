using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using PhiZoneApi.Constants;
using PhiZoneApi.Dtos.Deliverers;
using PhiZoneApi.Dtos.Requests;
using PhiZoneApi.Dtos.Responses;
using PhiZoneApi.Enums;
using PhiZoneApi.Hubs;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using StackExchange.Redis;

namespace PhiZoneApi.Controllers;

[Route("studio/submissions")]
[ApiVersion("2.0")]
[ApiController]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class SubmissionController(
    ISeekTuneService seekTuneService,
    ISongRepository songRepository,
    ISongSubmissionRepository songSubmissionRepository,
    IResourceRecordRepository resourceRecordRepository,
    IChartSubmissionRepository chartSubmissionRepository,
    IChartAssetSubmissionRepository chartAssetSubmissionRepository,
    UserManager<User> userManager,
    ISongService songService,
    IFileStorageService fileStorageService,
    IMapper mapper,
    IDtoMapper dtoMapper,
    IAdmissionRepository admissionRepository,
    IChartService chartService,
    IScriptService scriptService,
    IResourceService resourceService,
    IUserRepository userRepository,
    ICollaborationRepository collaborationRepository,
    IEventDivisionRepository eventDivisionRepository,
    IEventTeamRepository eventTeamRepository,
    INotificationService notificationService,
    ITemplateService templateService,
    IFeishuService feishuService,
    IConnectionMultiplexer redis,
    IHubContext<SubmissionHub, ISubmissionClient> hubContext,
    ILogger<SubmissionController> logger) : Controller
{
    /// <summary>
    ///     Creates a submission session.
    /// </summary>
    /// <returns>The ID of the user's submission session.</returns>
    /// <response code="201">Returns the status of the user's submission session.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPost]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(ResponseDto<CreatedResponseDto<Guid>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> CreateSubmissionSession()
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!resourceService.HasPermission(currentUser, UserRole.Member))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        var db = redis.GetDatabase();
        var id = Guid.NewGuid();

        while (await db.KeyExistsAsync($"phizone:session:submission:{id}")) id = Guid.NewGuid();
        var session = new SubmissionSession { Id = id, Status = SubmissionSessionStatus.Waiting };

        await db.StringSetAsync($"phizone:session:submission:{id}", JsonConvert.SerializeObject(session),
            TimeSpan.FromDays(1));
        return StatusCode(StatusCodes.Status201Created,
            new ResponseDto<CreatedResponseDto<Guid>>
            {
                Status = ResponseStatus.Ok,
                Code = ResponseCodes.Ok,
                Data = new CreatedResponseDto<Guid> { Id = session.Id }
            });
    }

    /// <summary>
    ///     Uploads song and illustration for the user's submission session.
    /// </summary>
    /// <returns>
    ///     A list of similar songs already available on PhiZone, one of similar song submissions on PhiZone, and one of
    ///     songs with potentially infringed copyright.
    /// </returns>
    /// <response code="201">
    ///     Returns a list of similar songs already available on PhiZone, one of similar song submissions on
    ///     PhiZone, and one of songs with potentially infringed copyright.
    /// </response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPost("{id:guid}/song")]
    [Consumes("multipart/form-data")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(ResponseDto<SubmissionSongDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> UploadSongAndIllustration([FromRoute] Guid id, [FromForm] SongIllustrationDto dto)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!resourceService.HasPermission(currentUser, UserRole.Member))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        var db = redis.GetDatabase();
        var key = $"phizone:session:submission:{id}";
        if (!await db.KeyExistsAsync(key))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidOperation
            });

        var session = JsonConvert.DeserializeObject<SubmissionSession>((await db.StringGetAsync(key))!)!;
        var songPath = await SaveFile(dto.Song, session.Id);
        var illustrationPath = await SaveFile(dto.Illustration, session.Id);

        await hubContext.Clients.Group(session.Id.ToString())
            .ReceiveFileProgress(SessionFileStatus.Analyzing, "Searching for potential song duplicates", null);
        var songResults = await seekTuneService.FindMatches(songPath, take: 5);
        await hubContext.Clients.Group(session.Id.ToString())
            .ReceiveFileProgress(SessionFileStatus.Analyzing, "Searching for potential copyright infringements", null);
        var resourceRecordResults = await seekTuneService.FindMatches(songPath, true, 5);

        if (songResults == null || resourceRecordResults == null)
        {
            await hubContext.Clients.Group(session.Id.ToString())
                .ReceiveFileProgress(SessionFileStatus.Failed, "Unable to search for matching results", 1);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });
        }

        await hubContext.Clients.Group(session.Id.ToString())
            .ReceiveFileProgress(SessionFileStatus.Finalizing, "Generating search summary", 1);
        session.SongPath = songPath;
        session.IllustrationPath = illustrationPath;
        session.SongResults = new SongResults
        {
            SongMatches = songResults, ResourceRecordMatches = resourceRecordResults
        };
        session.Status = SubmissionSessionStatus.SongFinished;

        await db.StringSetAsync(key, JsonConvert.SerializeObject(session), TimeSpan.FromDays(1));
        var summary = await GenerateMatchSummary(songResults, resourceRecordResults, currentUser);
        await hubContext.Clients.Group(session.Id.ToString()).ReceiveFileProgress(SessionFileStatus.Succeeded, null, 1);
        return StatusCode(StatusCodes.Status201Created,
            new ResponseDto<SubmissionSongDto> { Status = ResponseStatus.Ok, Code = ResponseCodes.Ok, Data = summary });
    }

    /// <summary>
    ///     Creates a new song submission for the user's submission session.
    /// </summary>
    /// <returns>The ID of the song submission.</returns>
    /// <response code="201">Returns the ID of the song submission.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPost("{id:guid}/song/new")]
    [Consumes("multipart/form-data")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(ResponseDto<CreatedResponseDto<Guid>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> CreateSongSubmission([FromRoute] Guid id, [FromForm] SessionSongCreationDto dto,
        [FromQuery] bool wait = false)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!resourceService.HasPermission(currentUser, UserRole.Member))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        var db = redis.GetDatabase();
        var key = $"phizone:session:submission:{id}";
        if (!await db.KeyExistsAsync(key))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidOperation
            });

        var session = JsonConvert.DeserializeObject<SubmissionSession>((await db.StringGetAsync(key))!)!;
        if (session.Status != SubmissionSessionStatus.SongFinished || session.SongPath == null ||
            session.IllustrationPath == null)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidOperation
            });

        var songFile = LoadFile(session.SongPath);
        var illustrationFile = LoadFile(session.IllustrationPath);

        (string, string, TimeSpan)? songSubmissionInfo = null;
        if (wait)
        {
            songSubmissionInfo = await songService.UploadAsync(dto.Title, songFile);
            if (songSubmissionInfo == null)
                return BadRequest(new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidData
                });

            if (!(TimeSpan.Zero <= dto.PreviewStart && dto.PreviewStart < dto.PreviewEnd &&
                  dto.PreviewEnd <= songSubmissionInfo.Value.Item3))
                return BadRequest(new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidTimeRelation
                });
            logger.LogInformation(LogEvents.SongInfo, "New song submission: {Title}", dto.Title);
        }
        else if (!(TimeSpan.Zero <= dto.PreviewStart && dto.PreviewStart < dto.PreviewEnd))
        {
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidTimeRelation
            });
        }

        var illustrationUrl = (await fileStorageService.UploadImage<Song>(dto.Title, illustrationFile, (16, 9))).Item1;
        await fileStorageService.SendUserInput(illustrationUrl, "Illustration", Request, currentUser);
        string? license = null;
        if (dto.License != null) license = (await fileStorageService.Upload<Song>(dto.Title, dto.License)).Item1;

        var authors = resourceService.GetAuthorIds(dto.AuthorName);
        string? originalityProof = null;
        if (dto.OriginalityProof != null)
        {
            if (!authors.Contains(currentUser.Id))
                return BadRequest(new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidAuthorInfo
                });
            originalityProof = (await fileStorageService.Upload<SongSubmission>(dto.Title, dto.OriginalityProof)).Item1;
        }

        var songSubmission = new SongSubmission
        {
            Title = dto.Title,
            EditionType = dto.EditionType,
            Edition = dto.Edition,
            AuthorName = dto.AuthorName,
            File = songSubmissionInfo?.Item1,
            FileChecksum = songSubmissionInfo?.Item2,
            Illustration = illustrationUrl,
            Illustrator = dto.Illustrator,
            Description = dto.Description,
            Accessibility = originalityProof != null ? dto.Accessibility : Accessibility.AllowAny,
            Lyrics = dto.Lyrics,
            Bpm = dto.Bpm,
            MinBpm = dto.MinBpm,
            MaxBpm = dto.MaxBpm,
            Offset = dto.Offset,
            License = license,
            OriginalityProof = originalityProof,
            Duration = songSubmissionInfo?.Item3,
            PreviewStart = dto.PreviewStart,
            PreviewEnd = dto.PreviewEnd,
            Tags = dto.Tags,
            Status = RequestStatus.Waiting,
            OwnerId = currentUser.Id,
            DateCreated = DateTimeOffset.UtcNow,
            DateFileUpdated = DateTimeOffset.UtcNow,
            DateUpdated = DateTimeOffset.UtcNow
        };

        var (eventDivision, eventTeam, response) =
            await CheckForEvent(songSubmission, currentUser, EventTaskType.PreSubmission, true);
        if (response != null) return response;

        if (!await songSubmissionRepository.CreateSongSubmissionAsync(songSubmission))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        foreach (var userId in authors.Where(e => e != currentUser.Id).Distinct())
        {
            var invitee = await userRepository.GetUserByIdAsync(userId);
            if (invitee == null)
                return BadRequest(new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.UserNotFound
                });

            await CreateCollaboration(songSubmission, songSubmission.GetDisplay(), invitee, null, currentUser);
        }

        if (!wait)
        {
            await songService.PublishAsync(songFile, songSubmission.Id, true);
            logger.LogInformation(LogEvents.SongInfo, "Scheduled new song submission: {Title}", dto.Title);
        }
        else
        {
            await feishuService.Notify(songSubmission, FeishuResources.ContentReviewalChat);
        }

        if (eventDivision != null && eventTeam != null)
            await scriptService.RunEventTaskAsync(eventTeam.DivisionId, songSubmission, eventTeam.Id, currentUser,
                [EventTaskType.PostSubmission]);

        return StatusCode(StatusCodes.Status201Created,
            new ResponseDto<CreatedResponseDto<Guid>>
            {
                Status = ResponseStatus.Ok,
                Code = ResponseCodes.Ok,
                Data = new CreatedResponseDto<Guid> { Id = songSubmission.Id }
            });
    }

    /// <summary>
    ///     Uploads file and metadata for a new chart submission for the user's submission session.
    /// </summary>
    /// <returns>An empty body.</returns>
    /// <response code="201">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPost("{id:guid}/chart")]
    [Consumes("multipart/form-data")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status201Created, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> UploadChart([FromRoute] Guid id, [FromForm] ChartSubmissionCreationDto dto)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!resourceService.HasPermission(currentUser, UserRole.Member))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        var db = redis.GetDatabase();
        var key = $"phizone:session:submission:{id}";
        if (!await db.KeyExistsAsync(key))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidOperation
            });

        var session = JsonConvert.DeserializeObject<SubmissionSession>((await db.StringGetAsync(key))!)!;
        if (session.Status != SubmissionSessionStatus.SongFinished)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidOperation
            });

        var authors = resourceService.GetAuthorIds(dto.AuthorName);
        if (!authors.Contains(currentUser.Id))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidAuthorInfo
            });

        Song? song = null;
        if (dto.SongId != null)
        {
            if (!await songRepository.SongExistsAsync(dto.SongId.Value))
                return NotFound(new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentNotFound
                });

            song = await songRepository.GetSongAsync(dto.SongId.Value);
            if (song.OwnerId != currentUser.Id && song.Accessibility == Accessibility.RefuseAny)
                return BadRequest(new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentIsPrivate
                });
        }

        SongSubmission? songSubmission = null;
        if (dto.SongSubmissionId != null)
        {
            if (!await songSubmissionRepository.SongSubmissionExistsAsync(dto.SongSubmissionId.Value))
                return NotFound(new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentNotFound
                });

            songSubmission = await songSubmissionRepository.GetSongSubmissionAsync(dto.SongSubmissionId.Value);
            if (songSubmission.OwnerId != currentUser.Id && songSubmission.Accessibility == Accessibility.RefuseAny)
                return BadRequest(new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentIsPrivate
                });
            if (songSubmission.RepresentationId != null)
            {
                song = await songRepository.GetSongAsync(songSubmission.RepresentationId.Value);
                songSubmission = null;
            }
        }

        if ((song != null && songSubmission != null) || (song == null && songSubmission == null))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorWithMessage,
                Code = ResponseCodes.InvalidData,
                Message = "Must enter one and only one field between song and song submission."
            });

        var (eventDivision, _, response) = await GetEvent(dto.Tags, currentUser, true);
        if (response != null) return response;

        var illustrationUrl = dto.Illustration != null
            ? (await fileStorageService.UploadImage<Chart>(
                dto.Title ?? (song != null ? song.Title : songSubmission!.Title), dto.Illustration, (16, 9))).Item1
            : null;
        if (illustrationUrl != null)
            await fileStorageService.SendUserInput(illustrationUrl, "Illustration", Request, currentUser);

        var chartSubmissionInfo = dto.File != null
            ? await chartService.Upload(dto.Title ?? (song != null ? song.Title : songSubmission!.Title), dto.File,
                eventDivision is { Anonymization: true },
                eventDivision is { Anonymization: true } && (song is { IsOriginal: true } ||
                                                             songSubmission is { OriginalityProof: not null }))
            : null;

        if (dto.File != null && chartSubmissionInfo == null)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.UnsupportedChartFormat
            });

        var chartSubmission = new ChartSubmission
        {
            Title = dto.Title,
            LevelType = dto.LevelType,
            Level = dto.Level,
            Difficulty = dto.Difficulty,
            Format = chartSubmissionInfo?.Item3 ?? ChartFormat.Unsupported,
            File = chartSubmissionInfo?.Item1,
            FileChecksum = chartSubmissionInfo?.Item2,
            AuthorName = dto.AuthorName,
            Illustration = illustrationUrl,
            Illustrator = dto.Illustrator,
            Description = dto.Description,
            Accessibility = dto.Accessibility,
            IsRanked = dto.IsRanked,
            NoteCount = chartSubmissionInfo?.Item4 ?? 0,
            Tags = dto.Tags,
            SongId = song?.Id,
            SongSubmissionId = songSubmission?.Id,
            Status = RequestStatus.Waiting,
            VolunteerStatus = RequestStatus.Waiting,
            AdmissionStatus =
                song != null
                    ? song.OwnerId == currentUser.Id || song.Accessibility == Accessibility.AllowAny
                        ? RequestStatus.Approved
                        : RequestStatus.Waiting
                    : songSubmission!.OwnerId == currentUser.Id ||
                      songSubmission.Accessibility == Accessibility.AllowAny
                        ? RequestStatus.Approved
                        : RequestStatus.Waiting,
            Owner = currentUser,
            DateCreated = DateTimeOffset.UtcNow,
            DateFileUpdated = DateTimeOffset.UtcNow,
            DateUpdated = DateTimeOffset.UtcNow
        };

        session.Chart = chartSubmission;
        session.Status = SubmissionSessionStatus.ChartFinished;

        await db.StringSetAsync(key, JsonConvert.SerializeObject(session), TimeSpan.FromDays(1));

        return StatusCode(StatusCodes.Status201Created);
    }

    /// <summary>
    ///     Creates a new chart submission's asset for the user's submission session.
    /// </summary>
    /// <param name="dto">The new asset.</param>
    /// <returns>An empty body.</returns>
    /// <response code="201">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPost("{id:guid}/chart/assets")]
    [Consumes("multipart/form-data")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status201Created, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> CreateChartSubmissionAsset([FromRoute] Guid id,
        [FromForm] ChartAssetCreationDto dto)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!resourceService.HasPermission(currentUser, UserRole.Member))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        var db = redis.GetDatabase();
        var key = $"phizone:session:submission:{id}";
        if (!await db.KeyExistsAsync(key))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidOperation
            });

        var session = JsonConvert.DeserializeObject<SubmissionSession>((await db.StringGetAsync(key))!)!;
        if (session.Status != SubmissionSessionStatus.ChartFinished || session.Chart == null)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidOperation
            });

        var chartSubmission = session.Chart;
        var assets = session.Assets;
        if (assets.Any(e => e.Name == dto.Name))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.NameOccupied
            });

        var chartAsset = new SessionChartAsset
        {
            Type = dto.Type,
            Name = dto.Name,
            File = (await fileStorageService.Upload<ChartAsset>(
                chartSubmission.Title ?? (chartSubmission.SongId != null
                    ? (await songRepository.GetSongAsync(chartSubmission.SongId.Value)).Title
                    : (await songSubmissionRepository.GetSongSubmissionAsync(
                        chartSubmission.SongSubmissionId!.Value)).Title), dto.File)).Item1
        };

        session.Assets.Add(chartAsset);

        await db.StringSetAsync(key, JsonConvert.SerializeObject(session), TimeSpan.FromDays(1));

        return StatusCode(StatusCodes.Status201Created);
    }

    /// <summary>
    ///     Creates new chart submission's assets for the user's submission session.
    /// </summary>
    /// <param name="dto">The new assets.</param>
    /// <returns>Am empty body.</returns>
    /// <response code="201">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPost("{id:guid}/chart/assets/batch")]
    [Consumes("multipart/form-data")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status201Created, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> CreateChartSubmissionAssets([FromRoute] Guid id,
        [FromForm] IEnumerable<ChartAssetCreationDto> dtos)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!resourceService.HasPermission(currentUser, UserRole.Member))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        var db = redis.GetDatabase();
        var key = $"phizone:session:submission:{id}";
        if (!await db.KeyExistsAsync(key))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidOperation
            });

        var session = JsonConvert.DeserializeObject<SubmissionSession>((await db.StringGetAsync(key))!)!;
        if (session.Status != SubmissionSessionStatus.ChartFinished || session.Chart == null)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidOperation
            });

        var chartSubmission = session.Chart;
        var assets = session.Assets;

        foreach (var dto in dtos)
        {
            if (assets.Any(e => e.Name == dto.Name))
                return BadRequest(new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.NameOccupied
                });

            var chartAsset = new SessionChartAsset
            {
                Type = dto.Type,
                Name = dto.Name,
                File = (await fileStorageService.Upload<ChartAsset>(
                    chartSubmission.Title ?? (chartSubmission.SongId != null
                        ? (await songRepository.GetSongAsync(chartSubmission.SongId.Value)).Title
                        : (await songSubmissionRepository.GetSongSubmissionAsync(chartSubmission.SongSubmissionId!
                            .Value)).Title), dto.File)).Item1
            };

            assets.Add(chartAsset);
        }

        await db.StringSetAsync(key, JsonConvert.SerializeObject(session), TimeSpan.FromDays(1));

        return StatusCode(StatusCodes.Status201Created);
    }

    /// <summary>
    ///     Creates the chart submission for the user's submission session.
    /// </summary>
    /// <returns>The ID of the chart submission.</returns>
    /// <response code="201">Returns the ID of the chart submission.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPost("{id:guid}/chart/new")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(ResponseDto<CreatedResponseDto<Guid>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> CreateChartSubmission([FromRoute] Guid id)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!resourceService.HasPermission(currentUser, UserRole.Member))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        var db = redis.GetDatabase();
        var key = $"phizone:session:submission:{id}";
        if (!await db.KeyExistsAsync(key))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidOperation
            });

        var session = JsonConvert.DeserializeObject<SubmissionSession>((await db.StringGetAsync(key))!)!;
        if (session.Chart == null)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidOperation
            });

        var chartSubmission = session.Chart;
        var (eventDivision, eventTeam, response) = await GetEvent(chartSubmission.Tags, currentUser, true);
        if (response != null) return response;

        if (eventTeam != null)
        {
            var firstFailure = await scriptService.RunEventTaskAsync(eventTeam.DivisionId, chartSubmission,
                eventTeam.Id, currentUser, [EventTaskType.PreSubmission]);

            if (firstFailure != null)
                return BadRequest(new ResponseDto<object>
                {
                    Status = firstFailure.Status, Code = firstFailure.Code, Message = firstFailure.Message
                });
        }

        var song = chartSubmission.SongId != null
            ? await songRepository.GetSongAsync(chartSubmission.SongId.Value)
            : null;
        var songSubmission = chartSubmission.SongSubmissionId != null
            ? await songSubmissionRepository.GetSongSubmissionAsync(chartSubmission.SongSubmissionId.Value)
            : null;

        if (!await chartSubmissionRepository.CreateChartSubmissionAsync(chartSubmission))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        if (song != null && song.OwnerId != currentUser.Id && song.Accessibility == Accessibility.RequireReview)
        {
            var admission = new Admission
            {
                AdmitterId = song.Id,
                AdmitteeId = chartSubmission.Id,
                Status = RequestStatus.Waiting,
                RequesterId = currentUser.Id,
                RequesteeId = song.OwnerId,
                DateCreated = DateTimeOffset.UtcNow,
                AdmitterType = AdmitterType.Song
            };
            await admissionRepository.CreateAdmissionAsync(admission);
            await notificationService.Notify((await userManager.FindByIdAsync(song.OwnerId.ToString()))!, currentUser,
                NotificationType.Requests, "song-admission",
                new Dictionary<string, string>
                {
                    { "User", resourceService.GetRichText<User>(currentUser.Id.ToString(), currentUser.UserName!) },
                    {
                        "Chart",
                        resourceService.GetRichText<ChartSubmission>(chartSubmission.Id.ToString(),
                            await resourceService.GetDisplayName(chartSubmission))
                    },
                    { "Song", resourceService.GetRichText<Song>(song.Id.ToString(), song.GetDisplay()) },
                    {
                        "Admission",
                        resourceService.GetComplexRichText("SongAdmission", admission.AdmitterId.ToString(),
                            admission.AdmitteeId.ToString(),
                            templateService.GetMessage("more-info", admission.Requestee.Language)!)
                    }
                });
        }
        else if (songSubmission != null && songSubmission.OwnerId != currentUser.Id &&
                 songSubmission.Accessibility == Accessibility.RequireReview)
        {
            var admission = new Admission
            {
                AdmitterId = songSubmission.Id,
                AdmitteeId = chartSubmission.Id,
                Status = RequestStatus.Waiting,
                RequesterId = currentUser.Id,
                RequesteeId = songSubmission.OwnerId,
                DateCreated = DateTimeOffset.UtcNow,
                AdmitterType = AdmitterType.SongSubmission
            };
            await admissionRepository.CreateAdmissionAsync(admission);
            await notificationService.Notify((await userManager.FindByIdAsync(songSubmission.OwnerId.ToString()))!,
                currentUser, NotificationType.Requests, "song-submission-admission",
                new Dictionary<string, string>
                {
                    { "User", resourceService.GetRichText<User>(currentUser.Id.ToString(), currentUser.UserName!) },
                    {
                        "Chart",
                        resourceService.GetRichText<ChartSubmission>(chartSubmission.Id.ToString(),
                            await resourceService.GetDisplayName(chartSubmission))
                    },
                    {
                        "Song",
                        resourceService.GetRichText<SongSubmission>(songSubmission.Id.ToString(),
                            songSubmission.GetDisplay())
                    },
                    {
                        "Admission",
                        resourceService.GetComplexRichText("SongSubmissionAdmission",
                            admission.AdmitterId.ToString(), admission.AdmitteeId.ToString(),
                            templateService.GetMessage("more-info", admission.Requestee.Language)!)
                    }
                });
        }

        var authors = resourceService.GetAuthorIds(chartSubmission.AuthorName);

        foreach (var userId in authors.Where(e => e != currentUser.Id).Distinct())
        {
            var invitee = await userRepository.GetUserByIdAsync(userId);
            if (invitee == null)
                return BadRequest(new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.UserNotFound
                });

            await CreateCollaboration(chartSubmission, await resourceService.GetDisplayName(chartSubmission), invitee,
                null, currentUser);
        }

        foreach (var chartAsset in session.Assets.Select(asset => new ChartAssetSubmission
                 {
                     ChartSubmissionId = chartSubmission.Id,
                     Type = asset.Type,
                     Name = asset.Name,
                     File = asset.File,
                     OwnerId = currentUser.Id,
                     DateCreated = DateTimeOffset.UtcNow,
                     DateUpdated = DateTimeOffset.UtcNow
                 }))
            if (!await chartAssetSubmissionRepository.CreateChartAssetSubmissionAsync(chartAsset))
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        chartSubmission.DateCreated = DateTimeOffset.UtcNow;
        chartSubmission.DateFileUpdated = DateTimeOffset.UtcNow;
        chartSubmission.DateUpdated = DateTimeOffset.UtcNow;

        if (!await chartSubmissionRepository.UpdateChartSubmissionAsync(chartSubmission))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        await db.KeyDeleteAsync(key);

        await feishuService.Notify(chartSubmission, FeishuResources.ContentReviewalChat);

        if (eventDivision != null && eventTeam != null)
            await scriptService.RunEventTaskAsync(eventTeam.DivisionId, chartSubmission, eventTeam.Id, currentUser,
                [EventTaskType.PostSubmission]);

        resourceService.CleanupSession(session);

        logger.LogInformation(LogEvents.ChartInfo, "New chart submission: {Title} [{Level} {Difficulty}]",
            chartSubmission.Title ?? song?.Title ?? songSubmission!.Title, chartSubmission.Level,
            Math.Floor(chartSubmission.Difficulty));

        return StatusCode(StatusCodes.Status201Created,
            new ResponseDto<CreatedResponseDto<Guid>>
            {
                Status = ResponseStatus.Ok,
                Code = ResponseCodes.Ok,
                Data = new CreatedResponseDto<Guid> { Id = chartSubmission.Id }
            });
    }

    private async Task<SubmissionSongDto> GenerateMatchSummary(List<SeekTuneFindResult> songResults,
        List<SeekTuneFindResult> resourceRecordResults, User currentUser)
    {
        var songIds = new List<Guid>();
        var songSubmissionIds = songResults.Select(e => e.Id);
        var songSubmissions =
            await songSubmissionRepository.GetSongSubmissionsAsync(predicate: e => songSubmissionIds.Contains(e.Id));
        foreach (var songSubmission in songSubmissions)
            if (songSubmission.RepresentationId != null)
                songIds.Add(songSubmission.RepresentationId!.Value);

        var songMatches =
            (await songRepository.GetSongsAsync(predicate: e =>
                songIds.Contains(e.Id) && (e.Accessibility != Accessibility.RefuseAny || e.OwnerId == currentUser.Id)))
            .Select(e => dtoMapper.MapSong<SongMatchDto>(e))
            .Select(e =>
            {
                e.Score = songResults.First(f =>
                        f.Id == e.Id || f.Id == songSubmissions
                            .First(g => g.RepresentationId != null && g.RepresentationId.Value == e.Id)
                            .Id)
                    .Score;
                return e;
            });
        var songSubmissionMatches = songSubmissions
            .Where(e => e.RepresentationId == null &&
                        (e.Accessibility != Accessibility.RefuseAny || e.OwnerId == currentUser.Id))
            .Select(e => dtoMapper.MapSongSubmission<SongSubmissionMatchDto>(e, currentUser))
            .Select(e =>
            {
                e.Score = songResults.First(f => f.Id == e.Id).Score;
                return e;
            });
        var resourceRecordIds = resourceRecordResults.Select(e => e.Id);
        var resourceRecordMatches = mapper
            .Map<IEnumerable<ResourceRecordMatchDto>>(await resourceRecordRepository.GetResourceRecordsAsync(
                predicate: e => resourceRecordIds.Contains(e.Id) && e.Strategy != ResourceRecordStrategy.Accept))
            .Select(e =>
            {
                e.Score = resourceRecordResults.First(f => f.Id == e.Id).Score;
                return e;
            });

        return new SubmissionSongDto
        {
            SongMatches = songMatches.OrderByDescending(e => e.Score),
            SongSubmissionMatches = songSubmissionMatches.OrderByDescending(e => e.Score),
            ResourceRecordMatches = resourceRecordMatches.OrderByDescending(e => e.Score)
        };
    }

    private async Task<(EventDivision?, EventTeam?, IActionResult?)> GetEvent(IEnumerable<string> tags,
        User currentUser, bool tagChanged = false)
    {
        var normalizedTags = tags.Select(resourceService.Normalize);
        var eventDivisions = await eventDivisionRepository.GetEventDivisionsAsync(predicate: e =>
            e.Type == EventDivisionType.Song && e.Status != EventDivisionStatus.Created &&
            normalizedTags.Contains(e.TagName));
        if (eventDivisions.Count == 0) return (null, null, null);

        var eventDivision = eventDivisions.FirstOrDefault(e =>
            tagChanged
                ? e.Status == EventDivisionStatus.Started
                : e.Status is EventDivisionStatus.Started or EventDivisionStatus.Ended);
        if (eventDivision == null)
            return (null, null,
                BadRequest(new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.DivisionNotStarted
                }));

        var eventTeams = await eventTeamRepository.GetEventTeamsAsync(predicate: e =>
            e.DivisionId == eventDivision.Id && e.Participations.Any(f => f.ParticipantId == currentUser.Id));

        if (eventTeams.Count == 0)
            return (eventDivision, null,
                BadRequest(new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.NotEnrolled
                }));

        var eventTeam = eventTeams.First();
        return (eventDivision, eventTeam, null);
    }

    private async Task<(EventDivision?, EventTeam?, IActionResult?)> CheckForEvent(SongSubmission songSubmission,
        User currentUser, EventTaskType taskType, bool tagChanged = false)
    {
        var owner = (await userRepository.GetUserByIdAsync(songSubmission.OwnerId))!;
        var result = await GetEvent(songSubmission.Tags, owner, tagChanged);
        if (result.Item1 == null || result.Item2 == null || result.Item3 != null) return result;

        var eventDivision = result.Item1;
        var eventTeam = result.Item2;

        var firstFailure = await scriptService.RunEventTaskAsync(eventTeam.DivisionId, songSubmission, eventTeam.Id,
            currentUser, [taskType]);

        if (firstFailure != null)
            return (eventDivision, eventTeam,
                BadRequest(new ResponseDto<object>
                {
                    Status = firstFailure.Status, Code = firstFailure.Code, Message = firstFailure.Message
                }));

        return (eventDivision, eventTeam, null);
    }

    private async Task CreateCollaboration<T>(T submission, string displayName, User invitee, string? position,
        User currentUser) where T : Submission
    {
        var collaboration = new Collaboration
        {
            SubmissionId = submission.Id,
            InviterId = currentUser.Id,
            InviteeId = invitee.Id,
            Position = position,
            DateCreated = DateTimeOffset.UtcNow
        };

        await collaborationRepository.CreateCollaborationAsync(collaboration);

        await notificationService.Notify(invitee, currentUser, NotificationType.Requests,
            typeof(T) == typeof(SongSubmission) ? "song-collab" : "chart-collab",
            new Dictionary<string, string>
            {
                { "User", resourceService.GetRichText<User>(currentUser.Id.ToString(), currentUser.UserName!) },
                {
                    typeof(T) == typeof(SongSubmission) ? "Song" : "Chart",
                    resourceService.GetRichText<T>(submission.Id.ToString(), displayName)
                },
                {
                    "Collaboration",
                    resourceService.GetRichText<Collaboration>(collaboration.Id.ToString(),
                        templateService.GetMessage("more-info", invitee.Language)!)
                }
            });
    }

    private static FormFile LoadFile(string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        var memoryStream = new MemoryStream();

        using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            stream.CopyTo(memoryStream);
        }

        memoryStream.Position = 0;

        var formFile = new FormFile(memoryStream, 0, fileInfo.Length, fileInfo.Name, fileInfo.Name)
        {
            Headers = new HeaderDictionary(), ContentType = "application/octet-stream"
        };

        return formFile;
    }

    private async Task<string> SaveFile(IFormFile formFile, Guid sessionId)
    {
        var directory = Path.Combine(Path.GetTempPath(), $"PZSubmissionSaves{DateTimeOffset.UtcNow:yyyyMMdd}");
        if (!Path.Exists(directory)) Directory.CreateDirectory(directory);

        var filePath = Path.Combine(directory, $"{sessionId}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");
        await using var fileStream = new FileStream(filePath, FileMode.Create);
        var buffer = new byte[8192];
        var totalBytes = formFile.Length;
        long bytesRead = 0;
        int currentBlockSize;

        var formFileStream = formFile.OpenReadStream();
        while ((currentBlockSize = await formFileStream.ReadAsync(buffer)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, currentBlockSize));
            bytesRead += currentBlockSize;

            var progress = bytesRead * 1d / totalBytes;
            await hubContext.Clients.Group(sessionId.ToString())
                .ReceiveFileProgress(SessionFileStatus.Uploading, $"Uploading {formFile.Name.ToLowerInvariant()}",
                    progress);
        }

        return filePath;
    }
}