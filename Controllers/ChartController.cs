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
public class ChartController : Controller
{
    private readonly IAuthorshipRepository _authorshipRepository;
    private readonly IChartRepository _chartRepository;
    private readonly IChartService _chartService;
    private readonly ICommentRepository _commentRepository;
    private readonly IOptions<DataSettings> _dataSettings;
    private readonly IDtoMapper _dtoMapper;
    private readonly IFileStorageService _fileStorageService;
    private readonly IFilterService _filterService;
    private readonly ILikeRepository _likeRepository;
    private readonly ILikeService _likeService;
    private readonly IMapper _mapper;
    private readonly ISongRepository _songRepository;
    private readonly UserManager<User> _userManager;
    private readonly IVoteRepository _voteRepository;
    private readonly IVoteService _voteService;

    public ChartController(IChartRepository chartRepository, IOptions<DataSettings> dataSettings,
        UserManager<User> userManager, IFilterService filterService, IFileStorageService fileStorageService,
        IDtoMapper dtoMapper, IMapper mapper, IChartService chartService, ISongRepository songRepository,
        ILikeRepository likeRepository, ILikeService likeService, ICommentRepository commentRepository,
        IVoteRepository voteRepository, IVoteService voteService, IAuthorshipRepository authorshipRepository)
    {
        _chartRepository = chartRepository;
        _dataSettings = dataSettings;
        _userManager = userManager;
        _filterService = filterService;
        _dtoMapper = dtoMapper;
        _mapper = mapper;
        _chartService = chartService;
        _songRepository = songRepository;
        _likeRepository = likeRepository;
        _likeService = likeService;
        _commentRepository = commentRepository;
        _voteRepository = voteRepository;
        _voteService = voteService;
        _authorshipRepository = authorshipRepository;
        _fileStorageService = fileStorageService;
    }

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
    public async Task<IActionResult> GetCharts([FromQuery] ArrayWithTimeRequestDto dto,
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
        var charts = await _chartRepository.GetChartsAsync(dto.Order, dto.Desc, position,
            dto.PerPage, dto.Search, predicateExpr);
        var total = await _chartRepository.CountChartsAsync(dto.Search, predicateExpr);
        var list = new List<ChartDto>();

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
    [Produces("application/json", "text/plain")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<ChartDto>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status304NotModified, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetChart([FromRoute] Guid id)
    {
        var currentUser = await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        if (!await _chartRepository.ChartExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var chart = await _chartRepository.GetChartAsync(id);

        if ((currentUser == null || !await _userManager.IsInRoleAsync(currentUser, Roles.Administrator)) &&
            chart.IsHidden)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var dto = await _dtoMapper.MapChartAsync<ChartDto>(chart, currentUser);

        return Ok(new ResponseDto<ChartDto> { Status = ResponseStatus.Ok, Code = ResponseCodes.Ok, Data = dto });
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
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Consumes("multipart/form-data")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status201Created, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> CreateChart([FromForm] ChartCreationDto dto)
    {
        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!await _userManager.IsInRoleAsync(currentUser, Roles.Administrator))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (!await _songRepository.SongExistsAsync(dto.SongId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentNotFound
            });

        var song = await _songRepository.GetSongAsync(dto.SongId);

        var illustrationUrl = dto.Illustration != null
            ? (await _fileStorageService.UploadImage<Chart>(dto.Title ?? song.Title, dto.Illustration, (16, 9))).Item1
            : null;

        var chartInfo = dto.File != null ? await _chartService.Upload(dto.Title ?? song.Title, dto.File) : null;

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

        if (!await _chartRepository.CreateChartAsync(chart))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        foreach (var authorship in dto.Authorships.Select(authorshipDto => new Authorship
                 {
                     ResourceId = chart.Id,
                     AuthorId = authorshipDto.AuthorId,
                     Position = authorshipDto.Position,
                     DateCreated = DateTimeOffset.UtcNow
                 }))
            await _authorshipRepository.CreateAuthorshipAsync(authorship);

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
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Consumes("application/json")]
    [Produces("text/plain", "application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> UpdateChart([FromRoute] Guid id,
        [FromBody] JsonPatchDocument<ChartUpdateDto> patchDocument)
    {
        if (!await _chartRepository.ChartExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var chart = await _chartRepository.GetChartAsync(id);

        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!await _userManager.IsInRoleAsync(currentUser, Roles.Administrator))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        var dto = _mapper.Map<ChartUpdateDto>(chart);
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

        if (!await _chartRepository.UpdateChartAsync(chart))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        foreach (var authorshipDto in dto.Authorships)
        {
            var authorship = new Authorship
            {
                ResourceId = id,
                AuthorId = authorshipDto.AuthorId,
                Position = authorshipDto.Position,
                DateCreated = DateTimeOffset.UtcNow
            };
            if (await _authorshipRepository.AuthorshipExistsAsync(id, authorshipDto.AuthorId))
                await _authorshipRepository.UpdateAuthorshipAsync(authorship);
            else
                await _authorshipRepository.CreateAuthorshipAsync(authorship);
        }

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
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Consumes("multipart/form-data")]
    [Produces("text/plain", "application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> UpdateChartFile([FromRoute] Guid id, [FromForm] FileDto dto)
    {
        if (!await _chartRepository.ChartExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var chart = await _chartRepository.GetChartAsync(id);

        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!await _userManager.IsInRoleAsync(currentUser, Roles.Administrator))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (dto.File != null)
        {
            var chartInfo = await _chartService.Upload(chart.Title ?? chart.Song.Title, dto.File);
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

        if (!await _chartRepository.UpdateChartAsync(chart))
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
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Consumes("multipart/form-data")]
    [Produces("text/plain", "application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> RemoveChartFile([FromRoute] Guid id)
    {
        if (!await _chartRepository.ChartExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var chart = await _chartRepository.GetChartAsync(id);

        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!await _userManager.IsInRoleAsync(currentUser, Roles.Administrator))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        chart.File = null;
        chart.DateUpdated = DateTimeOffset.UtcNow;

        if (!await _chartRepository.UpdateChartAsync(chart))
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
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Consumes("multipart/form-data")]
    [Produces("text/plain", "application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> UpdateChartIllustration([FromRoute] Guid id,
        [FromForm] ResourceIllustrationDto dto)
    {
        if (!await _chartRepository.ChartExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var chart = await _chartRepository.GetChartAsync(id);

        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!await _userManager.IsInRoleAsync(currentUser, Roles.Administrator))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (dto.Illustration != null)
        {
            chart.Illustration =
                (await _fileStorageService.UploadImage<Chart>(chart.Title ?? chart.Song.Title, dto.Illustration,
                    (16, 9))).Item1;
            chart.DateUpdated = DateTimeOffset.UtcNow;
        }

        if (!await _chartRepository.UpdateChartAsync(chart))
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
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Consumes("multipart/form-data")]
    [Produces("text/plain", "application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> RemoveChartIllustration([FromRoute] Guid id)
    {
        if (!await _chartRepository.ChartExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var chart = await _chartRepository.GetChartAsync(id);

        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!await _userManager.IsInRoleAsync(currentUser, Roles.Administrator))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        chart.Illustration = null;
        chart.DateUpdated = DateTimeOffset.UtcNow;

        if (!await _chartRepository.UpdateChartAsync(chart))
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
    [Produces("text/plain", "application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> RemoveChart([FromRoute] Guid id)
    {
        if (!await _chartRepository.ChartExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!await _userManager.IsInRoleAsync(currentUser, Roles.Administrator))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (!await _chartRepository.RemoveChartAsync(id))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return NoContent();
    }

    /// <summary>
    ///     Retrieves records from a specific chart.
    /// </summary>
    /// <param name="id">A chart's ID.</param>
    /// <returns>An array of records.</returns>
    /// <response code="200">Returns an array of records.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified chart is not found.</response>
    [HttpGet("{id:guid}/records")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<RecordDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetChartRecords([FromRoute] Guid id, [FromQuery] ArrayWithTimeRequestDto dto,
        [FromQuery] RecordFilterDto? filterDto = null)
    {
        var currentUser = await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        dto.PerPage = dto.PerPage > 0
            ? dto.PerPage <= _dataSettings.Value.PaginationMaxPerPage
                ? dto.PerPage
                : _dataSettings.Value.PaginationMaxPerPage
            : _dataSettings.Value.PaginationPerPage;
        var position = dto.PerPage * (dto.Page - 1);
        var predicateExpr = await _filterService.Parse(filterDto, dto.Predicate, currentUser);

        if (!await _chartRepository.ChartExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var chart = await _chartRepository.GetChartAsync(id);

        if ((currentUser == null || !await _userManager.IsInRoleAsync(currentUser, Roles.Administrator)) &&
            chart.IsHidden)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var records =
            await _chartRepository.GetChartRecordsAsync(id, dto.Order, dto.Desc, position, dto.PerPage, predicateExpr);
        var total = await _chartRepository.CountChartRecordsAsync(id, predicateExpr);
        var list = new List<RecordDto>();

        foreach (var record in records) list.Add(await _dtoMapper.MapRecordAsync<RecordDto>(record, currentUser));

        return Ok(new ResponseDto<IEnumerable<RecordDto>>
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
    public async Task<IActionResult> GetChartLikes([FromRoute] Guid id, [FromQuery] ArrayWithTimeRequestDto dto)
    {
        dto.PerPage = dto.PerPage > 0
            ? dto.PerPage <= _dataSettings.Value.PaginationMaxPerPage
                ? dto.PerPage
                : _dataSettings.Value.PaginationMaxPerPage
            : _dataSettings.Value.PaginationPerPage;
        var position = dto.PerPage * (dto.Page - 1);
        if (!await _chartRepository.ChartExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var currentUser = await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        var chart = await _chartRepository.GetChartAsync(id);
        if ((currentUser == null || !await _userManager.IsInRoleAsync(currentUser, Roles.Administrator)) &&
            chart.IsHidden)
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
        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!await _userManager.IsInRoleAsync(currentUser, Roles.Member))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        if (!await _chartRepository.ChartExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var chart = await _chartRepository.GetChartAsync(id);

        if (!await _userManager.IsInRoleAsync(currentUser, Roles.Administrator) && chart.IsHidden)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        if (chart.IsLocked)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.Locked
            });

        if (!await _likeService.CreateLikeAsync(chart, currentUser.Id))
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
        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!await _userManager.IsInRoleAsync(currentUser, Roles.Member))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        if (!await _chartRepository.ChartExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var chart = await _chartRepository.GetChartAsync(id);

        if (!await _userManager.IsInRoleAsync(currentUser, Roles.Administrator) && chart.IsHidden)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        if (chart.IsLocked)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.Locked
            });

        if (!await _likeService.RemoveLikeAsync(chart, currentUser.Id))
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
    public async Task<IActionResult> GetChartComments([FromRoute] Guid id, [FromQuery] ArrayWithTimeRequestDto dto)
    {
        var currentUser = await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        dto.PerPage = dto.PerPage > 0
            ? dto.PerPage <= _dataSettings.Value.PaginationMaxPerPage
                ? dto.PerPage
                : _dataSettings.Value.PaginationMaxPerPage
            : _dataSettings.Value.PaginationPerPage;
        var position = dto.PerPage * (dto.Page - 1);

        if (!await _chartRepository.ChartExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var chart = await _chartRepository.GetChartAsync(id);

        if ((currentUser == null || !await _userManager.IsInRoleAsync(currentUser, Roles.Administrator)) &&
            chart.IsHidden)
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
        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!await _userManager.IsInRoleAsync(currentUser, Roles.Member))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        if (!await _chartRepository.ChartExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var chart = await _chartRepository.GetChartAsync(id);

        if (!await _userManager.IsInRoleAsync(currentUser, Roles.Administrator) && chart.IsHidden)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        if (chart.IsLocked)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.Locked
            });

        var comment = new Comment
        {
            ResourceId = chart.Id,
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
    public async Task<IActionResult> GetChartVotes([FromRoute] Guid id, [FromQuery] ArrayWithTimeRequestDto dto)
    {
        dto.PerPage = dto.PerPage > 0
            ? dto.PerPage <= _dataSettings.Value.PaginationMaxPerPage
                ? dto.PerPage
                : _dataSettings.Value.PaginationMaxPerPage
            : _dataSettings.Value.PaginationPerPage;
        var position = dto.PerPage * (dto.Page - 1);

        if (!await _chartRepository.ChartExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var currentUser = await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        var chart = await _chartRepository.GetChartAsync(id);
        if ((currentUser == null || !await _userManager.IsInRoleAsync(currentUser, Roles.Administrator)) &&
            chart.IsHidden)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        if (chart.IsLocked)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.Locked
            });

        var votes = await _voteRepository.GetVotesAsync(dto.Order, dto.Desc, position, dto.PerPage,
            e => e.ChartId == id);
        var list = _mapper.Map<List<VoteDto>>(votes);
        var total = await _voteRepository.CountVotesAsync(e => e.ChartId == id);

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
    ///     Votes a specific chart.
    /// </summary>
    /// <param name="id">A chart's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="201">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="404">When the specified chart is not found.</response>
    [HttpPost("{id:guid}/votes")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status201Created, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> CreateVote([FromRoute] Guid id, [FromBody] VoteRequestDto dto)
    {
        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!await _userManager.IsInRoleAsync(currentUser, Roles.Member))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        if (!await _chartRepository.ChartExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var chart = await _chartRepository.GetChartAsync(id);

        if (!await _userManager.IsInRoleAsync(currentUser, Roles.Administrator) && chart.IsHidden)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        if (chart.IsLocked)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.Locked
            });

        if (!await _voteService.CreateVoteAsync(dto, chart, currentUser))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.AlreadyDone
            });

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
        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!await _userManager.IsInRoleAsync(currentUser, Roles.Member))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        if (!await _chartRepository.ChartExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var chart = await _chartRepository.GetChartAsync(id);

        if (!await _userManager.IsInRoleAsync(currentUser, Roles.Administrator) && chart.IsHidden)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        if (chart.IsLocked)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.Locked
            });

        if (!await _voteService.RemoveVoteAsync(chart, currentUser.Id))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.AlreadyDone
            });

        return NoContent();
    }
}