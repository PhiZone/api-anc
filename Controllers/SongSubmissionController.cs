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

[Route("studio/songs")]
[ApiVersion("2.0")]
[ApiController]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class SongSubmissionController(
    ISongSubmissionRepository songSubmissionRepository,
    IOptions<DataSettings> dataSettings,
    UserManager<User> userManager,
    IFilterService filterService,
    IFileStorageService fileStorageService,
    IMapper mapper,
    IDtoMapper dtoMapper,
    ISongService songService,
    IServiceScriptRepository serviceScriptRepository,
    ISubmissionService submissionService,
    IScriptService scriptService,
    IResourceService resourceService,
    IUserRepository userRepository,
    ICollaborationRepository collaborationRepository,
    IEventDivisionRepository eventDivisionRepository,
    IEventTeamRepository eventTeamRepository,
    INotificationService notificationService,
    ITemplateService templateService,
    IFeishuService feishuService,
    ILogger<SongSubmissionController> logger,
    IMeilisearchService meilisearchService) : Controller
{
    /// <summary>
    ///     Retrieves song submissions.
    /// </summary>
    /// <returns>An array of song submissions.</returns>
    /// <response code="200">Returns an array of song submissions.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<SongSubmissionDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetSongSubmissions([FromQuery] ArrayRequestDto dto,
        [FromQuery] SongSubmissionFilterDto? filterDto = null)
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
        IEnumerable<SongSubmission> songSubmissions;
        int total;
        if (dto.Search != null)
        {
            var result = await meilisearchService.SearchAsync<SongSubmission>(dto.Search, dto.PerPage, dto.Page,
                !isVolunteer ? currentUser.Id : null);
            songSubmissions = result.Hits;
            total = result.TotalHits;
        }
        else
        {
            songSubmissions = await songSubmissionRepository.GetSongSubmissionsAsync(dto.Order, dto.Desc, position,
                dto.PerPage, predicateExpr);
            total = await songSubmissionRepository.CountSongSubmissionsAsync(predicateExpr);
        }

        var list = mapper.Map<List<SongSubmissionDto>>(songSubmissions);

        return Ok(new ResponseDto<IEnumerable<SongSubmissionDto>>
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
    ///     Retrieves a specific song submission.
    /// </summary>
    /// <param name="id">A song submission's ID.</param>
    /// <returns>A song submission.</returns>
    /// <response code="200">Returns a song submission.</response>
    /// <response code="304">
    ///     When the resource has not been updated since last retrieval. Requires <c>If-None-Match</c>.
    /// </response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified song submission is not found.</response>
    [HttpGet("{id:guid}")]
    [ServiceFilter(typeof(ETagFilter))]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<SongSubmissionDto>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status304NotModified, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetSongSubmission([FromRoute] Guid id)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!resourceService.HasPermission(currentUser, UserRole.Qualified))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        if (!await songSubmissionRepository.SongSubmissionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var songSubmission = await songSubmissionRepository.GetSongSubmissionAsync(id);
        var dto = mapper.Map<SongSubmissionDto>(songSubmission);

        return Ok(
            new ResponseDto<SongSubmissionDto> { Status = ResponseStatus.Ok, Code = ResponseCodes.Ok, Data = dto });
    }

    /// <summary>
    ///     Creates a new song submission.
    /// </summary>
    /// <returns>The ID of the song submission.</returns>
    /// <response code="201">Returns the ID of the song submission.</response>
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
    public async Task<IActionResult> CreateSongSubmission([FromForm] SongSubmissionCreationDto dto,
        [FromQuery] bool wait = false)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!resourceService.HasPermission(currentUser, UserRole.Qualified))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        (string, string, TimeSpan)? songSubmissionInfo = null;
        if (wait)
        {
            songSubmissionInfo = await songService.UploadAsync(dto.Title, dto.File);
            if (songSubmissionInfo == null)
                return BadRequest(new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidData
                });

            if (!(TimeSpan.Zero <= dto.PreviewStart && dto.PreviewStart <= dto.PreviewEnd &&
                  dto.PreviewEnd <= songSubmissionInfo.Value.Item3))
                return BadRequest(new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidTimeRelation
                });
            logger.LogInformation(LogEvents.SongInfo, "[{Now}] New song submission: {Title}",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), dto.Title);
        }
        else if (!(TimeSpan.Zero <= dto.PreviewStart && dto.PreviewStart <= dto.PreviewEnd))
        {
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidTimeRelation
            });
        }

        var illustrationUrl = (await fileStorageService.UploadImage<Song>(dto.Title, dto.Illustration, (16, 9))).Item1;
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
            DateUpdated = DateTimeOffset.UtcNow
        };

        var (eventDivision, eventTeam, response) =
            await CheckForEvent(songSubmission, currentUser, EventTaskType.PreSubmission);
        if (response != null) return response;

        if (!await songSubmissionRepository.CreateSongSubmissionAsync(songSubmission))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        foreach (var id in authors.Where(e => e != currentUser.Id).Distinct())
        {
            var invitee = await userRepository.GetUserByIdAsync(id);
            if (invitee == null)
                return BadRequest(new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.UserNotFound
                });

            await CreateCollaboration(songSubmission, invitee, null, currentUser);
        }

        if (!wait)
        {
            await songService.PublishAsync(dto.File, songSubmission.Id, true);
            logger.LogInformation(LogEvents.SongInfo, "[{Now}] Scheduled new song submission: {Title}",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), dto.Title);
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
    ///     Updates a song submission.
    /// </summary>
    /// <param name="id">A song submission's ID.</param>
    /// <param name="patchDocument">A JSON Patch Document.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified song submission is not found.</response>
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
    public async Task<IActionResult> UpdateSongSubmission([FromRoute] Guid id,
        [FromBody] JsonPatchDocument<SongSubmissionUpdateDto> patchDocument)
    {
        if (!await songSubmissionRepository.SongSubmissionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var songSubmission = await songSubmissionRepository.GetSongSubmissionAsync(id);

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if ((songSubmission.OwnerId == currentUser.Id &&
             !resourceService.HasPermission(currentUser, UserRole.Qualified)) ||
            (songSubmission.OwnerId != currentUser.Id &&
             !resourceService.HasPermission(currentUser, UserRole.Moderator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        var dto = mapper.Map<SongSubmissionUpdateDto>(songSubmission);
        patchDocument.ApplyTo(dto, ModelState);

        if (!TryValidateModel(dto))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorDetailed,
                Code = ResponseCodes.InvalidData,
                Errors = ModelErrorTranslator.Translate(ModelState)
            });

        if (!(TimeSpan.Zero <= dto.PreviewStart && dto.PreviewStart <= dto.PreviewEnd &&
              dto.PreviewEnd <= songSubmission.Duration))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidTimeRelation
            });

        if (songSubmission.OriginalityProof != null)
            if (currentUser.Id == songSubmission.OwnerId &&
                !resourceService.GetAuthorIds(dto.AuthorName).Contains(currentUser.Id))
                return BadRequest(new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidAuthorInfo
                });
        var notify = songSubmission.Status != RequestStatus.Waiting;

        songSubmission.Title = dto.Title;
        songSubmission.EditionType = dto.EditionType;
        songSubmission.Edition = dto.Edition;
        songSubmission.AuthorName = dto.AuthorName;
        songSubmission.Illustrator = dto.Illustrator;
        songSubmission.Description = dto.Description;
        songSubmission.Accessibility =
            songSubmission.OriginalityProof != null ? dto.Accessibility : Accessibility.AllowAny;
        songSubmission.Lyrics = dto.Lyrics;
        songSubmission.Bpm = dto.Bpm;
        songSubmission.MinBpm = dto.MinBpm;
        songSubmission.MaxBpm = dto.MaxBpm;
        songSubmission.Offset = dto.Offset;
        songSubmission.PreviewStart = dto.PreviewStart;
        songSubmission.PreviewEnd = dto.PreviewEnd;
        songSubmission.Tags = dto.Tags;
        songSubmission.DateUpdated = DateTimeOffset.UtcNow;
        songSubmission.Status = RequestStatus.Waiting;

        var (eventDivision, eventTeam, response) =
            await CheckForEvent(songSubmission, currentUser, EventTaskType.PreUpdateSubmission);
        if (response != null) return response;

        if (!await songSubmissionRepository.UpdateSongSubmissionAsync(songSubmission))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        if (notify) await feishuService.Notify(songSubmission, FeishuResources.ContentReviewalChat);

        if (eventDivision != null && eventTeam != null)
            await scriptService.RunEventTaskAsync(eventTeam.DivisionId, songSubmission, eventTeam.Id, currentUser,
                [EventTaskType.PostUpdateSubmission]);

        return NoContent();
    }

    /// <summary>
    ///     Updates a song submission's file.
    /// </summary>
    /// <param name="id">A song submission's ID.</param>
    /// <param name="dto">The new file.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified song submission is not found.</response>
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
    public async Task<IActionResult> UpdateSongSubmissionFile([FromRoute] Guid id, [FromForm] FileDto dto,
        [FromQuery] bool wait = false)
    {
        if (!await songSubmissionRepository.SongSubmissionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var songSubmission = await songSubmissionRepository.GetSongSubmissionAsync(id);

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if ((songSubmission.OwnerId == currentUser.Id &&
             !resourceService.HasPermission(currentUser, UserRole.Qualified)) ||
            (songSubmission.OwnerId != currentUser.Id &&
             !resourceService.HasPermission(currentUser, UserRole.Moderator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        var notify = songSubmission.Status != RequestStatus.Waiting;

        if (dto.File != null)
        {
            if (!wait)
            {
                await songService.PublishAsync(dto.File, songSubmission.Id, true);
            }
            else
            {
                var songSubmissionInfo = await songService.UploadAsync(songSubmission.Title, dto.File);
                if (songSubmissionInfo == null)
                    return BadRequest(new ResponseDto<object>
                    {
                        Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidData
                    });

                songSubmission.File = songSubmissionInfo.Value.Item1;
                songSubmission.FileChecksum = songSubmissionInfo.Value.Item2;
                songSubmission.Duration = songSubmissionInfo.Value.Item3;
                songSubmission.DateUpdated = DateTimeOffset.UtcNow;
                songSubmission.Status = RequestStatus.Waiting;

                if (songSubmission.PreviewEnd > songSubmission.Duration)
                    songSubmission.PreviewEnd = songSubmission.Duration.Value;

                if (songSubmission.PreviewStart > songSubmission.PreviewEnd)
                    songSubmission.PreviewStart = TimeSpan.Zero;
            }
        }

        var (eventDivision, eventTeam, response) =
            await CheckForEvent(songSubmission, currentUser, EventTaskType.PreUpdateSubmission);
        if (response != null) return response;

        if (!await songSubmissionRepository.UpdateSongSubmissionAsync(songSubmission))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        if (wait && notify) await feishuService.Notify(songSubmission, FeishuResources.ContentReviewalChat);

        if (eventDivision != null && eventTeam != null)
            await scriptService.RunEventTaskAsync(eventTeam.DivisionId, songSubmission, eventTeam.Id, currentUser,
                [EventTaskType.PostUpdateSubmission]);

        return NoContent();
    }

    /// <summary>
    ///     Updates a song submission's illustration.
    /// </summary>
    /// <param name="id">A song submission's ID.</param>
    /// <param name="dto">The new illustration.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified song submission is not found.</response>
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
    public async Task<IActionResult> UpdateSongSubmissionIllustration([FromRoute] Guid id, [FromForm] FileDto dto)
    {
        if (!await songSubmissionRepository.SongSubmissionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var songSubmission = await songSubmissionRepository.GetSongSubmissionAsync(id);

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if ((songSubmission.OwnerId == currentUser.Id &&
             !resourceService.HasPermission(currentUser, UserRole.Qualified)) ||
            (songSubmission.OwnerId != currentUser.Id &&
             !resourceService.HasPermission(currentUser, UserRole.Moderator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        var notify = songSubmission.Status != RequestStatus.Waiting;

        if (dto.File != null)
        {
            songSubmission.Illustration =
                (await fileStorageService.UploadImage<Song>(songSubmission.Title, dto.File, (16, 9))).Item1;
            await fileStorageService.SendUserInput(songSubmission.Illustration, "Illustration", Request, currentUser);
            songSubmission.DateUpdated = DateTimeOffset.UtcNow;
            songSubmission.Status = RequestStatus.Waiting;
        }

        var (eventDivision, eventTeam, response) =
            await CheckForEvent(songSubmission, currentUser, EventTaskType.PreUpdateSubmission);
        if (response != null) return response;

        if (!await songSubmissionRepository.UpdateSongSubmissionAsync(songSubmission))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        if (notify) await feishuService.Notify(songSubmission, FeishuResources.ContentReviewalChat);

        if (eventDivision != null && eventTeam != null)
            await scriptService.RunEventTaskAsync(eventTeam.DivisionId, songSubmission, eventTeam.Id, currentUser,
                [EventTaskType.PostUpdateSubmission]);

        return NoContent();
    }

    /// <summary>
    ///     Updates a song submission's license.
    /// </summary>
    /// <param name="id">A song submission's ID.</param>
    /// <param name="dto">The new license.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified song submission is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPatch("{id:guid}/license")]
    [Consumes("multipart/form-data")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> UpdateSongSubmissionLicense([FromRoute] Guid id, [FromForm] FileDto dto)
    {
        if (!await songSubmissionRepository.SongSubmissionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var songSubmission = await songSubmissionRepository.GetSongSubmissionAsync(id);

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if ((songSubmission.OwnerId == currentUser.Id &&
             !resourceService.HasPermission(currentUser, UserRole.Qualified)) ||
            (songSubmission.OwnerId != currentUser.Id &&
             !resourceService.HasPermission(currentUser, UserRole.Moderator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        var notify = songSubmission.Status != RequestStatus.Waiting;

        if (dto.File != null)
        {
            songSubmission.License = (await fileStorageService.Upload<Song>(songSubmission.Title, dto.File)).Item1;
            songSubmission.DateUpdated = DateTimeOffset.UtcNow;
            songSubmission.Status = RequestStatus.Waiting;
        }

        var (eventDivision, eventTeam, response) =
            await CheckForEvent(songSubmission, currentUser, EventTaskType.PreUpdateSubmission);
        if (response != null) return response;

        if (!await songSubmissionRepository.UpdateSongSubmissionAsync(songSubmission))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        if (notify) await feishuService.Notify(songSubmission, FeishuResources.ContentReviewalChat);

        if (eventDivision != null && eventTeam != null)
            await scriptService.RunEventTaskAsync(eventTeam.DivisionId, songSubmission, eventTeam.Id, currentUser,
                [EventTaskType.PostUpdateSubmission]);

        return NoContent();
    }

    /// <summary>
    ///     Removes a song submission's license.
    /// </summary>
    /// <param name="id">A song submission's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified song submission is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpDelete("{id:guid}/license")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> RemoveSongSubmissionLicense([FromRoute] Guid id)
    {
        if (!await songSubmissionRepository.SongSubmissionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var songSubmission = await songSubmissionRepository.GetSongSubmissionAsync(id);

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if ((songSubmission.OwnerId == currentUser.Id &&
             !resourceService.HasPermission(currentUser, UserRole.Qualified)) ||
            (songSubmission.OwnerId != currentUser.Id &&
             !resourceService.HasPermission(currentUser, UserRole.Moderator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        var notify = songSubmission.Status != RequestStatus.Waiting;

        songSubmission.License = null;
        songSubmission.DateUpdated = DateTimeOffset.UtcNow;
        songSubmission.Status = RequestStatus.Waiting;

        var (eventDivision, eventTeam, response) =
            await CheckForEvent(songSubmission, currentUser, EventTaskType.PreUpdateSubmission);
        if (response != null) return response;

        if (!await songSubmissionRepository.UpdateSongSubmissionAsync(songSubmission))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        if (notify) await feishuService.Notify(songSubmission, FeishuResources.ContentReviewalChat);

        if (eventDivision != null && eventTeam != null)
            await scriptService.RunEventTaskAsync(eventTeam.DivisionId, songSubmission, eventTeam.Id, currentUser,
                [EventTaskType.PostUpdateSubmission]);

        return NoContent();
    }

    /// <summary>
    ///     Updates a song submission's originality proof.
    /// </summary>
    /// <param name="id">A song submission's ID.</param>
    /// <param name="dto">The new originality proof.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified song submission is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPatch("{id:guid}/originalityProof")]
    [Consumes("multipart/form-data")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> UpdateSongSubmissionOriginalityProof([FromRoute] Guid id, [FromForm] FileDto dto)
    {
        if (!await songSubmissionRepository.SongSubmissionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var songSubmission = await songSubmissionRepository.GetSongSubmissionAsync(id);

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if ((songSubmission.OwnerId == currentUser.Id &&
             !resourceService.HasPermission(currentUser, UserRole.Qualified)) ||
            (songSubmission.OwnerId != currentUser.Id &&
             !resourceService.HasPermission(currentUser, UserRole.Moderator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        var notify = songSubmission.Status != RequestStatus.Waiting;

        if (dto.File != null)
        {
            songSubmission.OriginalityProof =
                (await fileStorageService.Upload<SongSubmission>(songSubmission.Title, dto.File)).Item1;
            songSubmission.DateUpdated = DateTimeOffset.UtcNow;
            songSubmission.Status = RequestStatus.Waiting;
        }

        var (eventDivision, eventTeam, response) =
            await CheckForEvent(songSubmission, currentUser, EventTaskType.PreUpdateSubmission);
        if (response != null) return response;

        if (!await songSubmissionRepository.UpdateSongSubmissionAsync(songSubmission))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        if (notify) await feishuService.Notify(songSubmission, FeishuResources.ContentReviewalChat);

        if (eventDivision != null && eventTeam != null)
            await scriptService.RunEventTaskAsync(eventTeam.DivisionId, songSubmission, eventTeam.Id, currentUser,
                [EventTaskType.PostUpdateSubmission]);

        return NoContent();
    }

    /// <summary>
    ///     Removes a song submission's originality proof.
    /// </summary>
    /// <param name="id">A song submission's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified song submission is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpDelete("{id:guid}/originalityProof")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> RemoveSongSubmissionOriginalityProof([FromRoute] Guid id)
    {
        if (!await songSubmissionRepository.SongSubmissionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var songSubmission = await songSubmissionRepository.GetSongSubmissionAsync(id);

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if ((songSubmission.OwnerId == currentUser.Id &&
             !resourceService.HasPermission(currentUser, UserRole.Qualified)) ||
            (songSubmission.OwnerId != currentUser.Id &&
             !resourceService.HasPermission(currentUser, UserRole.Moderator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        var notify = songSubmission.Status != RequestStatus.Waiting;

        songSubmission.OriginalityProof = null;
        songSubmission.DateUpdated = DateTimeOffset.UtcNow;
        songSubmission.Status = RequestStatus.Waiting;

        var (eventDivision, eventTeam, response) =
            await CheckForEvent(songSubmission, currentUser, EventTaskType.PreUpdateSubmission);
        if (response != null) return response;

        if (!await songSubmissionRepository.UpdateSongSubmissionAsync(songSubmission))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        if (notify) await feishuService.Notify(songSubmission, FeishuResources.ContentReviewalChat);

        if (eventDivision != null && eventTeam != null)
            await scriptService.RunEventTaskAsync(eventTeam.DivisionId, songSubmission, eventTeam.Id, currentUser,
                [EventTaskType.PostUpdateSubmission]);

        return NoContent();
    }

    /// <summary>
    ///     Removes a song submission.
    /// </summary>
    /// <param name="id">A song submission's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified song submission is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpDelete("{id:guid}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> RemoveSongSubmission([FromRoute] Guid id)
    {
        if (!await songSubmissionRepository.SongSubmissionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var songSubmission = await songSubmissionRepository.GetSongSubmissionAsync(id);

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if ((songSubmission.OwnerId == currentUser.Id &&
             !resourceService.HasPermission(currentUser, UserRole.Qualified)) ||
            (songSubmission.OwnerId != currentUser.Id &&
             !resourceService.HasPermission(currentUser, UserRole.Moderator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (!await songSubmissionRepository.RemoveSongSubmissionAsync(id))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        var normalizedTags = songSubmission.Tags.Select(resourceService.Normalize);
        var eventDivisions = await eventDivisionRepository.GetEventDivisionsAsync(predicate: e =>
            e.Type == EventDivisionType.Song && e.Status == EventDivisionStatus.Started &&
            normalizedTags.Contains(e.TagName));
        if (eventDivisions.Count > 0)
        {
            var eventDivision = eventDivisions.First();
            var eventTeams = await eventTeamRepository.GetEventTeamsAsync(predicate: e =>
                e.DivisionId == eventDivision.Id &&
                e.Participations.Any(f => f.ParticipantId == songSubmission.OwnerId));

            if (eventTeams.Count > 0)
            {
                var eventTeam = eventTeams.First();
                await scriptService.RunEventTaskAsync(eventTeam.DivisionId, songSubmission, eventTeam.Id, currentUser,
                    [EventTaskType.OnDeletion]);
            }
        }

        return NoContent();
    }

    /// <summary>
    ///     Applies a specific song submission to a service script.
    /// </summary>
    /// <param name="id">A song submission's ID.</param>
    /// <returns>A service response.</returns>
    /// <response code="200">Returns a service response.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified song submission is not found.</response>
    [HttpPost("{id:guid}/useService/{serviceId:guid}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<ServiceResponseDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> ApplySongSubmissionToService([FromRoute] Guid id, [FromRoute] Guid serviceId,
        [FromBody] ServiceScriptUsageDto dto)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!resourceService.HasPermission(currentUser, UserRole.Qualified))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        if (!await songSubmissionRepository.SongSubmissionExistsAsync(id))
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
        if (service.TargetType != ServiceTargetType.SongSubmission)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidOperation
            });
        var songSubmission = await songSubmissionRepository.GetSongSubmissionAsync(id);
        var result = await scriptService.RunAsync(serviceId, songSubmission, dto.Parameters, currentUser);

        return Ok(new ResponseDto<ServiceResponseDto>
        {
            Status = ResponseStatus.Ok, Code = ResponseCodes.Ok, Data = result
        });
    }

    /// <summary>
    ///     Creates a new collaboration for a song.
    /// </summary>
    /// <returns>The ID of the collaboration.</returns>
    /// <response code="201">Returns the ID of the collaboration.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified song or author is not found.</response>
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
        if (!await songSubmissionRepository.SongSubmissionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var songSubmission = await songSubmissionRepository.GetSongSubmissionAsync(id);

        if (songSubmission.OriginalityProof == null)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidOperation
            });

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if ((songSubmission.OwnerId == currentUser.Id &&
             !resourceService.HasPermission(currentUser, UserRole.Qualified)) ||
            (songSubmission.OwnerId != currentUser.Id &&
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

        var collaboration = await CreateCollaboration(songSubmission, invitee, dto.Position, currentUser);
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
    ///     Reviews a song submission.
    /// </summary>
    /// <param name="id">A song submission's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified song submission is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPost("{id:guid}/review")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> ReviewSongSubmission([FromRoute] Guid id, [FromBody] SongSubmissionReviewDto dto)
    {
        if (!await songSubmissionRepository.SongSubmissionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        var songSubmission = await songSubmissionRepository.GetSongSubmissionAsync(id);

        if ((songSubmission.OwnerId == currentUser.Id &&
             !resourceService.HasPermission(currentUser, UserRole.Administrator)) ||
            !resourceService.HasPermission(currentUser, UserRole.Volunteer))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        songSubmission.Status = dto.Status;
        songSubmission.ReviewerId = currentUser.Id;
        songSubmission.Message = dto.Message;
        switch (songSubmission.Status)
        {
            case RequestStatus.Approved:
            {
                var song = await submissionService.ApproveSong(songSubmission, dto.IsOriginal, dto.IsHidden,
                    dto.IsLocked);
                songSubmission.RepresentationId = song.Id;
                break;
            }
            case RequestStatus.Rejected when songSubmission.Message == null:
                return BadRequest(new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorWithMessage,
                    Code = ResponseCodes.InvalidData,
                    Message = "Message is required on rejection."
                });
            case RequestStatus.Rejected:
                songSubmission.Status = RequestStatus.Rejected;
                await submissionService.RejectSong(songSubmission);
                break;
            case RequestStatus.Waiting:
            default:
                break;
        }

        if (!await songSubmissionRepository.UpdateSongSubmissionAsync(songSubmission))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

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
            e.Type == EventDivisionType.Song &&
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

    private async Task<(EventDivision?, EventTeam?, IActionResult?)> CheckForEvent(SongSubmission songSubmission,
        User currentUser, EventTaskType taskType)
    {
        var owner = (await userRepository.GetUserByIdAsync(songSubmission.OwnerId))!;
        var result = await GetEvent(songSubmission.Tags, owner);
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

    private async Task<Collaboration?> CreateCollaboration(SongSubmission songSubmission, User invitee,
        string? position, User currentUser)
    {
        var collaboration = new Collaboration
        {
            SubmissionId = songSubmission.Id,
            InviterId = currentUser.Id,
            InviteeId = invitee.Id,
            Position = position,
            DateCreated = DateTimeOffset.UtcNow
        };

        if (!await collaborationRepository.CreateCollaborationAsync(collaboration)) return null;

        await notificationService.Notify(invitee, currentUser, NotificationType.Requests, "song-collab",
            new Dictionary<string, string>
            {
                { "User", resourceService.GetRichText<User>(currentUser.Id.ToString(), currentUser.UserName!) },
                {
                    "Song",
                    resourceService.GetRichText<SongSubmission>(songSubmission.Id.ToString(),
                        songSubmission.GetDisplay())
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