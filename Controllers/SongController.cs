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
using HP = PhiZoneApi.Constants.HostshipPermissions;

// ReSharper disable RouteTemplates.ActionRoutePrefixCanBeExtractedToControllerRoute

// ReSharper disable RouteTemplates.ParameterConstraintCanBeSpecified

namespace PhiZoneApi.Controllers;

[Route("songs")]
[ApiVersion("2.0")]
[ApiController]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
    Policy = "AllowAnonymous")]
public class SongController(
    ISongRepository songRepository,
    IChartRepository chartRepository,
    IOptions<DataSettings> dataSettings,
    UserManager<User> userManager,
    IFilterService filterService,
    IFileStorageService fileStorageService,
    IDtoMapper dtoMapper,
    IMapper mapper,
    ISongService songService,
    ILikeRepository likeRepository,
    ILikeService likeService,
    ICommentRepository commentRepository,
    IChapterRepository chapterRepository,
    IAdmissionRepository admissionRepository,
    IAuthorshipRepository authorshipRepository,
    ITagRepository tagRepository,
    IEventResourceRepository eventResourceRepository,
    IEventDivisionRepository eventDivisionRepository,
    IEventRepository eventRepository,
    IResourceService resourceService,
    INotificationService notificationService,
    ITemplateService templateService,
    ILogger<SongController> logger,
    IMeilisearchService meilisearchService) : Controller
{
    /// <summary>
    ///     Retrieves songs.
    /// </summary>
    /// <returns>An array of songs.</returns>
    /// <response code="200">Returns an array of songs.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<SongDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetSongs([FromQuery] ArrayRequestDto dto,
        [FromQuery] SongFilterDto? filterDto = null, [FromQuery] ArrayTagDto? tagDto = null)
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
        var showAnonymous = filterDto is
        {
            RangeId: not null, ContainsAuthorName: null, EqualsAuthorName: null, RangeOwnerId: null, MinOwnerId: null,
            MaxOwnerId: null
        };
        IEnumerable<Song> songs;
        int total;
        if (dto.Search != null)
        {
            var result = await meilisearchService.SearchAsync<Song>(dto.Search, dto.PerPage, dto.Page,
                showHidden: currentUser is { Role: UserRole.Administrator });
            var idList = result.Hits.Select(item => item.Id).ToList();
            songs = (await songRepository.GetSongsAsync(predicate: e => idList.Contains(e.Id),
                currentUserId: currentUser?.Id)).OrderBy(e => idList.IndexOf(e.Id));
            total = result.TotalHits;
        }
        else
        {
            songs = await songRepository.GetSongsAsync(dto.Order, dto.Desc, position, dto.PerPage, predicateExpr,
                currentUser?.Id, showAnonymous);
            total = await songRepository.CountSongsAsync(predicateExpr, showAnonymous);
        }

        var list = songs.Select(e => dtoMapper.MapSong<SongDto>(e)).ToList();

        return Ok(new ResponseDto<IEnumerable<SongDto>>
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
    ///     Retrieves a specific song.
    /// </summary>
    /// <param name="id">A song's ID.</param>
    /// <returns>A song.</returns>
    /// <response code="200">Returns a song.</response>
    /// <response code="304">
    ///     When the resource has not been updated since last retrieval. Requires <c>If-None-Match</c>.
    /// </response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified song is not found.</response>
    [HttpGet("{id:guid}")]
    [ServiceFilter(typeof(ETagFilter))]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<SongDto>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status304NotModified, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetSong([FromRoute] Guid id)
    {
        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        if (!await songRepository.SongExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var song = await songRepository.GetSongAsync(id, currentUser?.Id);
        var dto = dtoMapper.MapSong<SongDto>(song);

        return Ok(new ResponseDto<SongDto> { Status = ResponseStatus.Ok, Code = ResponseCodes.Ok, Data = dto });
    }

    /// <summary>
    ///     Retrieves a random song.
    /// </summary>
    /// <returns>A random song.</returns>
    /// <response code="200">Returns a random song.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    [HttpGet("random")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<SongDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetRandomSong([FromQuery] ArrayRequestDto dto,
        [FromQuery] SongFilterDto? filterDto = null, [FromQuery] ArrayTagDto? tagDto = null)
    {
        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        var predicateExpr = await filterService.Parse(filterDto, dto.Predicate, currentUser,
            e => !e.IsHidden && (tagDto == null ||
                                 ((tagDto.TagsToInclude == null || e.Tags.Any(tag =>
                                     tagDto.TagsToInclude.Select(resourceService.Normalize)
                                         .Contains(tag.NormalizedName))) && (tagDto.TagsToExclude == null ||
                                                                             e.Tags.All(tag =>
                                                                                 !tagDto.TagsToExclude
                                                                                     .Select(resourceService.Normalize)
                                                                                     .Contains(tag.NormalizedName))))));
        var song = await songRepository.GetRandomSongAsync(predicateExpr, currentUser?.Id);

        if (song == null)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var songDto = dtoMapper.MapSong<SongDto>(song);

        return Ok(new ResponseDto<SongDto> { Status = ResponseStatus.Ok, Code = ResponseCodes.Ok, Data = songDto });
    }

    /// <summary>
    ///     Creates a new song.
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
    public async Task<IActionResult> CreateSong([FromForm] SongCreationDto dto, [FromQuery] bool wait = false)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!resourceService.HasPermission(currentUser, UserRole.Administrator))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        (string, string, TimeSpan)? songInfo = null;
        if (wait)
        {
            songInfo = await songService.UploadAsync(dto.Title, dto.File);
            if (songInfo == null)
                return BadRequest(new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidData
                });

            if (!(TimeSpan.Zero <= dto.PreviewStart && dto.PreviewStart <= dto.PreviewEnd &&
                  dto.PreviewEnd <= songInfo.Value.Item3))
                return BadRequest(new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidTimeRelation
                });
            logger.LogInformation(LogEvents.SongInfo, "[{Now}] New song: {Title}",
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

        var song = new Song
        {
            Title = dto.Title,
            EditionType = dto.EditionType,
            Edition = dto.Edition,
            AuthorName = dto.AuthorName,
            File = songInfo?.Item1,
            FileChecksum = songInfo?.Item2,
            Illustration = illustrationUrl,
            Illustrator = dto.Illustrator,
            Description = dto.Description,
            Accessibility = dto.Accessibility,
            IsHidden = dto.IsHidden,
            IsLocked = dto.IsLocked,
            Lyrics = dto.Lyrics,
            Bpm = dto.Bpm,
            MinBpm = dto.MinBpm,
            MaxBpm = dto.MaxBpm,
            Offset = dto.Offset,
            License = license,
            IsOriginal = dto.IsOriginal,
            Duration = songInfo?.Item3,
            PreviewStart = dto.PreviewStart,
            PreviewEnd = dto.PreviewEnd,
            OwnerId = currentUser.Id,
            DateCreated = DateTimeOffset.UtcNow,
            DateUpdated = DateTimeOffset.UtcNow
        };

        if (!await songRepository.CreateSongAsync(song))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        await tagRepository.CreateTagsAsync(dto.Tags, song);

        // ReSharper disable once InvertIf
        if (!wait)
        {
            await songService.PublishAsync(dto.File, song.Id);
            logger.LogInformation(LogEvents.SongInfo, "[{Now}] Scheduled new song: {Title}",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), dto.Title);
        }

        return StatusCode(StatusCodes.Status201Created);
    }

    /// <summary>
    ///     Updates a song.
    /// </summary>
    /// <param name="id">A song's ID.</param>
    /// <param name="patchDocument">A JSON Patch Document.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified song is not found.</response>
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
    public async Task<IActionResult> UpdateSong([FromRoute] Guid id,
        [FromBody] JsonPatchDocument<SongUpdateDto> patchDocument)
    {
        if (!await songRepository.SongExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var song = await songRepository.GetSongAsync(id);

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!resourceService.HasPermission(currentUser, UserRole.Administrator))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        var dto = mapper.Map<SongUpdateDto>(song);
        dto.Tags = song.Tags.Select(tag => tag.Name).ToList();
        patchDocument.ApplyTo(dto, ModelState);

        if (!TryValidateModel(dto))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorDetailed,
                Code = ResponseCodes.InvalidData,
                Errors = ModelErrorTranslator.Translate(ModelState)
            });

        if (!(TimeSpan.Zero <= dto.PreviewStart && dto.PreviewStart <= dto.PreviewEnd &&
              dto.PreviewEnd <= song.Duration))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidTimeRelation
            });

        song.Title = dto.Title;
        song.EditionType = dto.EditionType;
        song.Edition = dto.Edition;
        song.AuthorName = dto.AuthorName;
        song.Illustrator = dto.Illustrator;
        song.Description = dto.Description;
        song.Accessibility = dto.Accessibility;
        song.IsHidden = dto.IsHidden;
        song.IsLocked = dto.IsLocked;
        song.Lyrics = dto.Lyrics;
        song.Bpm = dto.Bpm;
        song.MinBpm = dto.MinBpm;
        song.MaxBpm = dto.MaxBpm;
        song.Offset = dto.Offset;
        song.IsOriginal = dto.IsOriginal;
        song.PreviewStart = dto.PreviewStart;
        song.PreviewEnd = dto.PreviewEnd;
        song.DateUpdated = DateTimeOffset.UtcNow;

        if (!await songRepository.UpdateSongAsync(song))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        await tagRepository.CreateTagsAsync(dto.Tags, song);

        return NoContent();
    }

    /// <summary>
    ///     Updates a song's file.
    /// </summary>
    /// <param name="id">A song's ID.</param>
    /// <param name="dto">The new file.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified song is not found.</response>
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
    public async Task<IActionResult> UpdateSongFile([FromRoute] Guid id, [FromForm] FileDto dto,
        [FromQuery] bool wait = false)
    {
        if (!await songRepository.SongExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var song = await songRepository.GetSongAsync(id);

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!resourceService.HasPermission(currentUser, UserRole.Administrator))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (dto.File != null)
        {
            if (!wait)
            {
                await songService.PublishAsync(dto.File, song.Id);
            }
            else
            {
                var songInfo = await songService.UploadAsync(song.Title, dto.File);
                if (songInfo == null)
                    return BadRequest(new ResponseDto<object>
                    {
                        Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidData
                    });

                song.File = songInfo.Value.Item1;
                song.FileChecksum = songInfo.Value.Item2;
                song.Duration = songInfo.Value.Item3;
                song.DateUpdated = DateTimeOffset.UtcNow;

                if (song.PreviewEnd > song.Duration) song.PreviewEnd = song.Duration.Value;

                if (song.PreviewStart > song.PreviewEnd) song.PreviewStart = TimeSpan.Zero;
            }
        }

        if (!await songRepository.UpdateSongAsync(song))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return NoContent();
    }

    /// <summary>
    ///     Updates a song's illustration.
    /// </summary>
    /// <param name="id">A song's ID.</param>
    /// <param name="dto">The new illustration.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified song is not found.</response>
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
    public async Task<IActionResult> UpdateSongIllustration([FromRoute] Guid id, [FromForm] FileDto dto)
    {
        if (!await songRepository.SongExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var song = await songRepository.GetSongAsync(id);

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!resourceService.HasPermission(currentUser, UserRole.Administrator))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (dto.File != null)
        {
            song.Illustration = (await fileStorageService.UploadImage<Song>(song.Title, dto.File, (16, 9))).Item1;
            await fileStorageService.SendUserInput(song.Illustration, "Illustration", Request, currentUser);
            song.DateUpdated = DateTimeOffset.UtcNow;
        }

        if (!await songRepository.UpdateSongAsync(song))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return NoContent();
    }

    /// <summary>
    ///     Updates a song's license.
    /// </summary>
    /// <param name="id">A song's ID.</param>
    /// <param name="dto">The new license.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified song is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPatch("{id:guid}/license")]
    [Consumes("multipart/form-data")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> UpdateSongLicense([FromRoute] Guid id, [FromForm] FileDto dto)
    {
        if (!await songRepository.SongExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var song = await songRepository.GetSongAsync(id);

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!resourceService.HasPermission(currentUser, UserRole.Administrator))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (dto.File != null)
        {
            song.License = (await fileStorageService.Upload<Song>(song.Title, dto.File)).Item1;
            song.DateUpdated = DateTimeOffset.UtcNow;
        }

        if (!await songRepository.UpdateSongAsync(song))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return NoContent();
    }

    /// <summary>
    ///     Removes a song's license.
    /// </summary>
    /// <param name="id">A song's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified song is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpDelete("{id:guid}/license")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> RemoveSongLicense([FromRoute] Guid id)
    {
        if (!await songRepository.SongExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var song = await songRepository.GetSongAsync(id);

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!resourceService.HasPermission(currentUser, UserRole.Administrator))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        song.License = null;
        song.DateUpdated = DateTimeOffset.UtcNow;

        if (!await songRepository.UpdateSongAsync(song))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return NoContent();
    }

    /// <summary>
    ///     Removes a song.
    /// </summary>
    /// <param name="id">A song's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified song is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpDelete("{id:guid}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> RemoveSong([FromRoute] Guid id)
    {
        if (!await songRepository.SongExistsAsync(id))
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

        if (!await songRepository.RemoveSongAsync(id))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return NoContent();
    }

    /// <summary>
    ///     Creates a new authorship for a song.
    /// </summary>
    /// <returns>An empty body.</returns>
    /// <response code="201">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified song or author is not found.</response>
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
        if (!await songRepository.SongExistsAsync(id))
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
    ///     Retrieves admissions received by chapters.
    /// </summary>
    /// <param name="id">A song's ID.</param>
    /// <returns>An array of chapter admitters.</returns>
    /// <response code="200">Returns an array of chapter admitters.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified song is not found.</response>
    [HttpGet("{id:guid}/chapters")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<ChapterAdmitterDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetAdmissions([FromRoute] Guid id, [FromQuery] ArrayRequestDto dto,
        [FromQuery] AdmissionFilterDto? filterDto = null)
    {
        if (!await songRepository.SongExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        dto.PerPage = dto.PerPage > 0 && dto.PerPage < dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? dataSettings.Value.PaginationPerPage : dataSettings.Value.PaginationMaxPerPage;
        dto.Page = dto.Page > 1 ? dto.Page : 1;
        var position = dto.PerPage * (dto.Page - 1);
        var song = await songRepository.GetSongAsync(id);
        var hasPermission = currentUser != null && (song.OwnerId == currentUser.Id ||
                                                    resourceService.HasPermission(currentUser, UserRole.Moderator));
        var predicateExpr = await filterService.Parse(filterDto, dto.Predicate, currentUser,
            e => (hasPermission || e.Status == RequestStatus.Approved ||
                  (currentUser != null && e.Admitter.OwnerId == currentUser.Id)) && e.AdmitteeId == id);

        var admissions = await admissionRepository.GetAdmissionsAsync(dto.Order, dto.Desc, position,
            dto.PerPage, predicateExpr);
        var total = await admissionRepository.CountAdmissionsAsync(predicateExpr);
        var list = new List<ChapterAdmitterDto>();

        foreach (var admission in admissions)
            list.Add(await dtoMapper.MapSongChapterAsync<ChapterAdmitterDto>(admission, currentUser));

        return Ok(new ResponseDto<IEnumerable<ChapterAdmitterDto>>
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
    /// <param name="id">A song's ID.</param>
    /// <param name="chapterId">A chapter's ID.</param>
    /// <returns>An admission.</returns>
    /// <response code="200">Returns an admission.</response>
    /// <response code="304">
    ///     When the resource has not been updated since last retrieval. Requires <c>If-None-Match</c>.
    /// </response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified song, chapter, or admission is not found.</response>
    [HttpGet("{id:guid}/chapters/{chapterId:guid}")]
    [ServiceFilter(typeof(ETagFilter))]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<AdmissionDto<ChapterDto, SongDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetAdmission([FromRoute] Guid id, [FromRoute] Guid chapterId)
    {
        if (!await songRepository.SongExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        if (!await chapterRepository.ChapterExistsAsync(chapterId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentNotFound
            });

        if (!await admissionRepository.AdmissionExistsAsync(chapterId, id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.RelationNotFound
            });

        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        var song = await songRepository.GetSongAsync(id);
        var chapter = await chapterRepository.GetChapterAsync(chapterId);
        var admission = await admissionRepository.GetAdmissionAsync(chapterId, id);
        if (((currentUser != null && (song.OwnerId == currentUser.Id || chapter.OwnerId == currentUser.Id) &&
              !resourceService.HasPermission(currentUser, UserRole.Qualified)) ||
             (currentUser != null && song.OwnerId != currentUser.Id && chapter.OwnerId != currentUser.Id &&
              !resourceService.HasPermission(currentUser, UserRole.Moderator))) &&
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
    ///     Makes a request to have a song admitted by a chapter.
    /// </summary>
    /// <param name="id">A song's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="201">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified song or chapter is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPost("{id:guid}/chapters")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> CollectSongIntoChapter([FromRoute] Guid id, [FromBody] AdmissionRequestDto dto)
    {
        if (!await songRepository.SongExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var song = await songRepository.GetSongAsync(id);

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if ((currentUser.Id == song.OwnerId && !resourceService.HasPermission(currentUser, UserRole.Member)) ||
            (currentUser.Id != song.OwnerId && !resourceService.HasPermission(currentUser, UserRole.Moderator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (!await chapterRepository.ChapterExistsAsync(dto.AdmitterId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentNotFound
            });

        if (await admissionRepository.AdmissionExistsAsync(dto.AdmitterId, id))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.AlreadyDone
            });

        var chapter = await chapterRepository.GetChapterAsync(dto.AdmitterId);

        if (chapter.OwnerId != currentUser.Id && chapter.Accessibility == Accessibility.RefuseAny)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentIsPrivate
            });

        var admission = new Admission
        {
            AdmitterId = chapter.Id,
            AdmitteeId = id,
            Status =
                chapter.Accessibility == Accessibility.AllowAny || chapter.OwnerId == currentUser.Id
                    ? RequestStatus.Approved
                    : RequestStatus.Waiting,
            Label = dto.Label,
            RequesterId = currentUser.Id,
            RequesteeId = chapter.OwnerId,
            DateCreated = DateTimeOffset.UtcNow,
            AdmitterType = AdmitterType.Chapter
        };

        if (!await admissionRepository.CreateAdmissionAsync(admission))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        if (chapter.OwnerId != currentUser.Id && chapter.Accessibility == Accessibility.RequireReview)
            await notificationService.Notify((await userManager.FindByIdAsync(chapter.OwnerId.ToString()))!,
                currentUser, NotificationType.Requests, "chapter-admission",
                new Dictionary<string, string>
                {
                    { "User", resourceService.GetRichText<User>(currentUser.Id.ToString(), currentUser.UserName!) },
                    { "Song", resourceService.GetRichText<Song>(song.Id.ToString(), song.GetDisplay()) },
                    {
                        "Chapter", resourceService.GetRichText<Chapter>(chapter.Id.ToString(), chapter.GetDisplay())
                    },
                    {
                        "Admission",
                        resourceService.GetComplexRichText("ChapterAdmission", admission.AdmitterId.ToString(),
                            admission.AdmitteeId.ToString(),
                            templateService.GetMessage("more-info", admission.Requestee.Language)!)
                    }
                });

        return StatusCode(StatusCodes.Status201Created);
    }

    /// <summary>
    ///     Removes a song from a chapter that has admitted the song.
    /// </summary>
    /// <param name="id">A song's ID.</param>
    /// <param name="chapterId">A chapter's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified song or chapter is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpDelete("{id:guid}/chapters/{chapterId:guid}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> RemoveSongFromChapter([FromRoute] Guid id, [FromRoute] Guid chapterId)
    {
        if (!await songRepository.SongExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var song = await songRepository.GetSongAsync(id);

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if ((currentUser.Id == song.OwnerId && !resourceService.HasPermission(currentUser, UserRole.Member)) ||
            (currentUser.Id != song.OwnerId && !resourceService.HasPermission(currentUser, UserRole.Administrator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (!await chapterRepository.ChapterExistsAsync(chapterId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentNotFound
            });

        if (!await admissionRepository.AdmissionExistsAsync(chapterId, id))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.AlreadyDone
            });

        var admission = await admissionRepository.GetAdmissionAsync(chapterId, id);
        if (admission.Status != RequestStatus.Approved)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidOperation
            });

        if (!await admissionRepository.RemoveAdmissionAsync(chapterId, id))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return NoContent();
    }

    /// <summary>
    ///     Retrieves charts from a specific song.
    /// </summary>
    /// <param name="id">A song's ID.</param>
    /// <returns>An array of charts.</returns>
    /// <response code="200">Returns an array of charts.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified song is not found.</response>
    [HttpGet("{id:guid}/charts")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<ChartDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetSongCharts([FromRoute] Guid id, [FromQuery] ArrayRequestDto dto,
        [FromQuery] ChartFilterDto? filterDto = null)
    {
        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        dto.PerPage = dto.PerPage > 0 && dto.PerPage < dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? dataSettings.Value.PaginationPerPage : dataSettings.Value.PaginationMaxPerPage;
        dto.Page = dto.Page > 1 ? dto.Page : 1;
        var position = dto.PerPage * (dto.Page - 1);
        var predicateExpr = await filterService.Parse(filterDto, dto.Predicate, currentUser, e => e.SongId == id);
        if (!await songRepository.SongExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var charts = await chartRepository.GetChartsAsync(dto.Order, dto.Desc, position, dto.PerPage, predicateExpr,
            currentUser?.Id);
        var total = await chartRepository.CountChartsAsync(predicateExpr);
        var list = charts.Select(e => dtoMapper.MapChart<ChartDto>(e)).ToList();

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
    ///     Retrieves likes from a specific song.
    /// </summary>
    /// <param name="id">A song's ID.</param>
    /// <returns>An array of likes.</returns>
    /// <response code="200">Returns an array of likes.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified song is not found.</response>
    [HttpGet("{id:guid}/likes")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<LikeDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetSongLikes([FromRoute] Guid id, [FromQuery] ArrayRequestDto dto)
    {
        dto.PerPage = dto.PerPage > 0 && dto.PerPage < dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? dataSettings.Value.PaginationPerPage : dataSettings.Value.PaginationMaxPerPage;
        dto.Page = dto.Page > 1 ? dto.Page : 1;
        var position = dto.PerPage * (dto.Page - 1);
        if (!await songRepository.SongExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        var song = await songRepository.GetSongAsync(id);
        if ((currentUser == null || !resourceService.HasPermission(currentUser, UserRole.Administrator)) &&
            song.IsLocked)
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
    ///     Likes a specific song.
    /// </summary>
    /// <param name="id">A song's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="201">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="404">When the specified song is not found.</response>
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
        if (!await songRepository.SongExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var song = await songRepository.GetSongAsync(id);
        if (await resourceService.IsBlacklisted(song.OwnerId, currentUser.Id))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.Blacklisted
            });
        if (song.IsLocked)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.Locked
            });

        if (!await likeService.CreateLikeAsync(song, currentUser.Id))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.AlreadyDone
            });

        return StatusCode(StatusCodes.Status201Created);
    }

    /// <summary>
    ///     Removes the like from a specific song.
    /// </summary>
    /// <param name="id">A song's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="404">When the specified song is not found.</response>
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
        if (!await songRepository.SongExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var song = await songRepository.GetSongAsync(id);

        if (song.IsLocked)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.Locked
            });

        if (!await likeService.RemoveLikeAsync(song, currentUser.Id))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.AlreadyDone
            });

        return NoContent();
    }

    /// <summary>
    ///     Retrieves comments from a specific song.
    /// </summary>
    /// <param name="id">A song's ID.</param>
    /// <returns>An array of comments.</returns>
    /// <response code="200">Returns an array of comments.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified song is not found.</response>
    [HttpGet("{id:guid}/comments")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<CommentDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetSongComments([FromRoute] Guid id, [FromQuery] ArrayRequestDto dto)
    {
        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        dto.PerPage = dto.PerPage > 0 && dto.PerPage < dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? dataSettings.Value.PaginationPerPage : dataSettings.Value.PaginationMaxPerPage;
        dto.Page = dto.Page > 1 ? dto.Page : 1;
        var position = dto.PerPage * (dto.Page - 1);
        if (!await songRepository.SongExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var song = await songRepository.GetSongAsync(id);
        if ((currentUser == null || !resourceService.HasPermission(currentUser, UserRole.Administrator)) &&
            song.IsLocked)
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
    ///     Comments on a specific song.
    /// </summary>
    /// <param name="id">A song's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="201">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified song is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPost("{id:guid}/comments")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
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
        if (!await songRepository.SongExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var song = await songRepository.GetSongAsync(id);
        if (await resourceService.IsBlacklisted(song.OwnerId, currentUser.Id))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.Blacklisted
            });
        if (song.IsLocked)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.Locked
            });

        var result = await resourceService.ParseUserContent(dto.Content);
        var comment = new Comment
        {
            ResourceId = song.Id,
            Content = result.Item1,
            Language = dto.Language,
            OwnerId = currentUser.Id,
            DateCreated = DateTimeOffset.UtcNow
        };
        if (!await commentRepository.CreateCommentAsync(comment))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        await notificationService.NotifyComment(comment, song, song.GetDisplay(), dto.Content);
        await notificationService.NotifyMentions(result.Item2, currentUser,
            resourceService.GetRichText<Comment>(comment.Id.ToString(), dto.Content));

        return StatusCode(StatusCodes.Status201Created);
    }

    /// <summary>
    ///     Links a song to a specific event division.
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
        if (!await songRepository.SongExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var song = await songRepository.GetSongAsync(id);

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
            ResourceId = song.Id,
            SignificantResourceId = song.Id,
            Type = dto.Type,
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