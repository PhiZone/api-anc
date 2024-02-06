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

// ReSharper disable RouteTemplates.ActionRoutePrefixCanBeExtractedToControllerRoute

// ReSharper disable RouteTemplates.ParameterConstraintCanBeSpecified

namespace PhiZoneApi.Controllers;

[Route("charts")]
[ApiVersion("2.0")]
[ApiController]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
    Policy = "AllowAnonymous")]
public class ChartController(
    IChartRepository chartRepository,
    IOptions<DataSettings> dataSettings,
    UserManager<User> userManager,
    IFilterService filterService,
    IFileStorageService fileStorageService,
    IDtoMapper dtoMapper,
    IMapper mapper,
    IChartService chartService,
    ISongRepository songRepository,
    ILikeRepository likeRepository,
    ILikeService likeService,
    ICommentRepository commentRepository,
    IVoteRepository voteRepository,
    IVoteService voteService,
    IAuthorshipRepository authorshipRepository,
    IResourceService resourceService,
    IChartAssetRepository chartAssetRepository,
    INotificationService notificationService,
    IRecordRepository recordRepository,
    ICollectionRepository collectionRepository,
    IAdmissionRepository admissionRepository,
    ITagRepository tagRepository,
    ITemplateService templateService,
    ILeaderboardService leaderboardService,
    ILogger<ChartController> logger,
    IMeilisearchService meilisearchService) : Controller
{
    /// <summary>
    ///     Retrieves charts.
    /// </summary>
    /// <returns>An array of charts.</returns>
    /// <response code="200">Returns an array of charts.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<ChartDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetCharts([FromQuery] ArrayRequestDto dto,
        [FromQuery] ChartFilterDto? filterDto = null, [FromQuery] ArrayTagDto? tagDto = null)
    {
        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        dto.PerPage = dto.PerPage > 0 && dto.PerPage < dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? dataSettings.Value.PaginationPerPage : dataSettings.Value.PaginationMaxPerPage;
        dto.Page = dto.Page > 1 ? dto.Page : 1;
        var position = dto.PerPage * (dto.Page - 1);
        var predicateExpr = await filterService.Parse(filterDto, dto.Predicate, currentUser,
            e => tagDto == null ||
                 ((tagDto.TagsToInclude == null || e.Tags.Any(tag =>
                      tagDto.TagsToInclude.Select(resourceService.Normalize).ToList().Contains(tag.NormalizedName))) &&
                  (tagDto.TagsToExclude == null || e.Tags.All(tag =>
                      !tagDto.TagsToExclude.Select(resourceService.Normalize).ToList().Contains(tag.NormalizedName)))));
        IEnumerable<Chart> charts;
        int total;
        if (dto.Search != null)
        {
            var result = await meilisearchService.SearchAsync<Chart>(dto.Search, dto.PerPage, dto.Page,
                showHidden: currentUser is { Role: UserRole.Administrator });
            var idList = result.Hits.Select(item => item.Id).ToList();
            charts = (await chartRepository.GetChartsAsync(["DateCreated"], [false], position, dto.PerPage,
                e => idList.Contains(e.Id), currentUser?.Id)).OrderBy(e => idList.IndexOf(e.Id));
            total = result.TotalHits;
        }
        else
        {
            charts = await chartRepository.GetChartsAsync(dto.Order, dto.Desc, position, dto.PerPage, predicateExpr,
                currentUser?.Id);
            total = await chartRepository.CountChartsAsync(predicateExpr);
        }

        var list = charts.Select(dtoMapper.MapChart<ChartDto>).ToList();

        return Ok(new ResponseDto<IEnumerable<ChartDto>>
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
    ///     Retrieves a specific chart.
    /// </summary>
    /// <param name="id">A chart's ID.</param>
    /// <returns>A chart.</returns>
    /// <response code="200">Returns a chart.</response>
    /// <response code="304">
    ///     When the resource has not been updated since last retrieval. Requires <c>If-None-Match</c>.
    /// </response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified chart is not found.</response>
    [HttpGet("{id:guid}")]
    [ServiceFilter(typeof(ETagFilter))]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<ChartDetailedDto>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status304NotModified, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetChart([FromRoute] Guid id)
    {
        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        if (!await chartRepository.ChartExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var chart = await chartRepository.GetChartAsync(id, currentUser?.Id);
        var dto = dtoMapper.MapChart<ChartDetailedDto>(chart);

        // ReSharper disable once InvertIf
        if (currentUser != null)
        {
            dto.PersonalBestScore = (await recordRepository.GetRecordsAsync(["Score"],
                    [true], 0, 1, r => r.OwnerId == currentUser.Id && r.ChartId == id)).FirstOrDefault()
                ?.Score;
            dto.PersonalBestAccuracy = (await recordRepository.GetRecordsAsync(["Accuracy"],
                    [true], 0, 1, r => r.OwnerId == currentUser.Id && r.ChartId == id)).FirstOrDefault()
                ?.Accuracy;
            dto.PersonalBestRks = (await recordRepository.GetRecordsAsync(["Rks"],
                    [true], 0, 1, r => r.OwnerId == currentUser.Id && r.ChartId == id)).FirstOrDefault()
                ?.Rks;
        }

        return Ok(new ResponseDto<ChartDetailedDto>
        {
            Status = ResponseStatus.Ok, Code = ResponseCodes.Ok, Data = dto
        });
    }

    /// <summary>
    ///     Retrieves a random chart.
    /// </summary>
    /// <returns>A random chart.</returns>
    /// <response code="200">Returns a random chart.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    [HttpGet("random")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<ChartDetailedDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetRandomChart([FromQuery] ArrayRequestDto dto,
        [FromQuery] ChartFilterDto? filterDto = null, [FromQuery] ArrayTagDto? tagDto = null)
    {
        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        var predicateExpr = await filterService.Parse(filterDto, dto.Predicate, currentUser,
            e => !e.IsHidden && (tagDto == null ||
                                 ((tagDto.TagsToInclude == null || e.Tags.Any(tag =>
                                     tagDto.TagsToInclude.Select(resourceService.Normalize)
                                         .ToList()
                                         .Contains(tag.NormalizedName))) && (tagDto.TagsToExclude == null ||
                                                                             e.Tags.All(tag =>
                                                                                 !tagDto.TagsToExclude
                                                                                     .Select(resourceService.Normalize)
                                                                                     .ToList()
                                                                                     .Contains(tag.NormalizedName))))));
        var chart = await chartRepository.GetRandomChartAsync(predicateExpr, currentUser?.Id);

        if (chart == null)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var chartDto = dtoMapper.MapChart<ChartDetailedDto>(chart);

        // ReSharper disable once InvertIf
        if (currentUser != null)
        {
            chartDto.PersonalBestScore = (await recordRepository.GetRecordsAsync(["Score"], [true], 0, 1,
                    r => r.OwnerId == currentUser.Id && r.ChartId == chart.Id)).FirstOrDefault()
                ?.Score;
            chartDto.PersonalBestAccuracy = (await recordRepository.GetRecordsAsync(["Accuracy"], [true], 0, 1,
                    r => r.OwnerId == currentUser.Id && r.ChartId == chart.Id)).FirstOrDefault()
                ?.Accuracy;
            chartDto.PersonalBestRks = (await recordRepository.GetRecordsAsync(["Rks"],
                    [true], 0, 1, r => r.OwnerId == currentUser.Id && r.ChartId == chart.Id)).FirstOrDefault()
                ?.Rks;
        }

        return Ok(new ResponseDto<ChartDetailedDto>
        {
            Status = ResponseStatus.Ok, Code = ResponseCodes.Ok, Data = chartDto
        });
    }

    /// <summary>
    ///     Creates a new chart.
    /// </summary>
    /// <returns>An empty body.</returns>
    /// <response code="201">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(void), StatusCodes.Status201Created, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> CreateChart([FromForm] ChartCreationDto dto)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!resourceService.HasPermission(currentUser, UserRole.Administrator))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (!await songRepository.SongExistsAsync(dto.SongId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentNotFound
            });

        var song = await songRepository.GetSongAsync(dto.SongId);

        var illustrationUrl = dto.Illustration != null
            ? (await fileStorageService.UploadImage<Chart>(dto.Title ?? song.Title, dto.Illustration, (16, 9))).Item1
            : null;
        if (illustrationUrl != null)
            await fileStorageService.SendUserInput(illustrationUrl, "Illustration", Request, currentUser);

        var chartInfo = dto.File != null ? await chartService.Upload(dto.Title ?? song.Title, dto.File) : null;

        if (dto.File != null && chartInfo == null)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.UnsupportedChartFormat
            });

        var chart = new Chart
        {
            Title = dto.Title,
            LevelType = dto.LevelType,
            Level = dto.Level,
            Difficulty = dto.Difficulty,
            Format = chartInfo?.Item3 ?? ChartFormat.Unsupported,
            File = chartInfo?.Item1,
            FileChecksum = chartInfo?.Item2,
            AuthorName = dto.AuthorName,
            Illustration = illustrationUrl,
            Illustrator = dto.Illustrator,
            Description = dto.Description,
            Accessibility = dto.Accessibility,
            IsHidden = dto.IsHidden,
            IsLocked = dto.IsLocked,
            IsRanked = dto.IsRanked,
            NoteCount = chartInfo?.Item4 ?? 0,
            SongId = dto.SongId,
            Owner = currentUser,
            DateCreated = DateTimeOffset.UtcNow,
            DateUpdated = DateTimeOffset.UtcNow
        };

        if (!await chartRepository.CreateChartAsync(chart))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });
        logger.LogInformation(LogEvents.ChartInfo, "[{Now}] New chart: {Title} [{Level} {Difficulty}]",
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), dto.Title ?? song.Title, dto.Level,
            Math.Floor(dto.Difficulty));

        await tagRepository.CreateTagsAsync(dto.Tags, chart);

        return StatusCode(StatusCodes.Status201Created);
    }

    /// <summary>
    ///     Updates a chart.
    /// </summary>
    /// <param name="id">A chart's ID.</param>
    /// <param name="patchDocument">A JSON Patch Document.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified chart is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPatch("{id:guid}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> UpdateChart([FromRoute] Guid id,
        [FromBody] JsonPatchDocument<ChartUpdateDto> patchDocument)
    {
        if (!await chartRepository.ChartExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var chart = await chartRepository.GetChartAsync(id);

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!resourceService.HasPermission(currentUser, UserRole.Administrator))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        var dto = mapper.Map<ChartUpdateDto>(chart);
        dto.Tags = chart.Tags.Select(tag => tag.Name).ToList();
        patchDocument.ApplyTo(dto, ModelState);

        if (!TryValidateModel(dto))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorDetailed,
                Code = ResponseCodes.InvalidData,
                Errors = ModelErrorTranslator.Translate(ModelState)
            });

        chart.Title = dto.Title;
        chart.LevelType = dto.LevelType;
        chart.Level = dto.Level;
        chart.Difficulty = dto.Difficulty;
        chart.AuthorName = dto.AuthorName;
        chart.Illustrator = dto.Illustrator;
        chart.Description = dto.Description;
        chart.Accessibility = dto.Accessibility;
        chart.IsHidden = dto.IsHidden;
        chart.IsLocked = dto.IsLocked;
        chart.IsRanked = dto.IsRanked;
        chart.SongId = dto.SongId;
        chart.DateUpdated = DateTimeOffset.UtcNow;

        if (!await chartRepository.UpdateChartAsync(chart))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        await tagRepository.CreateTagsAsync(dto.Tags, chart);

        return NoContent();
    }

    /// <summary>
    ///     Updates a chart's file.
    /// </summary>
    /// <param name="id">A chart's ID.</param>
    /// <param name="dto">The new file.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified chart is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPatch("{id:guid}/file")]
    [Consumes("multipart/form-data")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> UpdateChartFile([FromRoute] Guid id, [FromForm] FileDto dto)
    {
        if (!await chartRepository.ChartExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var chart = await chartRepository.GetChartAsync(id);

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!resourceService.HasPermission(currentUser, UserRole.Administrator))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (dto.File != null)
        {
            var chartInfo =
                await chartService.Upload(chart.Title ?? (await songRepository.GetSongAsync(chart.SongId)).Title,
                    dto.File);
            if (chartInfo == null)
                return BadRequest(new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.UnsupportedChartFormat
                });

            chart.File = chartInfo.Value.Item1;
            chart.FileChecksum = chartInfo.Value.Item2;
            chart.Format = chartInfo.Value.Item3;
            chart.NoteCount = chartInfo.Value.Item4;
            chart.DateUpdated = DateTimeOffset.UtcNow;
        }

        if (!await chartRepository.UpdateChartAsync(chart))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return NoContent();
    }

    /// <summary>
    ///     Removes a chart's file.
    /// </summary>
    /// <param name="id">A chart's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified chart is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpDelete("{id:guid}/file")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> RemoveChartFile([FromRoute] Guid id)
    {
        if (!await chartRepository.ChartExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var chart = await chartRepository.GetChartAsync(id);

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!resourceService.HasPermission(currentUser, UserRole.Administrator))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        chart.File = null;
        chart.DateUpdated = DateTimeOffset.UtcNow;

        if (!await chartRepository.UpdateChartAsync(chart))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return NoContent();
    }

    /// <summary>
    ///     Updates a chart's illustration.
    /// </summary>
    /// <param name="id">A chart's ID.</param>
    /// <param name="dto">The new illustration.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified chart is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPatch("{id:guid}/illustration")]
    [Consumes("multipart/form-data")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> UpdateChartIllustration([FromRoute] Guid id, [FromForm] FileDto dto)
    {
        if (!await chartRepository.ChartExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var chart = await chartRepository.GetChartAsync(id);

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!resourceService.HasPermission(currentUser, UserRole.Administrator))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (dto.File != null)
        {
            chart.Illustration =
                (await fileStorageService.UploadImage<Chart>(
                    chart.Title ?? (await songRepository.GetSongAsync(chart.SongId)).Title, dto.File, (16, 9))).Item1;
            await fileStorageService.SendUserInput(chart.Illustration, "Illustration", Request, currentUser);
            chart.DateUpdated = DateTimeOffset.UtcNow;
        }

        if (!await chartRepository.UpdateChartAsync(chart))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return NoContent();
    }

    /// <summary>
    ///     Removes a chart's illustration.
    /// </summary>
    /// <param name="id">A chart's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified chart is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpDelete("{id:guid}/illustration")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> RemoveChartIllustration([FromRoute] Guid id)
    {
        if (!await chartRepository.ChartExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var chart = await chartRepository.GetChartAsync(id);

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!resourceService.HasPermission(currentUser, UserRole.Administrator))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        chart.Illustration = null;
        chart.DateUpdated = DateTimeOffset.UtcNow;

        if (!await chartRepository.UpdateChartAsync(chart))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return NoContent();
    }

    /// <summary>
    ///     Removes a chart.
    /// </summary>
    /// <param name="id">A chart's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified chart is not found.</response>
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
    public async Task<IActionResult> RemoveChart([FromRoute] Guid id)
    {
        if (!await chartRepository.ChartExistsAsync(id))
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

        if (!await chartRepository.RemoveChartAsync(id))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return NoContent();
    }

    /// <summary>
    ///     Retrieves chart's assets.
    /// </summary>
    /// <returns>An array of chart's assets.</returns>
    /// <response code="200">Returns an array of chart assets.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    [HttpGet("{id:guid}/assets")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<ChartAssetDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetChartAssets([FromRoute] Guid id, [FromQuery] ArrayRequestDto dto,
        [FromQuery] ChartAssetFilterDto? filterDto = null)
    {
        if (!await chartRepository.ChartExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentNotFound
            });

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        dto.PerPage = dto.PerPage > 0 && dto.PerPage < dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? dataSettings.Value.PaginationPerPage : dataSettings.Value.PaginationMaxPerPage * 100;
        dto.Page = dto.Page > 1 ? dto.Page : 1;
        var position = dto.PerPage * (dto.Page - 1);
        var predicateExpr = await filterService.Parse(filterDto, dto.Predicate, currentUser, e => e.ChartId == id);
        var chartAssets = await chartAssetRepository.GetChartAssetsAsync(dto.Order, dto.Desc, position,
            dto.PerPage, predicateExpr);
        var list = mapper.Map<List<ChartAssetDto>>(chartAssets);
        var total = await chartAssetRepository.CountChartAssetsAsync(predicateExpr);

        return Ok(new ResponseDto<IEnumerable<ChartAssetDto>>
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
    ///     Retrieves a specific chart's asset.
    /// </summary>
    /// <param name="id">A chart's ID.</param>
    /// <param name="assetId">A chart asset's ID.</param>
    /// <returns>A chart's asset.</returns>
    /// <response code="200">Returns a chart's asset.</response>
    /// <response code="304">
    ///     When the resource has not been updated since last retrieval. Requires <c>If-None-Match</c>.
    /// </response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified chart or the asset is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpGet("{id:guid}/assets/{assetId:guid}")]
    [ServiceFilter(typeof(ETagFilter))]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<ChartAssetDto>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status304NotModified, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetChartAsset([FromRoute] Guid id, [FromRoute] Guid assetId)
    {
        if (!await chartRepository.ChartExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentNotFound
            });

        if (!await chartAssetRepository.ChartAssetExistsAsync(assetId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var chartAsset = await chartAssetRepository.GetChartAssetAsync(assetId);

        var dto = mapper.Map<ChartAssetDto>(chartAsset);

        return Ok(new ResponseDto<ChartAssetDto> { Status = ResponseStatus.Ok, Code = ResponseCodes.Ok, Data = dto });
    }

    /// <summary>
    ///     Creates a chart's asset.
    /// </summary>
    /// <param name="id">A chart's ID.</param>
    /// <param name="dto">The new asset.</param>
    /// <returns>An empty body.</returns>
    /// <response code="201">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified chart is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPost("{id:guid}/assets")]
    [Consumes("multipart/form-data")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status201Created, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> CreateChartAsset([FromRoute] Guid id, [FromForm] ChartAssetCreationDto dto)
    {
        if (!await chartRepository.ChartExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentNotFound
            });

        var chart = await chartRepository.GetChartAsync(id);

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!resourceService.HasPermission(currentUser, UserRole.Administrator))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (await chartAssetRepository.CountChartAssetsAsync(e => e.Name == dto.Name && e.ChartId == id) > 0)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.NameOccupied
            });

        var chartAsset = new ChartAsset
        {
            ChartId = id,
            Type = dto.Type,
            Name = dto.Name,
            File = (await fileStorageService.Upload<ChartAsset>(
                chart.Title ?? (await songRepository.GetSongAsync(chart.SongId)).Title, dto.File)).Item1,
            OwnerId = currentUser.Id,
            DateCreated = DateTimeOffset.UtcNow,
            DateUpdated = DateTimeOffset.UtcNow
        };

        if (!await chartAssetRepository.CreateChartAssetAsync(chartAsset) ||
            !await chartRepository.UpdateChartAsync(chart))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return StatusCode(StatusCodes.Status201Created);
    }

    /// <summary>
    ///     Updates a chart's asset.
    /// </summary>
    /// <param name="id">A chart's ID.</param>
    /// <param name="assetId">A chart asset's ID.</param>
    /// <param name="patchDocument">A JSON Patch Document.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified chart or the asset is not found.</response>
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
    public async Task<IActionResult> UpdateChartAsset([FromRoute] Guid id, [FromRoute] Guid assetId,
        [FromBody] JsonPatchDocument<ChartAssetUpdateDto> patchDocument)
    {
        if (!await chartRepository.ChartExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentNotFound
            });

        var chart = await chartRepository.GetChartAsync(id);

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!resourceService.HasPermission(currentUser, UserRole.Administrator))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (!await chartAssetRepository.ChartAssetExistsAsync(assetId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var chartAsset = await chartAssetRepository.GetChartAssetAsync(assetId);

        var dto = mapper.Map<ChartAssetUpdateDto>(chartAsset);
        patchDocument.ApplyTo(dto, ModelState);

        if (!TryValidateModel(dto))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorDetailed,
                Code = ResponseCodes.InvalidData,
                Errors = ModelErrorTranslator.Translate(ModelState)
            });

        if (dto.Name != chartAsset.Name && await chartAssetRepository.CountChartAssetsAsync(e =>
                e.Name == dto.Name && e.ChartId == id) > 0)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.NameOccupied
            });

        chartAsset.Type = dto.Type;
        chartAsset.Name = dto.Name;
        chartAsset.DateUpdated = DateTimeOffset.UtcNow;
        chart.DateUpdated = DateTimeOffset.UtcNow;

        if (!await chartAssetRepository.UpdateChartAssetAsync(chartAsset) ||
            !await chartRepository.UpdateChartAsync(chart))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return NoContent();
    }

    /// <summary>
    ///     Updates the file for a chart's asset.
    /// </summary>
    /// <param name="id">A chart's ID.</param>
    /// <param name="assetId">A chart asset's ID.</param>
    /// <param name="dto">The new file.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified chart or the asset is not found.</response>
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
    public async Task<IActionResult> UpdateChartAssetFile([FromRoute] Guid id, [FromRoute] Guid assetId,
        [FromForm] FileDto dto)
    {
        if (!await chartRepository.ChartExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentNotFound
            });

        var chart = await chartRepository.GetChartAsync(id);

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!resourceService.HasPermission(currentUser, UserRole.Administrator))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (!await chartAssetRepository.ChartAssetExistsAsync(assetId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var chartAsset = await chartAssetRepository.GetChartAssetAsync(assetId);

        if (dto.File != null)
        {
            chartAsset.File = (await fileStorageService.Upload<ChartAsset>(
                chart.Title ?? (await songRepository.GetSongAsync(chart.SongId)).Title, dto.File)).Item1;
            chartAsset.DateUpdated = DateTimeOffset.UtcNow;
            chart.DateUpdated = DateTimeOffset.UtcNow;
        }

        if (!await chartAssetRepository.UpdateChartAssetAsync(chartAsset) ||
            !await chartRepository.UpdateChartAsync(chart))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return NoContent();
    }

    /// <summary>
    ///     Removes a chart's asset.
    /// </summary>
    /// <param name="id">A chart's ID.</param>
    /// <param name="assetId">A chart asset's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified chart or the asset is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpDelete("{id:guid}/assets/{assetId:guid}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> RemoveChartAsset([FromRoute] Guid id, [FromRoute] Guid assetId)
    {
        if (!await chartRepository.ChartExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentNotFound
            });

        var chart = await chartRepository.GetChartAsync(id);

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!resourceService.HasPermission(currentUser, UserRole.Administrator))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (!await chartAssetRepository.ChartAssetExistsAsync(assetId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        chart.DateUpdated = DateTimeOffset.UtcNow;

        if (!await chartAssetRepository.RemoveChartAssetAsync(assetId) ||
            !await chartRepository.UpdateChartAsync(chart))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return NoContent();
    }

    /// <summary>
    ///     Creates a new authorship for a chart.
    /// </summary>
    /// <returns>An empty body.</returns>
    /// <response code="201">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified chart or author is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPost("{id:guid}/authorships")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(void), StatusCodes.Status201Created, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> CreateAuthorship([FromRoute] Guid id, [FromBody] AuthorshipRequestDto dto)
    {
        if (!await chartRepository.ChartExistsAsync(id))
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

        if (await userManager.FindByIdAsync(dto.AuthorId.ToString()) == null)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.UserNotFound
            });

        if (await authorshipRepository.AuthorshipExistsAsync(id, dto.AuthorId))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.AlreadyDone
            });

        var authorship = new Authorship
        {
            ResourceId = id, AuthorId = dto.AuthorId, Position = dto.Position, DateCreated = DateTimeOffset.UtcNow
        };

        if (!await authorshipRepository.CreateAuthorshipAsync(authorship))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return StatusCode(StatusCodes.Status201Created);
    }

    /// <summary>
    ///     Retrieves admissions received by collections.
    /// </summary>
    /// <param name="id">A chart's ID.</param>
    /// <returns>An array of collection admitters.</returns>
    /// <response code="200">Returns an array of collection admitters.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified chart is not found.</response>
    [HttpGet("{id:guid}/collections")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<CollectionAdmitterDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetAdmissions([FromRoute] Guid id, [FromQuery] ArrayRequestDto dto,
        [FromQuery] AdmissionFilterDto? filterDto = null)
    {
        if (!await chartRepository.ChartExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        dto.PerPage = dto.PerPage > 0 && dto.PerPage < dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? dataSettings.Value.PaginationPerPage : dataSettings.Value.PaginationMaxPerPage;
        dto.Page = dto.Page > 1 ? dto.Page : 1;
        var position = dto.PerPage * (dto.Page - 1);
        var chart = await chartRepository.GetChartAsync(id);
        var hasPermission = currentUser != null && (chart.OwnerId == currentUser.Id ||
                                                    resourceService.HasPermission(currentUser, UserRole.Moderator));
        var predicateExpr = await filterService.Parse(filterDto, dto.Predicate, currentUser,
            e => (hasPermission || e.Status == RequestStatus.Approved ||
                  (currentUser != null && e.Admitter.OwnerId == currentUser.Id)) && e.AdmitteeId == id);

        var admissions = await admissionRepository.GetAdmissionsAsync(dto.Order, dto.Desc, position,
            dto.PerPage, predicateExpr);
        var total = await admissionRepository.CountAdmissionsAsync(predicateExpr);
        var list = new List<CollectionAdmitterDto>();

        foreach (var admission in admissions)
            list.Add(await dtoMapper.MapChartCollectionAsync<CollectionAdmitterDto>(admission, currentUser));

        return Ok(new ResponseDto<IEnumerable<CollectionAdmitterDto>>
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
    ///     Retrieves an admission received by a collection.
    /// </summary>
    /// <param name="id">A chart's ID.</param>
    /// <param name="collectionId">A collection's ID.</param>
    /// <returns>An admission.</returns>
    /// <response code="200">Returns an admission.</response>
    /// <response code="304">
    ///     When the resource has not been updated since last retrieval. Requires <c>If-None-Match</c>.
    /// </response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified chart, collection, or admission is not found.</response>
    [HttpGet("{id:guid}/collections/{collectionId:guid}")]
    [ServiceFilter(typeof(ETagFilter))]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<AdmissionDto<CollectionDto, ChartDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetAdmission([FromRoute] Guid id, [FromRoute] Guid collectionId)
    {
        if (!await chartRepository.ChartExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        if (!await collectionRepository.CollectionExistsAsync(collectionId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentNotFound
            });

        if (!await admissionRepository.AdmissionExistsAsync(collectionId, id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.RelationNotFound
            });

        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        var chart = await chartRepository.GetChartAsync(id);
        var collection = await collectionRepository.GetCollectionAsync(collectionId);
        var admission = await admissionRepository.GetAdmissionAsync(collectionId, id);
        if (((currentUser != null && (chart.OwnerId == currentUser.Id || collection.OwnerId == currentUser.Id) &&
              !resourceService.HasPermission(currentUser, UserRole.Qualified)) ||
             (currentUser != null && chart.OwnerId != currentUser.Id && collection.OwnerId != currentUser.Id &&
              !resourceService.HasPermission(currentUser, UserRole.Moderator))) &&
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
    ///     Makes a request to have a chart admitted by a collection.
    /// </summary>
    /// <param name="id">A chart's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="201">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified chart or collection is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPost("{id:guid}/collections")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> CollectChartIntoCollection([FromRoute] Guid id, [FromBody] AdmissionRequestDto dto)
    {
        if (!await chartRepository.ChartExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var chart = await chartRepository.GetChartAsync(id);

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if ((currentUser.Id == chart.OwnerId && !resourceService.HasPermission(currentUser, UserRole.Member)) ||
            (currentUser.Id != chart.OwnerId && !resourceService.HasPermission(currentUser, UserRole.Moderator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (!await collectionRepository.CollectionExistsAsync(dto.AdmitterId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentNotFound
            });

        if (await admissionRepository.AdmissionExistsAsync(dto.AdmitterId, id))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.AlreadyDone
            });

        var collection = await collectionRepository.GetCollectionAsync(dto.AdmitterId);

        if (collection.OwnerId != currentUser.Id && collection.Accessibility == Accessibility.RefuseAny)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentIsPrivate
            });

        var admission = new Admission
        {
            AdmitterId = collection.Id,
            AdmitteeId = id,
            Status =
                collection.Accessibility == Accessibility.AllowAny || collection.OwnerId == currentUser.Id
                    ? RequestStatus.Approved
                    : RequestStatus.Waiting,
            Label = dto.Label,
            RequesterId = currentUser.Id,
            RequesteeId = collection.OwnerId,
            DateCreated = DateTimeOffset.UtcNow,
            AdmitterType = AdmitterType.Collection
        };

        if (!await admissionRepository.CreateAdmissionAsync(admission))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        if (collection.OwnerId != currentUser.Id && collection.Accessibility == Accessibility.RequireReview)
            await notificationService.Notify((await userManager.FindByIdAsync(collection.OwnerId.ToString()))!,
                currentUser, NotificationType.Requests, "collection-admission",
                new Dictionary<string, string>
                {
                    { "User", resourceService.GetRichText<User>(currentUser.Id.ToString(), currentUser.UserName!) },
                    { "Chart", resourceService.GetRichText<Chart>(chart.Id.ToString(), chart.GetDisplay()) },
                    {
                        "Collection",
                        resourceService.GetRichText<Collection>(collection.Id.ToString(), collection.GetDisplay())
                    },
                    {
                        "Admission",
                        resourceService.GetComplexRichText("CollectionAdmission", admission.AdmitterId.ToString(),
                            admission.AdmitteeId.ToString(),
                            templateService.GetMessage("more-info", admission.Requestee.Language)!)
                    }
                });

        return StatusCode(StatusCodes.Status201Created);
    }

    /// <summary>
    ///     Removes a chart from a collection that has admitted the chart.
    /// </summary>
    /// <param name="id">A chart's ID.</param>
    /// <param name="collectionId">A collection's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified chart or collection is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpDelete("{id:guid}/collections/{collectionId:guid}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> RemoveChartFromCollection([FromRoute] Guid id, [FromRoute] Guid collectionId)
    {
        if (!await chartRepository.ChartExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var chart = await chartRepository.GetChartAsync(id);

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if ((currentUser.Id == chart.OwnerId && !resourceService.HasPermission(currentUser, UserRole.Member)) ||
            (currentUser.Id != chart.OwnerId && !resourceService.HasPermission(currentUser, UserRole.Administrator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (!await collectionRepository.CollectionExistsAsync(collectionId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentNotFound
            });

        if (!await admissionRepository.AdmissionExistsAsync(collectionId, id))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.AlreadyDone
            });

        var admission = await admissionRepository.GetAdmissionAsync(collectionId, id);
        if (admission.Status != RequestStatus.Approved)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidOperation
            });

        if (!await admissionRepository.RemoveAdmissionAsync(collectionId, id))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return NoContent();
    }

    /// <summary>
    ///     Retrieves the leaderboard of a specific chart.
    /// </summary>
    /// <param name="id">A chart's ID.</param>
    /// <returns>The leaderboard (an array of records).</returns>
    /// <response code="200">Returns an array of records.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified chart is not found.</response>
    [HttpGet("{id:guid}/leaderboard")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<RecordDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetLeaderboard([FromRoute] Guid id, [FromQuery] LeaderboardRequestDto dto)
    {
        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);

        if (!await chartRepository.ChartExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var chart = await chartRepository.GetChartAsync(id);

        if ((currentUser == null || !resourceService.HasPermission(currentUser, UserRole.Administrator)) &&
            chart.IsLocked)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.Locked
            });

        var leaderboard = leaderboardService.ObtainChartLeaderboard(chart.Id);
        var rank = currentUser != null
            ? leaderboard.GetRank((await recordRepository.GetRecordsAsync(["Rks"], [true], 0, 1,
                e => e.ChartId == id && e.OwnerId == currentUser.Id)).FirstOrDefault())
            : null;
        List<RecordDto> list;
        if (currentUser == null || rank == null || rank <= dto.TopRange + dto.NeighborhoodRange + 1)
        {
            var take = rank == null ? dto.TopRange : Math.Max(dto.TopRange, rank.Value + dto.NeighborhoodRange);
            list = leaderboard.Range(0, take)
                .Select((e, i) =>
                {
                    var r = mapper.Map<RecordDto>(e);
                    r.Chart = null;
                    r.Position = i + 1;
                    return r;
                })
                .ToList();
        }
        else
        {
            list = [];
            list.AddRange(leaderboard.Range(0, dto.TopRange)
                .Select((e, i) =>
                {
                    var r = mapper.Map<RecordDto>(e);
                    r.Chart = null;
                    r.Position = i + 1;
                    return r;
                })
                .ToList());
            list.AddRange(leaderboard.Range(rank.Value - dto.NeighborhoodRange - 1, dto.NeighborhoodRange * 2 + 1)
                .Select((e, i) =>
                {
                    var r = mapper.Map<RecordDto>(e);
                    r.Chart = null;
                    r.Position = i + rank.Value - dto.NeighborhoodRange;
                    return r;
                })
                .ToList());
        }

        return Ok(new ResponseDto<IEnumerable<RecordDto>>
        {
            Status = ResponseStatus.Ok, Code = ResponseCodes.Ok, Data = list
        });
    }

    /// <summary>
    ///     Retrieves likes from a specific chart.
    /// </summary>
    /// <param name="id">A chart's ID.</param>
    /// <returns>An array of likes.</returns>
    /// <response code="200">Returns an array of likes.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified chart is not found.</response>
    [HttpGet("{id:guid}/likes")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<LikeDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetChartLikes([FromRoute] Guid id, [FromQuery] ArrayRequestDto dto)
    {
        dto.PerPage = dto.PerPage > 0 && dto.PerPage < dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? dataSettings.Value.PaginationPerPage : dataSettings.Value.PaginationMaxPerPage;
        dto.Page = dto.Page > 1 ? dto.Page : 1;
        var position = dto.PerPage * (dto.Page - 1);
        if (!await chartRepository.ChartExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        var chart = await chartRepository.GetChartAsync(id);
        if ((currentUser == null || !resourceService.HasPermission(currentUser, UserRole.Administrator)) &&
            chart.IsLocked)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.Locked
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
    ///     Likes a specific chart.
    /// </summary>
    /// <param name="id">A chart's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="201">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="404">When the specified chart is not found.</response>
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
        if (!await chartRepository.ChartExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var chart = await chartRepository.GetChartAsync(id);
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

        if (!await likeService.CreateLikeAsync(chart, currentUser.Id))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.AlreadyDone
            });

        return StatusCode(StatusCodes.Status201Created);
    }

    /// <summary>
    ///     Removes the like from a specific chart.
    /// </summary>
    /// <param name="id">A chart's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="404">When the specified chart is not found.</response>
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
        if (!await chartRepository.ChartExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var chart = await chartRepository.GetChartAsync(id);
        if (chart.IsLocked)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.Locked
            });

        if (!await likeService.RemoveLikeAsync(chart, currentUser.Id))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.AlreadyDone
            });

        return NoContent();
    }

    /// <summary>
    ///     Retrieves comments from a specific chart.
    /// </summary>
    /// <param name="id">A chart's ID.</param>
    /// <returns>An array of comments.</returns>
    /// <response code="200">Returns an array of comments.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified chart is not found.</response>
    [HttpGet("{id:guid}/comments")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<CommentDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetChartComments([FromRoute] Guid id, [FromQuery] ArrayRequestDto dto)
    {
        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        dto.PerPage = dto.PerPage > 0 && dto.PerPage < dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? dataSettings.Value.PaginationPerPage : dataSettings.Value.PaginationMaxPerPage;
        dto.Page = dto.Page > 1 ? dto.Page : 1;
        var position = dto.PerPage * (dto.Page - 1);

        if (!await chartRepository.ChartExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var chart = await chartRepository.GetChartAsync(id);

        if ((currentUser == null || !resourceService.HasPermission(currentUser, UserRole.Administrator)) &&
            chart.IsLocked)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.Locked
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
    ///     Comments on a specific chart.
    /// </summary>
    /// <param name="id">A chart's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="201">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified chart is not found.</response>
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
        if (!await chartRepository.ChartExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var chart = await chartRepository.GetChartAsync(id);
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

        var result = await resourceService.ParseUserContent(dto.Content);
        var comment = new Comment
        {
            ResourceId = chart.Id,
            Content = result.Item1,
            Language = dto.Language,
            OwnerId = currentUser.Id,
            DateCreated = DateTimeOffset.UtcNow
        };
        if (!await commentRepository.CreateCommentAsync(comment))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        await notificationService.NotifyComment(comment, chart, await resourceService.GetDisplayName(chart),
            dto.Content);
        await notificationService.NotifyMentions(result.Item2, currentUser,
            resourceService.GetRichText<Comment>(comment.Id.ToString(), dto.Content));

        return StatusCode(StatusCodes.Status201Created);
    }

    /// <summary>
    ///     Retrieves votes from a specific chart.
    /// </summary>
    /// <param name="id">A chart's ID.</param>
    /// <returns>An array of votes.</returns>
    /// <response code="200">Returns an array of votes.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified chart is not found.</response>
    [HttpGet("{id:guid}/votes")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<VoteDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetChartVotes([FromRoute] Guid id, [FromQuery] ArrayRequestDto dto,
        [FromQuery] VoteFilterDto? filterDto = null)
    {
        if (!await chartRepository.ChartExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        var chart = await chartRepository.GetChartAsync(id);
        if ((currentUser == null || !resourceService.HasPermission(currentUser, UserRole.Administrator)) &&
            chart.IsLocked)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.Locked
            });

        dto.PerPage = dto.PerPage > 0 && dto.PerPage < dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? dataSettings.Value.PaginationPerPage : dataSettings.Value.PaginationMaxPerPage;
        dto.Page = dto.Page > 1 ? dto.Page : 1;
        var position = dto.PerPage * (dto.Page - 1);
        var predicateExpr = await filterService.Parse(filterDto, dto.Predicate, currentUser, e => e.ChartId == id);
        var votes = await voteRepository.GetVotesAsync(dto.Order, dto.Desc, position, dto.PerPage, predicateExpr);
        var list = mapper.Map<List<VoteDto>>(votes);
        var total = await voteRepository.CountVotesAsync(e => e.ChartId == id);

        return Ok(new ResponseDto<IEnumerable<VoteDto>>
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
    ///     Votes a specific chart.
    /// </summary>
    /// <param name="id">A chart's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="201">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="404">When the specified chart is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPost("{id:guid}/votes")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status201Created, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> CreateVote([FromRoute] Guid id, [FromBody] VoteRequestDto dto)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!resourceService.HasPermission(currentUser, UserRole.Member))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        if (!await chartRepository.ChartExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var chart = await chartRepository.GetChartAsync(id);
        if (chart.IsLocked)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.Locked
            });

        if (!await voteService.CreateVoteAsync(dto, chart, currentUser))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return StatusCode(StatusCodes.Status201Created);
    }

    /// <summary>
    ///     Removes the vote from a specific chart.
    /// </summary>
    /// <param name="id">A chart's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="404">When the specified chart is not found.</response>
    [HttpDelete("{id:guid}/votes")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> RemoveVote([FromRoute] Guid id)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!resourceService.HasPermission(currentUser, UserRole.Member))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        if (!await chartRepository.ChartExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var chart = await chartRepository.GetChartAsync(id);
        if (chart.IsLocked)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.Locked
            });

        if (!await voteService.RemoveVoteAsync(chart, currentUser.Id))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.AlreadyDone
            });

        return NoContent();
    }
}