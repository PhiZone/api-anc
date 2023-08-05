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

[Route("chartSubmissions")]
[ApiVersion("2.0")]
[ApiController]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
    Policy = "AllowAnonymous")]
public class ChartSubmissionController : Controller
{
    private readonly IAdmissionRepository _admissionRepository;
    private readonly IChartService _chartService;
    private readonly IChartSubmissionRepository _chartSubmissionRepository;
    private readonly ICollaborationRepository _collaborationRepository;
    private readonly IOptions<DataSettings> _dataSettings;
    private readonly IFileStorageService _fileStorageService;
    private readonly IFilterService _filterService;
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
        IFileStorageService fileStorageService, IDtoMapper dtoMapper, IMapper mapper,
        ISubmissionService submissionService, ISongRepository songRepository, ILikeRepository likeRepository,
        ILikeService likeService, IVolunteerVoteService volunteerVoteService,
        IAuthorshipRepository authorshipRepository, IResourceService resourceService,
        ISongSubmissionRepository songSubmissionRepository, IChartService chartService,
        IVolunteerVoteRepository volunteerVoteRepository, IAdmissionRepository admissionRepository,
        INotificationService notificationService, ITemplateService templateService,
        ICollaborationRepository collaborationRepository)
    {
        _chartSubmissionRepository = chartSubmissionRepository;
        _dataSettings = dataSettings;
        _userManager = userManager;
        _filterService = filterService;
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
        _fileStorageService = fileStorageService;
    }

    /// <summary>
    ///     Retrieves chart submissions.
    /// </summary>
    /// <returns>An array of chart submissions.</returns>
    /// <response code="200">Returns an array of chart submissions.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<ChartSubmissionDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetChartSubmissions([FromQuery] ArrayWithTimeRequestDto dto,
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
        dto.PerPage = dto.PerPage > 0
            ? dto.PerPage <= _dataSettings.Value.PaginationMaxPerPage
                ? dto.PerPage
                : _dataSettings.Value.PaginationMaxPerPage
            : _dataSettings.Value.PaginationPerPage;
        var position = dto.PerPage * (dto.Page - 1);
        var predicateExpr = await _filterService.Parse(filterDto, dto.Predicate, currentUser,
            submission => isVolunteer || submission.OwnerId == currentUser.Id);
        var chartSubmissions = await _chartSubmissionRepository.GetChartSubmissionsAsync(dto.Order, dto.Desc, position,
            dto.PerPage, dto.Search, predicateExpr);
        var total = await _chartSubmissionRepository.CountChartSubmissionsAsync(dto.Search, predicateExpr);
        var list = chartSubmissions.Select(chartSubmission => _mapper.Map<ChartSubmissionDto>(chartSubmission))
            .ToList();

        return Ok(new ResponseDto<IEnumerable<ChartSubmissionDto>>
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
    ///     Retrieves a specific chart submission.
    /// </summary>
    /// <param name="id">A chart submission's ID.</param>
    /// <returns>A chart submission.</returns>
    /// <response code="200">Returns a chart submission.</response>
    /// <response code="304">
    ///     When the resource has not been updated since last retrieval. Requires <c>If-None-Match</c>.
    /// </response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified chart submission is not found.</response>
    [HttpGet("{id:guid}")]
    [ServiceFilter(typeof(ETagFilter))]
    [Produces("application/json", "text/plain")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<ChartSubmissionDto>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status304NotModified, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
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

        var dto = _mapper.Map<ChartSubmissionDto>(chartSubmission);

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
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Consumes("multipart/form-data")]
    [Produces("text/plain", "application/json")]
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
        }

        if ((song != null && songSubmission != null) || (song == null && songSubmission == null))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorWithMessage,
                Code = ResponseCodes.InvalidData,
                Message = "Must enter one and only one field between song and song submission."
            });

        var illustrationUrl = dto.Illustration != null
            ? (await _fileStorageService.UploadImage<ChartSubmission>(
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
                            chartSubmission.GetDisplay())
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
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Consumes("application/json")]
    [Produces("text/plain", "application/json")]
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

        await _volunteerVoteRepository.RemoveVolunteerVotesAsync(
            await _volunteerVoteRepository.GetVolunteerVotesAsync("DateCreated", true, 0, -1,
                e => e.ChartId == chartSubmission.Id));

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
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Consumes("multipart/form-data")]
    [Produces("text/plain", "application/json")]
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
                chartSubmission.Title ?? (chartSubmission.Song != null
                    ? chartSubmission.Song.Title
                    : chartSubmission.SongSubmission!.Title), dto.File);
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

        await _volunteerVoteRepository.RemoveVolunteerVotesAsync(
            await _volunteerVoteRepository.GetVolunteerVotesAsync("DateCreated", true, 0, -1,
                e => e.ChartId == chartSubmission.Id));

        return NoContent();
    }

    /// <summary>
    ///     Removes a chart submission's file.
    /// </summary>
    /// <param name="id">A chart submission's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified chartSubmission is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpDelete("{id:guid}/file")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Consumes("multipart/form-data")]
    [Produces("text/plain", "application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> RemoveChartSubmissionFile([FromRoute] Guid id)
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

        chartSubmission.File = null;
        chartSubmission.Status = RequestStatus.Waiting;
        chartSubmission.VolunteerStatus = RequestStatus.Waiting;
        chartSubmission.DateUpdated = DateTimeOffset.UtcNow;

        if (!await _chartSubmissionRepository.UpdateChartSubmissionAsync(chartSubmission))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        await _volunteerVoteRepository.RemoveVolunteerVotesAsync(
            await _volunteerVoteRepository.GetVolunteerVotesAsync("DateCreated", true, 0, -1,
                e => e.ChartId == chartSubmission.Id));

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
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Consumes("multipart/form-data")]
    [Produces("text/plain", "application/json")]
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
            chartSubmission.Illustration = (await _fileStorageService.UploadImage<ChartSubmission>(
                chartSubmission.Title ?? (chartSubmission.Song != null
                    ? chartSubmission.Song.Title
                    : chartSubmission.SongSubmission!.Title), dto.File, (16, 9))).Item1;
            chartSubmission.Status = RequestStatus.Waiting;
            chartSubmission.VolunteerStatus = RequestStatus.Waiting;
            chartSubmission.DateUpdated = DateTimeOffset.UtcNow;
        }

        if (!await _chartSubmissionRepository.UpdateChartSubmissionAsync(chartSubmission))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        await _volunteerVoteRepository.RemoveVolunteerVotesAsync(
            await _volunteerVoteRepository.GetVolunteerVotesAsync("DateCreated", true, 0, -1,
                e => e.ChartId == chartSubmission.Id));

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
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Consumes("multipart/form-data")]
    [Produces("text/plain", "application/json")]
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

        await _volunteerVoteRepository.RemoveVolunteerVotesAsync(
            await _volunteerVoteRepository.GetVolunteerVotesAsync("DateCreated", true, 0, -1,
                e => e.ChartId == chartSubmission.Id));

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
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Produces("text/plain", "application/json")]
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
    [Produces("text/plain", "application/json")]
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
                        chartSubmission.GetDisplay())
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
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<VoteDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetChartSubmissionVotes([FromRoute] Guid id,
        [FromQuery] ArrayWithTimeRequestDto dto)
    {
        dto.PerPage = dto.PerPage > 0
            ? dto.PerPage <= _dataSettings.Value.PaginationMaxPerPage
                ? dto.PerPage
                : _dataSettings.Value.PaginationMaxPerPage
            : _dataSettings.Value.PaginationPerPage;
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
             !await _resourceService.HasPermission(currentUser, Roles.Moderator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        var votes = await _volunteerVoteRepository.GetVolunteerVotesAsync(dto.Order, dto.Desc, position, dto.PerPage,
            e => e.ChartId == id);
        var list = _mapper.Map<List<VoteDto>>(votes);
        var total = await _volunteerVoteRepository.CountVolunteerVotesAsync(e => e.ChartId == id);

        return Ok(new ResponseDto<IEnumerable<VoteDto>>
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
    ///     Votes a specific chart submission.
    /// </summary>
    /// <param name="id">A chart submission's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="201">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="404">When the specified chartSubmission is not found.</response>
    [HttpPost("{id:guid}/votes")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Produces("text/plain", "application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status201Created, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
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
             !await _resourceService.HasPermission(currentUser, Roles.Moderator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (!await _volunteerVoteService.CreateVolunteerVoteAsync(dto, chartSubmission, currentUser))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.AlreadyDone
            });

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
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
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
        if ((chartSubmission.OwnerId == currentUser.Id &&
             !await _resourceService.HasPermission(currentUser, Roles.Qualified)) ||
            (chartSubmission.OwnerId != currentUser.Id &&
             !await _resourceService.HasPermission(currentUser, Roles.Moderator)))
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