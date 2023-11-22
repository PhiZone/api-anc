using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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

// ReSharper disable RouteTemplates.ActionRoutePrefixCanBeExtractedToControllerRoute

namespace PhiZoneApi.Controllers;

[Route("admissions")]
[ApiVersion("2.0")]
[ApiController]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
    Policy = "AllowAnonymous")]
public class AdmissionController(IAdmissionRepository admissionRepository, UserManager<User> userManager,
    IResourceService resourceService, ITemplateService templateService, INotificationService notificationService,
    IChapterRepository chapterRepository, ISongRepository songRepository, IFilterService filterService,
    IDtoMapper dtoMapper, IChartSubmissionRepository chartSubmissionRepository, ISubmissionService submissionService,
    IOptions<DataSettings> dataSettings) : Controller
{
    /// <summary>
    ///     Retrieves song admissions.
    /// </summary>
    /// <returns>An array of song admissions.</returns>
    /// <response code="200">Returns an array of song admissions.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    [HttpGet("songs")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK,
        Type = typeof(ResponseDto<IEnumerable<AdmissionDto<ChapterDto, SongDto>>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetSongAdmissions([FromQuery] ArrayRequestDto dto,
        [FromQuery] AdmissionFilterDto? filterDto = null, [FromQuery] bool all = false)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        var isModerator = await resourceService.HasPermission(currentUser, Roles.Moderator);
        if (!await resourceService.HasPermission(currentUser, Roles.Qualified))
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
            admission => (isModerator && all) || admission.RequesteeId == currentUser.Id ||
                         admission.RequesterId == currentUser.Id);
        var admissions =
            await admissionRepository.GetAdmissionsAsync(dto.Order, dto.Desc, position, dto.PerPage, predicateExpr);
        var total = await admissionRepository.CountAdmissionsAsync(predicateExpr);
        var list = new List<AdmissionDto<ChapterDto, SongDto>>();

        foreach (var admission in admissions)
            list.Add(await dtoMapper.MapSongAdmissionAsync<ChapterDto, SongDto>(admission, currentUser));

        return Ok(new ResponseDto<IEnumerable<AdmissionDto<ChapterDto, SongDto>>>
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
    ///     Retrieves chart admissions.
    /// </summary>
    /// <returns>An array of chart admissions.</returns>
    /// <response code="200">Returns an array of chart admissions.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    [HttpGet("charts")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK,
        Type = typeof(ResponseDto<IEnumerable<AdmissionDto<SongDto, ChartSubmissionDto>>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetChartAdmissions([FromQuery] ArrayRequestDto dto,
        [FromQuery] AdmissionFilterDto? filterDto = null, [FromQuery] bool all = false)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        var isModerator = await resourceService.HasPermission(currentUser, Roles.Moderator);
        if (!await resourceService.HasPermission(currentUser, Roles.Qualified))
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
            admission => (isModerator && all) || admission.RequesteeId == currentUser.Id ||
                         admission.RequesterId == currentUser.Id);
        var admissions =
            await admissionRepository.GetAdmissionsAsync(dto.Order, dto.Desc, position, dto.PerPage, predicateExpr);
        var total = await admissionRepository.CountAdmissionsAsync(predicateExpr);
        var list = new List<AdmissionDto<SongDto, ChartSubmissionDto>>();

        foreach (var admission in admissions)
            list.Add(await dtoMapper.MapChartAdmissionAsync<SongDto, ChartSubmissionDto>(admission, currentUser));

        return Ok(new ResponseDto<IEnumerable<AdmissionDto<SongDto, ChartSubmissionDto>>>
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
    ///     Retrieves an admission requested from a song.
    /// </summary>
    /// <param name="songId">A song's ID.</param>
    /// <param name="chapterId">A chapter's ID.</param>
    /// <returns>An admission.</returns>
    /// <response code="200">Returns an admission.</response>
    /// <response code="304">
    ///     When the resource has not been updated since last retrieval. Requires <c>If-None-Match</c>.
    /// </response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified song, chapter, or admission is not found.</response>
    [HttpGet("songs/{songId:guid}/{chapterId:guid}")]
    [ServiceFilter(typeof(ETagFilter))]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<AdmissionDto<ChapterDto, SongDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetSongAdmission([FromRoute] Guid songId, [FromRoute] Guid chapterId)
    {
        if (!await songRepository.SongExistsAsync(songId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        if (!await chapterRepository.ChapterExistsAsync(chapterId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentNotFound
            });

        if (!await admissionRepository.AdmissionExistsAsync(chapterId, songId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.RelationNotFound
            });

        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        var song = await songRepository.GetSongAsync(songId);

        var admission = await admissionRepository.GetAdmissionAsync(chapterId, songId);
        if (!(currentUser != null && (song.OwnerId == currentUser.Id ||
                                      await resourceService.HasPermission(currentUser, Roles.Moderator))) &&
            admission.Status != RequestStatus.Approved)
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        var dto = await dtoMapper.MapSongAdmissionAsync<ChapterDto, SongDto>(admission, currentUser);

        return Ok(new ResponseDto<AdmissionDto<ChapterDto, SongDto>>
        {
            Status = ResponseStatus.Ok, Code = ResponseCodes.Ok, Data = dto
        });
    }

    /// <summary>
    ///     Retrieves an admission requested from a chart.
    /// </summary>
    /// <param name="chartId">A chart's ID.</param>
    /// <param name="songId">A song's ID.</param>
    /// <returns>An admission.</returns>
    /// <response code="200">Returns an admission.</response>
    /// <response code="304">
    ///     When the resource has not been updated since last retrieval. Requires <c>If-None-Match</c>.
    /// </response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified chart, song, or admission is not found.</response>
    [HttpGet("charts/{chartId:guid}/{songId:guid}")]
    [ServiceFilter(typeof(ETagFilter))]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK,
        Type = typeof(ResponseDto<AdmissionDto<SongDto, ChartSubmissionDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetChartAdmission([FromRoute] Guid chartId, [FromRoute] Guid songId)
    {
        if (!await chartSubmissionRepository.ChartSubmissionExistsAsync(chartId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        if (!await songRepository.SongExistsAsync(songId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentNotFound
            });

        if (!await admissionRepository.AdmissionExistsAsync(songId, chartId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.RelationNotFound
            });

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        var chart = await chartSubmissionRepository.GetChartSubmissionAsync(chartId);
        if ((chart.OwnerId == currentUser.Id && !await resourceService.HasPermission(currentUser, Roles.Qualified)) ||
            (chart.OwnerId != currentUser.Id && !await resourceService.HasPermission(currentUser, Roles.Moderator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        var admission = await admissionRepository.GetAdmissionAsync(songId, chartId);
        var dto = await dtoMapper.MapChartAdmissionAsync<SongDto, ChartSubmissionDto>(admission, currentUser);

        return Ok(new ResponseDto<AdmissionDto<SongDto, ChartSubmissionDto>>
        {
            Status = ResponseStatus.Ok, Code = ResponseCodes.Ok, Data = dto
        });
    }

    /// <summary>
    ///     Removes a song admission.
    /// </summary>
    /// <param name="songId">A song's ID.</param>
    /// <param name="chapterId">A chapter's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified admission is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpDelete("songs/{songId:guid}/{chapterId:guid}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> RemoveSongAdmission([FromRoute] Guid songId, [FromRoute] Guid chapterId)
    {
        if (!await songRepository.SongExistsAsync(songId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        if (!await chapterRepository.ChapterExistsAsync(chapterId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentNotFound
            });
        if (!await admissionRepository.AdmissionExistsAsync(chapterId, songId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var admission = await admissionRepository.GetAdmissionAsync(chapterId, songId);
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if ((admission.RequesterId == currentUser.Id &&
             !await resourceService.HasPermission(currentUser, Roles.Qualified)) ||
            (admission.RequesterId != currentUser.Id &&
             !await resourceService.HasPermission(currentUser, Roles.Moderator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        if (!await admissionRepository.RemoveAdmissionAsync(chapterId, songId))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return NoContent();
    }

    /// <summary>
    ///     Removes a chart admission.
    /// </summary>
    /// <param name="chartId">A chart's ID.</param>
    /// <param name="songId">A song's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified admission is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpDelete("charts/{chartId:guid}/{songId:guid}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> RemoveChartAdmission([FromRoute] Guid chartId, [FromRoute] Guid songId)
    {
        if (!await chartSubmissionRepository.ChartSubmissionExistsAsync(chartId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        if (!await songRepository.SongExistsAsync(songId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentNotFound
            });
        if (!await admissionRepository.AdmissionExistsAsync(songId, chartId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var admission = await admissionRepository.GetAdmissionAsync(songId, chartId);
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if ((admission.RequesterId == currentUser.Id &&
             !await resourceService.HasPermission(currentUser, Roles.Qualified)) ||
            (admission.RequesterId != currentUser.Id &&
             !await resourceService.HasPermission(currentUser, Roles.Moderator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        if (!await admissionRepository.RemoveAdmissionAsync(songId, chartId))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return NoContent();
    }

    /// <summary>
    ///     Reviews a song admission.
    /// </summary>
    /// <param name="songId">A song's ID.</param>
    /// <param name="chapterId">A chapter's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified admission is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPost("songs/{songId:guid}/{chapterId:guid}/review")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> ReviewSongAdmission([FromRoute] Guid songId, [FromRoute] Guid chapterId,
        bool approve)
    {
        if (!await songRepository.SongExistsAsync(songId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        if (!await chapterRepository.ChapterExistsAsync(chapterId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentNotFound
            });
        if (!await admissionRepository.AdmissionExistsAsync(chapterId, songId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var admission = await admissionRepository.GetAdmissionAsync(chapterId, songId);
        if (admission.Status != RequestStatus.Waiting)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidOperation
            });

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if ((admission.RequesteeId == currentUser.Id &&
             !await resourceService.HasPermission(currentUser, Roles.Qualified)) ||
            (admission.RequesteeId != currentUser.Id &&
             !await resourceService.HasPermission(currentUser, Roles.Moderator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        string key;
        if (approve)
        {
            admission.Status = RequestStatus.Approved;
            key = "admission-approval";
        }
        else
        {
            admission.Status = RequestStatus.Rejected;
            key = "admission-rejection";
        }

        if (!await admissionRepository.UpdateAdmissionAsync(admission))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        await notificationService.Notify(admission.Requester, admission.Requestee, NotificationType.Requests, key,
            new Dictionary<string, string>
            {
                {
                    "User",
                    resourceService.GetRichText<User>(admission.RequesteeId.ToString(), admission.Requestee.UserName!)
                },
                {
                    "Admission",
                    resourceService.GetComplexRichText<Admission>(admission.AdmitteeId.ToString(),
                        admission.AdmitterId.ToString(),
                        templateService.GetMessage("more-info", admission.Requestee.Language)!)
                }
            });

        return NoContent();
    }

    /// <summary>
    ///     Reviews a chart admission.
    /// </summary>
    /// <param name="chartId">A chart's ID.</param>
    /// <param name="songId">A song's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified admission is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPost("charts/{chartId:guid}/{songId:guid}/review")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> ReviewChartAdmission([FromRoute] Guid chartId, [FromRoute] Guid songId,
        bool approve)
    {
        if (!await chartSubmissionRepository.ChartSubmissionExistsAsync(chartId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        if (!await songRepository.SongExistsAsync(songId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentNotFound
            });
        if (!await admissionRepository.AdmissionExistsAsync(songId, chartId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var admission = await admissionRepository.GetAdmissionAsync(songId, chartId);
        if (admission.Status != RequestStatus.Waiting)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidOperation
            });

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if ((admission.RequesteeId == currentUser.Id &&
             !await resourceService.HasPermission(currentUser, Roles.Qualified)) ||
            (admission.RequesteeId != currentUser.Id &&
             !await resourceService.HasPermission(currentUser, Roles.Moderator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        var chart = await chartSubmissionRepository.GetChartSubmissionAsync(chartId);
        string key;
        if (approve)
        {
            admission.Status = RequestStatus.Approved;
            key = "admission-approval";
            chart.AdmissionStatus = RequestStatus.Approved;
            if (chart.VolunteerStatus == RequestStatus.Approved) await submissionService.ApproveChart(chart);
        }
        else
        {
            admission.Status = RequestStatus.Rejected;
            key = "admission-rejection";
            chart.AdmissionStatus = RequestStatus.Rejected;
            chart.Status = RequestStatus.Rejected;
            await submissionService.RejectChart(chart);
        }

        if (!await admissionRepository.UpdateAdmissionAsync(admission) ||
            !await chartSubmissionRepository.UpdateChartSubmissionAsync(chart))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        await notificationService.Notify(admission.Requester, admission.Requestee, NotificationType.Requests, key,
            new Dictionary<string, string>
            {
                {
                    "User",
                    resourceService.GetRichText<User>(admission.RequesteeId.ToString(), admission.Requestee.UserName!)
                },
                {
                    "Admission",
                    resourceService.GetComplexRichText<Admission>(admission.AdmitteeId.ToString(),
                        admission.AdmitterId.ToString(),
                        templateService.GetMessage("more-info", admission.Requestee.Language)!)
                }
            });

        return NoContent();
    }
}