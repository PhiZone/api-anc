using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
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

// ReSharper disable InvertIf

// ReSharper disable RouteTemplates.ActionRoutePrefixCanBeExtractedToControllerRoute

// ReSharper disable RouteTemplates.ParameterConstraintCanBeSpecified

namespace PhiZoneApi.Controllers;

[Route("studio/charts")]
[ApiVersion("2.0")]
[ApiController]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class ChartSubmissionController(
    IChartSubmissionRepository chartSubmissionRepository,
    IOptions<DataSettings> dataSettings,
    UserManager<User> userManager,
    IFilterService filterService,
    IFileStorageService fileStorageService,
    IDtoMapper dtoMapper,
    IMapper mapper,
    ISongRepository songRepository,
    IVolunteerVoteService volunteerVoteService,
    IResourceService resourceService,
    ISongSubmissionRepository songSubmissionRepository,
    IChartService chartService,
    IVolunteerVoteRepository volunteerVoteRepository,
    IServiceScriptRepository serviceScriptRepository,
    IScriptService scriptService,
    IAdmissionRepository admissionRepository,
    INotificationService notificationService,
    ITemplateService templateService,
    IUserRepository userRepository,
    ICollaborationRepository collaborationRepository,
    IChartAssetSubmissionRepository chartAssetSubmissionRepository,
    IEventDivisionRepository eventDivisionRepository,
    IEventTeamRepository eventTeamRepository,
    ILogger<ChartSubmissionController> logger,
    IFeishuService feishuService,
    IMeilisearchService meilisearchService) : Controller
{
    /// <summary>
    ///     Retrieves chart submissions.
    /// </summary>
    /// <returns>An array of chart submissions.</returns>
    /// <response code="200">Returns an array of chart submissions.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<ChartSubmissionDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetChartSubmissions([FromQuery] ArrayRequestDto dto,
        [FromQuery] ChartSubmissionFilterDto? filterDto = null)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        var isVolunteer = resourceService.HasPermission(currentUser, UserRole.Volunteer);
        if (!resourceService.HasPermission(currentUser, UserRole.Qualified))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        dto.PerPage = dto.PerPage > 0 && dto.PerPage < dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? dataSettings.Value.PaginationPerPage : dataSettings.Value.PaginationMaxPerPage;
        dto.Page = dto.Page > 1 ? dto.Page : 1;
        var position = dto.PerPage * (dto.Page - 1);
        var predicateExpr = await filterService.Parse(filterDto, dto.Predicate, currentUser,
            submission => isVolunteer || submission.OwnerId == currentUser.Id);
        IEnumerable<ChartSubmission> chartSubmissions;
        int total;
        if (dto.Search != null)
        {
            var result = await meilisearchService.SearchAsync<ChartSubmission>(dto.Search, dto.PerPage, dto.Page,
                !isVolunteer ? currentUser.Id : null);
            var idList = result.Hits.Select(item => item.Id).ToList();
            chartSubmissions =
                (await chartSubmissionRepository.GetChartSubmissionsAsync(predicate: e => idList.Contains(e.Id),
                    currentUserId: currentUser.Id)).OrderBy(e => idList.IndexOf(e.Id));
            total = result.TotalHits;
        }
        else
        {
            chartSubmissions = await chartSubmissionRepository.GetChartSubmissionsAsync(dto.Order, dto.Desc, position,
                dto.PerPage, predicateExpr, currentUser.Id);
            total = await chartSubmissionRepository.CountChartSubmissionsAsync(predicateExpr);
        }

        var list = chartSubmissions.Select(dtoMapper.MapChartSubmission<ChartSubmissionDto>).ToList();

        return Ok(new ResponseDto<IEnumerable<ChartSubmissionDto>>
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
    ///     Retrieves a specific chart submission.
    /// </summary>
    /// <param name="id">A chart submission's ID.</param>
    /// <returns>A chart submission.</returns>
    /// <response code="200">Returns a chart submission.</response>
    /// <response code="304">
    ///     When the resource has not been updated since last retrieval. Requires <c>If-None-Match</c>.
    /// </response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified chart submission is not found.</response>
    [HttpGet("{id:guid}")]
    [ServiceFilter(typeof(ETagFilter))]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<ChartSubmissionDto>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status304NotModified, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetChartSubmission([FromRoute] Guid id)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!resourceService.HasPermission(currentUser, UserRole.Qualified))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        if (!await chartSubmissionRepository.ChartSubmissionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var chartSubmission = await chartSubmissionRepository.GetChartSubmissionAsync(id, currentUser.Id);
        var dto = dtoMapper.MapChartSubmission<ChartSubmissionDto>(chartSubmission);

        return Ok(new ResponseDto<ChartSubmissionDto>
        {
            Status = ResponseStatus.Ok, Code = ResponseCodes.Ok, Data = dto
        });
    }

    /// <summary>
    ///     Creates a new chart submission.
    /// </summary>
    /// <returns>The ID of the chart submission.</returns>
    /// <response code="201">Returns the ID of the chart submission.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(ResponseDto<CreatedResponseDto<Guid>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> CreateChartSubmission([FromForm] ChartSubmissionCreationDto dto)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!resourceService.HasPermission(currentUser, UserRole.Qualified))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
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

        var (eventDivision, eventTeam, response) = await GetEvent(dto.Tags, currentUser);
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
            DateUpdated = DateTimeOffset.UtcNow
        };

        if (eventTeam != null)
        {
            var firstFailure = await scriptService.RunEventTaskAsync(eventTeam.DivisionId, chartSubmission,
                eventTeam.Id, currentUser, [EventTaskType.PreSubmission]);

            if (firstFailure != null)
                return BadRequest(new ResponseDto<EventTaskResponseDto>
                {
                    Status = ResponseStatus.ErrorWithData, Code = ResponseCodes.InvalidData, Data = firstFailure
                });
        }

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

        foreach (var id in authors.Where(e => e != currentUser.Id).Distinct())
        {
            var invitee = await userRepository.GetUserByIdAsync(id);
            if (invitee == null)
                return BadRequest(new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.UserNotFound
                });

            await CreateCollaboration(chartSubmission, invitee, null, currentUser);
        }

        await feishuService.Notify(chartSubmission, FeishuResources.ContentReviewalChat);

        if (eventDivision != null && eventTeam != null)
            await scriptService.RunEventTaskAsync(eventTeam.DivisionId, chartSubmission, eventTeam.Id, currentUser,
                [EventTaskType.PostSubmission]);

        logger.LogInformation(LogEvents.ChartInfo, "[{Now}] New chart submission: {Title} [{Level} {Difficulty}]",
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), dto.Title ?? song?.Title ?? songSubmission!.Title, dto.Level,
            Math.Floor(dto.Difficulty));

        return StatusCode(StatusCodes.Status201Created,
            new ResponseDto<CreatedResponseDto<Guid>>
            {
                Status = ResponseStatus.Ok,
                Code = ResponseCodes.Ok,
                Data = new CreatedResponseDto<Guid> { Id = chartSubmission.Id }
            });
    }

    /// <summary>
    ///     Updates a chart submission.
    /// </summary>
    /// <param name="id">A chart submission's ID.</param>
    /// <param name="patchDocument">A JSON Patch Document.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified chart submission is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPatch("{id:guid}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> UpdateChartSubmission([FromRoute] Guid id,
        [FromBody] JsonPatchDocument<ChartSubmissionUpdateDto> patchDocument)
    {
        if (!await chartSubmissionRepository.ChartSubmissionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var chartSubmission = await chartSubmissionRepository.GetChartSubmissionAsync(id);

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if ((chartSubmission.OwnerId == currentUser.Id &&
             !resourceService.HasPermission(currentUser, UserRole.Qualified)) ||
            (chartSubmission.OwnerId != currentUser.Id &&
             !resourceService.HasPermission(currentUser, UserRole.Moderator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        var dto = mapper.Map<ChartSubmissionUpdateDto>(chartSubmission);
        patchDocument.ApplyTo(dto, ModelState);

        if (!TryValidateModel(dto))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorDetailed,
                Code = ResponseCodes.InvalidData,
                Errors = ModelErrorTranslator.Translate(ModelState)
            });

        if (currentUser.Id == chartSubmission.OwnerId &&
            !resourceService.GetAuthorIds(dto.AuthorName).Contains(currentUser.Id))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidAuthorInfo
            });
        var notify = chartSubmission.VolunteerStatus != RequestStatus.Waiting;

        chartSubmission.Title = dto.Title;
        chartSubmission.LevelType = dto.LevelType;
        chartSubmission.Level = dto.Level;
        chartSubmission.Difficulty = dto.Difficulty;
        chartSubmission.AuthorName = dto.AuthorName;
        chartSubmission.Illustrator = dto.Illustrator;
        chartSubmission.Description = dto.Description;
        chartSubmission.Accessibility = dto.Accessibility;
        chartSubmission.IsRanked = dto.IsRanked;
        chartSubmission.Tags = dto.Tags;
        chartSubmission.Status = RequestStatus.Waiting;
        chartSubmission.VolunteerStatus = RequestStatus.Waiting;
        chartSubmission.DateUpdated = DateTimeOffset.UtcNow;

        var owner = (await userRepository.GetUserByIdAsync(chartSubmission.OwnerId))!;
        var (eventDivision, eventTeam, response) =
            await CheckForEvent(chartSubmission, owner, EventTaskType.PreUpdateSubmission);
        if (response != null) return response;

        if (!await chartSubmissionRepository.UpdateChartSubmissionAsync(chartSubmission))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        if (notify) await feishuService.Notify(chartSubmission, FeishuResources.ContentReviewalChat);

        if (eventDivision != null && eventTeam != null)
            await scriptService.RunEventTaskAsync(eventTeam.DivisionId, chartSubmission, eventTeam.Id, owner,
                [EventTaskType.PostUpdateSubmission]);

        return NoContent();
    }

    /// <summary>
    ///     Updates a chart submission's file.
    /// </summary>
    /// <param name="id">A chart submission's ID.</param>
    /// <param name="dto">The new file.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified chart submission is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPatch("{id:guid}/file")]
    [Consumes("multipart/form-data")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> UpdateChartSubmissionFile([FromRoute] Guid id, [FromForm] FileDto dto)
    {
        if (!await chartSubmissionRepository.ChartSubmissionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var chartSubmission = await chartSubmissionRepository.GetChartSubmissionAsync(id);

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if ((chartSubmission.OwnerId == currentUser.Id &&
             !resourceService.HasPermission(currentUser, UserRole.Qualified)) ||
            (chartSubmission.OwnerId != currentUser.Id &&
             !resourceService.HasPermission(currentUser, UserRole.Moderator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        var owner = (await userRepository.GetUserByIdAsync(chartSubmission.OwnerId))!;
        var (eventDivision, eventTeam, response) = await GetEvent(chartSubmission.Tags, owner);
        if (response != null) return response;

        var notify = chartSubmission.VolunteerStatus != RequestStatus.Waiting;

        if (dto.File != null)
        {
            Song? song = null;
            SongSubmission? songSubmission = null;
            if (chartSubmission.SongId != null)
                song = await songRepository.GetSongAsync(chartSubmission.SongId.Value);
            else if (chartSubmission.SongSubmissionId != null)
                songSubmission =
                    await songSubmissionRepository.GetSongSubmissionAsync(chartSubmission.SongSubmissionId.Value);

            var chartSubmissionInfo = await chartService.Upload(
                chartSubmission.Title ?? (chartSubmission.SongId != null
                    ? (await songRepository.GetSongAsync(chartSubmission.SongId.Value)).Title
                    : (await songSubmissionRepository.GetSongSubmissionAsync(chartSubmission.SongSubmissionId!.Value))
                    .Title), dto.File, eventDivision is { Anonymization: true },
                eventDivision is { Anonymization: true } && (song is { IsOriginal: true } ||
                                                             songSubmission is { OriginalityProof: not null }));
            if (chartSubmissionInfo == null)
                return BadRequest(new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.UnsupportedChartFormat
                });

            chartSubmission.File = chartSubmissionInfo.Value.Item1;
            chartSubmission.FileChecksum = chartSubmissionInfo.Value.Item2;
            chartSubmission.Format = chartSubmissionInfo.Value.Item3;
            chartSubmission.NoteCount = chartSubmissionInfo.Value.Item4;
            chartSubmission.Status = RequestStatus.Waiting;
            chartSubmission.VolunteerStatus = RequestStatus.Waiting;
            chartSubmission.DateUpdated = DateTimeOffset.UtcNow;
        }

        if (eventTeam != null)
        {
            var firstFailure = await scriptService.RunEventTaskAsync(eventTeam.DivisionId, chartSubmission,
                eventTeam.Id, owner, [EventTaskType.PreUpdateSubmission]);

            if (firstFailure != null)
                return BadRequest(new ResponseDto<EventTaskResponseDto>
                {
                    Status = ResponseStatus.ErrorWithData, Code = ResponseCodes.InvalidData, Data = firstFailure
                });
        }

        if (!await chartSubmissionRepository.UpdateChartSubmissionAsync(chartSubmission))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        if (notify) await feishuService.Notify(chartSubmission, FeishuResources.ContentReviewalChat);

        if (eventDivision != null && eventTeam != null)
            await scriptService.RunEventTaskAsync(eventTeam.DivisionId, chartSubmission, eventTeam.Id, owner,
                [EventTaskType.PostUpdateSubmission]);

        return NoContent();
    }

    /// <summary>
    ///     Updates a chart submission's illustration.
    /// </summary>
    /// <param name="id">A chart submission's ID.</param>
    /// <param name="dto">The new illustration.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified chart submission is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPatch("{id:guid}/illustration")]
    [Consumes("multipart/form-data")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> UpdateChartSubmissionIllustration([FromRoute] Guid id, [FromForm] FileDto dto)
    {
        if (!await chartSubmissionRepository.ChartSubmissionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var chartSubmission = await chartSubmissionRepository.GetChartSubmissionAsync(id);

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if ((chartSubmission.OwnerId == currentUser.Id &&
             !resourceService.HasPermission(currentUser, UserRole.Qualified)) ||
            (chartSubmission.OwnerId != currentUser.Id &&
             !resourceService.HasPermission(currentUser, UserRole.Moderator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        var notify = chartSubmission.VolunteerStatus != RequestStatus.Waiting;

        if (dto.File != null)
        {
            chartSubmission.Illustration = (await fileStorageService.UploadImage<Chart>(
                chartSubmission.Title ?? (chartSubmission.SongId != null
                    ? (await songRepository.GetSongAsync(chartSubmission.SongId.Value)).Title
                    : (await songSubmissionRepository.GetSongSubmissionAsync(chartSubmission.SongSubmissionId!.Value))
                    .Title), dto.File, (16, 9))).Item1;
            await fileStorageService.SendUserInput(chartSubmission.Illustration, "Illustration", Request, currentUser);
            chartSubmission.Status = RequestStatus.Waiting;
            chartSubmission.VolunteerStatus = RequestStatus.Waiting;
            chartSubmission.DateUpdated = DateTimeOffset.UtcNow;
        }

        var owner = (await userRepository.GetUserByIdAsync(chartSubmission.OwnerId))!;
        var (eventDivision, eventTeam, response) =
            await CheckForEvent(chartSubmission, owner, EventTaskType.PreUpdateSubmission);
        if (response != null) return response;

        if (!await chartSubmissionRepository.UpdateChartSubmissionAsync(chartSubmission))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        if (notify) await feishuService.Notify(chartSubmission, FeishuResources.ContentReviewalChat);

        if (eventDivision != null && eventTeam != null)
            await scriptService.RunEventTaskAsync(eventTeam.DivisionId, chartSubmission, eventTeam.Id, owner,
                [EventTaskType.PostUpdateSubmission]);

        return NoContent();
    }

    /// <summary>
    ///     Removes a chart submission's illustration.
    /// </summary>
    /// <param name="id">A chart submission's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified chart submission is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpDelete("{id:guid}/illustration")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> RemoveChartSubmissionIllustration([FromRoute] Guid id)
    {
        if (!await chartSubmissionRepository.ChartSubmissionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var chartSubmission = await chartSubmissionRepository.GetChartSubmissionAsync(id);

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if ((chartSubmission.OwnerId == currentUser.Id &&
             !resourceService.HasPermission(currentUser, UserRole.Qualified)) ||
            (chartSubmission.OwnerId != currentUser.Id &&
             !resourceService.HasPermission(currentUser, UserRole.Moderator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        var notify = chartSubmission.VolunteerStatus != RequestStatus.Waiting;

        chartSubmission.Illustration = null;
        chartSubmission.Status = RequestStatus.Waiting;
        chartSubmission.VolunteerStatus = RequestStatus.Waiting;
        chartSubmission.DateUpdated = DateTimeOffset.UtcNow;

        var owner = (await userRepository.GetUserByIdAsync(chartSubmission.OwnerId))!;
        var (eventDivision, eventTeam, response) =
            await CheckForEvent(chartSubmission, owner, EventTaskType.PreUpdateSubmission);
        if (response != null) return response;

        if (!await chartSubmissionRepository.UpdateChartSubmissionAsync(chartSubmission))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        if (notify) await feishuService.Notify(chartSubmission, FeishuResources.ContentReviewalChat);

        if (eventDivision != null && eventTeam != null)
            await scriptService.RunEventTaskAsync(eventTeam.DivisionId, chartSubmission, eventTeam.Id, owner,
                [EventTaskType.PostUpdateSubmission]);

        return NoContent();
    }

    /// <summary>
    ///     Removes a chart submission.
    /// </summary>
    /// <param name="id">A chart submission's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified chart submission is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpDelete("{id:guid}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> RemoveChartSubmission([FromRoute] Guid id)
    {
        if (!await chartSubmissionRepository.ChartSubmissionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var chartSubmission = await chartSubmissionRepository.GetChartSubmissionAsync(id);

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if ((chartSubmission.OwnerId == currentUser.Id &&
             !resourceService.HasPermission(currentUser, UserRole.Qualified)) ||
            (chartSubmission.OwnerId != currentUser.Id &&
             !resourceService.HasPermission(currentUser, UserRole.Moderator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (!await chartSubmissionRepository.RemoveChartSubmissionAsync(id))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        var owner = (await userRepository.GetUserByIdAsync(chartSubmission.OwnerId))!;
        var normalizedTags = chartSubmission.Tags.Select(resourceService.Normalize);
        var eventDivisions = await eventDivisionRepository.GetEventDivisionsAsync(predicate: e =>
            e.Type == EventDivisionType.Chart && e.Status == EventDivisionStatus.Started &&
            normalizedTags.Contains(e.TagName));
        if (eventDivisions.Count > 0)
        {
            var eventDivision = eventDivisions.First();
            var eventTeams = await eventTeamRepository.GetEventTeamsAsync(predicate: e =>
                e.DivisionId == eventDivision.Id && e.Participations.Any(f => f.ParticipantId == owner.Id));

            if (eventTeams.Count > 0)
            {
                var eventTeam = eventTeams.First();
                await scriptService.RunEventTaskAsync(eventTeam.DivisionId, chartSubmission, eventTeam.Id, owner,
                    [EventTaskType.OnDeletion]);
            }
        }

        return NoContent();
    }

    /// <summary>
    ///     Retrieves chart submission's assets.
    /// </summary>
    /// <returns>An array of chart submission's assets.</returns>
    /// <response code="200">Returns an array of chart assets.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    [HttpGet("{id:guid}/assets")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<ChartAssetSubmissionDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetChartSubmissionAssets([FromRoute] Guid id, [FromQuery] ArrayRequestDto dto,
        [FromQuery] ChartAssetSubmissionFilterDto? filterDto = null)
    {
        if (!await chartSubmissionRepository.ChartSubmissionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentNotFound
            });

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!resourceService.HasPermission(currentUser, UserRole.Qualified))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        dto.PerPage = dto.PerPage > 0 && dto.PerPage < dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? dataSettings.Value.PaginationPerPage : dataSettings.Value.PaginationMaxPerPage * 100;
        dto.Page = dto.Page > 1 ? dto.Page : 1;
        var position = dto.PerPage * (dto.Page - 1);
        var predicateExpr = await filterService.Parse(filterDto, dto.Predicate, currentUser,
            e => e.ChartSubmissionId == id);
        var chartAssets = await chartAssetSubmissionRepository.GetChartAssetSubmissionsAsync(dto.Order, dto.Desc,
            position, dto.PerPage, predicateExpr);
        var list = mapper.Map<List<ChartAssetSubmissionDto>>(chartAssets);
        var total = await chartAssetSubmissionRepository.CountChartAssetSubmissionsAsync(predicateExpr);

        return Ok(new ResponseDto<IEnumerable<ChartAssetSubmissionDto>>
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
    ///     Retrieves a specific chart submission's asset.
    /// </summary>
    /// <param name="id">A chart submission's ID.</param>
    /// <param name="assetId">A chart submission asset's ID.</param>
    /// <returns>A chart submission's asset.</returns>
    /// <response code="200">Returns a chart submission's asset.</response>
    /// <response code="304">
    ///     When the resource has not been updated since last retrieval. Requires <c>If-None-Match</c>.
    /// </response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified chart submission or the asset is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpGet("{id:guid}/assets/{assetId:guid}")]
    [ServiceFilter(typeof(ETagFilter))]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<ChartAssetSubmissionDto>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status304NotModified, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetChartSubmissionAsset([FromRoute] Guid id, [FromRoute] Guid assetId)
    {
        if (!await chartSubmissionRepository.ChartSubmissionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentNotFound
            });

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!resourceService.HasPermission(currentUser, UserRole.Qualified))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (!await chartAssetSubmissionRepository.ChartAssetSubmissionExistsAsync(assetId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var chartAsset = await chartAssetSubmissionRepository.GetChartAssetSubmissionAsync(assetId);

        var dto = mapper.Map<ChartAssetSubmissionDto>(chartAsset);

        return Ok(new ResponseDto<ChartAssetSubmissionDto>
        {
            Status = ResponseStatus.Ok, Code = ResponseCodes.Ok, Data = dto
        });
    }

    /// <summary>
    ///     Creates a new chart submission's asset.
    /// </summary>
    /// <param name="id">A chart submission's ID.</param>
    /// <param name="dto">The new asset.</param>
    /// <returns>The ID of the chart submission's asset.</returns>
    /// <response code="201">Returns the ID of the chart submission's asset.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified chart submission is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPost("{id:guid}/assets")]
    [Consumes("multipart/form-data")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(ResponseDto<CreatedResponseDto<Guid>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> CreateChartSubmissionAsset([FromRoute] Guid id,
        [FromForm] ChartAssetCreationDto dto)
    {
        if (!await chartSubmissionRepository.ChartSubmissionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentNotFound
            });

        var chartSubmission = await chartSubmissionRepository.GetChartSubmissionAsync(id);

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if ((chartSubmission.OwnerId == currentUser.Id &&
             !resourceService.HasPermission(currentUser, UserRole.Qualified)) ||
            (chartSubmission.OwnerId != currentUser.Id &&
             !resourceService.HasPermission(currentUser, UserRole.Moderator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (await chartAssetSubmissionRepository.CountChartAssetSubmissionsAsync(e =>
                e.Name == dto.Name && e.ChartSubmissionId == id) > 0)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.NameOccupied
            });

        var chartAsset = new ChartAssetSubmission
        {
            ChartSubmissionId = id,
            Type = dto.Type,
            Name = dto.Name,
            File = (await fileStorageService.Upload<ChartAsset>(
                chartSubmission.Title ?? (chartSubmission.SongId != null
                    ? (await songRepository.GetSongAsync(chartSubmission.SongId.Value)).Title
                    : (await songSubmissionRepository.GetSongSubmissionAsync(
                        chartSubmission.SongSubmissionId!.Value)).Title), dto.File)).Item1,
            OwnerId = currentUser.Id,
            DateCreated = DateTimeOffset.UtcNow,
            DateUpdated = DateTimeOffset.UtcNow
        };

        var notify = chartSubmission.VolunteerStatus != RequestStatus.Waiting;

        chartAsset.Type = dto.Type;
        chartAsset.Name = dto.Name;
        chartAsset.DateUpdated = DateTimeOffset.UtcNow;
        chartSubmission.Status = RequestStatus.Waiting;
        chartSubmission.VolunteerStatus = RequestStatus.Waiting;
        chartSubmission.DateUpdated = DateTimeOffset.UtcNow;

        var owner = (await userRepository.GetUserByIdAsync(chartSubmission.OwnerId))!;
        var (eventDivision, eventTeam, response) =
            await CheckForEvent(chartSubmission, owner, EventTaskType.PreUpdateSubmission);
        if (response != null) return response;

        if (!await chartAssetSubmissionRepository.CreateChartAssetSubmissionAsync(chartAsset) ||
            !await chartSubmissionRepository.UpdateChartSubmissionAsync(chartSubmission))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        if (notify) await feishuService.Notify(chartSubmission, FeishuResources.ContentReviewalChat);

        if (eventDivision != null && eventTeam != null)
            await scriptService.RunEventTaskAsync(eventTeam.DivisionId, chartSubmission, eventTeam.Id, owner,
                [EventTaskType.PostUpdateSubmission]);

        return StatusCode(StatusCodes.Status201Created,
            new ResponseDto<CreatedResponseDto<Guid>>
            {
                Status = ResponseStatus.Ok,
                Code = ResponseCodes.Ok,
                Data = new CreatedResponseDto<Guid> { Id = chartAsset.Id }
            });
    }

    /// <summary>
    ///     Updates a chart submission's asset.
    /// </summary>
    /// <param name="id">A chart submission's ID.</param>
    /// <param name="assetId">A chart submission asset's ID.</param>
    /// <param name="patchDocument">A JSON Patch Document.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified chart submission or the asset is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPatch("{id:guid}/assets/{assetId:guid}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> UpdateChartSubmissionAsset([FromRoute] Guid id, [FromRoute] Guid assetId,
        [FromBody] JsonPatchDocument<ChartAssetUpdateDto> patchDocument)
    {
        if (!await chartSubmissionRepository.ChartSubmissionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentNotFound
            });

        var chartSubmission = await chartSubmissionRepository.GetChartSubmissionAsync(id);

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if ((chartSubmission.OwnerId == currentUser.Id &&
             !resourceService.HasPermission(currentUser, UserRole.Qualified)) ||
            (chartSubmission.OwnerId != currentUser.Id &&
             !resourceService.HasPermission(currentUser, UserRole.Moderator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (!await chartAssetSubmissionRepository.ChartAssetSubmissionExistsAsync(assetId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var chartAsset = await chartAssetSubmissionRepository.GetChartAssetSubmissionAsync(assetId);

        var dto = mapper.Map<ChartAssetUpdateDto>(chartAsset);

        patchDocument.ApplyTo(dto, ModelState);

        if (!TryValidateModel(dto))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorDetailed,
                Code = ResponseCodes.InvalidData,
                Errors = ModelErrorTranslator.Translate(ModelState)
            });

        if (dto.Name != chartAsset.Name && await chartAssetSubmissionRepository.CountChartAssetSubmissionsAsync(e =>
                e.Name == dto.Name && e.ChartSubmissionId == id) > 0)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.NameOccupied
            });

        var notify = chartSubmission.VolunteerStatus != RequestStatus.Waiting;

        chartAsset.Type = dto.Type;
        chartAsset.Name = dto.Name;
        chartAsset.DateUpdated = DateTimeOffset.UtcNow;
        chartSubmission.Status = RequestStatus.Waiting;
        chartSubmission.VolunteerStatus = RequestStatus.Waiting;
        chartSubmission.DateUpdated = DateTimeOffset.UtcNow;

        var owner = (await userRepository.GetUserByIdAsync(chartSubmission.OwnerId))!;
        var (eventDivision, eventTeam, response) =
            await CheckForEvent(chartSubmission, owner, EventTaskType.PreUpdateSubmission);
        if (response != null) return response;

        if (!await chartAssetSubmissionRepository.UpdateChartAssetSubmissionAsync(chartAsset) ||
            !await chartSubmissionRepository.UpdateChartSubmissionAsync(chartSubmission))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        if (notify) await feishuService.Notify(chartSubmission, FeishuResources.ContentReviewalChat);

        if (eventDivision != null && eventTeam != null)
            await scriptService.RunEventTaskAsync(eventTeam.DivisionId, chartSubmission, eventTeam.Id, owner,
                [EventTaskType.PostUpdateSubmission]);

        return NoContent();
    }

    /// <summary>
    ///     Updates the file for a chart submission's asset.
    /// </summary>
    /// <param name="id">A chart submission's ID.</param>
    /// <param name="assetId">A chart submission asset's ID.</param>
    /// <param name="dto">The new file.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified chart submission or the asset is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPatch("{id:guid}/assets/{assetId:guid}/file")]
    [Consumes("multipart/form-data")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> UpdateChartSubmissionAssetFile([FromRoute] Guid id, [FromRoute] Guid assetId,
        [FromForm] FileDto dto)
    {
        if (!await chartSubmissionRepository.ChartSubmissionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentNotFound
            });

        var chartSubmission = await chartSubmissionRepository.GetChartSubmissionAsync(id);

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if ((chartSubmission.OwnerId == currentUser.Id &&
             !resourceService.HasPermission(currentUser, UserRole.Qualified)) ||
            (chartSubmission.OwnerId != currentUser.Id &&
             !resourceService.HasPermission(currentUser, UserRole.Moderator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (!await chartAssetSubmissionRepository.ChartAssetSubmissionExistsAsync(assetId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var chartAsset = await chartAssetSubmissionRepository.GetChartAssetSubmissionAsync(assetId);
        var notify = chartSubmission.VolunteerStatus != RequestStatus.Waiting;

        if (dto.File != null)
        {
            chartAsset.File = (await fileStorageService.Upload<ChartAsset>(
                chartSubmission.Title ?? (chartSubmission.SongId != null
                    ? (await songRepository.GetSongAsync(chartSubmission.SongId.Value)).Title
                    : (await songSubmissionRepository.GetSongSubmissionAsync(chartSubmission.SongSubmissionId!.Value))
                    .Title), dto.File)).Item1;
            chartAsset.DateUpdated = DateTimeOffset.UtcNow;
            chartSubmission.Status = RequestStatus.Waiting;
            chartSubmission.VolunteerStatus = RequestStatus.Waiting;
            chartSubmission.DateUpdated = DateTimeOffset.UtcNow;
        }

        var owner = (await userRepository.GetUserByIdAsync(chartSubmission.OwnerId))!;
        var (eventDivision, eventTeam, response) =
            await CheckForEvent(chartSubmission, owner, EventTaskType.PreUpdateSubmission);
        if (response != null) return response;

        if (!await chartAssetSubmissionRepository.UpdateChartAssetSubmissionAsync(chartAsset) ||
            !await chartSubmissionRepository.UpdateChartSubmissionAsync(chartSubmission))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        if (notify) await feishuService.Notify(chartSubmission, FeishuResources.ContentReviewalChat);

        if (eventDivision != null && eventTeam != null)
            await scriptService.RunEventTaskAsync(eventTeam.DivisionId, chartSubmission, eventTeam.Id, owner,
                [EventTaskType.PostUpdateSubmission]);

        return NoContent();
    }

    /// <summary>
    ///     Removes a chart submission's asset.
    /// </summary>
    /// <param name="id">A chart submission's ID.</param>
    /// <param name="assetId">A chart submission asset's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified chart submission or the asset is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpDelete("{id:guid}/assets/{assetId:guid}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> RemoveChartSubmissionAsset([FromRoute] Guid id, [FromRoute] Guid assetId)
    {
        if (!await chartSubmissionRepository.ChartSubmissionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentNotFound
            });

        var chartSubmission = await chartSubmissionRepository.GetChartSubmissionAsync(id);

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if ((chartSubmission.OwnerId == currentUser.Id &&
             !resourceService.HasPermission(currentUser, UserRole.Qualified)) ||
            (chartSubmission.OwnerId != currentUser.Id &&
             !resourceService.HasPermission(currentUser, UserRole.Moderator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (!await chartAssetSubmissionRepository.ChartAssetSubmissionExistsAsync(assetId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var notify = chartSubmission.VolunteerStatus != RequestStatus.Waiting;

        chartSubmission.Status = RequestStatus.Waiting;
        chartSubmission.VolunteerStatus = RequestStatus.Waiting;
        chartSubmission.DateUpdated = DateTimeOffset.UtcNow;

        var owner = (await userRepository.GetUserByIdAsync(chartSubmission.OwnerId))!;
        var (eventDivision, eventTeam, response) =
            await CheckForEvent(chartSubmission, owner, EventTaskType.PreUpdateSubmission);
        if (response != null) return response;

        if (!await chartAssetSubmissionRepository.RemoveChartAssetSubmissionAsync(assetId) ||
            !await chartSubmissionRepository.UpdateChartSubmissionAsync(chartSubmission))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        if (notify) await feishuService.Notify(chartSubmission, FeishuResources.ContentReviewalChat);

        if (eventDivision != null && eventTeam != null)
            await scriptService.RunEventTaskAsync(eventTeam.DivisionId, chartSubmission, eventTeam.Id, owner,
                [EventTaskType.PostUpdateSubmission]);

        return NoContent();
    }

    /// <summary>
    ///     Applies a specific chart submission to a service script.
    /// </summary>
    /// <param name="id">A chart submission's ID.</param>
    /// <returns>A service response.</returns>
    /// <response code="200">Returns a service response.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified chart submission is not found.</response>
    [HttpPost("{id:guid}/useService/{serviceId:guid}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<ServiceResponseDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> ApplyChartSubmissionToService([FromRoute] Guid id, [FromRoute] Guid serviceId,
        [FromBody] ServiceScriptUsageDto dto)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!resourceService.HasPermission(currentUser, UserRole.Qualified))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        if (!await chartSubmissionRepository.ChartSubmissionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        if (!await serviceScriptRepository.ServiceScriptExistsAsync(serviceId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var service = await serviceScriptRepository.GetServiceScriptAsync(serviceId);
        if (service.TargetType != ServiceTargetType.ChartSubmission)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidOperation
            });
        var chartSubmission = await chartSubmissionRepository.GetChartSubmissionAsync(id);
        var result = await scriptService.RunAsync(serviceId, chartSubmission, dto.Parameters, currentUser);

        return Ok(new ResponseDto<ServiceResponseDto>
        {
            Status = ResponseStatus.Ok, Code = ResponseCodes.Ok, Data = result
        });
    }

    /// <summary>
    ///     Creates a new collaboration for a chart.
    /// </summary>
    /// <returns>The ID of the collaboration.</returns>
    /// <response code="201">Returns the ID of the collaboration.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified chart or author is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPost("{id:guid}/collaborations")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(ResponseDto<CreatedResponseDto<Guid>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> CreateCollaboration([FromRoute] Guid id, [FromBody] CollaborationCreationDto dto)
    {
        if (!await chartSubmissionRepository.ChartSubmissionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var chartSubmission = await chartSubmissionRepository.GetChartSubmissionAsync(id);

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if ((chartSubmission.OwnerId == currentUser.Id &&
             !resourceService.HasPermission(currentUser, UserRole.Qualified)) ||
            (chartSubmission.OwnerId != currentUser.Id &&
             !resourceService.HasPermission(currentUser, UserRole.Moderator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        var invitee = await userManager.FindByIdAsync(dto.InviteeId.ToString());
        if (invitee == null)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.UserNotFound
            });

        if (await collaborationRepository.CollaborationExistsAsync(id, dto.InviteeId))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.AlreadyDone
            });

        var collaboration = await CreateCollaboration(chartSubmission, invitee, dto.Position, currentUser);
        if (collaboration == null)
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return StatusCode(StatusCodes.Status201Created,
            new ResponseDto<CreatedResponseDto<Guid>>
            {
                Status = ResponseStatus.Ok,
                Code = ResponseCodes.Ok,
                Data = new CreatedResponseDto<Guid> { Id = collaboration.Id }
            });
    }

    /// <summary>
    ///     Retrieves votes from a specified chart submission.
    /// </summary>
    /// <param name="id">A chart submission's ID.</param>
    /// <returns>An array of votes.</returns>
    /// <response code="200">Returns an array of votes.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified chart submission is not found.</response>
    [HttpGet("{id:guid}/votes")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<VolunteerVoteDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetChartSubmissionVotes([FromRoute] Guid id, [FromQuery] ArrayRequestDto dto)
    {
        dto.PerPage = dto.PerPage > 0 && dto.PerPage < dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? dataSettings.Value.PaginationPerPage : dataSettings.Value.PaginationMaxPerPage;
        dto.Page = dto.Page > 1 ? dto.Page : 1;
        var position = dto.PerPage * (dto.Page - 1);

        if (!await chartSubmissionRepository.ChartSubmissionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        var chartSubmission = await chartSubmissionRepository.GetChartSubmissionAsync(id);
        if ((chartSubmission.OwnerId == currentUser.Id &&
             !resourceService.HasPermission(currentUser, UserRole.Qualified)) ||
            (chartSubmission.OwnerId != currentUser.Id &&
             !resourceService.HasPermission(currentUser, UserRole.Volunteer)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        var votes = await volunteerVoteRepository.GetVolunteerVotesAsync(dto.Order, dto.Desc, position, dto.PerPage,
            e => e.ChartId == id);
        var list = mapper.Map<List<VolunteerVoteDto>>(votes);
        var total = await volunteerVoteRepository.CountVolunteerVotesAsync(e => e.ChartId == id);

        return Ok(new ResponseDto<IEnumerable<VolunteerVoteDto>>
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
    ///     Votes a specific chart submission.
    /// </summary>
    /// <param name="id">A chart submission's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="201">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="404">When the specified chart submission is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPost("{id:guid}/votes")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status201Created, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> CreateVote([FromRoute] Guid id, [FromBody] VolunteerVoteRequestDto dto)
    {
        if (!await chartSubmissionRepository.ChartSubmissionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        var chartSubmission = await chartSubmissionRepository.GetChartSubmissionAsync(id);
        if ((chartSubmission.OwnerId == currentUser.Id &&
             !resourceService.HasPermission(currentUser, UserRole.Administrator)) ||
            (chartSubmission.OwnerId != currentUser.Id &&
             !resourceService.HasPermission(currentUser, UserRole.Volunteer)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (!await volunteerVoteService.CreateVolunteerVoteAsync(dto, chartSubmission, currentUser))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return StatusCode(StatusCodes.Status201Created);
    }

    /// <summary>
    ///     Removes the vote from a specified chart submission.
    /// </summary>
    /// <param name="id">A chart submission's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="404">When the specified chart submission is not found.</response>
    [HttpDelete("{id:guid}/votes")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> RemoveVote([FromRoute] Guid id)
    {
        if (!await chartSubmissionRepository.ChartSubmissionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        var chartSubmission = await chartSubmissionRepository.GetChartSubmissionAsync(id);
        var vote = await volunteerVoteRepository.GetVolunteerVoteAsync(chartSubmission.Id, currentUser.Id);
        if ((vote.OwnerId == currentUser.Id && !resourceService.HasPermission(currentUser, UserRole.Volunteer)) ||
            (vote.OwnerId != currentUser.Id && !resourceService.HasPermission(currentUser, UserRole.Moderator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (!await volunteerVoteService.RemoveVolunteerVoteAsync(chartSubmission, currentUser.Id))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.AlreadyDone
            });

        return NoContent();
    }

    /// <summary>
    ///     Checks for any event participation with provided tags.
    /// </summary>
    /// <returns>An event division and a team, if found.</returns>
    /// <response code="200">Returns an event division and a team, if found.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPost("checkEvent")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status200OK, Type = typeof(ResponseDto<EventParticipationInfoDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> CheckForEvent([FromBody] StringArrayDto dto)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        var result = await GetEvent(dto.Strings, currentUser);

        if (result.Item3 != null) return result.Item3;

        return Ok(new ResponseDto<EventParticipationInfoDto>
        {
            Status = ResponseStatus.Ok,
            Code = ResponseCodes.Ok,
            Data = new EventParticipationInfoDto
            {
                Division = result.Item1 != null
                    ? await dtoMapper.MapEventDivisionAsync<EventDivisionDto>(result.Item1)
                    : null,
                Team = result.Item2 != null ? dtoMapper.MapEventTeam<EventTeamDto>(result.Item2) : null
            }
        });
    }

    private async Task<(EventDivision?, EventTeam?, IActionResult?)> GetEvent(IEnumerable<string> tags,
        User currentUser)
    {
        var normalizedTags = tags.Select(resourceService.Normalize);
        var eventDivisions = await eventDivisionRepository.GetEventDivisionsAsync(predicate: e =>
            e.Type == EventDivisionType.Chart &&
            (e.Status == EventDivisionStatus.Unveiled || e.Status == EventDivisionStatus.Started) &&
            normalizedTags.Contains(e.TagName));
        if (eventDivisions.Count == 0) return (null, null, null);

        var eventDivision = eventDivisions.FirstOrDefault(e => e.Status == EventDivisionStatus.Started);
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

    private async Task<(EventDivision?, EventTeam?, IActionResult?)> CheckForEvent(ChartSubmission chartSubmission,
        User currentUser, EventTaskType taskType)
    {
        var result = await GetEvent(chartSubmission.Tags, currentUser);
        if (result.Item1 == null || result.Item2 == null || result.Item3 != null) return result;

        var eventDivision = result.Item1!;
        var eventTeam = result.Item2!;

        var firstFailure = await scriptService.RunEventTaskAsync(eventTeam.DivisionId, chartSubmission, eventTeam.Id,
            currentUser, [taskType]);

        if (firstFailure != null)
            return (eventDivision, eventTeam,
                BadRequest(new ResponseDto<EventTaskResponseDto>
                {
                    Status = ResponseStatus.ErrorWithData, Code = ResponseCodes.InvalidData, Data = firstFailure
                }));

        return (eventDivision, eventTeam, null);
    }

    private async Task<Collaboration?> CreateCollaboration(ChartSubmission chartSubmission, User invitee,
        string? position, User currentUser)
    {
        var collaboration = new Collaboration
        {
            SubmissionId = chartSubmission.Id,
            InviterId = currentUser.Id,
            InviteeId = invitee.Id,
            Position = position,
            DateCreated = DateTimeOffset.UtcNow
        };

        if (!await collaborationRepository.CreateCollaborationAsync(collaboration)) return null;

        await notificationService.Notify(invitee, currentUser, NotificationType.Requests, "chart-collab",
            new Dictionary<string, string>
            {
                { "User", resourceService.GetRichText<User>(currentUser.Id.ToString(), currentUser.UserName!) },
                {
                    "Chart",
                    resourceService.GetRichText<ChartSubmission>(chartSubmission.Id.ToString(),
                        await resourceService.GetDisplayName(chartSubmission))
                },
                {
                    "Collaboration",
                    resourceService.GetRichText<Collaboration>(collaboration.Id.ToString(),
                        templateService.GetMessage("more-info", invitee.Language)!)
                }
            });
        return collaboration;
    }
}