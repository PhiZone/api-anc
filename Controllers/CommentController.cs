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

namespace PhiZoneApi.Controllers;

[Route("comments")]
[ApiVersion("2.0")]
[ApiController]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
    Policy = "AllowAnonymous")]
public class CommentController : Controller
{
    private readonly ICommentRepository _commentRepository;
    private readonly IOptions<DataSettings> _dataSettings;
    private readonly IDtoMapper _dtoMapper;
    private readonly IFilterService _filterService;
    private readonly ILikeRepository _likeRepository;
    private readonly ILikeService _likeService;
    private readonly IMapper _mapper;
    private readonly IReplyRepository _replyRepository;
    private readonly UserManager<User> _userManager;

    public CommentController(ICommentRepository commentRepository, IOptions<DataSettings> dataSettings,
        IDtoMapper dtoMapper, IFilterService filterService, UserManager<User> userManager,
        IReplyRepository replyRepository, ILikeRepository likeRepository, ILikeService likeService, IMapper mapper)
    {
        _commentRepository = commentRepository;
        _dataSettings = dataSettings;
        _dtoMapper = dtoMapper;
        _filterService = filterService;
        _userManager = userManager;
        _replyRepository = replyRepository;
        _likeRepository = likeRepository;
        _likeService = likeService;
        _mapper = mapper;
    }

    /// <summary>
    ///     Retrieves comments.
    /// </summary>
    /// <returns>An array of comments.</returns>
    /// <response code="200">Returns an array of comments.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<CommentDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetComments([FromQuery] ArrayWithTimeRequestDto dto,
        [FromQuery] CommentFilterDto? filterDto = null)
    {
        var currentUser = await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        dto.PerPage = dto.PerPage > 0
            ? dto.PerPage <= _dataSettings.Value.PaginationMaxPerPage
                ? dto.PerPage
                : _dataSettings.Value.PaginationMaxPerPage
            : _dataSettings.Value.PaginationPerPage;
        var position = dto.PerPage * (dto.Page - 1);
        var predicateExpr = await _filterService.Parse(filterDto, dto.Predicate, currentUser);
        var comments = await _commentRepository.GetCommentsAsync(dto.Order, dto.Desc, position,
            dto.PerPage, predicateExpr);
        var total = await _commentRepository.CountCommentsAsync(predicateExpr);
        var list = new List<CommentDto>();

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
    ///     Retrieves a specific comment.
    /// </summary>
    /// <param name="id">Comment's ID.</param>
    /// <returns>A comment.</returns>
    /// <response code="200">Returns a comment.</response>
    /// <response code="304">
    ///     When the resource has not been updated since last retrieval. Requires <c>If-None-Match</c>.
    /// </response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified comment is not found.</response>
    [HttpGet("{id}")]
    [ServiceFilter(typeof(ETagFilter))]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<CommentDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetComment([FromRoute] Guid id)
    {
        var currentUser = await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        if (!await _commentRepository.CommentExistsAsync(id)) return NotFound();
        var comment = await _commentRepository.GetCommentAsync(id);
        var dto = await _dtoMapper.MapCommentAsync<CommentDto>(comment, currentUser);

        return Ok(new ResponseDto<CommentDto> { Status = ResponseStatus.Ok, Code = ResponseCodes.Ok, Data = dto });
    }

    /// <summary>
    ///     Removes a comment.
    /// </summary>
    /// <param name="id">A comment's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified comment is not found.</response>
    [HttpDelete("{id}")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Produces("text/plain", "application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> RemoveComment([FromRoute] Guid id)
    {
        var currentUser = await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);

        if (!await _commentRepository.CommentExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var comment = await _commentRepository.GetCommentAsync(id);
        if ((currentUser!.Id == comment.OwnerId && !await _userManager.IsInRoleAsync(currentUser, "Member")) ||
            (currentUser.Id != comment.OwnerId && !await _userManager.IsInRoleAsync(currentUser, "Admin")))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (!await _commentRepository.RemoveCommentAsync(id))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return NoContent();
    }

    /// <summary>
    ///     Retrieves replies from a specific comment.
    /// </summary>
    /// <param name="id">A comment's ID.</param>
    /// <returns>An array of replies.</returns>
    /// <response code="200">Returns an array of replies.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified comment is not found.</response>
    [HttpGet("{id}/replies")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<ReplyDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetCommentReplies([FromRoute] Guid id, [FromQuery] ArrayWithTimeRequestDto dto,
        [FromQuery] ReplyFilterDto? filterDto = null)
    {
        var currentUser = await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        dto.PerPage = dto.PerPage > 0
            ? dto.PerPage <= _dataSettings.Value.PaginationMaxPerPage
                ? dto.PerPage
                : _dataSettings.Value.PaginationMaxPerPage
            : _dataSettings.Value.PaginationPerPage;
        var position = dto.PerPage * (dto.Page - 1);
        var predicateExpr = await _filterService.Parse(filterDto, dto.Predicate, currentUser);
        if (!await _commentRepository.CommentExistsAsync(id)) return NotFound();
        var replies =
            await _commentRepository.GetCommentRepliesAsync(id, dto.Order, dto.Desc, position, dto.PerPage,
                predicateExpr);
        var total = await _commentRepository.CountCommentRepliesAsync(id, predicateExpr);
        var list = new List<ReplyDto>();

        foreach (var reply in replies) list.Add(await _dtoMapper.MapReplyAsync<ReplyDto>(reply, currentUser));

        return Ok(new ResponseDto<IEnumerable<ReplyDto>>
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
    ///     Replies to a specific comment.
    /// </summary>
    /// <param name="id">A comment's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="201">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified comment is not found.</response>
    [HttpPost("{id}/replies")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> CreateReply([FromRoute] Guid id, [FromBody] ReplyCreationDto dto)
    {
        var currentUser = await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        if (!await _userManager.IsInRoleAsync(currentUser!, Roles.Member))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        if (!await _commentRepository.CommentExistsAsync(id)) return NotFound();
        var comment = await _commentRepository.GetCommentAsync(id);
        var reply = new Reply
        {
            CommentId = comment.Id,
            Content = dto.Content,
            Language = dto.Language,
            OwnerId = currentUser!.Id,
            DateCreated = DateTimeOffset.UtcNow
        };
        if (!await _replyRepository.CreateReplyAsync(reply))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.AlreadyDone
            });

        return StatusCode(StatusCodes.Status201Created);
    }

    /// <summary>
    ///     Retrieves likes from a specific comment.
    /// </summary>
    /// <param name="id">A comment's ID.</param>
    /// <returns>An array of likes.</returns>
    /// <response code="200">Returns an array of likes.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified comment is not found.</response>
    [HttpGet("{id}/likes")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<LikeDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetCommentLikes([FromRoute] Guid id, [FromQuery] ArrayWithTimeRequestDto dto)
    {
        dto.PerPage = dto.PerPage > 0
            ? dto.PerPage <= _dataSettings.Value.PaginationMaxPerPage
                ? dto.PerPage
                : _dataSettings.Value.PaginationMaxPerPage
            : _dataSettings.Value.PaginationPerPage;
        var position = dto.PerPage * (dto.Page - 1);
        if (!await _commentRepository.CommentExistsAsync(id)) return NotFound();
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
    ///     Likes a specific comment.
    /// </summary>
    /// <param name="id">A comment's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="201">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified comment is not found.</response>
    [HttpPost("{id}/likes")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> CreateLike([FromRoute] Guid id)
    {
        var currentUser = await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        if (!await _userManager.IsInRoleAsync(currentUser!, Roles.Member))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        if (!await _commentRepository.CommentExistsAsync(id)) return NotFound();
        var comment = await _commentRepository.GetCommentAsync(id);
        if (!await _likeService.CreateLikeAsync(comment, currentUser!.Id))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.AlreadyDone
            });

        return StatusCode(StatusCodes.Status201Created);
    }

    /// <summary>
    ///     Removes the like from a specific comment.
    /// </summary>
    /// <param name="id">A comment's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified comment is not found.</response>
    [HttpDelete("{id}/likes")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> RemoveLike([FromRoute] Guid id)
    {
        var currentUser = await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        if (!await _userManager.IsInRoleAsync(currentUser!, Roles.Member))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        if (!await _commentRepository.CommentExistsAsync(id)) return NotFound();
        var comment = await _commentRepository.GetCommentAsync(id);
        if (!await _likeService.RemoveLikeAsync(comment, currentUser!.Id))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.AlreadyDone
            });

        return NoContent();
    }
}