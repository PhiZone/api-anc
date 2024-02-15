using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using PhiZoneApi.Constants;
using PhiZoneApi.Dtos.Responses;
using PhiZoneApi.Enums;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using StackExchange.Redis;

namespace PhiZoneApi.Controllers;

[Route("")]
[ApiVersion("2.0")]
[ApiController]
public class RootController(
    IUserRepository userRepository,
    IChapterRepository chapterRepository,
    ISongRepository songRepository,
    IChartRepository chartRepository,
    ICommentRepository commentRepository,
    ILikeRepository likeRepository,
    IRecordRepository recordRepository,
    IReplyRepository replyRepository,
    IConnectionMultiplexer redis,
    UserManager<User> userManager,
    IResourceService resourceService)
    : Controller
{
    /// <summary>
    ///     Retrieves an abstract of site info.
    /// </summary>
    /// <returns>An abstract of site info.</returns>
    /// <response code="200">Returns an abstract of site info.</response>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<AbstractDto>))]
    public async Task<IActionResult> GetAbstract()
    {
        return Ok(new ResponseDto<AbstractDto>
        {
            Status = ResponseStatus.Ok,
            Code = ResponseCodes.Ok,
            Data = new AbstractDto
            {
                UserCount = await userRepository.CountUsersAsync(),
                RecordCount = await recordRepository.CountRecordsAsync(),
                ChartCount = await chartRepository.CountChartsAsync(e => !e.IsHidden),
                SongCount = await songRepository.CountSongsAsync(e => !e.IsHidden),
                ChapterCount = await chapterRepository.CountChaptersAsync(e => !e.IsHidden),
                LikeCount = await likeRepository.CountLikesAsync(),
                CommentCount = await commentRepository.CountCommentsAsync(),
                ReplyCount = await replyRepository.CountRepliesAsync()
            }
        });
    }

    /// <summary>
    ///     Retrieves headline.
    /// </summary>
    /// <returns>Headline.</returns>
    /// <response code="200">Returns headline.</response>
    [HttpGet("headline")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<HeadlineDto>))]
    public async Task<IActionResult> GetHeadline()
    {
        var db = redis.GetDatabase();
        return Ok(new ResponseDto<HeadlineDto>
        {
            Status = ResponseStatus.Ok,
            Code = ResponseCodes.Ok,
            Data = new HeadlineDto { Headline = await db.StringGetAsync("phizone:headline") }
        });
    }

    /// <summary>
    ///     Retrieves studio's headline.
    /// </summary>
    /// <returns>Studio's Headline.</returns>
    /// <response code="200">Returns studio's headline.</response>
    /// <response code="401">When the user is not authorized.</response>
    [HttpGet("studio/headline")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<HeadlineDto>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    public async Task<IActionResult> GetStudioHeadline()
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!resourceService.HasPermission(currentUser, UserRole.Qualified))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        var db = redis.GetDatabase();
        return Ok(new ResponseDto<HeadlineDto>
        {
            Status = ResponseStatus.Ok,
            Code = ResponseCodes.Ok,
            Data = new HeadlineDto { Headline = await db.StringGetAsync("phizone:studio_headline") }
        });
    }
}