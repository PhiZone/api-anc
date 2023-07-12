using Microsoft.AspNetCore.Mvc;
using PhiZoneApi.Dtos.Responses;
using PhiZoneApi.Enums;
using PhiZoneApi.Interfaces;

namespace PhiZoneApi.Controllers;

[Route("")]
[ApiVersion("2.0")]
[ApiController]
public class RootController : Controller
{
    private readonly IChapterRepository _chapterRepository;
    private readonly IChartRepository _chartRepository;
    private readonly ICommentRepository _commentRepository;
    private readonly ILikeRepository _likeRepository;
    private readonly IRecordRepository _recordRepository;
    private readonly IReplyRepository _replyRepository;
    private readonly ISongRepository _songRepository;
    private readonly IUserRepository _userRepository;

    public RootController(IUserRepository userRepository, IChapterRepository chapterRepository,
        ISongRepository songRepository, IChartRepository chartRepository, ICommentRepository commentRepository,
        ILikeRepository likeRepository, IRecordRepository recordRepository, IReplyRepository replyRepository)
    {
        _userRepository = userRepository;
        _chapterRepository = chapterRepository;
        _songRepository = songRepository;
        _chartRepository = chartRepository;
        _commentRepository = commentRepository;
        _likeRepository = likeRepository;
        _recordRepository = recordRepository;
        _replyRepository = replyRepository;
    }

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
            Data = new AbstractDto
            {
                UserCount = await _userRepository.CountUsersAsync(),
                RecordCount = await _recordRepository.CountRecordsAsync(),
                ChartCount = await _chartRepository.CountChartsAsync(),
                SongCount = await _songRepository.CountSongsAsync(),
                ChapterCount = await _chapterRepository.CountChaptersAsync(),
                LikeCount = await _likeRepository.CountLikesAsync(),
                CommentCount = await _commentRepository.CountCommentsAsync(),
                ReplyCount = await _replyRepository.CountRepliesAsync()
            }
        });
    }
}