using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using PhiZoneApi.Configurations;
using PhiZoneApi.Constants;
using PhiZoneApi.Dtos.Deliverers;
using PhiZoneApi.Dtos.Filters;
using PhiZoneApi.Dtos.Requests;
using PhiZoneApi.Dtos.Responses;
using PhiZoneApi.Enums;
using PhiZoneApi.Filters;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using StackExchange.Redis;

namespace PhiZoneApi.Controllers;

[Route("pet")]
[ApiVersion("2.0")]
[ApiController]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class PetController : Controller
{
    private readonly IOptions<DataSettings> _dataSettings;
    private readonly IDtoMapper _dtoMapper;
    private readonly IFeishuService _feishuService;
    private readonly IFilterService _filterService;
    private readonly IMeilisearchService _meilisearchService;
    private readonly INotificationService _notificationService;
    private readonly IPetAnswerRepository _petAnswerRepository;
    private readonly IPetQuestionRepository _petQuestionRepository;
    private readonly IConnectionMultiplexer _redis;
    private readonly IResourceService _resourceService;
    private readonly Dictionary<UserRole, int> _scores;
    private readonly UserManager<User> _userManager;

    public PetController(IConnectionMultiplexer redis, IPetQuestionRepository petQuestionRepository,
        IPetAnswerRepository petAnswerRepository, UserManager<User> userManager, IResourceService resourceService,
        IConfiguration config, IOptions<DataSettings> dataSettings, IDtoMapper dtoMapper, IFilterService filterService,
        INotificationService notificationService, IFeishuService feishuService, IMeilisearchService meilisearchService)
    {
        _redis = redis;
        _userManager = userManager;
        _resourceService = resourceService;
        _dataSettings = dataSettings;
        _dtoMapper = dtoMapper;
        _filterService = filterService;
        _notificationService = notificationService;
        _feishuService = feishuService;
        _meilisearchService = meilisearchService;
        _petQuestionRepository = petQuestionRepository;
        _petAnswerRepository = petAnswerRepository;
        _scores = new Dictionary<UserRole, int>
            {
                {
                    UserRole.Qualified,
                    config.GetSection("PrivilegeEscalationTest").GetValue<int>(UserRole.Qualified.ToString())
                },
                {
                    UserRole.Volunteer,
                    config.GetSection("PrivilegeEscalationTest").GetValue<int>(UserRole.Volunteer.ToString())
                }
            }.OrderByDescending(e => e.Value)
            .ToDictionary(pair => pair.Key, pair => pair.Value);
    }

    /// <summary>
    ///     Retrieves objective questions.
    /// </summary>
    /// <returns>An array of objective questions.</returns>
    /// <response code="200">Returns an array of objective questions.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    [HttpGet("objective")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<PetQuestionDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetObjectiveQuestions()
    {
        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!_resourceService.HasPermission(currentUser, UserRole.Member))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (_resourceService.HasPermission(currentUser, UserRole.Volunteer))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.AlreadyDone
            });
        if (await _petAnswerRepository.CountPetAnswersAsync(answer =>
                answer.OwnerId == currentUser.Id && answer.SubjectiveScore == null) > 0)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidOperation
            });

        var db = _redis.GetDatabase();
        var key = $"phizone:pet:0:{currentUser.Id}";

        var questions = new List<PetQuestionDto>();
        var deliverer = new PetDelivererDto();

        for (var i = 1; i <= 15; i++)
        {
            var question = await _petQuestionRepository.GetRandomPetQuestionAsync(i, currentUser.Language);
            if (question == null) continue;

            var choices = await _petQuestionRepository.GetQuestionChoicesAsync(question.Id);

            deliverer.Questions.Add(new PetQuestionDeliverer
            {
                Id = question.Id,
                Type = question.Type,
                Choices = choices.Select(e => new PetChoiceDeliverer { Id = e.Id, IsCorrect = e.IsCorrect })
                    .ToList()
            });
            questions.Add(new PetQuestionDto
            {
                Position = question.Position,
                Type = question.Type,
                Content = question.Content,
                Language = question.Language,
                Choices = choices.Select(e => e.Content).ToList()
            });
        }

        deliverer.DateStarted = DateTimeOffset.UtcNow;

        await db.StringSetAsync(key, JsonConvert.SerializeObject(deliverer), TimeSpan.FromHours(1));
        return Ok(new ResponseDto<IEnumerable<PetQuestionDto>>
        {
            Status = ResponseStatus.Ok, Code = ResponseCodes.Ok, Data = questions
        });
    }

    /// <summary>
    ///     Answers objective questions and retrieves subjective questions.
    /// </summary>
    /// <returns>An array of subjective questions.</returns>
    /// <response code="200">Returns an array of subjective questions.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    [HttpPost("objective")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<PetQuestionDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetSubjectiveQuestions([FromBody] List<PetObjectiveAnswerDto> dtos)
    {
        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!_resourceService.HasPermission(currentUser, UserRole.Member))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (_resourceService.HasPermission(currentUser, UserRole.Volunteer))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.AlreadyDone
            });

        if (dtos.Count != 15)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorWithMessage,
                Code = ResponseCodes.InvalidData,
                Message = "Must submit exactly fifteen answers."
            });

        var db = _redis.GetDatabase();
        var key = $"phizone:pet:0:{currentUser.Id}";
        if (!await db.KeyExistsAsync(key))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidOperation
            });

        var objectiveDeliverer = JsonConvert.DeserializeObject<PetDelivererDto>((await db.StringGetAsync(key))!)!;
        var subjectiveDeliverer = new PetDelivererDto();
        await db.KeyDeleteAsync(key);
        var score = 0;
        for (var i = 0; i < 15; i++)
        {
            var question = objectiveDeliverer.Questions[i];
            var choices = dtos[i].Choices;
            if (choices.Count == 0 || choices.Count > question.Choices!.Count ||
                choices.Any(choice => !question.Choices[choice].IsCorrect))
                continue;

            score += question.Type == PetQuestionType.Single ||
                     choices.Count < question.Choices.Count(choice => choice.IsCorrect)
                ? 2
                : 4;
        }

        var questions = new List<PetQuestionDto>();

        for (var i = 16; i <= 18; i++)
        {
            var question = await _petQuestionRepository.GetRandomPetQuestionAsync(i, currentUser.Language);
            if (question == null) continue;

            subjectiveDeliverer.Questions.Add(new PetQuestionDeliverer { Id = question.Id, Type = question.Type });
            questions.Add(new PetQuestionDto
            {
                Position = question.Position,
                Type = question.Type,
                Content = question.Content,
                Language = question.Language
            });
        }

        subjectiveDeliverer.Score = score;
        subjectiveDeliverer.DateStarted = objectiveDeliverer.DateStarted;

        await db.StringSetAsync($"phizone:pet:1:{currentUser.Id}", JsonConvert.SerializeObject(subjectiveDeliverer),
            TimeSpan.FromHours(1));
        return Ok(new ResponseDto<IEnumerable<PetQuestionDto>>
        {
            Status = ResponseStatus.Ok, Code = ResponseCodes.Ok, Data = questions
        });
    }

    /// <summary>
    ///     Answers subjective questions.
    /// </summary>
    /// <returns>An empty body.</returns>
    /// <response code="201">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPost("subjective")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(ResponseDto<CreatedResponseDto<Guid>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> SubmitSubjectiveAnswer([FromBody] PetSubjectiveAnswerDto dto)
    {
        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!_resourceService.HasPermission(currentUser, UserRole.Member))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (_resourceService.HasPermission(currentUser, UserRole.Volunteer))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.AlreadyDone
            });

        var db = _redis.GetDatabase();
        var key = $"phizone:pet:1:{currentUser.Id}";
        if (!await db.KeyExistsAsync(key))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidOperation
            });

        var deliverer = JsonConvert.DeserializeObject<PetDelivererDto>((await db.StringGetAsync(key))!)!;
        await db.KeyDeleteAsync(key);
        var answer = new PetAnswer
        {
            Question1 = deliverer.Questions[0].Id,
            Question2 = deliverer.Questions[1].Id,
            Question3 = deliverer.Questions[2].Id,
            Answer1 = dto.Answer1,
            Answer2 = dto.Answer2,
            Answer3 = dto.Answer3,
            Chart = dto.Chart,
            ObjectiveScore = deliverer.Score,
            OwnerId = currentUser.Id,
            DateCreated = DateTimeOffset.UtcNow,
            DateUpdated = DateTimeOffset.UtcNow
        };
        if (!await _petAnswerRepository.CreatePetAnswerAsync(answer))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        await _feishuService.Notify(answer, deliverer.DateStarted, FeishuResources.QualificationReviewalChat);

        return StatusCode(StatusCodes.Status201Created,
            new ResponseDto<CreatedResponseDto<Guid>>
            {
                Status = ResponseStatus.Ok,
                Code = ResponseCodes.Ok,
                Data = new CreatedResponseDto<Guid> { Id = answer.Id }
            });
    }

    /// <summary>
    ///     Retrieves answers.
    /// </summary>
    /// <returns>An array of answers.</returns>
    /// <response code="200">Returns an array of answers.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    [HttpGet("answers")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<PetAnswerDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetAnswers([FromQuery] ArrayRequestDto dto,
        [FromQuery] PetAnswerFilterDto? filterDto = null)
    {
        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        var isModerator = _resourceService.HasPermission(currentUser, UserRole.Moderator);
        dto.PerPage = dto.PerPage > 0 && dto.PerPage < _dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? _dataSettings.Value.PaginationPerPage : _dataSettings.Value.PaginationMaxPerPage;
        dto.Page = dto.Page > 1 ? dto.Page : 1;
        var position = dto.PerPage * (dto.Page - 1);
        var predicateExpr = await _filterService.Parse(filterDto, dto.Predicate, currentUser,
            e => isModerator || e.OwnerId == currentUser.Id);
        IEnumerable<PetAnswer> answers;
        int total;
        if (dto.Search != null)
        {
            var result = await _meilisearchService.SearchAsync<PetAnswer>(dto.Search, dto.PerPage, dto.Page,
                !isModerator ? currentUser.Id : null);
            var idList = result.Hits.Select(item => item.Id).ToList();
            answers =
                (await _petAnswerRepository.GetPetAnswersAsync(predicate: e => idList.Contains(e.Id))).OrderBy(e =>
                    idList.IndexOf(e.Id));
            total = result.TotalHits;
        }
        else
        {
            answers = await _petAnswerRepository.GetPetAnswersAsync(dto.Order, dto.Desc, position, dto.PerPage,
                predicateExpr);
            total = await _petAnswerRepository.CountPetAnswersAsync(predicateExpr);
        }

        var list = new List<PetAnswerDto>();

        foreach (var answer in answers) list.Add(await _dtoMapper.MapPetAnswerAsync<PetAnswerDto>(answer));

        return Ok(new ResponseDto<IEnumerable<PetAnswerDto>>
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
    ///     Retrieves a specific answer.
    /// </summary>
    /// <param name="id">An answer's ID.</param>
    /// <returns>An answer.</returns>
    /// <response code="200">Returns an answer.</response>
    /// <response code="304">
    ///     When the resource has not been updated since last retrieval. Requires <c>If-None-Match</c>.
    /// </response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified answer is not found.</response>
    [HttpGet("answers/{id:guid}")]
    [ServiceFilter(typeof(ETagFilter))]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<PetAnswerDto>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status304NotModified, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetAnswer([FromRoute] Guid id)
    {
        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!await _petAnswerRepository.PetAnswerExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var answer = await _petAnswerRepository.GetPetAnswerAsync(id);
        if (answer.OwnerId != currentUser.Id && !_resourceService.HasPermission(currentUser, UserRole.Moderator))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        var dto = await _dtoMapper.MapPetAnswerAsync<PetAnswerDto>(answer);

        return Ok(new ResponseDto<PetAnswerDto> { Status = ResponseStatus.Ok, Code = ResponseCodes.Ok, Data = dto });
    }

    /// <summary>
    ///     Reviews an answer.
    /// </summary>
    /// <param name="id">An answer's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified pet answer is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPatch("answers/{id:guid}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> ReviewPetAnswer([FromRoute] Guid id, [FromBody] PetAnswerReviewDto dto)
    {
        if (!await _petAnswerRepository.PetAnswerExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var petAnswer = await _petAnswerRepository.GetPetAnswerAsync(id);

        var currentUser = (await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!_resourceService.HasPermission(currentUser, UserRole.Moderator))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        petAnswer.SubjectiveScore = dto.Score;
        petAnswer.TotalScore = petAnswer.ObjectiveScore + petAnswer.SubjectiveScore;
        petAnswer.AssessorId = currentUser.Id;
        petAnswer.DateUpdated = DateTimeOffset.UtcNow;

        var user = (await _userManager.FindByIdAsync(petAnswer.OwnerId.ToString()))!;

        // ReSharper disable once ReplaceWithFirstOrDefault.1
        KeyValuePair<UserRole, int>? pair = _scores.Any(e => petAnswer.TotalScore >= e.Value)
            ? _scores.First(e => petAnswer.TotalScore >= e.Value)
            : null;
        if (pair != null)
        {
            user.Role = pair.Value.Key;
            await _userManager.UpdateAsync(user);
        }

        if (!await _petAnswerRepository.UpdatePetAnswerAsync(petAnswer))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        await _notificationService.Notify(user, null, NotificationType.System,
            pair == null ? "pet-failed" : pair.Value.Key == UserRole.Volunteer ? "pet-volunteer" : "pet-qualified",
            new Dictionary<string, string> { { "Score", petAnswer.TotalScore.ToString()! } }, "pet-result");

        return NoContent();
    }
}