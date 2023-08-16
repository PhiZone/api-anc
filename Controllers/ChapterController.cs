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

[Route("chapters")]
[ApiVersion("2.0")]
[ApiController]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
    Policy = "AllowAnonymous")]
public class ChapterController : Controller
{
    private readonly IChapterRepository _chapterRepository;
    private readonly ICommentRepository _commentRepository;
    private readonly IOptions<DataSettings> _dataSettings;
    private readonly IDtoMapper _dtoMapper;
    private readonly IFileStorageService _fileStorageService;
    private readonly IFilterService _filterService;
    private readonly ILikeRepository _likeRepository;
    private readonly ILikeService _likeService;
    private readonly IMapper _mapper;
    private readonly IResourceService _resourceService;
    private readonly UserManager<User> _userManager;

    public ChapterController(IChapterRepository chapterRepository, IOptions<DataSettings> dataSettings,
        UserManager<User> userManager, IFilterService filterService, IFileStorageService fileStorageService,
        IDtoMapper dtoMapper, IMapper mapper, ILikeRepository likeRepository, ILikeService likeService,
        ICommentRepository commentRepository, IResourceService resourceService)
    {
        _chapterRepository = chapterRepository;
        _dataSettings = dataSettings;
        _userManager = userManager;
        _filterService = filterService;
        _dtoMapper = dtoMapper;
        _mapper = mapper;
        _likeRepository = likeRepository;
        _likeService = likeService;
        _commentRepository = commentRepository;
        _resourceService = resourceService;
        _fileStorageService = fileStorageService;
    }

    /// <summary>
    ///     Retrieves chapters.
    /// </summary>
    /// <returns>An array of chapters.</returns>
    /// <response code="200">Returns an array of chapters.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<ChapterDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetChapters([FromQuery] ArrayWithTimeRequestDto dto,
        [FromQuery] ChapterFilterDto? filterDto = null)
    {
        var currentUser = await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        dto.PerPage = dto.PerPage > 0 && dto.PerPage < _dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? _dataSettings.Value.PaginationPerPage : _dataSettings.Value.PaginationMaxPerPage;
        var position = dto.PerPage * (dto.Page - 1);
        var predicateExpr = await _filterService.Parse(filterDto, dto.Predicate, currentUser);
        var chapters = await _chapterRepository.GetChaptersAsync(dto.Order, dto.Desc, position,
            dto.PerPage, dto.Search, predicateExpr);
        var total = await _chapterRepository.CountChaptersAsync(dto.Search, predicateExpr);
        var list = new List<ChapterDto>();

        foreach (var chapter in chapters) list.Add(await _dtoMapper.MapChapterAsync<ChapterDto>(chapter, currentUser));

        return Ok(new ResponseDto<IEnumerable<ChapterDto>>
        {
            Status = ResponseStatus.Ok,
            Code = ResponseCodes.Ok,
            Total = total,
            PerPage = dto.PerPage,
            HasPrevious = position > 0,
            HasNext = dto.PerPage > 0 && position < total - total % dto.PerPage,
            Data = list
        });
    }

    /// <summary>
    ///     Retrieves a specific chapter.
    /// </summary>
    /// <param name="id">A chapter's ID.</param>
    /// <returns>A chapter.</returns>
    /// <response code="200">Returns a chapter.</response>
    /// <response code="304">
    ///     When the resource has not been updated since last retrieval. Requires <c>If-None-Match</c>.
    /// </response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified chapter is not found.</response>
    [HttpGet("{id:guid}")]
    [ServiceFilter(typeof(ETagFilter))]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<ChapterDto>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status304NotModified, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetChapter([FromRoute] Guid id)
    {
        var currentUser = await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        if (!await _chapterRepository.ChapterExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var chapter = await _chapterRepository.GetChapterAsync(id);
        var dto = await _dtoMapper.MapChapterAsync<ChapterDto>(chapter, currentUser);

        return Ok(new ResponseDto<ChapterDto> { Status = ResponseStatus.Ok, Code = ResponseCodes.Ok, Data = dto });
    }

    /// <summary>
    ///     Creates a new chapter.
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
    public async Task<IActionResult> CreateChapter([FromForm] ChapterCreationDto dto)
    {
        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!await _resourceService.HasPermission(currentUser, Roles.Administrator))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        var illustrationUrl = (await _fileStorageService.UploadImage<Chapter>(dto.Title, dto.Illustration, (16, 9)))
            .Item1;
        var chapter = new Chapter
        {
            Title = dto.Title,
            Subtitle = dto.Subtitle,
            Illustration = illustrationUrl,
            Illustrator = dto.Illustrator,
            Description = dto.Description,
            Accessibility = dto.Accessibility,
            IsHidden = dto.IsHidden,
            IsLocked = dto.IsLocked,
            OwnerId = currentUser.Id,
            DateCreated = DateTimeOffset.UtcNow,
            DateUpdated = DateTimeOffset.UtcNow
        };
        if (!await _chapterRepository.CreateChapterAsync(chapter))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return StatusCode(StatusCodes.Status201Created);
    }

    /// <summary>
    ///     Updates a chapter.
    /// </summary>
    /// <param name="id">A chapter's ID.</param>
    /// <param name="patchDocument">A JSON Patch Document.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified chapter is not found.</response>
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
    public async Task<IActionResult> UpdateChapter([FromRoute] Guid id,
        [FromBody] JsonPatchDocument<ChapterUpdateDto> patchDocument)
    {
        if (!await _chapterRepository.ChapterExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var chapter = await _chapterRepository.GetChapterAsync(id);

        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!await _resourceService.HasPermission(currentUser, Roles.Administrator))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        var dto = _mapper.Map<ChapterUpdateDto>(chapter);
        patchDocument.ApplyTo(dto, ModelState);

        if (!TryValidateModel(dto))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorDetailed,
                Code = ResponseCodes.InvalidData,
                Errors = ModelErrorTranslator.Translate(ModelState)
            });

        chapter.Title = dto.Title;
        chapter.Subtitle = dto.Subtitle;
        chapter.Illustrator = dto.Illustrator;
        chapter.Description = dto.Description;
        chapter.Accessibility = dto.Accessibility;
        chapter.IsHidden = dto.IsHidden;
        chapter.IsLocked = dto.IsLocked;
        chapter.DateUpdated = DateTimeOffset.UtcNow;

        if (!await _chapterRepository.UpdateChapterAsync(chapter))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return NoContent();
    }

    /// <summary>
    ///     Updates a chapter's illustration.
    /// </summary>
    /// <param name="id">A chapter's ID.</param>
    /// <param name="dto">The new illustration.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified chapter is not found.</response>
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
    public async Task<IActionResult> UpdateChapterIllustration([FromRoute] Guid id,
        [FromForm] FileDto dto)
    {
        if (!await _chapterRepository.ChapterExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var chapter = await _chapterRepository.GetChapterAsync(id);

        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!await _resourceService.HasPermission(currentUser, Roles.Administrator))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        if (dto.File != null)
        {
            chapter.Illustration =
                (await _fileStorageService.UploadImage<Chapter>(chapter.Title, dto.File, (16, 9))).Item1;
            chapter.DateUpdated = DateTimeOffset.UtcNow;
        }

        if (!await _chapterRepository.UpdateChapterAsync(chapter))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });
        return NoContent();
    }

    /// <summary>
    ///     Removes a chapter.
    /// </summary>
    /// <param name="id">A chapter's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified chapter is not found.</response>
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
    public async Task<IActionResult> RemoveChapter([FromRoute] Guid id)
    {
        if (!await _chapterRepository.ChapterExistsAsync(id))
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

        if (!await _chapterRepository.RemoveChapterAsync(id))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return NoContent();
    }

    /// <summary>
    ///     Retrieves songs from a specific chapter.
    /// </summary>
    /// <param name="id">A chapter's ID.</param>
    /// <returns>An array of songs.</returns>
    /// <response code="200">Returns an array of songs.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified chapter is not found.</response>
    [HttpGet("{id:guid}/songs")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<SongAdmitteeDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetChapterSongs([FromRoute] Guid id, [FromQuery] ArrayWithTimeRequestDto dto,
        [FromQuery] AdmissionFilterDto? filterDto = null)
    {
        var currentUser = await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        dto.PerPage = dto.PerPage > 0 && dto.PerPage < _dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? _dataSettings.Value.PaginationPerPage : _dataSettings.Value.PaginationMaxPerPage;
        var position = dto.PerPage * (dto.Page - 1);
        var predicateExpr = await _filterService.Parse(filterDto, dto.Predicate, currentUser);
        if (!await _chapterRepository.ChapterExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var chapter = await _chapterRepository.GetChapterAsync(id);
        if ((currentUser == null || !await _resourceService.HasPermission(currentUser, Roles.Administrator)) &&
            chapter.IsHidden)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var admissions = await _chapterRepository.GetChapterSongsAsync(id, dto.Order, dto.Desc, position, dto.PerPage,
            dto.Search, predicateExpr);
        var list = new List<SongAdmitteeDto>();
        var total = await _chapterRepository.CountChapterSongsAsync(id, dto.Search, predicateExpr);

        foreach (var admission in admissions)
            list.Add(await _dtoMapper.MapChapterSongAsync<SongAdmitteeDto>(admission, currentUser));

        return Ok(new ResponseDto<IEnumerable<SongAdmitteeDto>>
        {
            Status = ResponseStatus.Ok,
            Code = ResponseCodes.Ok,
            Total = total,
            PerPage = dto.PerPage,
            HasPrevious = position > 0,
            HasNext = dto.PerPage > 0 && position < total - total % dto.PerPage,
            Data = list
        });
    }

    /// <summary>
    ///     Retrieves likes from a specific chapter.
    /// </summary>
    /// <param name="id">A chapter's ID.</param>
    /// <returns>An array of likes.</returns>
    /// <response code="200">Returns an array of likes.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified chapter is not found.</response>
    [HttpGet("{id:guid}/likes")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<LikeDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetChapterLikes([FromRoute] Guid id, [FromQuery] ArrayWithTimeRequestDto dto)
    {
        dto.PerPage = dto.PerPage > 0 && dto.PerPage < _dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? _dataSettings.Value.PaginationPerPage : _dataSettings.Value.PaginationMaxPerPage;
        var position = dto.PerPage * (dto.Page - 1);

        if (!await _chapterRepository.ChapterExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var currentUser = await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        var chapter = await _chapterRepository.GetChapterAsync(id);
        if ((currentUser == null || !await _resourceService.HasPermission(currentUser, Roles.Administrator)) &&
            chapter.IsHidden)
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
            HasNext = dto.PerPage > 0 && position < total - total % dto.PerPage,
            Data = list
        });
    }

    /// <summary>
    ///     Likes a specific chapter.
    /// </summary>
    /// <param name="id">A chapter's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="201">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="404">When the specified chapter is not found.</response>
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
        if (!await _chapterRepository.ChapterExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var chapter = await _chapterRepository.GetChapterAsync(id);

        if (!await _resourceService.HasPermission(currentUser, Roles.Administrator) && chapter.IsHidden)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        if (chapter.IsLocked)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.Locked
            });

        if (!await _likeService.CreateLikeAsync(chapter, currentUser.Id))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.AlreadyDone
            });

        return StatusCode(StatusCodes.Status201Created);
    }

    /// <summary>
    ///     Removes the like from a specific chapter.
    /// </summary>
    /// <param name="id">A chapter's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified chapter is not found.</response>
    [HttpDelete("{id:guid}/likes")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> RemoveLike([FromRoute] Guid id)
    {
        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!await _chapterRepository.ChapterExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var chapter = await _chapterRepository.GetChapterAsync(id);

        if (!await _resourceService.HasPermission(currentUser, Roles.Administrator) && chapter.IsHidden)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        if (chapter.IsLocked)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.Locked
            });

        if (!await _likeService.RemoveLikeAsync(chapter, currentUser.Id))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.AlreadyDone
            });

        return NoContent();
    }

    /// <summary>
    ///     Retrieves comments from a specific chapter.
    /// </summary>
    /// <param name="id">A chapter's ID.</param>
    /// <returns>An array of comments.</returns>
    /// <response code="200">Returns an array of comments.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified chapter is not found.</response>
    [HttpGet("{id:guid}/comments")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<CommentDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetChapterComments([FromRoute] Guid id, [FromQuery] ArrayWithTimeRequestDto dto)
    {
        var currentUser = await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        dto.PerPage = dto.PerPage > 0 && dto.PerPage < _dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? _dataSettings.Value.PaginationPerPage : _dataSettings.Value.PaginationMaxPerPage;
        var position = dto.PerPage * (dto.Page - 1);
        if (!await _chapterRepository.ChapterExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var chapter = await _chapterRepository.GetChapterAsync(id);
        if ((currentUser == null || !await _resourceService.HasPermission(currentUser, Roles.Administrator)) &&
            chapter.IsHidden)
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
            HasNext = dto.PerPage > 0 && position < total - total % dto.PerPage,
            Data = list
        });
    }

    /// <summary>
    ///     Comments on a specific chapter.
    /// </summary>
    /// <param name="id">A chapter's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="201">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified chapter is not found.</response>
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
        if (!await _chapterRepository.ChapterExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var chapter = await _chapterRepository.GetChapterAsync(id);

        if (!await _resourceService.HasPermission(currentUser, Roles.Administrator) && chapter.IsHidden)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        if (chapter.IsLocked)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.Locked
            });

        var comment = new Comment
        {
            ResourceId = chapter.Id,
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