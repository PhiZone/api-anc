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
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class AdmissionController(
    IAdmissionRepository admissionRepository,
    UserManager<User> userManager,
    IResourceService resourceService,
    ITemplateService templateService,
    INotificationService notificationService,
    IChapterRepository chapterRepository,
    ISongRepository songRepository,
    ICollectionRepository collectionRepository,
    IChartRepository chartRepository,
    ISongSubmissionRepository songSubmissionRepository,
    IFilterService filterService,
    IDtoMapper dtoMapper,
    IChartSubmissionRepository chartSubmissionRepository,
    ISubmissionService submissionService,
    IOptions<DataSettings> dataSettings) : Controller
{
    /// <summary>
    ///     Retrieves admissions received by chapters.
    /// </summary>
    /// <returns>An array of admissions.</returns>
    /// <response code="200">Returns an array of admissions.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    [HttpGet("chapters")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK,
        Type = typeof(ResponseDto<IEnumerable<AdmissionDto<ChapterDto, SongDto>>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetChapterAdmissions([FromQuery] ArrayRequestDto dto,
        [FromQuery] AdmissionFilterDto? filterDto = null, [FromQuery] bool all = false)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!resourceService.HasPermission(currentUser, UserRole.Member))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        var isModerator = resourceService.HasPermission(currentUser, UserRole.Moderator);

        dto.PerPage = dto.PerPage > 0 && dto.PerPage < dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? dataSettings.Value.PaginationPerPage : dataSettings.Value.PaginationMaxPerPage;
        dto.Page = dto.Page > 1 ? dto.Page : 1;
        var position = dto.PerPage * (dto.Page - 1);
        var predicateExpr = await filterService.Parse(filterDto, dto.Predicate, currentUser,
            admission => admission.AdmitterType == AdmitterType.Chapter && ((isModerator && all) ||
                                                                            admission.RequesteeId == currentUser.Id ||
                                                                            admission.RequesterId == currentUser.Id));
        var admissions =
            await admissionRepository.GetAdmissionsAsync(dto.Order, dto.Desc, position, dto.PerPage, predicateExpr);
        var total = await admissionRepository.CountAdmissionsAsync(predicateExpr);
        var list = new List<AdmissionDto<ChapterDto, SongDto>>();

        foreach (var admission in admissions)
            list.Add(await dtoMapper.MapChapterAdmissionAsync<ChapterDto, SongDto>(admission, currentUser));

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
    ///     Retrieves admissions received by collections.
    /// </summary>
    /// <returns>An array of admissions.</returns>
    /// <response code="200">Returns an array of admissions.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    [HttpGet("collections")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK,
        Type = typeof(ResponseDto<IEnumerable<AdmissionDto<CollectionDto, ChartDto>>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetCollectionAdmissions([FromQuery] ArrayRequestDto dto,
        [FromQuery] AdmissionFilterDto? filterDto = null, [FromQuery] bool all = false)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!resourceService.HasPermission(currentUser, UserRole.Member))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        var isModerator = resourceService.HasPermission(currentUser, UserRole.Moderator);

        dto.PerPage = dto.PerPage > 0 && dto.PerPage < dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? dataSettings.Value.PaginationPerPage : dataSettings.Value.PaginationMaxPerPage;
        dto.Page = dto.Page > 1 ? dto.Page : 1;
        var position = dto.PerPage * (dto.Page - 1);
        var predicateExpr = await filterService.Parse(filterDto, dto.Predicate, currentUser,
            admission => admission.AdmitterType == AdmitterType.Collection && ((isModerator && all) ||
                                                                               admission.RequesteeId ==
                                                                               currentUser.Id ||
                                                                               admission.RequesterId ==
                                                                               currentUser.Id));
        var admissions =
            await admissionRepository.GetAdmissionsAsync(dto.Order, dto.Desc, position, dto.PerPage, predicateExpr);
        var total = await admissionRepository.CountAdmissionsAsync(predicateExpr);
        var list = new List<AdmissionDto<CollectionDto, ChartDto>>();

        foreach (var admission in admissions)
            list.Add(await dtoMapper.MapCollectionAdmissionAsync<CollectionDto, ChartDto>(admission, currentUser));

        return Ok(new ResponseDto<IEnumerable<AdmissionDto<CollectionDto, ChartDto>>>
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
    ///     Retrieves admissions received by songs.
    /// </summary>
    /// <returns>An array of admissions.</returns>
    /// <response code="200">Returns an array of admissions.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    [HttpGet("songs")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK,
        Type = typeof(ResponseDto<IEnumerable<AdmissionDto<SongDto, ChartSubmissionDto>>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetSongAdmissions([FromQuery] ArrayRequestDto dto,
        [FromQuery] AdmissionFilterDto? filterDto = null, [FromQuery] bool all = false)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!resourceService.HasPermission(currentUser, UserRole.Member))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        var isModerator = resourceService.HasPermission(currentUser, UserRole.Moderator);

        dto.PerPage = dto.PerPage > 0 && dto.PerPage < dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? dataSettings.Value.PaginationPerPage : dataSettings.Value.PaginationMaxPerPage;
        dto.Page = dto.Page > 1 ? dto.Page : 1;
        var position = dto.PerPage * (dto.Page - 1);
        var predicateExpr = await filterService.Parse(filterDto, dto.Predicate, currentUser,
            admission => admission.AdmitterType == AdmitterType.Song && ((isModerator && all) ||
                                                                         admission.RequesteeId == currentUser.Id ||
                                                                         admission.RequesterId == currentUser.Id));
        var admissions =
            await admissionRepository.GetAdmissionsAsync(dto.Order, dto.Desc, position, dto.PerPage, predicateExpr);
        var total = await admissionRepository.CountAdmissionsAsync(predicateExpr);
        var list = new List<AdmissionDto<SongDto, ChartSubmissionDto>>();

        foreach (var admission in admissions)
            list.Add(await dtoMapper.MapSongAdmissionAsync<SongDto, ChartSubmissionDto>(admission, currentUser));

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
    ///     Retrieves admissions received by song submissions.
    /// </summary>
    /// <returns>An array of admissions.</returns>
    /// <response code="200">Returns an array of admissions.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    [HttpGet("songSubmissions")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK,
        Type = typeof(ResponseDto<IEnumerable<AdmissionDto<SongSubmissionDto, ChartSubmissionDto>>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetSongSubmissionAdmissions([FromQuery] ArrayRequestDto dto,
        [FromQuery] AdmissionFilterDto? filterDto = null, [FromQuery] bool all = false)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!resourceService.HasPermission(currentUser, UserRole.Member))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        var isModerator = resourceService.HasPermission(currentUser, UserRole.Moderator);

        dto.PerPage = dto.PerPage > 0 && dto.PerPage < dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? dataSettings.Value.PaginationPerPage : dataSettings.Value.PaginationMaxPerPage;
        dto.Page = dto.Page > 1 ? dto.Page : 1;
        var position = dto.PerPage * (dto.Page - 1);
        var predicateExpr = await filterService.Parse(filterDto, dto.Predicate, currentUser,
            admission => admission.AdmitterType == AdmitterType.SongSubmission && ((isModerator && all) ||
                admission.RequesteeId == currentUser.Id || admission.RequesterId == currentUser.Id));
        var admissions =
            await admissionRepository.GetAdmissionsAsync(dto.Order, dto.Desc, position, dto.PerPage, predicateExpr);
        var total = await admissionRepository.CountAdmissionsAsync(predicateExpr);
        var list = new List<AdmissionDto<SongSubmissionDto, ChartSubmissionDto>>();

        foreach (var admission in admissions)
            list.Add(await dtoMapper.MapSongSubmissionAdmissionAsync<SongSubmissionDto, ChartSubmissionDto>(admission,
                currentUser));

        return Ok(new ResponseDto<IEnumerable<AdmissionDto<SongSubmissionDto, ChartSubmissionDto>>>
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
    ///     Retrieves an admission received by a chapter.
    /// </summary>
    /// <param name="chapterId">A chapter's ID.</param>
    /// <param name="songId">A song's ID.</param>
    /// <returns>An admission.</returns>
    /// <response code="200">Returns an admission.</response>
    /// <response code="304">
    ///     When the resource has not been updated since last retrieval. Requires <c>If-None-Match</c>.
    /// </response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified song, chapter, or admission is not found.</response>
    [HttpGet("chapters/{chapterId:guid}/{songId:guid}")]
    [ServiceFilter(typeof(ETagFilter))]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<AdmissionDto<ChapterDto, SongDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetChapterAdmission([FromRoute] Guid chapterId, [FromRoute] Guid songId)
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

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!resourceService.HasPermission(currentUser, UserRole.Member))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        var song = await songRepository.GetSongAsync(songId);
        var chapter = await chapterRepository.GetChapterAsync(chapterId);
        var admission = await admissionRepository.GetAdmissionAsync(chapterId, songId);
        if (!(song.OwnerId == currentUser.Id || chapter.OwnerId == currentUser.Id ||
              resourceService.HasPermission(currentUser, UserRole.Moderator)) &&
            admission.Status != RequestStatus.Approved)
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        var dto = await dtoMapper.MapChapterAdmissionAsync<ChapterDto, SongDto>(admission, currentUser);

        return Ok(new ResponseDto<AdmissionDto<ChapterDto, SongDto>>
        {
            Status = ResponseStatus.Ok, Code = ResponseCodes.Ok, Data = dto
        });
    }

    /// <summary>
    ///     Retrieves an admission received by a collection.
    /// </summary>
    /// <param name="collectionId">A collection's ID.</param>
    /// <param name="chartId">A chart's ID.</param>
    /// <returns>An admission.</returns>
    /// <response code="200">Returns an admission.</response>
    /// <response code="304">
    ///     When the resource has not been updated since last retrieval. Requires <c>If-None-Match</c>.
    /// </response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified chart, collection, or admission is not found.</response>
    [HttpGet("collections/{collectionId:guid}/{chartId:guid}")]
    [ServiceFilter(typeof(ETagFilter))]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<AdmissionDto<CollectionDto, ChartDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetCollectionAdmission([FromRoute] Guid collectionId, [FromRoute] Guid chartId)
    {
        if (!await chartRepository.ChartExistsAsync(chartId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        if (!await collectionRepository.CollectionExistsAsync(collectionId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentNotFound
            });

        if (!await admissionRepository.AdmissionExistsAsync(collectionId, chartId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.RelationNotFound
            });

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!resourceService.HasPermission(currentUser, UserRole.Member))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        var chart = await chartRepository.GetChartAsync(chartId);
        var collection = await collectionRepository.GetCollectionAsync(collectionId);
        var admission = await admissionRepository.GetAdmissionAsync(collectionId, chartId);
        if (!(chart.OwnerId == currentUser.Id || collection.OwnerId == currentUser.Id ||
              resourceService.HasPermission(currentUser, UserRole.Moderator)) &&
            admission.Status != RequestStatus.Approved)
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        var dto = await dtoMapper.MapCollectionAdmissionAsync<CollectionDto, ChartDto>(admission, currentUser);

        return Ok(new ResponseDto<AdmissionDto<CollectionDto, ChartDto>>
        {
            Status = ResponseStatus.Ok, Code = ResponseCodes.Ok, Data = dto
        });
    }

    /// <summary>
    ///     Retrieves an admission received by a song.
    /// </summary>
    /// <param name="songId">A song's ID.</param>
    /// <param name="chartSubmissionId">A chart submission's ID.</param>
    /// <returns>An admission.</returns>
    /// <response code="200">Returns an admission.</response>
    /// <response code="304">
    ///     When the resource has not been updated since last retrieval. Requires <c>If-None-Match</c>.
    /// </response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified chart submission, song, or admission is not found.</response>
    [HttpGet("songs/{songId:guid}/{chartSubmissionId:guid}")]
    [ServiceFilter(typeof(ETagFilter))]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK,
        Type = typeof(ResponseDto<AdmissionDto<SongDto, ChartSubmissionDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetSongAdmission([FromRoute] Guid songId, [FromRoute] Guid chartSubmissionId)
    {
        if (!await songRepository.SongExistsAsync(songId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentNotFound
            });

        if (!await chartSubmissionRepository.ChartSubmissionExistsAsync(chartSubmissionId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        if (!await admissionRepository.AdmissionExistsAsync(songId, chartSubmissionId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.RelationNotFound
            });

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!resourceService.HasPermission(currentUser, UserRole.Member))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        var chart = await chartSubmissionRepository.GetChartSubmissionAsync(chartSubmissionId);
        var song = await songRepository.GetSongAsync(songId);
        var admission = await admissionRepository.GetAdmissionAsync(songId, chartSubmissionId);
        if (!(chart.OwnerId == currentUser.Id || song.OwnerId == currentUser.Id ||
              resourceService.HasPermission(currentUser, UserRole.Moderator)) &&
            admission.Status != RequestStatus.Approved)
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        var dto = await dtoMapper.MapSongAdmissionAsync<SongDto, ChartSubmissionDto>(admission, currentUser);

        return Ok(new ResponseDto<AdmissionDto<SongDto, ChartSubmissionDto>>
        {
            Status = ResponseStatus.Ok, Code = ResponseCodes.Ok, Data = dto
        });
    }

    /// <summary>
    ///     Retrieves an admission received by a song submission.
    /// </summary>
    /// <param name="songSubmissionId">A song submission's ID.</param>
    /// <param name="chartSubmissionId">A chart submission's ID.</param>
    /// <returns>An admission.</returns>
    /// <response code="200">Returns an admission.</response>
    /// <response code="304">
    ///     When the resource has not been updated since last retrieval. Requires <c>If-None-Match</c>.
    /// </response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified chart submission, song submission, or admission is not found.</response>
    [HttpGet("songSubmissions/{songSubmissionId:guid}/{chartSubmissionId:guid}")]
    [ServiceFilter(typeof(ETagFilter))]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK,
        Type = typeof(ResponseDto<AdmissionDto<SongSubmissionDto, ChartSubmissionDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetSongSubmissionAdmission([FromRoute] Guid songSubmissionId,
        [FromRoute] Guid chartSubmissionId)
    {
        if (!await songSubmissionRepository.SongSubmissionExistsAsync(songSubmissionId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentNotFound
            });

        if (!await chartSubmissionRepository.ChartSubmissionExistsAsync(chartSubmissionId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        if (!await admissionRepository.AdmissionExistsAsync(songSubmissionId, chartSubmissionId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.RelationNotFound
            });

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!resourceService.HasPermission(currentUser, UserRole.Member))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        var chart = await chartSubmissionRepository.GetChartSubmissionAsync(chartSubmissionId);
        var songSubmission = await songSubmissionRepository.GetSongSubmissionAsync(songSubmissionId);
        var admission = await admissionRepository.GetAdmissionAsync(songSubmissionId, chartSubmissionId);
        if (!(chart.OwnerId == currentUser.Id || songSubmission.OwnerId == currentUser.Id ||
              resourceService.HasPermission(currentUser, UserRole.Moderator)) &&
            admission.Status != RequestStatus.Approved)
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        var dto = await dtoMapper.MapSongSubmissionAdmissionAsync<SongSubmissionDto, ChartSubmissionDto>(admission,
            currentUser);

        return Ok(new ResponseDto<AdmissionDto<SongSubmissionDto, ChartSubmissionDto>>
        {
            Status = ResponseStatus.Ok, Code = ResponseCodes.Ok, Data = dto
        });
    }

    /// <summary>
    ///     Removes an admission received by a chapter.
    /// </summary>
    /// <param name="chapterId">A chapter's ID.</param>
    /// <param name="songId">A song's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified admission is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpDelete("chapters/{chapterId:guid}/{songId:guid}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> RemoveChapterAdmission([FromRoute] Guid chapterId, [FromRoute] Guid songId)
    {
        if (!await chapterRepository.ChapterExistsAsync(chapterId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentNotFound
            });

        if (!await songRepository.SongExistsAsync(songId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        if (!await admissionRepository.AdmissionExistsAsync(chapterId, songId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.AlreadyDone
            });

        var admission = await admissionRepository.GetAdmissionAsync(chapterId, songId);
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!resourceService.HasPermission(currentUser, UserRole.Member))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        if (!(admission.RequesterId == currentUser.Id ||
              resourceService.HasPermission(currentUser, UserRole.Administrator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (admission.Status == RequestStatus.Approved)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidOperation
            });

        if (!await admissionRepository.RemoveAdmissionAsync(chapterId, songId))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return NoContent();
    }

    /// <summary>
    ///     Removes an admission received by a collection.
    /// </summary>
    /// <param name="collectionId">A collection's ID.</param>
    /// <param name="chartId">A chart's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified admission is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpDelete("collections/{collectionId:guid}/{chartId:guid}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> RemoveCollectionAdmission([FromRoute] Guid collectionId, [FromRoute] Guid chartId)
    {
        if (!await collectionRepository.CollectionExistsAsync(collectionId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentNotFound
            });

        if (!await chartRepository.ChartExistsAsync(chartId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        if (!await admissionRepository.AdmissionExistsAsync(collectionId, chartId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.AlreadyDone
            });

        var admission = await admissionRepository.GetAdmissionAsync(collectionId, chartId);
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!resourceService.HasPermission(currentUser, UserRole.Member))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        if (!(admission.RequesterId == currentUser.Id ||
              resourceService.HasPermission(currentUser, UserRole.Administrator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (admission.Status == RequestStatus.Approved)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidOperation
            });

        if (!await admissionRepository.RemoveAdmissionAsync(collectionId, chartId))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return NoContent();
    }

    /// <summary>
    ///     Reviews an admission received by a chapter.
    /// </summary>
    /// <param name="chapterId">A chapter's ID.</param>
    /// <param name="songId">A song's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified admission is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPost("chapters/{chapterId:guid}/{songId:guid}/review")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> ReviewChapterAdmission([FromRoute] Guid chapterId, [FromRoute] Guid songId,
        [FromBody] RequestReviewDto dto)
    {
        if (!await chapterRepository.ChapterExistsAsync(chapterId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentNotFound
            });

        if (!await songRepository.SongExistsAsync(songId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
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
        if (!resourceService.HasPermission(currentUser, UserRole.Member))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        if (!(admission.RequesteeId == currentUser.Id ||
              resourceService.HasPermission(currentUser, UserRole.Moderator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        string key;
        if (dto.Approve)
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

        var requester = (await userManager.FindByIdAsync(admission.RequesterId.ToString()))!;
        var requestee = (await userManager.FindByIdAsync(admission.RequesteeId.ToString()))!;
        await notificationService.Notify(requester, requestee, NotificationType.Requests, key,
            new Dictionary<string, string>
            {
                {
                    "User", resourceService.GetRichText<User>(admission.RequesteeId.ToString(), requestee.UserName!)
                },
                {
                    "Admission",
                    resourceService.GetComplexRichText("ChapterAdmission", admission.AdmitterId.ToString(),
                        admission.AdmitteeId.ToString(), templateService.GetMessage("more-info", requester.Language)!)
                }
            });

        return NoContent();
    }

    /// <summary>
    ///     Reviews an admission received by a collection.
    /// </summary>
    /// <param name="collectionId">A collection's ID.</param>
    /// <param name="chartId">A chart's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified admission is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPost("collections/{collectionId:guid}/{chartId:guid}/review")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> ReviewCollectionAdmission([FromRoute] Guid collectionId, [FromRoute] Guid chartId,
        [FromBody] RequestReviewDto dto)
    {
        if (!await collectionRepository.CollectionExistsAsync(collectionId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentNotFound
            });

        if (!await chartRepository.ChartExistsAsync(chartId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        if (!await admissionRepository.AdmissionExistsAsync(collectionId, chartId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var admission = await admissionRepository.GetAdmissionAsync(collectionId, chartId);
        if (admission.Status != RequestStatus.Waiting)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidOperation
            });

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!resourceService.HasPermission(currentUser, UserRole.Member))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        if (!(admission.RequesteeId == currentUser.Id ||
              resourceService.HasPermission(currentUser, UserRole.Moderator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        string key;
        if (dto.Approve)
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

        var requester = (await userManager.FindByIdAsync(admission.RequesterId.ToString()))!;
        var requestee = (await userManager.FindByIdAsync(admission.RequesteeId.ToString()))!;
        await notificationService.Notify(requester, requestee, NotificationType.Requests, key,
            new Dictionary<string, string>
            {
                {
                    "User", resourceService.GetRichText<User>(admission.RequesteeId.ToString(), requestee.UserName!)
                },
                {
                    "Admission",
                    resourceService.GetComplexRichText("CollectionAdmission", admission.AdmitterId.ToString(),
                        admission.AdmitteeId.ToString(), templateService.GetMessage("more-info", requester.Language)!)
                }
            });

        return NoContent();
    }

    /// <summary>
    ///     Reviews an admission received by a song.
    /// </summary>
    /// <param name="songId">A song's ID.</param>
    /// <param name="chartId">A chart's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified admission is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPost("songs/{songId:guid}/{chartId:guid}/review")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> ReviewSongAdmission([FromRoute] Guid songId, [FromRoute] Guid chartId,
        [FromBody] RequestReviewDto dto)
    {
        if (!await songRepository.SongExistsAsync(songId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentNotFound
            });

        if (!await chartSubmissionRepository.ChartSubmissionExistsAsync(chartId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
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
        if (!resourceService.HasPermission(currentUser, UserRole.Member))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        if (!(admission.RequesteeId == currentUser.Id ||
              resourceService.HasPermission(currentUser, UserRole.Administrator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        var chart = await chartSubmissionRepository.GetChartSubmissionAsync(chartId);
        string key;
        if (dto.Approve)
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

        var requester = (await userManager.FindByIdAsync(admission.RequesterId.ToString()))!;
        var requestee = (await userManager.FindByIdAsync(admission.RequesteeId.ToString()))!;
        await notificationService.Notify(requester, requestee, NotificationType.Requests, key,
            new Dictionary<string, string>
            {
                {
                    "User", resourceService.GetRichText<User>(admission.RequesteeId.ToString(), requestee.UserName!)
                },
                {
                    "Admission",
                    resourceService.GetComplexRichText("SongAdmission", admission.AdmitterId.ToString(),
                        admission.AdmitteeId.ToString(), templateService.GetMessage("more-info", requester.Language)!)
                }
            });

        return NoContent();
    }

    /// <summary>
    ///     Reviews an admission received by a song submission.
    /// </summary>
    /// <param name="songSubmissionId">A song submission's ID.</param>
    /// <param name="chartId">A chart's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified admission is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPost("songSubmissions/{songSubmissionId:guid}/{chartId:guid}/review")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> ReviewSongSubmissionAdmission([FromRoute] Guid songSubmissionId,
        [FromRoute] Guid chartId, [FromBody] RequestReviewDto dto)
    {
        if (!await songSubmissionRepository.SongSubmissionExistsAsync(songSubmissionId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentNotFound
            });

        if (!await chartSubmissionRepository.ChartSubmissionExistsAsync(chartId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        if (!await admissionRepository.AdmissionExistsAsync(songSubmissionId, chartId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var admission = await admissionRepository.GetAdmissionAsync(songSubmissionId, chartId);
        if (admission.Status != RequestStatus.Waiting)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidOperation
            });

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!resourceService.HasPermission(currentUser, UserRole.Member))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        if (!(admission.RequesteeId == currentUser.Id ||
              resourceService.HasPermission(currentUser, UserRole.Moderator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        var chart = await chartSubmissionRepository.GetChartSubmissionAsync(chartId);
        string key;
        if (dto.Approve)
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

        var requester = (await userManager.FindByIdAsync(admission.RequesterId.ToString()))!;
        var requestee = (await userManager.FindByIdAsync(admission.RequesteeId.ToString()))!;
        await notificationService.Notify(requester, requestee, NotificationType.Requests, key,
            new Dictionary<string, string>
            {
                {
                    "User", resourceService.GetRichText<User>(admission.RequesteeId.ToString(), requestee.UserName!)
                },
                {
                    "Admission",
                    resourceService.GetComplexRichText("SongSubmissionAdmission", admission.AdmitterId.ToString(),
                        admission.AdmitteeId.ToString(), templateService.GetMessage("more-info", requester.Language)!)
                }
            });

        return NoContent();
    }
}