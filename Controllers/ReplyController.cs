using AutoMapper;
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

[Route("replies")]
[ApiVersion("2.0")]
[ApiController]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
    Policy = "AllowAnonymous")]
public class ReplyController : Controller
{
    private readonly IOptions<DataSettings> _dataSettings;
    private readonly IDtoMapper _dtoMapper;
    private readonly IFilterService _filterService;
    private readonly ILikeRepository _likeRepository;
    private readonly ILikeService _likeService;
    private readonly IMapper _mapper;
    private readonly IReplyRepository _replyRepository;
    private readonly IResourceService _resourceService;
    private readonly UserManager<User> _userManager;

    public ReplyController(IReplyRepository replyRepository, IOptions<DataSettings> dataSettings,
        IDtoMapper dtoMapper, IFilterService filterService, UserManager<User> userManager,
        ILikeRepository likeRepository, ILikeService likeService, IMapper mapper, IResourceService resourceService)
    {
        _replyRepository = replyRepository;
        _dataSettings = dataSettings;
        _dtoMapper = dtoMapper;
        _filterService = filterService;
        _userManager = userManager;
        _likeRepository = likeRepository;
        _likeService = likeService;
        _mapper = mapper;
        _resourceService = resourceService;
    }

    /// <summary>
    ///     Retrieves replies.
    /// </summary>
    /// <returns>An array of replies.</returns>
    /// <response code="200">Returns an array of replies.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<ReplyDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetReplies([FromQuery] ArrayWithTimeRequestDto dto,
        [FromQuery] ReplyFilterDto? filterDto = null)
    {
        var currentUser = await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        dto.PerPage = dto.PerPage > 0 && dto.PerPage < _dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? _dataSettings.Value.PaginationPerPage : _dataSettings.Value.PaginationMaxPerPage;
        var position = dto.PerPage * (dto.Page - 1);
        var predicateExpr = await _filterService.Parse(filterDto, dto.Predicate, currentUser);
        var replies = await _replyRepository.GetRepliesAsync(dto.Order, dto.Desc, position,
            dto.PerPage, predicateExpr);
        var total = await _replyRepository.CountRepliesAsync(predicateExpr);
        var list = new List<ReplyDto>();

        foreach (var reply in replies) list.Add(await _dtoMapper.MapReplyAsync<ReplyDto>(reply, currentUser));

        return Ok(new ResponseDto<IEnumerable<ReplyDto>>
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
    ///     Retrieves a specific reply.
    /// </summary>
    /// <param name="id">A reply's ID.</param>
    /// <returns>A reply.</returns>
    /// <response code="200">Returns a reply.</response>
    /// <response code="304">
    ///     When the resource has not been updated since last retrieval. Requires <c>If-None-Match</c>.
    /// </response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified reply is not found.</response>
    [HttpGet("{id:guid}")]
    [ServiceFilter(typeof(ETagFilter))]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<ReplyDto>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status304NotModified, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetReply([FromRoute] Guid id)
    {
        var currentUser = await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        if (!await _replyRepository.ReplyExistsAsync(id))
            return NotFound(new ResponseDto<object>
                { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound });
        var reply = await _replyRepository.GetReplyAsync(id);
        var dto = await _dtoMapper.MapReplyAsync<ReplyDto>(reply, currentUser);

        return Ok(new ResponseDto<ReplyDto> { Status = ResponseStatus.Ok, Code = ResponseCodes.Ok, Data = dto });
    }

    /// <summary>
    ///     Removes a reply.
    /// </summary>
    /// <param name="id">A reply's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified reply is not found.</response>
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
    public async Task<IActionResult> RemoveReply([FromRoute] Guid id)
    {
        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;

        if (!await _replyRepository.ReplyExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var reply = await _replyRepository.GetReplyAsync(id);
        if ((currentUser.Id == reply.OwnerId && !await _resourceService.HasPermission(currentUser, Roles.Member)) ||
            (currentUser.Id != reply.OwnerId &&
             !await _resourceService.HasPermission(currentUser, Roles.Administrator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (!await _replyRepository.RemoveReplyAsync(id))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return NoContent();
    }

    /// <summary>
    ///     Retrieves likes from a specific reply.
    /// </summary>
    /// <param name="id">A reply's ID.</param>
    /// <returns>An array of likes.</returns>
    /// <response code="200">Returns an array of likes.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified reply is not found.</response>
    [HttpGet("{id:guid}/likes")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<LikeDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetReplyLikes([FromRoute] Guid id, [FromQuery] ArrayWithTimeRequestDto dto)
    {
        dto.PerPage = dto.PerPage > 0 && dto.PerPage < _dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? _dataSettings.Value.PaginationPerPage : _dataSettings.Value.PaginationMaxPerPage;
        var position = dto.PerPage * (dto.Page - 1);
        if (!await _replyRepository.ReplyExistsAsync(id))
            return NotFound(new ResponseDto<object>
                { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound });
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
    ///     Likes a specific reply.
    /// </summary>
    /// <param name="id">A reply's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="201">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="404">When the specified reply is not found.</response>
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
        if (!await _replyRepository.ReplyExistsAsync(id))
            return NotFound(new ResponseDto<object>
                { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound });
        var reply = await _replyRepository.GetReplyAsync(id);
        if (!await _likeService.CreateLikeAsync(reply, currentUser.Id))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.AlreadyDone
            });

        return StatusCode(StatusCodes.Status201Created);
    }

    /// <summary>
    ///     Removes the like from a specific reply.
    /// </summary>
    /// <param name="id">A reply's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="404">When the specified reply is not found.</response>
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
        if (!await _replyRepository.ReplyExistsAsync(id))
            return NotFound(new ResponseDto<object>
                { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound });
        var reply = await _replyRepository.GetReplyAsync(id);
        if (!await _likeService.RemoveLikeAsync(reply, currentUser.Id))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.AlreadyDone
            });

        return NoContent();
    }
}