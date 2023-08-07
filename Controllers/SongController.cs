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

[Route("songs")]
[ApiVersion("2.0")]
[ApiController]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
    Policy = "AllowAnonymous")]
public class SongController : Controller
{
    private readonly IAdmissionRepository _admissionRepository;
    private readonly IAuthorshipRepository _authorshipRepository;
    private readonly IChapterRepository _chapterRepository;
    private readonly ICommentRepository _commentRepository;
    private readonly IOptions<DataSettings> _dataSettings;
    private readonly IDtoMapper _dtoMapper;
    private readonly IFileStorageService _fileStorageService;
    private readonly IFilterService _filterService;
    private readonly ILikeRepository _likeRepository;
    private readonly ILikeService _likeService;
    private readonly IMapper _mapper;
    private readonly INotificationService _notificationService;
    private readonly IResourceService _resourceService;
    private readonly ISongRepository _songRepository;
    private readonly ISongService _songService;
    private readonly ITemplateService _templateService;
    private readonly UserManager<User> _userManager;

    public SongController(ISongRepository songRepository, IOptions<DataSettings> dataSettings,
        UserManager<User> userManager, IFilterService filterService, IFileStorageService fileStorageService,
        IDtoMapper dtoMapper, IMapper mapper, ISongService songService, ILikeRepository likeRepository,
        ILikeService likeService, ICommentRepository commentRepository, IChapterRepository chapterRepository,
        IAdmissionRepository admissionRepository, IAuthorshipRepository authorshipRepository,
        IResourceService resourceService, INotificationService notificationService, ITemplateService templateService)
    {
        _songRepository = songRepository;
        _dataSettings = dataSettings;
        _userManager = userManager;
        _filterService = filterService;
        _dtoMapper = dtoMapper;
        _mapper = mapper;
        _songService = songService;
        _likeRepository = likeRepository;
        _likeService = likeService;
        _commentRepository = commentRepository;
        _chapterRepository = chapterRepository;
        _admissionRepository = admissionRepository;
        _authorshipRepository = authorshipRepository;
        _resourceService = resourceService;
        _notificationService = notificationService;
        _templateService = templateService;
        _fileStorageService = fileStorageService;
    }

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
    public async Task<IActionResult> GetSongs([FromQuery] ArrayWithTimeRequestDto dto,
        [FromQuery] SongFilterDto? filterDto = null)
    {
        var currentUser = await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        dto.PerPage = dto.PerPage > 0
            ? dto.PerPage <= _dataSettings.Value.PaginationMaxPerPage
                ? dto.PerPage
                : _dataSettings.Value.PaginationMaxPerPage
            : _dataSettings.Value.PaginationPerPage;
        var position = dto.PerPage * (dto.Page - 1);
        var predicateExpr = await _filterService.Parse(filterDto, dto.Predicate, currentUser);
        var songs = await _songRepository.GetSongsAsync(dto.Order, dto.Desc, position, dto.PerPage, dto.Search,
            predicateExpr);
        var total = await _songRepository.CountSongsAsync(dto.Search, predicateExpr);
        var list = new List<SongDto>();

        foreach (var song in songs) list.Add(await _dtoMapper.MapSongAsync<SongDto>(song, currentUser));

        return Ok(new ResponseDto<IEnumerable<SongDto>>
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
        var currentUser = await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        if (!await _songRepository.SongExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var song = await _songRepository.GetSongAsync(id);

        if ((currentUser == null || !await _resourceService.HasPermission(currentUser, Roles.Administrator)) &&
            song.IsHidden)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var dto = await _dtoMapper.MapSongAsync<SongDto>(song, currentUser);

        return Ok(new ResponseDto<SongDto> { Status = ResponseStatus.Ok, Code = ResponseCodes.Ok, Data = dto });
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
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Consumes("multipart/form-data")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status201Created, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> CreateSong([FromForm] SongCreationDto dto, [FromQuery] bool wait = false)
    {
        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!await _resourceService.HasPermission(currentUser, Roles.Administrator))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        (string, string, TimeSpan)? songInfo = null;
        if (wait)
        {
            songInfo = await _songService.UploadAsync(dto.Title, dto.File);
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
        }
        else if (!(TimeSpan.Zero <= dto.PreviewStart && dto.PreviewStart <= dto.PreviewEnd))
        {
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidTimeRelation
            });
        }

        var illustrationUrl = (await _fileStorageService.UploadImage<Song>(dto.Title, dto.Illustration, (16, 9))).Item1;
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
            IsOriginal = dto.IsOriginal,
            Duration = songInfo?.Item3,
            PreviewStart = dto.PreviewStart,
            PreviewEnd = dto.PreviewEnd,
            OwnerId = currentUser.Id,
            DateCreated = DateTimeOffset.UtcNow,
            DateUpdated = DateTimeOffset.UtcNow
        };

        if (!await _songRepository.CreateSongAsync(song))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        if (!wait) await _songService.PublishAsync(dto.File, song.Id);

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
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> UpdateSong([FromRoute] Guid id,
        [FromBody] JsonPatchDocument<SongUpdateDto> patchDocument)
    {
        if (!await _songRepository.SongExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var song = await _songRepository.GetSongAsync(id);

        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!await _resourceService.HasPermission(currentUser, Roles.Administrator))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        var dto = _mapper.Map<SongUpdateDto>(song);
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

        if (!await _songRepository.UpdateSongAsync(song))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

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
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Consumes("multipart/form-data")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> UpdateSongFile([FromRoute] Guid id, [FromForm] FileDto dto,
        [FromQuery] bool wait = false)
    {
        if (!await _songRepository.SongExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var song = await _songRepository.GetSongAsync(id);

        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!await _resourceService.HasPermission(currentUser, Roles.Administrator))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (dto.File != null)
        {
            if (!wait)
            {
                await _songService.PublishAsync(dto.File, song.Id);
            }
            else
            {
                var songInfo = await _songService.UploadAsync(song.Title, dto.File);
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

        if (!await _songRepository.UpdateSongAsync(song))
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
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Consumes("multipart/form-data")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> UpdateSongIllustration([FromRoute] Guid id, [FromForm] FileDto dto)
    {
        if (!await _songRepository.SongExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var song = await _songRepository.GetSongAsync(id);

        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!await _resourceService.HasPermission(currentUser, Roles.Administrator))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (dto.File != null)
        {
            song.Illustration = (await _fileStorageService.UploadImage<Song>(song.Title, dto.File, (16, 9))).Item1;
            song.DateUpdated = DateTimeOffset.UtcNow;
        }

        if (!await _songRepository.UpdateSongAsync(song))
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
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified song is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpDelete("{id:guid}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> RemoveSong([FromRoute] Guid id)
    {
        if (!await _songRepository.SongExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!await _resourceService.HasPermission(currentUser, Roles.Administrator))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (!await _songRepository.RemoveSongAsync(id))
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
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status201Created, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> CreateAuthorship([FromRoute] Guid id, [FromBody] AuthorshipRequestDto dto)
    {
        if (!await _songRepository.SongExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!await _resourceService.HasPermission(currentUser, Roles.Administrator))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (await _userManager.FindByIdAsync(dto.AuthorId.ToString()) == null)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.UserNotFound
            });

        if (await _authorshipRepository.AuthorshipExistsAsync(id, dto.AuthorId))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.AlreadyDone
            });

        var authorship = new Authorship
        {
            ResourceId = id, AuthorId = dto.AuthorId, Position = dto.Position, DateCreated = DateTimeOffset.UtcNow
        };

        if (!await _authorshipRepository.CreateAuthorshipAsync(authorship))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return StatusCode(StatusCodes.Status201Created);
    }

    /// <summary>
    ///     Retrieves admissions requested from a song.
    /// </summary>
    /// <param name="id">A song's ID.</param>
    /// <returns>An array of admissions.</returns>
    /// <response code="200">Returns an array of admissions.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified song is not found.</response>
    [HttpGet("{id:guid}/chapters")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK,
        Type = typeof(ResponseDto<IEnumerable<AdmissionDto<ChapterDto, SongDto>>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetSongAdmissions([FromRoute] Guid id, [FromQuery] ArrayWithTimeRequestDto dto,
        [FromQuery] AdmissionFilterDto? filterDto = null)
    {
        if (!await _songRepository.SongExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var currentUser = await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        dto.PerPage = dto.PerPage > 0
            ? dto.PerPage <= _dataSettings.Value.PaginationMaxPerPage
                ? dto.PerPage
                : _dataSettings.Value.PaginationMaxPerPage
            : _dataSettings.Value.PaginationPerPage;
        var position = dto.PerPage * (dto.Page - 1);
        var song = await _songRepository.GetSongAsync(id);
        var hasPermission = currentUser != null && (song.OwnerId == currentUser.Id ||
                                                    await _resourceService.HasPermission(currentUser,
                                                        Roles.Administrator));
        var predicateExpr = await _filterService.Parse(filterDto, dto.Predicate, currentUser,
            e => hasPermission || e.Status == RequestStatus.Approved);

        if ((currentUser == null || !await _resourceService.HasPermission(currentUser, Roles.Administrator)) &&
            song.IsHidden)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var admissions = await _admissionRepository.GetAdmissionsAsync(dto.Order, dto.Desc, position,
            dto.PerPage, predicateExpr);
        var total = await _admissionRepository.CountAdmissionsAsync(predicateExpr);
        var list = new List<AdmissionDto<ChapterDto, SongDto>>();

        foreach (var admission in admissions)
            list.Add(await _dtoMapper.MapSongAdmissionAsync<ChapterDto, SongDto>(admission, currentUser));

        return Ok(new ResponseDto<IEnumerable<AdmissionDto<ChapterDto, SongDto>>>
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
    ///     Retrieves an admission requested from a song.
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
    public async Task<IActionResult> GetSongAdmission([FromRoute] Guid id, [FromRoute] Guid chapterId)
    {
        if (!await _songRepository.SongExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        if (!await _chapterRepository.ChapterExistsAsync(chapterId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentNotFound
            });

        if (!await _admissionRepository.AdmissionExistsAsync(chapterId, id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.RelationNotFound
            });

        var currentUser = await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        var song = await _songRepository.GetSongAsync(id);
        if ((currentUser == null || !await _resourceService.HasPermission(currentUser, Roles.Administrator)) &&
            song.IsHidden)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var admission = await _admissionRepository.GetAdmissionAsync(chapterId, id);
        if (!(currentUser != null && (song.OwnerId == currentUser.Id ||
                                      await _resourceService.HasPermission(currentUser, Roles.Administrator))) &&
            admission.Status != RequestStatus.Approved)
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        var dto = await _dtoMapper.MapSongAdmissionAsync<ChapterDto, SongDto>(admission, currentUser);

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
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> CollectSongIntoChapter([FromRoute] Guid id, [FromBody] AdmissionRequestDto dto)
    {
        if (!await _songRepository.SongExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var song = await _songRepository.GetSongAsync(id);

        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if ((currentUser.Id == song.OwnerId && !await _resourceService.HasPermission(currentUser, Roles.Member)) ||
            (currentUser.Id != song.OwnerId && !await _resourceService.HasPermission(currentUser, Roles.Administrator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (!await _chapterRepository.ChapterExistsAsync(dto.AdmitterId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentNotFound
            });

        if (await _admissionRepository.AdmissionExistsAsync(dto.AdmitterId, id))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.AlreadyDone
            });

        var chapter = await _chapterRepository.GetChapterAsync(dto.AdmitterId);

        if (chapter.OwnerId != currentUser.Id && chapter.Accessibility == Accessibility.RefuseAny)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief,
                Code = ResponseCodes.ParentIsPrivate
            });

        var admission = new Admission
        {
            AdmitterId = chapter.Id,
            AdmitteeId = id,
            Status =
                chapter.Accessibility == Accessibility.AllowAny ? RequestStatus.Approved : RequestStatus.Waiting,
            Label = dto.Label,
            RequesterId = currentUser.Id,
            RequesteeId = chapter.OwnerId,
            DateCreated = DateTimeOffset.UtcNow
        };

        if (!await _admissionRepository.CreateAdmissionAsync(admission))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        if (chapter.OwnerId != currentUser.Id && chapter.Accessibility == Accessibility.RequireReview)
            await _notificationService.Notify(chapter.Owner, currentUser, NotificationType.Requests, "song-admission",
                new Dictionary<string, string>
                {
                    { "User", _resourceService.GetRichText<User>(currentUser.Id.ToString(), currentUser.UserName!) },
                    { "Song", _resourceService.GetRichText<Song>(song.Id.ToString(), song.GetDisplay()) },
                    { "Chapter", _resourceService.GetRichText<Chapter>(chapter.Id.ToString(), chapter.GetDisplay()) },
                    {
                        "Admission",
                        _resourceService.GetComplexRichText<Admission>(admission.AdmitteeId.ToString(),
                            admission.AdmitterId.ToString(),
                            _templateService.GetMessage("more-info", admission.Requestee.Language)!)
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
        if (!await _songRepository.SongExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var song = await _songRepository.GetSongAsync(id);

        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if ((currentUser.Id == song.OwnerId && !await _resourceService.HasPermission(currentUser, Roles.Member)) ||
            (currentUser.Id != song.OwnerId && !await _resourceService.HasPermission(currentUser, Roles.Administrator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (!await _chapterRepository.ChapterExistsAsync(chapterId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentNotFound
            });

        if (await _admissionRepository.AdmissionExistsAsync(chapterId, id))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.AlreadyDone
            });

        if (!await _admissionRepository.RemoveAdmissionAsync(chapterId, id))
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
    public async Task<IActionResult> GetSongCharts([FromRoute] Guid id, [FromQuery] ArrayWithTimeRequestDto dto,
        [FromQuery] ChartFilterDto? filterDto = null)
    {
        var currentUser = await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        dto.PerPage = dto.PerPage > 0
            ? dto.PerPage <= _dataSettings.Value.PaginationMaxPerPage
                ? dto.PerPage
                : _dataSettings.Value.PaginationMaxPerPage
            : _dataSettings.Value.PaginationPerPage;
        var position = dto.PerPage * (dto.Page - 1);
        var predicateExpr = await _filterService.Parse(filterDto, dto.Predicate, currentUser);
        if (!await _songRepository.SongExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var song = await _songRepository.GetSongAsync(id);
        if ((currentUser == null || !await _resourceService.HasPermission(currentUser, Roles.Administrator)) &&
            song.IsHidden)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var charts = await _songRepository.GetSongChartsAsync(id, dto.Order, dto.Desc, position, dto.PerPage,
            dto.Search, predicateExpr);
        var list = new List<ChartDto>();
        var total = await _songRepository.CountSongChartsAsync(id, dto.Search, predicateExpr);

        foreach (var chart in charts) list.Add(await _dtoMapper.MapChartAsync<ChartDto>(chart, currentUser));

        return Ok(new ResponseDto<IEnumerable<ChartDto>>
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
    public async Task<IActionResult> GetSongLikes([FromRoute] Guid id, [FromQuery] ArrayWithTimeRequestDto dto)
    {
        dto.PerPage = dto.PerPage > 0
            ? dto.PerPage <= _dataSettings.Value.PaginationMaxPerPage
                ? dto.PerPage
                : _dataSettings.Value.PaginationMaxPerPage
            : _dataSettings.Value.PaginationPerPage;
        var position = dto.PerPage * (dto.Page - 1);
        if (!await _songRepository.SongExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var currentUser = await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        var song = await _songRepository.GetSongAsync(id);
        if ((currentUser == null || !await _resourceService.HasPermission(currentUser, Roles.Administrator)) &&
            song.IsHidden)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var likes = await _likeRepository.GetLikesAsync(dto.Order, dto.Desc, position, dto.PerPage,
            e => e.ResourceId == id);
        var list = _mapper.Map<List<LikeDto>>(likes);
        var total = await _likeRepository.CountLikesAsync(e => e.ResourceId == id);

        return Ok(new ResponseDto<IEnumerable<LikeDto>>
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
    ///     Likes a specific song.
    /// </summary>
    /// <param name="id">A song's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="201">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="404">When the specified song is not found.</response>
    [HttpPost("{id:guid}/likes")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status201Created, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> CreateLike([FromRoute] Guid id)
    {
        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!await _songRepository.SongExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var song = await _songRepository.GetSongAsync(id);

        if (!await _resourceService.HasPermission(currentUser, Roles.Administrator) && song.IsHidden)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        if (song.IsLocked)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.Locked
            });

        if (!await _likeService.CreateLikeAsync(song, currentUser.Id))
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
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> RemoveLike([FromRoute] Guid id)
    {
        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!await _songRepository.SongExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var song = await _songRepository.GetSongAsync(id);

        if (!await _resourceService.HasPermission(currentUser, Roles.Administrator) && song.IsHidden)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        if (song.IsLocked)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.Locked
            });

        if (!await _likeService.RemoveLikeAsync(song, currentUser.Id))
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
    public async Task<IActionResult> GetSongComments([FromRoute] Guid id, [FromQuery] ArrayWithTimeRequestDto dto)
    {
        var currentUser = await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        dto.PerPage = dto.PerPage > 0
            ? dto.PerPage <= _dataSettings.Value.PaginationMaxPerPage
                ? dto.PerPage
                : _dataSettings.Value.PaginationMaxPerPage
            : _dataSettings.Value.PaginationPerPage;
        var position = dto.PerPage * (dto.Page - 1);
        if (!await _songRepository.SongExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var song = await _songRepository.GetSongAsync(id);
        if ((currentUser == null || !await _resourceService.HasPermission(currentUser, Roles.Administrator)) &&
            song.IsHidden)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var comments = await _commentRepository.GetCommentsAsync(dto.Order, dto.Desc, position, dto.PerPage,
            e => e.ResourceId == id);
        var list = new List<CommentDto>();
        var total = await _commentRepository.CountCommentsAsync(e => e.ResourceId == id);

        foreach (var comment in comments) list.Add(await _dtoMapper.MapCommentAsync<CommentDto>(comment, currentUser));

        return Ok(new ResponseDto<IEnumerable<CommentDto>>
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
        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!await _resourceService.HasPermission(currentUser, Roles.Member))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        if (!await _songRepository.SongExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var song = await _songRepository.GetSongAsync(id);

        if (!await _resourceService.HasPermission(currentUser, Roles.Administrator) && song.IsHidden)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        if (song.IsLocked)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.Locked
            });

        var comment = new Comment
        {
            ResourceId = song.Id,
            Content = dto.Content,
            Language = dto.Language,
            OwnerId = currentUser.Id,
            DateCreated = DateTimeOffset.UtcNow
        };
        if (!await _commentRepository.CreateCommentAsync(comment))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return StatusCode(StatusCodes.Status201Created);
    }
}