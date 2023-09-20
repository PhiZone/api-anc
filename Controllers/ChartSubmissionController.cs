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

[Route("studio/charts")]
[ApiVersion("2.0")]
[ApiController]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class ChartSubmissionController : Controller
{
    private readonly IAdmissionRepository _admissionRepository;
    private readonly IChartAssetSubmissionRepository _chartAssetSubmissionRepository;
    private readonly IChartService _chartService;
    private readonly IChartSubmissionRepository _chartSubmissionRepository;
    private readonly ICollaborationRepository _collaborationRepository;
    private readonly IOptions<DataSettings> _dataSettings;
    private readonly IDtoMapper _dtoMapper;
    private readonly IFeishuService _feishuService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IFilterService _filterService;
    private readonly ILogger<ChartSubmissionController> _logger;
    private readonly IMapper _mapper;
    private readonly INotificationService _notificationService;
    private readonly IResourceService _resourceService;
    private readonly ISongRepository _songRepository;
    private readonly ISongSubmissionRepository _songSubmissionRepository;
    private readonly ITemplateService _templateService;
    private readonly UserManager<User> _userManager;
    private readonly IVolunteerVoteRepository _volunteerVoteRepository;
    private readonly IVolunteerVoteService _volunteerVoteService;

    public ChartSubmissionController(IChartSubmissionRepository chartSubmissionRepository,
        IOptions<DataSettings> dataSettings, UserManager<User> userManager, IFilterService filterService,
        IFileStorageService fileStorageService, IDtoMapper dtoMapper, IMapper mapper, ISongRepository songRepository,
        IVolunteerVoteService volunteerVoteService, IResourceService resourceService,
        ISongSubmissionRepository songSubmissionRepository, IChartService chartService,
        IVolunteerVoteRepository volunteerVoteRepository, IAdmissionRepository admissionRepository,
        INotificationService notificationService, ITemplateService templateService,
        ICollaborationRepository collaborationRepository,
        IChartAssetSubmissionRepository chartAssetSubmissionRepository, ILogger<ChartSubmissionController> logger,
        IFeishuService feishuService)
    {
        _chartSubmissionRepository = chartSubmissionRepository;
        _dataSettings = dataSettings;
        _userManager = userManager;
        _filterService = filterService;
        _dtoMapper = dtoMapper;
        _mapper = mapper;
        _songRepository = songRepository;
        _volunteerVoteService = volunteerVoteService;
        _resourceService = resourceService;
        _songSubmissionRepository = songSubmissionRepository;
        _chartService = chartService;
        _volunteerVoteRepository = volunteerVoteRepository;
        _admissionRepository = admissionRepository;
        _notificationService = notificationService;
        _templateService = templateService;
        _collaborationRepository = collaborationRepository;
        _chartAssetSubmissionRepository = chartAssetSubmissionRepository;
        _logger = logger;
        _feishuService = feishuService;
        _fileStorageService = fileStorageService;
    }

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
        var position = dto.PerPage * (dto.Page - 1);
        var predicateExpr = await _filterService.Parse(filterDto, dto.Predicate, currentUser,
            submission => isVolunteer || submission.OwnerId == currentUser.Id);
        var chartSubmissions = await _chartSubmissionRepository.GetChartSubmissionsAsync(dto.Order, dto.Desc, position,
            dto.PerPage, dto.Search, predicateExpr);
        var total = await _chartSubmissionRepository.CountChartSubmissionsAsync(dto.Search, predicateExpr);
        var list = new List<ChartSubmissionDto>();

        foreach (var chartSubmission in chartSubmissions)
            list.Add(await _dtoMapper.MapChartSubmissionAsync<ChartSubmissionDto>(chartSubmission, currentUser));

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
        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!await _resourceService.HasPermission(currentUser, Roles.Qualified))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        if (!await _chartSubmissionRepository.ChartSubmissionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var chartSubmission = await _chartSubmissionRepository.GetChartSubmissionAsync(id);

        var dto = await _dtoMapper.MapChartSubmissionAsync<ChartSubmissionDto>(chartSubmission);

        return Ok(new ResponseDto<ChartSubmissionDto>
        {
            Status = ResponseStatus.Ok, Code = ResponseCodes.Ok, Data = dto
        });
    }

    /// <summary>
    ///     Creates a new chart submission.
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
    public async Task<IActionResult> CreateChartSubmission([FromForm] ChartSubmissionCreationDto dto)
    {
        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!await _resourceService.HasPermission(currentUser, Roles.Qualified))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (!_resourceService.GetAuthorIds(dto.AuthorName).Contains(currentUser.Id))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidAuthorInfo
            });

        Song? song = null;
        if (dto.SongId != null)
        {
            if (!await _songRepository.SongExistsAsync(dto.SongId.Value))
                return NotFound(new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentNotFound
                });

            song = await _songRepository.GetSongAsync(dto.SongId.Value);
            if (song.OwnerId != currentUser.Id && song.Accessibility == Accessibility.RefuseAny)
                return BadRequest(new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentIsPrivate
                });
        }

        SongSubmission? songSubmission = null;
        if (dto.SongSubmissionId != null)
        {
            if (!await _songSubmissionRepository.SongSubmissionExistsAsync(dto.SongSubmissionId.Value))
                return NotFound(new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentNotFound
                });

            songSubmission = await _songSubmissionRepository.GetSongSubmissionAsync(dto.SongSubmissionId.Value);
            if (songSubmission.OwnerId != currentUser.Id)
                return BadRequest(new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentIsPrivate
                });
            if (songSubmission.RepresentationId != null)
            {
                song = await _songRepository.GetSongAsync(songSubmission.RepresentationId.Value);
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

        var illustrationUrl = dto.Illustration != null
            ? (await _fileStorageService.UploadImage<Chart>(
                dto.Title ?? (song != null ? song.Title : songSubmission!.Title), dto.Illustration, (16, 9))).Item1
            : null;

        var chartSubmissionInfo = dto.File != null
            ? await _chartService.Upload(dto.Title ?? (song != null ? song.Title : songSubmission!.Title), dto.File)
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

        if (!await _chartSubmissionRepository.CreateChartSubmissionAsync(chartSubmission))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        // ReSharper disable once InvertIf
        if (song != null && song.OwnerId != currentUser.Id && song.Accessibility == Accessibility.RequireReview)
        {
            var admission = new Admission
            {
                AdmitterId = song.Id,
                AdmitteeId = chartSubmission.Id,
                Status = RequestStatus.Waiting,
                RequesterId = currentUser.Id,
                RequesteeId = song.OwnerId,
                DateCreated = DateTimeOffset.UtcNow
            };
            await _admissionRepository.CreateAdmissionAsync(admission);
            await _notificationService.Notify(song.Owner, currentUser, NotificationType.Requests, "chart-admission",
                new Dictionary<string, string>
                {
                    {
                        "User", _resourceService.GetRichText<User>(currentUser.Id.ToString(), currentUser.UserName!)
                    },
                    {
                        "Chart",
                        _resourceService.GetRichText<ChartSubmission>(chartSubmission.Id.ToString(),
                            await _resourceService.GetDisplayName(chartSubmission))
                    },
                    { "Song", _resourceService.GetRichText<Song>(song.Id.ToString(), song.GetDisplay()) },
                    {
                        "Admission",
                        _resourceService.GetComplexRichText<Admission>(admission.AdmitteeId.ToString(),
                            admission.AdmitterId.ToString(),
                            _templateService.GetMessage("more-info", admission.Requestee.Language)!)
                    }
                });
        }

        await _feishuService.Notify(chartSubmission, FeishuResources.ContentReviewalChat);
        _logger.LogInformation(LogEvents.ChartInfo, "New chart submission: {Title} [{Level} {Difficulty}]",
            dto.Title ?? song?.Title ?? songSubmission!.Title, dto.Level, Math.Floor(dto.Difficulty));

        return StatusCode(StatusCodes.Status201Created);
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
        if (!await _chartSubmissionRepository.ChartSubmissionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var chartSubmission = await _chartSubmissionRepository.GetChartSubmissionAsync(id);

        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if ((chartSubmission.OwnerId == currentUser.Id &&
             !await _resourceService.HasPermission(currentUser, Roles.Qualified)) ||
            (chartSubmission.OwnerId != currentUser.Id &&
             !await _resourceService.HasPermission(currentUser, Roles.Moderator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        var dto = _mapper.Map<ChartSubmissionUpdateDto>(chartSubmission);
        patchDocument.ApplyTo(dto, ModelState);

        if (!TryValidateModel(dto))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorDetailed,
                Code = ResponseCodes.InvalidData,
                Errors = ModelErrorTranslator.Translate(ModelState)
            });

        if (currentUser.Id == chartSubmission.OwnerId &&
            !_resourceService.GetAuthorIds(dto.AuthorName).Contains(currentUser.Id))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidAuthorInfo
            });

        chartSubmission.Title = dto.Title;
        chartSubmission.LevelType = dto.LevelType;
        chartSubmission.Level = dto.Level;
        chartSubmission.Difficulty = dto.Difficulty;
        chartSubmission.AuthorName = dto.AuthorName;
        chartSubmission.Illustrator = dto.Illustrator;
        chartSubmission.Description = dto.Description;
        chartSubmission.Accessibility = dto.Accessibility;
        chartSubmission.IsRanked = dto.IsRanked;
        chartSubmission.Status = RequestStatus.Waiting;
        chartSubmission.VolunteerStatus = RequestStatus.Waiting;
        chartSubmission.DateUpdated = DateTimeOffset.UtcNow;

        if (!await _chartSubmissionRepository.UpdateChartSubmissionAsync(chartSubmission))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        await _feishuService.Notify(chartSubmission, FeishuResources.ContentReviewalChat);
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
    /// <response code="404">When the specified chartSubmission is not found.</response>
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
        if (!await _chartSubmissionRepository.ChartSubmissionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var chartSubmission = await _chartSubmissionRepository.GetChartSubmissionAsync(id);

        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if ((chartSubmission.OwnerId == currentUser.Id &&
             !await _resourceService.HasPermission(currentUser, Roles.Qualified)) ||
            (chartSubmission.OwnerId != currentUser.Id &&
             !await _resourceService.HasPermission(currentUser, Roles.Moderator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (dto.File != null)
        {
            var chartSubmissionInfo = await _chartService.Upload(
                chartSubmission.Title ?? (chartSubmission.SongId != null
                    ? (await _songRepository.GetSongAsync(chartSubmission.SongId.Value)).Title
                    : (await _songSubmissionRepository.GetSongSubmissionAsync(chartSubmission.SongSubmissionId!.Value))
                    .Title), dto.File);
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

        if (!await _chartSubmissionRepository.UpdateChartSubmissionAsync(chartSubmission))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        await _feishuService.Notify(chartSubmission, FeishuResources.ContentReviewalChat);
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
    /// <response code="404">When the specified chartSubmission is not found.</response>
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
        if (!await _chartSubmissionRepository.ChartSubmissionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var chartSubmission = await _chartSubmissionRepository.GetChartSubmissionAsync(id);

        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if ((chartSubmission.OwnerId == currentUser.Id &&
             !await _resourceService.HasPermission(currentUser, Roles.Qualified)) ||
            (chartSubmission.OwnerId != currentUser.Id &&
             !await _resourceService.HasPermission(currentUser, Roles.Moderator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (dto.File != null)
        {
            chartSubmission.Illustration = (await _fileStorageService.UploadImage<Chart>(
                chartSubmission.Title ?? (chartSubmission.SongId != null
                    ? (await _songRepository.GetSongAsync(chartSubmission.SongId.Value)).Title
                    : (await _songSubmissionRepository.GetSongSubmissionAsync(chartSubmission.SongSubmissionId!.Value))
                    .Title), dto.File, (16, 9))).Item1;
            chartSubmission.Status = RequestStatus.Waiting;
            chartSubmission.VolunteerStatus = RequestStatus.Waiting;
            chartSubmission.DateUpdated = DateTimeOffset.UtcNow;
        }

        if (!await _chartSubmissionRepository.UpdateChartSubmissionAsync(chartSubmission))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        await _feishuService.Notify(chartSubmission, FeishuResources.ContentReviewalChat);
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
    /// <response code="404">When the specified chartSubmission is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpDelete("{id:guid}/illustration")]
    [Consumes("multipart/form-data")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> RemoveChartSubmissionIllustration([FromRoute] Guid id)
    {
        if (!await _chartSubmissionRepository.ChartSubmissionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var chartSubmission = await _chartSubmissionRepository.GetChartSubmissionAsync(id);

        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if ((chartSubmission.OwnerId == currentUser.Id &&
             !await _resourceService.HasPermission(currentUser, Roles.Qualified)) ||
            (chartSubmission.OwnerId != currentUser.Id &&
             !await _resourceService.HasPermission(currentUser, Roles.Moderator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        chartSubmission.Illustration = null;
        chartSubmission.Status = RequestStatus.Waiting;
        chartSubmission.VolunteerStatus = RequestStatus.Waiting;
        chartSubmission.DateUpdated = DateTimeOffset.UtcNow;

        if (!await _chartSubmissionRepository.UpdateChartSubmissionAsync(chartSubmission))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        await _feishuService.Notify(chartSubmission, FeishuResources.ContentReviewalChat);
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
        if (!await _chartSubmissionRepository.ChartSubmissionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var chartSubmission = await _chartSubmissionRepository.GetChartSubmissionAsync(id);

        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if ((chartSubmission.OwnerId == currentUser.Id &&
             !await _resourceService.HasPermission(currentUser, Roles.Qualified)) ||
            (chartSubmission.OwnerId != currentUser.Id &&
             !await _resourceService.HasPermission(currentUser, Roles.Moderator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (!await _chartSubmissionRepository.RemoveChartSubmissionAsync(id))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

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
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<ChartAssetDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetChartSubmissionAssets([FromRoute] Guid id,
        [FromQuery] ArrayRequestDto dto, [FromQuery] ChartAssetSubmissionFilterDto? filterDto = null)
    {
        if (!await _chartSubmissionRepository.ChartSubmissionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentNotFound
            });

        var chartSubmission = await _chartSubmissionRepository.GetChartSubmissionAsync(id);

        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if ((chartSubmission.OwnerId == currentUser.Id &&
             !await _resourceService.HasPermission(currentUser, Roles.Qualified)) ||
            (chartSubmission.OwnerId != currentUser.Id &&
             !await _resourceService.HasPermission(currentUser, Roles.Moderator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        dto.PerPage = dto.PerPage > 0 && dto.PerPage < _dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? _dataSettings.Value.PaginationPerPage : _dataSettings.Value.PaginationMaxPerPage;
        var position = dto.PerPage * (dto.Page - 1);
        var predicateExpr = await _filterService.Parse(filterDto, dto.Predicate, currentUser,
            e => e.ChartSubmissionId == id);
        var chartAssets = await _chartAssetSubmissionRepository.GetChartAssetSubmissionsAsync(dto.Order, dto.Desc,
            position, dto.PerPage, predicateExpr);
        var total = await _chartAssetSubmissionRepository.CountChartAssetSubmissionsAsync(predicateExpr);
        var list = chartAssets.Select(chartAsset => _mapper.Map<ChartAssetDto>(chartAsset)).ToList();

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
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<ChartAssetDto>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status304NotModified, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetChartSubmissionAsset([FromRoute] Guid id, [FromRoute] Guid assetId)
    {
        if (!await _chartSubmissionRepository.ChartSubmissionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentNotFound
            });

        var chartSubmission = await _chartSubmissionRepository.GetChartSubmissionAsync(id);

        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if ((chartSubmission.OwnerId == currentUser.Id &&
             !await _resourceService.HasPermission(currentUser, Roles.Qualified)) ||
            (chartSubmission.OwnerId != currentUser.Id &&
             !await _resourceService.HasPermission(currentUser, Roles.Moderator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (!await _chartAssetSubmissionRepository.ChartAssetSubmissionExistsAsync(assetId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var chartAsset = await _chartAssetSubmissionRepository.GetChartAssetSubmissionAsync(assetId);

        var dto = _mapper.Map<ChartAssetDto>(chartAsset);

        return Ok(new ResponseDto<ChartAssetDto> { Status = ResponseStatus.Ok, Code = ResponseCodes.Ok, Data = dto });
    }

    /// <summary>
    ///     Creates a chart submission's asset.
    /// </summary>
    /// <param name="id">A chart submission's ID.</param>
    /// <param name="dto">The new asset.</param>
    /// <returns>An empty body.</returns>
    /// <response code="201">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified chart submission is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPatch("{id:guid}/assets")]
    [Consumes("multipart/form-data")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status201Created, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> CreateChartSubmissionAsset([FromRoute] Guid id,
        [FromForm] ChartAssetCreationDto dto)
    {
        if (!await _chartSubmissionRepository.ChartSubmissionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentNotFound
            });

        var chartSubmission = await _chartSubmissionRepository.GetChartSubmissionAsync(id);

        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if ((chartSubmission.OwnerId == currentUser.Id &&
             !await _resourceService.HasPermission(currentUser, Roles.Qualified)) ||
            (chartSubmission.OwnerId != currentUser.Id &&
             !await _resourceService.HasPermission(currentUser, Roles.Moderator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        var chartAsset = new ChartAssetSubmission
        {
            ChartSubmissionId = id,
            Type = dto.Type,
            Name = dto.Name,
            File = (await _fileStorageService.Upload<ChartAsset>(
                chartSubmission.Title ?? (chartSubmission.SongId != null
                    ? (await _songRepository.GetSongAsync(chartSubmission.SongId.Value)).Title
                    : (await _songSubmissionRepository.GetSongSubmissionAsync(chartSubmission.SongSubmissionId!
                        .Value)).Title), dto.File)).Item1,
            OwnerId = currentUser.Id,
            DateCreated = DateTimeOffset.UtcNow,
            DateUpdated = DateTimeOffset.UtcNow
        };

        if (!await _chartAssetSubmissionRepository.CreateChartAssetSubmissionAsync(chartAsset) ||
            !await _chartSubmissionRepository.UpdateChartSubmissionAsync(chartSubmission))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return StatusCode(StatusCodes.Status201Created);
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
    [Consumes("multipart/form-data")]
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
        if (!await _chartSubmissionRepository.ChartSubmissionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentNotFound
            });

        var chartSubmission = await _chartSubmissionRepository.GetChartSubmissionAsync(id);

        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if ((chartSubmission.OwnerId == currentUser.Id &&
             !await _resourceService.HasPermission(currentUser, Roles.Qualified)) ||
            (chartSubmission.OwnerId != currentUser.Id &&
             !await _resourceService.HasPermission(currentUser, Roles.Moderator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (!await _chartAssetSubmissionRepository.ChartAssetSubmissionExistsAsync(assetId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var chartAsset = await _chartAssetSubmissionRepository.GetChartAssetSubmissionAsync(assetId);

        var dto = _mapper.Map<ChartAssetUpdateDto>(chartAsset);
        patchDocument.ApplyTo(dto, ModelState);

        if (!TryValidateModel(dto))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorDetailed,
                Code = ResponseCodes.InvalidData,
                Errors = ModelErrorTranslator.Translate(ModelState)
            });

        chartAsset.Type = dto.Type;
        chartAsset.Name = dto.Name;
        chartAsset.DateUpdated = DateTimeOffset.UtcNow;
        chartSubmission.Status = RequestStatus.Waiting;
        chartSubmission.VolunteerStatus = RequestStatus.Waiting;
        chartSubmission.DateUpdated = DateTimeOffset.UtcNow;

        if (!await _chartAssetSubmissionRepository.UpdateChartAssetSubmissionAsync(chartAsset) ||
            !await _chartSubmissionRepository.UpdateChartSubmissionAsync(chartSubmission))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        await _feishuService.Notify(chartSubmission, FeishuResources.ContentReviewalChat);
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
        if (!await _chartSubmissionRepository.ChartSubmissionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentNotFound
            });

        var chartSubmission = await _chartSubmissionRepository.GetChartSubmissionAsync(id);

        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if ((chartSubmission.OwnerId == currentUser.Id &&
             !await _resourceService.HasPermission(currentUser, Roles.Qualified)) ||
            (chartSubmission.OwnerId != currentUser.Id &&
             !await _resourceService.HasPermission(currentUser, Roles.Moderator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (!await _chartAssetSubmissionRepository.ChartAssetSubmissionExistsAsync(assetId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var chartAsset = await _chartAssetSubmissionRepository.GetChartAssetSubmissionAsync(assetId);

        if (dto.File != null)
        {
            chartAsset.File = (await _fileStorageService.Upload<ChartAsset>(
                chartSubmission.Title ?? (chartSubmission.SongId != null
                    ? (await _songRepository.GetSongAsync(chartSubmission.SongId.Value)).Title
                    : (await _songSubmissionRepository.GetSongSubmissionAsync(chartSubmission.SongSubmissionId!.Value))
                    .Title), dto.File)).Item1;
            chartAsset.DateUpdated = DateTimeOffset.UtcNow;
            chartSubmission.Status = RequestStatus.Waiting;
            chartSubmission.VolunteerStatus = RequestStatus.Waiting;
            chartSubmission.DateUpdated = DateTimeOffset.UtcNow;
        }

        if (!await _chartAssetSubmissionRepository.UpdateChartAssetSubmissionAsync(chartAsset) ||
            !await _chartSubmissionRepository.UpdateChartSubmissionAsync(chartSubmission))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        await _feishuService.Notify(chartSubmission, FeishuResources.ContentReviewalChat);
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
    [Consumes("multipart/form-data")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> RemoveChartSubmissionAsset([FromRoute] Guid id, [FromRoute] Guid assetId)
    {
        if (!await _chartSubmissionRepository.ChartSubmissionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentNotFound
            });

        var chartSubmission = await _chartSubmissionRepository.GetChartSubmissionAsync(id);

        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if ((chartSubmission.OwnerId == currentUser.Id &&
             !await _resourceService.HasPermission(currentUser, Roles.Qualified)) ||
            (chartSubmission.OwnerId != currentUser.Id &&
             !await _resourceService.HasPermission(currentUser, Roles.Moderator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (!await _chartAssetSubmissionRepository.ChartAssetSubmissionExistsAsync(assetId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        chartSubmission.Status = RequestStatus.Waiting;
        chartSubmission.VolunteerStatus = RequestStatus.Waiting;
        chartSubmission.DateUpdated = DateTimeOffset.UtcNow;

        if (!await _chartAssetSubmissionRepository.RemoveChartAssetSubmissionAsync(assetId) ||
            !await _chartSubmissionRepository.UpdateChartSubmissionAsync(chartSubmission))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        await _feishuService.Notify(chartSubmission, FeishuResources.ContentReviewalChat);
        return NoContent();
    }

    /// <summary>
    ///     Creates a new collaboration for a chart.
    /// </summary>
    /// <returns>An empty body.</returns>
    /// <response code="201">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified chart or author is not found.</response>
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
        if (!await _chartSubmissionRepository.ChartSubmissionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var chartSubmission = await _chartSubmissionRepository.GetChartSubmissionAsync(id);

        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if ((chartSubmission.OwnerId == currentUser.Id &&
             !await _resourceService.HasPermission(currentUser, Roles.Qualified)) ||
            (chartSubmission.OwnerId != currentUser.Id &&
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

        await _notificationService.Notify(invitee, currentUser, NotificationType.Requests, "chart-collab",
            new Dictionary<string, string>
            {
                { "User", _resourceService.GetRichText<User>(currentUser.Id.ToString(), currentUser.UserName!) },
                {
                    "Chart",
                    _resourceService.GetRichText<ChartSubmission>(chartSubmission.Id.ToString(),
                        await _resourceService.GetDisplayName(chartSubmission))
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
    ///     Retrieves votes from a specific chart submission.
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
    public async Task<IActionResult> GetChartSubmissionVotes([FromRoute] Guid id,
        [FromQuery] ArrayRequestDto dto)
    {
        dto.PerPage = dto.PerPage > 0 && dto.PerPage < _dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? _dataSettings.Value.PaginationPerPage : _dataSettings.Value.PaginationMaxPerPage;
        var position = dto.PerPage * (dto.Page - 1);

        if (!await _chartSubmissionRepository.ChartSubmissionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        var chartSubmission = await _chartSubmissionRepository.GetChartSubmissionAsync(id);
        if ((chartSubmission.OwnerId == currentUser.Id &&
             !await _resourceService.HasPermission(currentUser, Roles.Qualified)) ||
            (chartSubmission.OwnerId != currentUser.Id &&
             !await _resourceService.HasPermission(currentUser, Roles.Volunteer)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        var votes = await _volunteerVoteRepository.GetVolunteerVotesAsync(dto.Order, dto.Desc, position, dto.PerPage,
            e => e.ChartId == id);
        var list = _mapper.Map<List<VolunteerVoteDto>>(votes);
        var total = await _volunteerVoteRepository.CountVolunteerVotesAsync(e => e.ChartId == id);

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
    /// <response code="404">When the specified chartSubmission is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPost("{id:guid}/votes")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status201Created, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> CreateVote([FromRoute] Guid id, [FromBody] VolunteerVoteRequestDto dto)
    {
        if (!await _chartSubmissionRepository.ChartSubmissionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        var chartSubmission = await _chartSubmissionRepository.GetChartSubmissionAsync(id);
        if ((chartSubmission.OwnerId == currentUser.Id &&
             !await _resourceService.HasPermission(currentUser, Roles.Qualified)) ||
            (chartSubmission.OwnerId != currentUser.Id &&
             !await _resourceService.HasPermission(currentUser, Roles.Volunteer)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (!await _volunteerVoteService.CreateVolunteerVoteAsync(dto, chartSubmission, currentUser))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return StatusCode(StatusCodes.Status201Created);
    }

    /// <summary>
    ///     Removes the vote from a specific chart submission.
    /// </summary>
    /// <param name="id">A chart submission's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="404">When the specified chartSubmission is not found.</response>
    [HttpDelete("{id:guid}/votes")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> RemoveVote([FromRoute] Guid id)
    {
        if (!await _chartSubmissionRepository.ChartSubmissionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        var chartSubmission = await _chartSubmissionRepository.GetChartSubmissionAsync(id);
        var vote = await _volunteerVoteRepository.GetVolunteerVoteAsync(chartSubmission.Id, currentUser.Id);
        if ((vote.OwnerId == currentUser.Id && !await _resourceService.HasPermission(currentUser, Roles.Volunteer)) ||
            (vote.OwnerId != currentUser.Id && !await _resourceService.HasPermission(currentUser, Roles.Moderator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (!await _volunteerVoteService.RemoveVolunteerVoteAsync(chartSubmission, currentUser.Id))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.AlreadyDone
            });

        return NoContent();
    }
}