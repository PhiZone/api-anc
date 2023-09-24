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

[Route("studio/songs")]
[ApiVersion("2.0")]
[ApiController]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class SongSubmissionController : Controller
{
    private readonly IAuthorshipRepository _authorshipRepository;
    private readonly ICollaborationRepository _collaborationRepository;
    private readonly IOptions<DataSettings> _dataSettings;
    private readonly IFeishuService _feishuService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IFilterService _filterService;
    private readonly ILogger<SongSubmissionController> _logger;
    private readonly IMapper _mapper;
    private readonly INotificationService _notificationService;
    private readonly IResourceService _resourceService;
    private readonly ISongService _songService;
    private readonly ISongSubmissionRepository _songSubmissionRepository;
    private readonly ISubmissionService _submissionService;
    private readonly ITemplateService _templateService;
    private readonly UserManager<User> _userManager;

    public SongSubmissionController(ISongSubmissionRepository songSubmissionRepository,
        IOptions<DataSettings> dataSettings, UserManager<User> userManager, IFilterService filterService,
        IFileStorageService fileStorageService, IMapper mapper, ISongService songService,
        IAuthorshipRepository authorshipRepository, ISubmissionService submissionService,
        IResourceService resourceService, ICollaborationRepository collaborationRepository,
        INotificationService notificationService, ITemplateService templateService, IFeishuService feishuService,
        ILogger<SongSubmissionController> logger)
    {
        _songSubmissionRepository = songSubmissionRepository;
        _dataSettings = dataSettings;
        _userManager = userManager;
        _filterService = filterService;
        _mapper = mapper;
        _songService = songService;
        _authorshipRepository = authorshipRepository;
        _submissionService = submissionService;
        _resourceService = resourceService;
        _collaborationRepository = collaborationRepository;
        _notificationService = notificationService;
        _templateService = templateService;
        _logger = logger;
        _feishuService = feishuService;
        _fileStorageService = fileStorageService;
    }

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
        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        var isVolunteer = await _resourceService.HasPermission(currentUser, Roles.Volunteer);
        if (!await _resourceService.HasPermission(currentUser, Roles.Qualified))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        dto.PerPage = dto.PerPage > 0 && dto.PerPage < _dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? _dataSettings.Value.PaginationPerPage : _dataSettings.Value.PaginationMaxPerPage;
        dto.Page = dto.Page > 1 ? dto.Page : 1;
        var position = dto.PerPage * (dto.Page - 1);
        var predicateExpr = await _filterService.Parse(filterDto, dto.Predicate, currentUser,
            submission => isVolunteer || submission.OwnerId == currentUser.Id);
        var songSubmissions = await _songSubmissionRepository.GetSongSubmissionsAsync(dto.Order, dto.Desc, position,
            dto.PerPage, dto.Search, predicateExpr);
        var total = await _songSubmissionRepository.CountSongSubmissionsAsync(dto.Search, predicateExpr);
        var list = songSubmissions.Select(songSubmission => _mapper.Map<SongSubmissionDto>(songSubmission)).ToList();

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
        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!await _resourceService.HasPermission(currentUser, Roles.Qualified))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        if (!await _songSubmissionRepository.SongSubmissionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var songSubmission = await _songSubmissionRepository.GetSongSubmissionAsync(id);
        var dto = _mapper.Map<SongSubmissionDto>(songSubmission);

        return Ok(
            new ResponseDto<SongSubmissionDto> { Status = ResponseStatus.Ok, Code = ResponseCodes.Ok, Data = dto });
    }

    /// <summary>
    ///     Creates a new song submission.
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
    [ProducesResponseType(typeof(void), StatusCodes.Status201Created, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> CreateSongSubmission([FromForm] SongSubmissionCreationDto dto,
        [FromQuery] bool wait = false)
    {
        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!await _resourceService.HasPermission(currentUser, Roles.Qualified))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        (string, string, TimeSpan)? songSubmissionInfo = null;
        if (wait)
        {
            songSubmissionInfo = await _songService.UploadAsync(dto.Title, dto.File);
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
            _logger.LogInformation(LogEvents.SongInfo, "New song submission: {Title}", dto.Title);
        }
        else if (!(TimeSpan.Zero <= dto.PreviewStart && dto.PreviewStart <= dto.PreviewEnd))
        {
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidTimeRelation
            });
        }

        var illustrationUrl = (await _fileStorageService.UploadImage<Song>(dto.Title, dto.Illustration, (16, 9))).Item1;
        string? license = null;
        if (dto.License != null) license = (await _fileStorageService.Upload<Song>(dto.Title, dto.License)).Item1;

        string? originalityProof = null;
        if (dto.OriginalityProof != null)
        {
            if (!_resourceService.GetAuthorIds(dto.AuthorName).Contains(currentUser.Id))
                return BadRequest(new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidAuthorInfo
                });
            originalityProof = (await _fileStorageService.Upload<SongSubmission>(dto.Title, dto.OriginalityProof))
                .Item1;
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
            Status = RequestStatus.Waiting,
            OwnerId = currentUser.Id,
            DateCreated = DateTimeOffset.UtcNow,
            DateUpdated = DateTimeOffset.UtcNow
        };

        if (!await _songSubmissionRepository.CreateSongSubmissionAsync(songSubmission))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        var authorship = new Authorship
        {
            ResourceId = songSubmission.Id, AuthorId = currentUser.Id, DateCreated = DateTimeOffset.UtcNow
        };

        await _authorshipRepository.CreateAuthorshipAsync(authorship);

        if (!wait)
        {
            await _songService.PublishAsync(dto.File, songSubmission.Id, true);
            _logger.LogInformation(LogEvents.SongInfo, "Scheduled new song submission: {Title}", dto.Title);
        }
        else
        {
            await _feishuService.Notify(songSubmission, FeishuResources.ContentReviewalChat);
        }

        return StatusCode(StatusCodes.Status201Created);
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
        if (!await _songSubmissionRepository.SongSubmissionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var songSubmission = await _songSubmissionRepository.GetSongSubmissionAsync(id);

        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if ((songSubmission.OwnerId == currentUser.Id &&
             !await _resourceService.HasPermission(currentUser, Roles.Qualified)) ||
            (songSubmission.OwnerId != currentUser.Id &&
             !await _resourceService.HasPermission(currentUser, Roles.Moderator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        var dto = _mapper.Map<SongSubmissionUpdateDto>(songSubmission);
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
                !_resourceService.GetAuthorIds(dto.AuthorName).Contains(currentUser.Id))
                return BadRequest(new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidAuthorInfo
                });

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
        songSubmission.DateUpdated = DateTimeOffset.UtcNow;
        songSubmission.Status = RequestStatus.Waiting;

        if (!await _songSubmissionRepository.UpdateSongSubmissionAsync(songSubmission))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        await _feishuService.Notify(songSubmission, FeishuResources.ContentReviewalChat);
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
        if (!await _songSubmissionRepository.SongSubmissionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var songSubmission = await _songSubmissionRepository.GetSongSubmissionAsync(id);

        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if ((songSubmission.OwnerId == currentUser.Id &&
             !await _resourceService.HasPermission(currentUser, Roles.Qualified)) ||
            (songSubmission.OwnerId != currentUser.Id &&
             !await _resourceService.HasPermission(currentUser, Roles.Moderator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (dto.File != null)
        {
            if (!wait)
            {
                await _songService.PublishAsync(dto.File, songSubmission.Id, true);
            }
            else
            {
                var songSubmissionInfo = await _songService.UploadAsync(songSubmission.Title, dto.File);
                if (songSubmissionInfo == null)
                    return BadRequest(new ResponseDto<object>
                    {
                        Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidData
                    });

                songSubmission.File = songSubmissionInfo.Value.Item1;
                songSubmission.FileChecksum = songSubmissionInfo.Value.Item2;
                songSubmission.Duration = songSubmissionInfo.Value.Item3;
                songSubmission.DateUpdated = DateTimeOffset.UtcNow;

                if (songSubmission.PreviewEnd > songSubmission.Duration)
                    songSubmission.PreviewEnd = songSubmission.Duration.Value;

                if (songSubmission.PreviewStart > songSubmission.PreviewEnd)
                    songSubmission.PreviewStart = TimeSpan.Zero;
            }

            songSubmission.Status = RequestStatus.Waiting;
        }

        if (!await _songSubmissionRepository.UpdateSongSubmissionAsync(songSubmission))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        await _feishuService.Notify(songSubmission, FeishuResources.ContentReviewalChat);
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
        if (!await _songSubmissionRepository.SongSubmissionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var songSubmission = await _songSubmissionRepository.GetSongSubmissionAsync(id);

        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if ((songSubmission.OwnerId == currentUser.Id &&
             !await _resourceService.HasPermission(currentUser, Roles.Qualified)) ||
            (songSubmission.OwnerId != currentUser.Id &&
             !await _resourceService.HasPermission(currentUser, Roles.Moderator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        if (dto.File != null)
        {
            songSubmission.Illustration =
                (await _fileStorageService.UploadImage<Song>(songSubmission.Title, dto.File, (16, 9))).Item1;
            songSubmission.DateUpdated = DateTimeOffset.UtcNow;
            songSubmission.Status = RequestStatus.Waiting;
        }

        if (!await _songSubmissionRepository.UpdateSongSubmissionAsync(songSubmission))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        await _feishuService.Notify(songSubmission, FeishuResources.ContentReviewalChat);
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
        if (!await _songSubmissionRepository.SongSubmissionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var songSubmission = await _songSubmissionRepository.GetSongSubmissionAsync(id);

        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if ((songSubmission.OwnerId == currentUser.Id &&
             !await _resourceService.HasPermission(currentUser, Roles.Qualified)) ||
            (songSubmission.OwnerId != currentUser.Id &&
             !await _resourceService.HasPermission(currentUser, Roles.Moderator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (dto.File != null)
        {
            songSubmission.License = (await _fileStorageService.Upload<Song>(songSubmission.Title, dto.File)).Item1;
            songSubmission.DateUpdated = DateTimeOffset.UtcNow;
            songSubmission.Status = RequestStatus.Waiting;
        }

        if (!await _songSubmissionRepository.UpdateSongSubmissionAsync(songSubmission))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        await _feishuService.Notify(songSubmission, FeishuResources.ContentReviewalChat);
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
        if (!await _songSubmissionRepository.SongSubmissionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var songSubmission = await _songSubmissionRepository.GetSongSubmissionAsync(id);

        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if ((songSubmission.OwnerId == currentUser.Id &&
             !await _resourceService.HasPermission(currentUser, Roles.Qualified)) ||
            (songSubmission.OwnerId != currentUser.Id &&
             !await _resourceService.HasPermission(currentUser, Roles.Moderator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        songSubmission.License = null;
        songSubmission.DateUpdated = DateTimeOffset.UtcNow;
        songSubmission.Status = RequestStatus.Waiting;

        if (!await _songSubmissionRepository.UpdateSongSubmissionAsync(songSubmission))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        await _feishuService.Notify(songSubmission, FeishuResources.ContentReviewalChat);
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
        if (!await _songSubmissionRepository.SongSubmissionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var songSubmission = await _songSubmissionRepository.GetSongSubmissionAsync(id);

        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if ((songSubmission.OwnerId == currentUser.Id &&
             !await _resourceService.HasPermission(currentUser, Roles.Qualified)) ||
            (songSubmission.OwnerId != currentUser.Id &&
             !await _resourceService.HasPermission(currentUser, Roles.Moderator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (dto.File != null)
        {
            songSubmission.OriginalityProof =
                (await _fileStorageService.Upload<SongSubmission>(songSubmission.Title, dto.File)).Item1;
            songSubmission.DateUpdated = DateTimeOffset.UtcNow;
            songSubmission.Status = RequestStatus.Waiting;
        }

        if (!await _songSubmissionRepository.UpdateSongSubmissionAsync(songSubmission))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        await _feishuService.Notify(songSubmission, FeishuResources.ContentReviewalChat);
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
        if (!await _songSubmissionRepository.SongSubmissionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var songSubmission = await _songSubmissionRepository.GetSongSubmissionAsync(id);

        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if ((songSubmission.OwnerId == currentUser.Id &&
             !await _resourceService.HasPermission(currentUser, Roles.Qualified)) ||
            (songSubmission.OwnerId != currentUser.Id &&
             !await _resourceService.HasPermission(currentUser, Roles.Moderator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        songSubmission.OriginalityProof = null;
        songSubmission.DateUpdated = DateTimeOffset.UtcNow;
        songSubmission.Status = RequestStatus.Waiting;

        if (!await _songSubmissionRepository.UpdateSongSubmissionAsync(songSubmission))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        await _feishuService.Notify(songSubmission, FeishuResources.ContentReviewalChat);
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
        if (!await _songSubmissionRepository.SongSubmissionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var songSubmission = await _songSubmissionRepository.GetSongSubmissionAsync(id);

        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if ((songSubmission.OwnerId == currentUser.Id &&
             !await _resourceService.HasPermission(currentUser, Roles.Qualified)) ||
            (songSubmission.OwnerId != currentUser.Id &&
             !await _resourceService.HasPermission(currentUser, Roles.Moderator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (!await _songSubmissionRepository.RemoveSongSubmissionAsync(id))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return NoContent();
    }

    /// <summary>
    ///     Creates a new collaboration for a song.
    /// </summary>
    /// <returns>An empty body.</returns>
    /// <response code="201">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified song or author is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPost("{id:guid}/collaborations")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status201Created, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> CreateCollaboration([FromRoute] Guid id, [FromBody] CollaborationCreationDto dto)
    {
        if (!await _songSubmissionRepository.SongSubmissionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var songSubmission = await _songSubmissionRepository.GetSongSubmissionAsync(id);

        if (songSubmission.OriginalityProof == null)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidOperation
            });

        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if ((songSubmission.OwnerId == currentUser.Id &&
             !await _resourceService.HasPermission(currentUser, Roles.Qualified)) ||
            (songSubmission.OwnerId != currentUser.Id &&
             !await _resourceService.HasPermission(currentUser, Roles.Moderator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        var invitee = await _userManager.FindByIdAsync(dto.InviteeId.ToString());
        if (invitee == null)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.UserNotFound
            });

        if (await _collaborationRepository.CollaborationExistsAsync(id, dto.InviteeId))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.AlreadyDone
            });

        var collaboration = new Collaboration
        {
            SubmissionId = id,
            InviterId = currentUser.Id,
            InviteeId = dto.InviteeId,
            Position = dto.Position,
            DateCreated = DateTimeOffset.UtcNow
        };

        if (!await _collaborationRepository.CreateCollaborationAsync(collaboration))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        await _notificationService.Notify(invitee, currentUser, NotificationType.Requests, "song-collab",
            new Dictionary<string, string>
            {
                { "User", _resourceService.GetRichText<User>(currentUser.Id.ToString(), currentUser.UserName!) },
                {
                    "Song",
                    _resourceService.GetRichText<SongSubmission>(songSubmission.Id.ToString(),
                        songSubmission.GetDisplay())
                },
                {
                    "Collaboration",
                    _resourceService.GetRichText<Collaboration>(collaboration.Id.ToString(),
                        _templateService.GetMessage("more-info", invitee.Language)!)
                }
            });
        return StatusCode(StatusCodes.Status201Created);
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
        if (!await _songSubmissionRepository.SongSubmissionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!await _resourceService.HasPermission(currentUser, Roles.Volunteer))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        var songSubmission = await _songSubmissionRepository.GetSongSubmissionAsync(id);

        songSubmission.Status = dto.Status;
        songSubmission.ReviewerId = currentUser.Id;
        songSubmission.Message = dto.Message;
        switch (songSubmission.Status)
        {
            case RequestStatus.Approved:
            {
                var song = await _submissionService.ApproveSong(songSubmission, dto.IsOriginal, dto.IsHidden);
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
                await _submissionService.RejectSong(songSubmission);
                break;
            case RequestStatus.Waiting:
            default:
                break;
        }

        if (!await _songSubmissionRepository.UpdateSongSubmissionAsync(songSubmission))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return NoContent();
    }
}