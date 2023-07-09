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

// ReSharper disable RouteTemplates.ParameterConstraintCanBeSpecified

namespace PhiZoneApi.Controllers;

[Route("songs")]
[ApiVersion("2.0")]
[ApiController]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
    Policy = "AllowAnonymous")]
public class SongController : Controller
{
    private readonly IOptions<DataSettings> _dataSettings;
    private readonly IDtoMapper _dtoMapper;
    private readonly IMapper _mapper;
    private readonly IFilterService _filterService;
    private readonly IFileStorageService _fileStorageService;
    private readonly ISongService _songService;
    private readonly ISongRepository _songRepository;
    private readonly UserManager<User> _userManager;

    public SongController(ISongRepository songRepository, IOptions<DataSettings> dataSettings,
        UserManager<User> userManager, IFilterService filterService, IFileStorageService fileStorageService,
        IDtoMapper dtoMapper, IMapper mapper, ISongService songService)
    {
        _songRepository = songRepository;
        _dataSettings = dataSettings;
        _userManager = userManager;
        _filterService = filterService;
        _dtoMapper = dtoMapper;
        _mapper = mapper;
        _songService = songService;
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
        var total = await _songRepository.CountAsync(dto.Search, predicateExpr);
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
    /// <param name="id">Song's ID.</param>
    /// <returns>A song.</returns>
    /// <response code="200">Returns a song.</response>
    /// <response code="304">When the resource has not been updated since last retrieval (requires header <c>If-None-Match</c>).</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified song is not found.</response>
    [HttpGet("{id}")]
    [ServiceFilter(typeof(ETagFilter))]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<SongDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetSong([FromRoute] Guid id)
    {
        var currentUser = await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        if (!await _songRepository.SongExistsAsync(id)) return NotFound();
        var song = await _songRepository.GetSongAsync(id);
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
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateSong([FromForm] SongCreationDto dto, [FromQuery] bool wait = false)
    {
        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!await _userManager.IsInRoleAsync(currentUser, Roles.Administrator))
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        }

        (string, TimeSpan)? songInfo = null;
        if (wait)
        {
            songInfo = await _songService.UploadAsync(dto.Title, dto.File);
            if (songInfo == null)
            {
                return BadRequest(new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.DataInvalid
                });
            }
        }
        
        var illustrationUrl = await _fileStorageService.UploadImage<Song>(dto.Title, dto.Illustration, (16, 9));
        var song = new Song
        {
            Title = dto.Title,
            EditionType = dto.EditionType,
            Edition = dto.Edition,
            AuthorName = dto.AuthorName,
            File = songInfo?.Item1,
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
            Duration = songInfo?.Item2,
            PreviewStart = dto.PreviewStart,
            PreviewEnd = dto.PreviewEnd,
            OwnerId = currentUser.Id,
            DateCreated = DateTimeOffset.UtcNow,
            DateUpdated = DateTimeOffset.UtcNow
        };
        
        if (!await _songRepository.CreateSongAsync(song))
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });
        }
        if (!wait)
        {
            await _songService.PublishAsync(dto.File, song.Id);
        }

        return StatusCode(StatusCodes.Status201Created);
    }

    /// <summary>
    ///     Updates a song.
    /// </summary>
    /// <param name="id">Song's ID.</param>
    /// <param name="patchDocument">A JSON Patch Document.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified song is not found.</response>
    [HttpPatch("{id}")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Consumes("application/json")]
    [Produces("text/plain", "application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
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
        if (!await _userManager.IsInRoleAsync(currentUser, Roles.Administrator))
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        }

        var dto = _mapper.Map<SongUpdateDto>(song);
        patchDocument.ApplyTo(dto, ModelState);

        if (!TryValidateModel(dto))
        {
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorDetailed,
                Code = ResponseCodes.DataInvalid,
                Errors = ModelErrorTranslator.Translate(ModelState)
            });
        }

        song = _mapper.Map<Song>(dto);

        if (!await _songRepository.UpdateSongAsync(song))
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });
        }

        return NoContent();
    }

    /// <summary>
    ///     Updates a song's file.
    /// </summary>
    /// <param name="id">Song's ID.</param>
    /// <param name="dto">The new file.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified song is not found.</response>
    [HttpPatch("{id}/file")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Consumes("multipart/form-data")]
    [Produces("text/plain", "application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> UpdateSongFile([FromRoute] Guid id, [FromForm] FileDto dto, [FromQuery] bool wait = false)
    {
        if (!await _songRepository.SongExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var song = await _songRepository.GetSongAsync(id);

        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!await _userManager.IsInRoleAsync(currentUser, Roles.Administrator))
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        }

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
                {
                    return BadRequest(new ResponseDto<object>
                    {
                        Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.DataInvalid
                    });
                }

                song.File = songInfo.Value.Item1;
                song.Duration = songInfo.Value.Item2;
            }
        }

        if (!await _songRepository.UpdateSongAsync(song))
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });
        }

        return NoContent();
    }

    /// <summary>
    ///     Updates a song's illustration.
    /// </summary>
    /// <param name="id">Song's ID.</param>
    /// <param name="dto">The new illustration.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified song is not found.</response>
    [HttpPatch("{id}/illustration")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Consumes("multipart/form-data")]
    [Produces("text/plain", "application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> UpdateSongIllustration([FromRoute] Guid id, [FromForm] ResourceIllustrationDto dto)
    {
        if (!await _songRepository.SongExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var song = await _songRepository.GetSongAsync(id);

        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!await _userManager.IsInRoleAsync(currentUser, Roles.Administrator))
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        }

        if (dto.Illustration != null)
        {
            song.Illustration = await _fileStorageService.UploadImage<Song>(song.Title, dto.Illustration, (16, 9));
        }

        if (!await _songRepository.UpdateSongAsync(song))
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });
        }

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
    [HttpGet("{id}/charts")]
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
        if (!await _songRepository.SongExistsAsync(id)) return NotFound();
        var charts = await _songRepository.GetSongChartsAsync(id, dto.Order, dto.Desc, position, dto.PerPage,
            dto.Search, predicateExpr);
        var list = new List<ChartDto>();
        var total = await _songRepository.CountChartsAsync(id, dto.Search, predicateExpr);

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
}