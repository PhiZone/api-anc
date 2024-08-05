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
using HP = PhiZoneApi.Constants.HostshipPermissions;

// ReSharper disable RouteTemplates.ActionRoutePrefixCanBeExtractedToControllerRoute

namespace PhiZoneApi.Controllers;

[Route("events/divisions")]
[ApiVersion("2.0")]
[ApiController]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
    Policy = "AllowAnonymous")]
public class EventDivisionController(
    IEventDivisionRepository eventDivisionRepository,
    IEventRepository eventRepository,
    IEventTeamRepository eventTeamRepository,
    IEventTaskRepository eventTaskRepository,
    IOptions<DataSettings> dataSettings,
    IDtoMapper dtoMapper,
    IFilterService filterService,
    UserManager<User> userManager,
    ILikeRepository likeRepository,
    ILikeService likeService,
    ILeaderboardService leaderboardService,
    IMapper mapper,
    ICommentRepository commentRepository,
    IFileStorageService fileStorageService,
    IResourceService resourceService,
    ISongRepository songRepository,
    IChartRepository chartRepository,
    IRecordRepository recordRepository,
    ITagRepository tagRepository,
    INotificationService notificationService,
    IMeilisearchService meilisearchService) : Controller
{
    /// <summary>
    ///     Retrieves event divisions.
    /// </summary>
    /// <returns>An array of event divisions.</returns>
    /// <response code="200">Returns an array of event divisions.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<EventDivisionDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetEventDivisions([FromQuery] ArrayRequestDto dto,
        [FromQuery] EventDivisionFilterDto? filterDto = null)
    {
        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        var permission = HP.Gen(HP.Retrieve, HP.Division);
        dto.PerPage = dto.PerPage > 0 && dto.PerPage < dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? dataSettings.Value.PaginationPerPage : dataSettings.Value.PaginationMaxPerPage;
        dto.Page = dto.Page > 1 ? dto.Page : 1;
        var position = dto.PerPage * (dto.Page - 1);
        var predicateExpr = await filterService.Parse(filterDto, dto.Predicate, currentUser,
            e => e.DateUnveiled <= DateTimeOffset.UtcNow || (currentUser != null &&
                                                             (resourceService.HasPermission(currentUser,
                                                                  UserRole.Administrator) ||
                                                              e.Event.Hostships.Any(f =>
                                                                  f.UserId == currentUser.Id &&
                                                                  (f.IsAdmin || f.Permissions.Contains(permission))))));
        IEnumerable<EventDivision> eventDivisions;
        int total;
        if (dto.Search != null)
        {
            var result = await meilisearchService.SearchAsync<EventDivision>(dto.Search, dto.PerPage, dto.Page);
            var idList = result.Hits.Select(item => item.Id).ToList();
            eventDivisions =
                (await eventDivisionRepository.GetEventDivisionsAsync(predicate: e => idList.Contains(e.Id),
                    currentUserId: currentUser?.Id)).OrderBy(e => idList.IndexOf(e.Id));
            total = result.TotalHits;
        }
        else
        {
            eventDivisions = await eventDivisionRepository.GetEventDivisionsAsync(dto.Order, dto.Desc, position,
                dto.PerPage, predicateExpr, currentUser?.Id);
            total = await eventDivisionRepository.CountEventDivisionsAsync(predicateExpr);
        }

        List<EventDivisionDto> list = [];
        foreach (var eventDivision in eventDivisions)
            list.Add(await dtoMapper.MapEventDivisionAsync<EventDivisionDto>(eventDivision));

        return Ok(new ResponseDto<IEnumerable<EventDivisionDto>>
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
    ///     Retrieves a specific event division.
    /// </summary>
    /// <param name="id">An event division's ID.</param>
    /// <returns>An event division.</returns>
    /// <response code="200">Returns an event division.</response>
    /// <response code="304">
    ///     When the resource has not been updated since last retrieval. Requires <c>If-None-Match</c>.
    /// </response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified event division is not found.</response>
    [HttpGet("{id:guid}")]
    [ServiceFilter(typeof(ETagFilter))]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<EventDivisionDto>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status304NotModified, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetEventDivision([FromRoute] Guid id)
    {
        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        if (!await eventDivisionRepository.EventDivisionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var eventDivision = await eventDivisionRepository.GetEventDivisionAsync(id, currentUser?.Id);
        var eventEntity = await eventRepository.GetEventAsync(eventDivision.EventId);
        var permission = HP.Gen(HP.Retrieve, HP.Division);
        if ((currentUser == null ||
             !(eventEntity.Hostships.Any(f =>
                   f.UserId == currentUser.Id && (f.IsAdmin || f.Permissions.Contains(permission))) ||
               resourceService.HasPermission(currentUser, UserRole.Administrator))) &&
            eventDivision.DateUnveiled >= DateTimeOffset.UtcNow)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var dto = await dtoMapper.MapEventDivisionAsync<EventDivisionDto>(eventDivision);

        return Ok(new ResponseDto<EventDivisionDto>
        {
            Status = ResponseStatus.Ok, Code = ResponseCodes.Ok, Data = dto
        });
    }

    /// <summary>
    ///     Retrieves song prompts of a specified event division.
    /// </summary>
    /// <param name="id">An event division's ID.</param>
    /// <returns>An array of songs.</returns>
    /// <response code="200">Returns an array of songs.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    [HttpGet("{id:guid}/prompts/songs")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<EventSongPromptDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetSongPrompts([FromRoute] Guid id, [FromQuery] ArrayRequestDto dto,
        [FromQuery] SongFilterDto? filterDto = null, [FromQuery] ArrayTagDto? tagDto = null)
    {
        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        if (!await eventDivisionRepository.EventDivisionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var eventDivision = await eventDivisionRepository.GetEventDivisionAsync(id, currentUser?.Id);
        var eventEntity = await eventRepository.GetEventAsync(eventDivision.EventId);
        var permission = HP.Gen(HP.Retrieve, HP.Division);
        if ((currentUser == null ||
             !(eventEntity.Hostships.Any(f =>
                   f.UserId == currentUser.Id && (f.IsAdmin || f.Permissions.Contains(permission))) ||
               resourceService.HasPermission(currentUser, UserRole.Administrator))) &&
            eventDivision.DateUnveiled >= DateTimeOffset.UtcNow)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        if (eventDivision.Type != EventDivisionType.Chart)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidOperation
            });

        dto.PerPage = dto.PerPage > 0 && dto.PerPage < dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? dataSettings.Value.PaginationPerPage : dataSettings.Value.PaginationMaxPerPage;
        dto.Page = dto.Page > 1 ? dto.Page : 1;
        var position = dto.PerPage * (dto.Page - 1);
        var isAdmin = currentUser != null && (resourceService.HasPermission(currentUser, UserRole.Administrator) ||
                                              eventEntity.Hostships.Any(f =>
                                                  f.UserId == currentUser.Id && (f.IsAdmin ||
                                                      f.Permissions.Contains(HP.Gen(HP.Retrieve, HP.Resource)))));
        var predicateExpr = await filterService.Parse(filterDto, dto.Predicate, currentUser,
            e => e.EventPresences.Any(f => f.DivisionId == id) && (tagDto == null ||
                                                                   ((tagDto.TagsToInclude == null || e.Tags.Any(tag =>
                                                                        tagDto.TagsToInclude
                                                                            .Select(resourceService.Normalize)
                                                                            .ToList()
                                                                            .Contains(tag.NormalizedName))) &&
                                                                    (tagDto.TagsToExclude == null || e.Tags.All(tag =>
                                                                        !tagDto.TagsToExclude
                                                                            .Select(resourceService.Normalize)
                                                                            .ToList()
                                                                            .Contains(tag.NormalizedName))))), isAdmin);
        IEnumerable<Song> songs;
        int total;
        if (dto.Search != null)
        {
            var result = await meilisearchService.SearchAsync<Song>(dto.Search, dto.PerPage, dto.Page);
            var idList = result.Hits.Select(item => item.Id).ToList();
            songs = (await songRepository.GetSongsAsync(predicate: e => idList.Contains(e.Id),
                currentUserId: currentUser?.Id)).OrderBy(e => idList.IndexOf(e.Id));
            total = result.TotalHits;
        }
        else
        {
            songs = await songRepository.GetSongsAsync(dto.Order, dto.Desc, position, dto.PerPage, predicateExpr,
                currentUser?.Id);
            total = await songRepository.CountSongsAsync(predicateExpr);
        }

        var list = songs.Select(song =>
        {
            var songDto = mapper.Map<EventSongPromptDto>(song);
            songDto.Label = song.EventPresences.First(f => f.DivisionId == id).Label;
            songDto.EventDescription = song.EventPresences.First(f => f.DivisionId == id).Description;
            return songDto;
        });

        return Ok(new ResponseDto<IEnumerable<EventSongPromptDto>>
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
    ///     Retrieves chart prompts of a specified event division.
    /// </summary>
    /// <param name="id">An event division's ID.</param>
    /// <returns>An array of charts.</returns>
    /// <response code="200">Returns an array of charts.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    [HttpGet("{id:guid}/prompts/charts")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<EventChartPromptDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetChartPrompts([FromRoute] Guid id, [FromQuery] ArrayRequestDto dto,
        [FromQuery] ChartFilterDto? filterDto = null, [FromQuery] ArrayTagDto? tagDto = null)
    {
        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        if (!await eventDivisionRepository.EventDivisionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var eventDivision = await eventDivisionRepository.GetEventDivisionAsync(id, currentUser?.Id);
        var eventEntity = await eventRepository.GetEventAsync(eventDivision.EventId);
        var permission = HP.Gen(HP.Retrieve, HP.Division);
        if ((currentUser == null ||
             !(eventEntity.Hostships.Any(f =>
                   f.UserId == currentUser.Id && (f.IsAdmin || f.Permissions.Contains(permission))) ||
               resourceService.HasPermission(currentUser, UserRole.Administrator))) &&
            eventDivision.DateUnveiled >= DateTimeOffset.UtcNow)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        if (eventDivision.Type != EventDivisionType.Play)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidOperation
            });

        dto.PerPage = dto.PerPage > 0 && dto.PerPage < dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? dataSettings.Value.PaginationPerPage : dataSettings.Value.PaginationMaxPerPage;
        dto.Page = dto.Page > 1 ? dto.Page : 1;
        var position = dto.PerPage * (dto.Page - 1);
        var tagsToInclude = tagDto?.TagsToInclude?.Select(resourceService.Normalize).ToList();
        var tagsToExclude = tagDto?.TagsToExclude?.Select(resourceService.Normalize).ToList();
        var isAdmin = currentUser != null && (resourceService.HasPermission(currentUser, UserRole.Administrator) ||
                                              eventEntity.Hostships.Any(f =>
                                                  f.UserId == currentUser.Id && (f.IsAdmin ||
                                                      f.Permissions.Contains(HP.Gen(HP.Retrieve, HP.Resource)))));
        var predicateExpr = await filterService.Parse(filterDto, dto.Predicate, currentUser,
            e => e.EventPresences.Any(f => f.DivisionId == id) && (tagDto == null ||
                                                                   ((tagDto.TagsToInclude == null ||
                                                                     e.Tags.Any(tag =>
                                                                         tagsToInclude!.Contains(tag.NormalizedName)) ||
                                                                     e.Song.Tags.Any(tag =>
                                                                         tagsToInclude!.Contains(
                                                                             tag.NormalizedName))) &&
                                                                    (tagDto.TagsToExclude == null ||
                                                                     (e.Tags.All(tag =>
                                                                          !tagsToExclude!.Contains(
                                                                              tag.NormalizedName)) &&
                                                                      e.Song.Tags.All(tag =>
                                                                          !tagsToExclude!.Contains(
                                                                              tag.NormalizedName)))))), isAdmin);
        IEnumerable<Chart> charts;
        int total;
        if (dto.Search != null)
        {
            var result = await meilisearchService.SearchAsync<Chart>(dto.Search, dto.PerPage, dto.Page);
            var idList = result.Hits.Select(item => item.Id).ToList();
            charts = (await chartRepository.GetChartsAsync(predicate: e => idList.Contains(e.Id),
                currentUserId: currentUser?.Id)).OrderBy(e => idList.IndexOf(e.Id));
            total = result.TotalHits;
        }
        else
        {
            charts = await chartRepository.GetChartsAsync(dto.Order, dto.Desc, position, dto.PerPage, predicateExpr,
                currentUser?.Id);
            total = await chartRepository.CountChartsAsync(predicateExpr);
        }

        var list = charts.Select(chart =>
        {
            var chartDto = mapper.Map<EventChartPromptDto>(chart);
            chartDto.Label = chart.EventPresences.First(f => f.DivisionId == id).Label;
            chartDto.EventDescription = chart.EventPresences.First(f => f.DivisionId == id).Description;
            return chartDto;
        });

        return Ok(new ResponseDto<IEnumerable<EventChartPromptDto>>
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
    ///     Retrieves tags of a specified event division.
    /// </summary>
    /// <param name="id">An event division's ID.</param>
    /// <returns>An array of tags.</returns>
    /// <response code="200">Returns an array of tags.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    [HttpGet("{id:guid}/tags")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<TagDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetTags([FromRoute] Guid id, [FromQuery] ArrayRequestDto dto,
        [FromQuery] TagFilterDto? filterDto = null)
    {
        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        if (!await eventDivisionRepository.EventDivisionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var eventDivision = await eventDivisionRepository.GetEventDivisionAsync(id, currentUser?.Id);
        var eventEntity = await eventRepository.GetEventAsync(eventDivision.EventId);
        var permission = HP.Gen(HP.Retrieve, HP.Division);
        if ((currentUser == null ||
             !(eventEntity.Hostships.Any(f =>
                   f.UserId == currentUser.Id && (f.IsAdmin || f.Permissions.Contains(permission))) ||
               resourceService.HasPermission(currentUser, UserRole.Administrator))) &&
            eventDivision.DateUnveiled >= DateTimeOffset.UtcNow)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        dto.PerPage = dto.PerPage > 0 && dto.PerPage < dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? dataSettings.Value.PaginationPerPage : dataSettings.Value.PaginationMaxPerPage;
        dto.Page = dto.Page > 1 ? dto.Page : 1;
        var position = dto.PerPage * (dto.Page - 1);
        var isAdmin = currentUser != null && (resourceService.HasPermission(currentUser, UserRole.Administrator) ||
                                              eventEntity.Hostships.Any(f =>
                                                  f.UserId == currentUser.Id && (f.IsAdmin ||
                                                      f.Permissions.Contains(HP.Gen(HP.Retrieve, HP.Resource)))));
        var predicateExpr = await filterService.Parse(filterDto, dto.Predicate, currentUser,
            e => e.EventPresences.Any(f => f.DivisionId == id), isAdmin);
        IEnumerable<Tag> tags;
        int total;
        if (dto.Search != null)
        {
            var result = await meilisearchService.SearchAsync<Tag>(dto.Search, dto.PerPage, dto.Page);
            var idList = result.Hits.Select(item => item.Id).ToList();
            tags = (await tagRepository.GetTagsAsync(predicate: e => idList.Contains(e.Id))).OrderBy(e =>
                idList.IndexOf(e.Id));
            total = result.TotalHits;
        }
        else
        {
            tags = await tagRepository.GetTagsAsync(dto.Order, dto.Desc, position, dto.PerPage, predicateExpr);
            total = await tagRepository.CountTagsAsync(predicateExpr);
        }

        var list = tags.Select(tag =>
        {
            var tagDto = mapper.Map<EventTagDto>(tag);
            tagDto.Label = tag.EventPresences.First(f => f.DivisionId == id).Label;
            tagDto.EventDescription = tag.EventPresences.First(f => f.DivisionId == id).Description;
            return tagDto;
        });

        return Ok(new ResponseDto<IEnumerable<EventTagDto>>
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
    ///     Retrieves song entries of a specified event division. Note that the search string and authorship-related filters
    ///     are
    ///     disabled for divisions that require anonymization.
    /// </summary>
    /// <param name="id">An event division's ID.</param>
    /// <returns>An array of songs.</returns>
    /// <response code="200">Returns an array of songs.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    [HttpGet("{id:guid}/entries/songs")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<EventSongEntryDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetSongEntries([FromRoute] Guid id, [FromQuery] ArrayRequestDto dto,
        [FromQuery] SongFilterDto? filterDto = null, [FromQuery] ArrayTagDto? tagDto = null)
    {
        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        if (!await eventDivisionRepository.EventDivisionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var eventDivision = await eventDivisionRepository.GetEventDivisionAsync(id, currentUser?.Id);
        var eventEntity = await eventRepository.GetEventAsync(eventDivision.EventId);
        var permission = HP.Gen(HP.Retrieve, HP.Division);
        if ((currentUser == null ||
             !(eventEntity.Hostships.Any(f =>
                   f.UserId == currentUser.Id && (f.IsAdmin || f.Permissions.Contains(permission))) ||
               resourceService.HasPermission(currentUser, UserRole.Administrator))) &&
            eventDivision.DateUnveiled >= DateTimeOffset.UtcNow)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        if (eventDivision.Type != EventDivisionType.Song)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidOperation
            });

        if (filterDto != null && eventDivision.Anonymization)
        {
            filterDto.RangeOwnerId = null;
            filterDto.MinOwnerId = null;
            filterDto.MaxOwnerId = null;
            filterDto.ContainsAuthorName = null;
            filterDto.EqualsAuthorName = null;
        }

        dto.PerPage = dto.PerPage > 0 && dto.PerPage < dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? dataSettings.Value.PaginationPerPage : dataSettings.Value.PaginationMaxPerPage;
        dto.Page = dto.Page > 1 ? dto.Page : 1;
        var position = dto.PerPage * (dto.Page - 1);
        var isAdmin = currentUser != null && (resourceService.HasPermission(currentUser, UserRole.Administrator) ||
                                              eventEntity.Hostships.Any(f =>
                                                  f.UserId == currentUser.Id && (f.IsAdmin ||
                                                      f.Permissions.Contains(HP.Gen(HP.Retrieve, HP.Resource)))));
        var predicateExpr = await filterService.Parse(filterDto, dto.Predicate, currentUser,
            e => e.EventPresences.Any(f => f.DivisionId == id) &&
                 (!e.IsHidden || isAdmin || (currentUser != null && e.EventPresences.Any(f =>
                     f.Type == EventResourceType.Entry && f.Team!.Participants.Any(g => g.Id == currentUser.Id)))) &&
                 (tagDto == null ||
                  ((tagDto.TagsToInclude == null || e.Tags.Any(tag =>
                       tagDto.TagsToInclude.Select(resourceService.Normalize).ToList().Contains(tag.NormalizedName))) &&
                   (tagDto.TagsToExclude == null || e.Tags.All(tag =>
                       !tagDto.TagsToExclude.Select(resourceService.Normalize)
                           .ToList()
                           .Contains(tag.NormalizedName))))),
            isAdmin, true);
        IEnumerable<Song> songs = await songRepository.GetSongsAsync(dto.Order, dto.Desc, position, dto.PerPage,
            predicateExpr, currentUser?.Id, true);
        var total = await songRepository.CountSongsAsync(predicateExpr, true);

        var list = songs.Select(song =>
        {
            var songDto = dtoMapper.MapSong<EventSongEntryDto>(song);
            var team = song.EventPresences
                .FirstOrDefault(f => f.IsAnonymous != null && !f.IsAnonymous.Value && f.DivisionId == id)
                ?.Team;
            songDto.Team = team != null ? dtoMapper.MapEventTeam<EventTeamDto>(team) : null;
            return songDto;
        });

        return Ok(new ResponseDto<IEnumerable<EventSongEntryDto>>
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
    ///     Retrieves chart entries of a specified event division. Note that the search string and authorship-related filters
    ///     are disabled for divisions that require anonymization.
    /// </summary>
    /// <param name="id">An event division's ID.</param>
    /// <returns>An array of charts.</returns>
    /// <response code="200">Returns an array of charts.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    [HttpGet("{id:guid}/entries/charts")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<EventChartEntryDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetChartEntries([FromRoute] Guid id, [FromQuery] ArrayRequestDto dto,
        [FromQuery] ChartFilterDto? filterDto = null, [FromQuery] ArrayTagDto? tagDto = null)
    {
        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        if (!await eventDivisionRepository.EventDivisionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var eventDivision = await eventDivisionRepository.GetEventDivisionAsync(id, currentUser?.Id);
        var eventEntity = await eventRepository.GetEventAsync(eventDivision.EventId);
        var permission = HP.Gen(HP.Retrieve, HP.Division);
        if ((currentUser == null ||
             !(eventEntity.Hostships.Any(f =>
                   f.UserId == currentUser.Id && (f.IsAdmin || f.Permissions.Contains(permission))) ||
               resourceService.HasPermission(currentUser, UserRole.Administrator))) &&
            eventDivision.DateUnveiled >= DateTimeOffset.UtcNow)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        if (eventDivision.Type != EventDivisionType.Chart)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidOperation
            });

        if (filterDto != null && eventDivision.Anonymization)
        {
            filterDto.RangeOwnerId = null;
            filterDto.MinOwnerId = null;
            filterDto.MaxOwnerId = null;
            filterDto.ContainsAuthorName = null;
            filterDto.EqualsAuthorName = null;
        }

        dto.PerPage = dto.PerPage > 0 && dto.PerPage < dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? dataSettings.Value.PaginationPerPage : dataSettings.Value.PaginationMaxPerPage;
        dto.Page = dto.Page > 1 ? dto.Page : 1;
        var position = dto.PerPage * (dto.Page - 1);
        var tagsToInclude = tagDto?.TagsToInclude?.Select(resourceService.Normalize).ToList();
        var tagsToExclude = tagDto?.TagsToExclude?.Select(resourceService.Normalize).ToList();
        var isAdmin = currentUser != null && (resourceService.HasPermission(currentUser, UserRole.Administrator) ||
                                              eventEntity.Hostships.Any(f =>
                                                  f.UserId == currentUser.Id && (f.IsAdmin ||
                                                      f.Permissions.Contains(HP.Gen(HP.Retrieve, HP.Resource)))));
        var predicateExpr = await filterService.Parse(filterDto, dto.Predicate, currentUser,
            e => e.EventPresences.Any(f => f.DivisionId == id) &&
                 (!e.IsHidden || isAdmin || (currentUser != null && e.EventPresences.Any(f =>
                     f.Type == EventResourceType.Entry && f.Team!.Participants.Any(g => g.Id == currentUser.Id)))) &&
                 (tagDto == null ||
                  ((tagDto.TagsToInclude == null || e.Tags.Any(tag => tagsToInclude!.Contains(tag.NormalizedName)) ||
                    e.Song.Tags.Any(tag => tagsToInclude!.Contains(tag.NormalizedName))) &&
                   (tagDto.TagsToExclude == null || (e.Tags.All(tag => !tagsToExclude!.Contains(tag.NormalizedName)) &&
                                                     e.Song.Tags.All(tag =>
                                                         !tagsToExclude!.Contains(tag.NormalizedName)))))), isAdmin,
            true);
        IEnumerable<Chart> charts = await chartRepository.GetChartsAsync(dto.Order, dto.Desc, position, dto.PerPage,
            predicateExpr, currentUser?.Id, true);
        var total = await chartRepository.CountChartsAsync(predicateExpr, true);

        var list = charts.Select(chart =>
        {
            var chartDto = dtoMapper.MapChart<EventChartEntryDto>(chart);
            var team = chart.EventPresences
                .FirstOrDefault(f => f.IsAnonymous != null && !f.IsAnonymous.Value && f.DivisionId == id)
                ?.Team;
            chartDto.Team = team != null ? dtoMapper.MapEventTeam<EventTeamDto>(team) : null;
            return chartDto;
        });

        return Ok(new ResponseDto<IEnumerable<EventChartEntryDto>>
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
    ///     Retrieves record entries of a specified event division. Note that the search string and authorship-related filters
    ///     are disabled for divisions that require anonymization.
    /// </summary>
    /// <param name="id">An event division's ID.</param>
    /// <returns>An array of records.</returns>
    /// <response code="200">Returns an array of records.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    [HttpGet("{id:guid}/entries/records")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<EventRecordEntryDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetRecordEntries([FromRoute] Guid id, [FromQuery] ArrayRequestDto dto,
        [FromQuery] RecordFilterDto? filterDto = null)
    {
        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        if (!await eventDivisionRepository.EventDivisionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var eventDivision = await eventDivisionRepository.GetEventDivisionAsync(id, currentUser?.Id);
        var eventEntity = await eventRepository.GetEventAsync(eventDivision.EventId);
        var permission = HP.Gen(HP.Retrieve, HP.Division);
        if ((currentUser == null ||
             !(eventEntity.Hostships.Any(f =>
                   f.UserId == currentUser.Id && (f.IsAdmin || f.Permissions.Contains(permission))) ||
               resourceService.HasPermission(currentUser, UserRole.Administrator))) &&
            eventDivision.DateUnveiled >= DateTimeOffset.UtcNow)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        if (filterDto != null && eventDivision.Anonymization)
        {
            filterDto.RangeOwnerId = null;
            filterDto.MinOwnerId = null;
            filterDto.MaxOwnerId = null;
        }

        dto.PerPage = dto.PerPage > 0 && dto.PerPage < dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? dataSettings.Value.PaginationPerPage : dataSettings.Value.PaginationMaxPerPage;
        dto.Page = dto.Page > 1 ? dto.Page : 1;
        var position = dto.PerPage * (dto.Page - 1);
        var isAdmin = currentUser != null && (resourceService.HasPermission(currentUser, UserRole.Administrator) ||
                                              eventEntity.Hostships.Any(f =>
                                                  f.UserId == currentUser.Id && (f.IsAdmin ||
                                                      f.Permissions.Contains(HP.Gen(HP.Retrieve, HP.Resource)))));
        var predicateExpr = await filterService.Parse(filterDto, dto.Predicate, currentUser,
            e => e.EventPresences.Any(f => f.DivisionId == id), isAdmin);
        IEnumerable<Record> records = await recordRepository.GetRecordsAsync(dto.Order, dto.Desc, position, dto.PerPage,
            predicateExpr, true, currentUser?.Id, true);
        var total = await recordRepository.CountRecordsAsync(predicateExpr, true);

        var list = records.Select(record =>
        {
            var recordDto = dtoMapper.MapRecord<EventRecordEntryDto>(record);
            var team = record.EventPresences
                .FirstOrDefault(f => f.IsAnonymous != null && !f.IsAnonymous.Value && f.DivisionId == id)
                ?.Team;
            recordDto.Team = team != null ? dtoMapper.MapEventTeam<EventTeamDto>(team) : null;
            return recordDto;
        });

        return Ok(new ResponseDto<IEnumerable<EventRecordEntryDto>>
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
    ///     Retrieves the leaderboard of a specified event division.
    /// </summary>
    /// <param name="id">An event division's ID.</param>
    /// <returns>The leaderboard (an array of event teams).</returns>
    /// <response code="200">Returns an array of event teams.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified event division is not found.</response>
    [HttpGet("{id:guid}/leaderboard")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<EventTeamDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetLeaderboard([FromRoute] Guid id, [FromQuery] LeaderboardRequestDto dto)
    {
        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);

        if (!await eventDivisionRepository.EventDivisionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var eventDivision = await eventDivisionRepository.GetEventDivisionAsync(id);

        if ((currentUser == null || !resourceService.HasPermission(currentUser, UserRole.Administrator)) &&
            eventDivision.IsLocked)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.Locked
            });

        var leaderboard = leaderboardService.ObtainEventDivisionLeaderboard(eventDivision.Id);
        var rank = currentUser != null
            ? leaderboard.GetRank((await eventTeamRepository.GetEventTeamsAsync(predicate: e =>
                    e.DivisionId == eventDivision.Id && e.Participations.Any(f => f.ParticipantId == currentUser.Id)))
                .FirstOrDefault())
            : null;
        List<EventTeamDto> list;
        if (currentUser == null || rank == null || rank <= dto.TopRange + dto.NeighborhoodRange + 1)
        {
            var take = rank == null ? dto.TopRange : Math.Max(dto.TopRange, rank.Value + dto.NeighborhoodRange);
            list = leaderboard.Range(0, take)
                .Select((e, i) =>
                {
                    var r = mapper.Map<EventTeamDto>(e);
                    r.Position = i + 1;
                    return r;
                })
                .ToList();
        }
        else
        {
            list = [];
            list.AddRange(leaderboard.Range(0, dto.TopRange)
                .Select((e, i) =>
                {
                    var r = mapper.Map<EventTeamDto>(e);
                    r.Position = i + 1;
                    return r;
                })
                .ToList());
            list.AddRange(leaderboard.Range(rank.Value - dto.NeighborhoodRange - 1, dto.NeighborhoodRange * 2 + 1)
                .Select((e, i) =>
                {
                    var r = mapper.Map<EventTeamDto>(e);
                    r.Position = i + rank.Value - dto.NeighborhoodRange;
                    return r;
                })
                .ToList());
        }

        return Ok(new ResponseDto<IEnumerable<EventTeamDto>>
        {
            Status = ResponseStatus.Ok, Code = ResponseCodes.Ok, Data = list
        });
    }

    /// <summary>
    ///     Retrieves tasks of a specified event division.
    /// </summary>
    /// <param name="id">An event division's ID.</param>
    /// <returns>An array of event tasks.</returns>
    /// <response code="200">Returns an array of event tasks.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    [HttpGet("{id:guid}/tasks")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<EventTaskDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetEventTasks([FromRoute] Guid id, [FromQuery] ArrayRequestDto dto,
        [FromQuery] EventTaskFilterDto? filterDto = null)
    {
        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        if (!await eventDivisionRepository.EventDivisionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var eventDivision = await eventDivisionRepository.GetEventDivisionAsync(id, currentUser?.Id);
        var eventEntity = await eventRepository.GetEventAsync(eventDivision.EventId);
        var permission = HP.Gen(HP.Retrieve, HP.Division);
        if ((currentUser == null ||
             !(eventEntity.Hostships.Any(f =>
                   f.UserId == currentUser.Id && (f.IsAdmin || f.Permissions.Contains(permission))) ||
               resourceService.HasPermission(currentUser, UserRole.Administrator))) &&
            eventDivision.DateUnveiled >= DateTimeOffset.UtcNow)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        dto.PerPage = dto.PerPage > 0 && dto.PerPage < dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? dataSettings.Value.PaginationPerPage : dataSettings.Value.PaginationMaxPerPage;
        dto.Page = dto.Page > 1 ? dto.Page : 1;
        var position = dto.PerPage * (dto.Page - 1);
        var predicateExpr = await filterService.Parse(filterDto, dto.Predicate, currentUser, e => e.DivisionId == id,
            currentUser != null && eventEntity.Hostships.Any(f => f.UserId == currentUser.Id && f.IsAdmin));
        IEnumerable<EventTask> eventTasks;
        int total;
        if (dto.Search != null)
        {
            var result = await meilisearchService.SearchAsync<EventTask>(dto.Search, dto.PerPage, dto.Page);
            var idList = result.Hits.Select(item => item.Id).ToList();
            eventTasks =
                (await eventTaskRepository.GetEventTasksAsync(predicate: e => idList.Contains(e.Id))).OrderBy(e =>
                    idList.IndexOf(e.Id));
            total = result.TotalHits;
        }
        else
        {
            eventTasks = await eventTaskRepository.GetEventTasksAsync(dto.Order, dto.Desc, position, dto.PerPage,
                predicateExpr);
            total = await eventTaskRepository.CountEventTasksAsync(predicateExpr);
        }

        var list = eventTasks.Select(mapper.Map<EventTaskDto>).ToList();

        return Ok(new ResponseDto<IEnumerable<EventTaskDto>>
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
    ///     Retrieves reserved fields of a specified event division.
    /// </summary>
    /// <param name="id">An event division's ID.</param>
    /// <returns>An array of reserved fields.</returns>
    /// <response code="200">Returns an array of reserved fields.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    [HttpGet("{id:guid}/reservedFields")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<ReservedFieldDto?>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetReservedFields([FromRoute] Guid id)
    {
        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        if (!await eventDivisionRepository.EventDivisionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var eventDivision = await eventDivisionRepository.GetEventDivisionAsync(id, currentUser?.Id);
        var eventEntity = await eventRepository.GetEventAsync(eventDivision.EventId);
        var permission = HP.Gen(HP.Retrieve, HP.Division);
        if (currentUser == null ||
            (!(eventEntity.Hostships.Any(f =>
                   f.UserId == currentUser.Id && (f.IsAdmin || f.Permissions.Contains(permission))) ||
               resourceService.HasPermission(currentUser, UserRole.Administrator)) &&
             eventDivision.DateUnveiled >= DateTimeOffset.UtcNow))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        IEnumerable<ReservedFieldDto?> list =
            eventDivision.Reserved.Select((e, i) => new ReservedFieldDto { Index = i + 1, Content = e });

        // ReSharper disable once InvertIf
        if (!(eventEntity.Hostships.Any(f => f.UserId == currentUser.Id && f.IsAdmin) ||
              resourceService.HasPermission(currentUser, UserRole.Administrator)))
        {
            permission = HP.Gen(HP.Retrieve, HP.ReservedField);
            var hostship = eventEntity.Hostships.FirstOrDefault(f =>
                f.UserId == currentUser.Id && (f.IsAdmin || f.Permissions.Any(e => e.SameAs(permission))));
            if (hostship == null)
                list = [];
            else if (hostship.Permissions.All(e => e != permission))
                list = hostship.Permissions.Where(e => e.SameAs(permission))
                    .Select(HP.GetIndex)
                    .Select(index => list.ElementAtOrDefault(index - 1))
                    .ToList();
        }

        return Ok(new ResponseDto<IEnumerable<ReservedFieldDto?>>
        {
            Status = ResponseStatus.Ok, Code = ResponseCodes.Ok, Data = list
        });
    }

    /// <summary>
    ///     Creates a new event division.
    /// </summary>
    /// <returns>The ID of the event division.</returns>
    /// <response code="201">Returns the ID of the event division.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(ResponseDto<CreatedResponseDto<Guid>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> CreateEventDivision([FromForm] EventDivisionCreationDto dto)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        var eventEntity = await eventRepository.GetEventAsync(dto.EventId);
        var permission = HP.Gen(HP.Create, HP.Division);
        if (!(resourceService.HasPermission(currentUser, UserRole.Administrator) || eventEntity.Hostships.Any(f =>
                f.UserId == currentUser.Id && (f.IsAdmin || f.Permissions.Contains(permission)))))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        var illustrationUrl = dto.Illustration != null
            ? (await fileStorageService.UploadImage<EventDivision>(dto.Title, dto.Illustration, (16, 9))).Item1
            : null;
        if (illustrationUrl != null)
            await fileStorageService.SendUserInput(illustrationUrl, "Illustration", Request, currentUser);

        var tagName = dto.TagId != null ? (await tagRepository.GetTagAsync(dto.TagId.Value)).NormalizedName : null;

        var eventDivision = new EventDivision
        {
            Title = dto.Title,
            Subtitle = dto.Subtitle,
            Type = dto.Type,
            Status = dto.Status,
            Illustration = illustrationUrl,
            Illustrator = dto.Illustrator,
            Description = dto.Description,
            TagId = dto.TagId,
            TagName = tagName,
            MinTeamCount = dto.MinTeamCount,
            MaxTeamCount = dto.MaxTeamCount,
            MinParticipantPerTeamCount = dto.MinParticipantPerTeamCount,
            MaxParticipantPerTeamCount = dto.MaxParticipantPerTeamCount,
            MinSubmissionCount = dto.MinSubmissionCount,
            MaxSubmissionCount = dto.MaxSubmissionCount,
            Anonymization = dto.Anonymization,
            SuggestedEntrySearch = dto.SuggestedEntrySearch,
            Reserved = dto.Reserved,
            Accessibility = dto.Accessibility,
            IsHidden = dto.IsHidden,
            IsLocked = dto.IsLocked,
            EventId = dto.EventId,
            OwnerId = dto.OwnerId,
            DateUnveiled = dto.DateUnveiled,
            DateStarted = dto.DateStarted,
            DateEnded = dto.DateEnded,
            DateCreated = DateTimeOffset.UtcNow,
            DateUpdated = DateTimeOffset.UtcNow
        };
        if (!await eventDivisionRepository.CreateEventDivisionAsync(eventDivision))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return StatusCode(StatusCodes.Status201Created,
            new ResponseDto<CreatedResponseDto<Guid>>
            {
                Status = ResponseStatus.Ok,
                Code = ResponseCodes.Ok,
                Data = new CreatedResponseDto<Guid> { Id = eventDivision.Id }
            });
    }

    /// <summary>
    ///     Updates an event division.
    /// </summary>
    /// <param name="id">An event division's ID.</param>
    /// <param name="patchDocument">A JSON Patch Document.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified event division is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPatch("{id:guid}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> UpdateEventDivision([FromRoute] Guid id,
        [FromBody] JsonPatchDocument<EventDivisionUpdateDto> patchDocument)
    {
        if (!await eventDivisionRepository.EventDivisionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var eventDivision = await eventDivisionRepository.GetEventDivisionAsync(id);
        var eventEntity = await eventRepository.GetEventAsync(eventDivision.EventId);
        var permission = HP.Gen(HP.Update, HP.Division);

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!(resourceService.HasPermission(currentUser, UserRole.Administrator) || eventEntity.Hostships.Any(f =>
                f.UserId == currentUser.Id && (f.IsAdmin || f.Permissions.Contains(permission)))))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        var dto = mapper.Map<EventDivisionUpdateDto>(eventDivision);
        patchDocument.ApplyTo(dto, ModelState);

        if (!TryValidateModel(dto))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorDetailed,
                Code = ResponseCodes.InvalidData,
                Errors = ModelErrorTranslator.Translate(ModelState)
            });

        var tagName = dto.TagId != null ? (await tagRepository.GetTagAsync(dto.TagId.Value)).NormalizedName : null;

        eventDivision.Title = dto.Title;
        eventDivision.Subtitle = dto.Subtitle;
        eventDivision.Type = dto.Type;
        eventDivision.Status = dto.Status;
        eventDivision.Illustrator = dto.Illustrator;
        eventDivision.Description = dto.Description;
        eventDivision.TagId = dto.TagId;
        eventDivision.TagName = tagName;
        eventDivision.MinTeamCount = dto.MinTeamCount;
        eventDivision.MaxTeamCount = dto.MaxTeamCount;
        eventDivision.MinParticipantPerTeamCount = dto.MinParticipantPerTeamCount;
        eventDivision.MaxParticipantPerTeamCount = dto.MaxParticipantPerTeamCount;
        eventDivision.MinSubmissionCount = dto.MinSubmissionCount;
        eventDivision.MaxSubmissionCount = dto.MaxSubmissionCount;
        eventDivision.Anonymization = dto.Anonymization;
        eventDivision.SuggestedEntrySearch = dto.SuggestedEntrySearch;
        eventDivision.Reserved = dto.Reserved;
        eventDivision.Accessibility = dto.Accessibility;
        eventDivision.IsHidden = dto.IsHidden;
        eventDivision.IsLocked = dto.IsLocked;
        eventDivision.EventId = dto.EventId;
        eventDivision.OwnerId = dto.OwnerId;
        eventDivision.DateUnveiled = dto.DateUnveiled;
        eventDivision.DateStarted = dto.DateStarted;
        eventDivision.DateEnded = dto.DateEnded;
        eventDivision.DateUpdated = DateTimeOffset.UtcNow;

        if (!await eventDivisionRepository.UpdateEventDivisionAsync(eventDivision))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return NoContent();
    }

    /// <summary>
    ///     Updates an event division's illustration.
    /// </summary>
    /// <param name="id">EventDivision's ID.</param>
    /// <param name="dto">The new illustration.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified event division is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPatch("{id:guid}/illustration")]
    [Consumes("multipart/form-data")]
    [Produces("event/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> UpdateEventDivisionIllustration([FromRoute] Guid id, [FromForm] FileDto dto)
    {
        if (!await eventDivisionRepository.EventDivisionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var eventDivision = await eventDivisionRepository.GetEventDivisionAsync(id);
        var eventEntity = await eventRepository.GetEventAsync(eventDivision.EventId);
        var permission = HP.Gen(HP.Update, HP.Division);

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!(resourceService.HasPermission(currentUser, UserRole.Administrator) || eventEntity.Hostships.Any(f =>
                f.UserId == currentUser.Id && (f.IsAdmin || f.Permissions.Contains(permission)))))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        if (dto.File != null)
        {
            eventDivision.Illustration =
                (await fileStorageService.UploadImage<EventDivision>(eventDivision.Title, dto.File, (16, 9))).Item1;
            await fileStorageService.SendUserInput(eventDivision.Illustration, "Illustration", Request, currentUser);
            eventDivision.DateUpdated = DateTimeOffset.UtcNow;
        }

        if (!await eventDivisionRepository.UpdateEventDivisionAsync(eventDivision))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });
        return NoContent();
    }

    /// <summary>
    ///     Removes an event division's illustration.
    /// </summary>
    /// <param name="id">An event division's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified event division is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpDelete("{id:guid}/illustration")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> RemoveEventDivisionIllustration([FromRoute] Guid id)
    {
        if (!await eventDivisionRepository.EventDivisionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var eventDivision = await eventDivisionRepository.GetEventDivisionAsync(id);
        var eventEntity = await eventRepository.GetEventAsync(eventDivision.EventId);
        var permission = HP.Gen(HP.Update, HP.Division);

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!(resourceService.HasPermission(currentUser, UserRole.Administrator) || eventEntity.Hostships.Any(f =>
                f.UserId == currentUser.Id && (f.IsAdmin || f.Permissions.Contains(permission)))))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        eventDivision.Illustration = null;
        eventDivision.DateUpdated = DateTimeOffset.UtcNow;

        if (!await eventDivisionRepository.UpdateEventDivisionAsync(eventDivision))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return NoContent();
    }

    /// <summary>
    ///     Removes an event division.
    /// </summary>
    /// <param name="id">An event division's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified event division is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpDelete("{id:guid}")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> RemoveEventDivision([FromRoute] Guid id)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;

        if (!await eventDivisionRepository.EventDivisionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var eventDivision = await eventDivisionRepository.GetEventDivisionAsync(id);
        var eventEntity = await eventRepository.GetEventAsync(eventDivision.EventId);
        var permission = HP.Gen(HP.Remove, HP.Division);

        if (!(resourceService.HasPermission(currentUser, UserRole.Administrator) || eventEntity.Hostships.Any(f =>
                f.UserId == currentUser.Id && (f.IsAdmin || f.Permissions.Contains(permission)))))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (!await eventDivisionRepository.RemoveEventDivisionAsync(id))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return NoContent();
    }

    /// <summary>
    ///     Retrieves likes from a specified event division.
    /// </summary>
    /// <param name="id">An event division's ID.</param>
    /// <returns>An array of likes.</returns>
    /// <response code="200">Returns an array of likes.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified event division is not found.</response>
    [HttpGet("{id:guid}/likes")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<LikeDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetEventDivisionLikes([FromRoute] Guid id, [FromQuery] ArrayRequestDto dto)
    {
        dto.PerPage = dto.PerPage > 0 && dto.PerPage < dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? dataSettings.Value.PaginationPerPage : dataSettings.Value.PaginationMaxPerPage;
        dto.Page = dto.Page > 1 ? dto.Page : 1;
        var position = dto.PerPage * (dto.Page - 1);
        if (!await eventDivisionRepository.EventDivisionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var likes = await likeRepository.GetLikesAsync(dto.Order, dto.Desc, position, dto.PerPage,
            e => e.ResourceId == id);
        var list = mapper.Map<List<LikeDto>>(likes);
        var total = await likeRepository.CountLikesAsync(e => e.ResourceId == id);

        return Ok(new ResponseDto<IEnumerable<LikeDto>>
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
    ///     Likes a specific event division.
    /// </summary>
    /// <param name="id">An event division's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="201">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="404">When the specified event division is not found.</response>
    [HttpPost("{id:guid}/likes")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(void), StatusCodes.Status201Created, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> CreateLike([FromRoute] Guid id)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!await eventDivisionRepository.EventDivisionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var eventDivision = await eventDivisionRepository.GetEventDivisionAsync(id);
        var eventEntity = await eventRepository.GetEventAsync(eventDivision.EventId);
        var permission = HP.Gen(HP.Retrieve, HP.Division);
        if (!(eventEntity.Hostships.Any(f =>
                  f.UserId == currentUser.Id && (f.IsAdmin || f.Permissions.Contains(permission))) ||
              resourceService.HasPermission(currentUser, UserRole.Administrator)) &&
            eventDivision.DateUnveiled >= DateTimeOffset.UtcNow)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        if (await resourceService.IsBlacklisted(eventDivision.OwnerId, currentUser.Id))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.Blacklisted
            });
        if (!await likeService.CreateLikeAsync(eventDivision, currentUser.Id))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.AlreadyDone
            });

        return StatusCode(StatusCodes.Status201Created);
    }

    /// <summary>
    ///     Removes the like from a specified event division.
    /// </summary>
    /// <param name="id">An event division's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="404">When the specified event division is not found.</response>
    [HttpDelete("{id:guid}/likes")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> RemoveLike([FromRoute] Guid id)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!await eventDivisionRepository.EventDivisionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var eventDivision = await eventDivisionRepository.GetEventDivisionAsync(id);
        var eventEntity = await eventRepository.GetEventAsync(eventDivision.EventId);
        var permission = HP.Gen(HP.Retrieve, HP.Division);
        if (!(eventEntity.Hostships.Any(f =>
                  f.UserId == currentUser.Id && (f.IsAdmin || f.Permissions.Contains(permission))) ||
              resourceService.HasPermission(currentUser, UserRole.Administrator)) &&
            eventDivision.DateUnveiled >= DateTimeOffset.UtcNow)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        if (!await likeService.RemoveLikeAsync(eventDivision, currentUser.Id))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.AlreadyDone
            });

        return NoContent();
    }

    /// <summary>
    ///     Retrieves comments from a specified event division.
    /// </summary>
    /// <param name="id">An event division's ID.</param>
    /// <returns>An array of comments.</returns>
    /// <response code="200">Returns an array of comments.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified event division is not found.</response>
    [HttpGet("{id:guid}/comments")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<CommentDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetEventDivisionComments([FromRoute] Guid id, [FromQuery] ArrayRequestDto dto)
    {
        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        dto.PerPage = dto.PerPage > 0 && dto.PerPage < dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? dataSettings.Value.PaginationPerPage : dataSettings.Value.PaginationMaxPerPage;
        dto.Page = dto.Page > 1 ? dto.Page : 1;
        var position = dto.PerPage * (dto.Page - 1);
        if (!await eventDivisionRepository.EventDivisionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var eventDivision = await eventDivisionRepository.GetEventDivisionAsync(id);
        var eventEntity = await eventRepository.GetEventAsync(eventDivision.EventId);
        var permission = HP.Gen(HP.Retrieve, HP.Division);
        if ((currentUser == null || !(eventEntity.Hostships.Any(f =>
                                          f.UserId == currentUser.Id &&
                                          (f.IsAdmin || f.Permissions.Contains(permission))) ||
                                      resourceService.HasPermission(currentUser, UserRole.Administrator))) &&
            eventDivision.DateUnveiled >= DateTimeOffset.UtcNow)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var comments = await commentRepository.GetCommentsAsync(dto.Order, dto.Desc, position, dto.PerPage,
            e => e.ResourceId == id, currentUser?.Id);
        var total = await commentRepository.CountCommentsAsync(e => e.ResourceId == id);
        var list = comments.Select(dtoMapper.MapComment<CommentDto>).ToList();

        return Ok(new ResponseDto<IEnumerable<CommentDto>>
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
    ///     Comments on a specific event division.
    /// </summary>
    /// <param name="id">An event division's ID.</param>
    /// <returns>The ID of the comment.</returns>
    /// <response code="201">Returns the ID of the comment.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified event division is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPost("{id:guid}/comments")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(ResponseDto<CreatedResponseDto<Guid>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> CreateComment([FromRoute] Guid id, [FromBody] CommentCreationDto dto)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!resourceService.HasPermission(currentUser, UserRole.Member))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        if (!await eventDivisionRepository.EventDivisionExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var eventDivision = await eventDivisionRepository.GetEventDivisionAsync(id);
        var eventEntity = await eventRepository.GetEventAsync(eventDivision.EventId);
        var permission = HP.Gen(HP.Retrieve, HP.Division);
        if (!(eventEntity.Hostships.Any(f =>
                  f.UserId == currentUser.Id && (f.IsAdmin || f.Permissions.Contains(permission))) ||
              resourceService.HasPermission(currentUser, UserRole.Administrator)) &&
            eventDivision.DateUnveiled >= DateTimeOffset.UtcNow)
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        if (await resourceService.IsBlacklisted(eventDivision.OwnerId, currentUser.Id))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.Blacklisted
            });

        var result = await resourceService.ParseUserContent(dto.Content);
        var comment = new Comment
        {
            ResourceId = eventDivision.Id,
            Content = result.Item1,
            Language = dto.Language,
            OwnerId = currentUser.Id,
            DateCreated = DateTimeOffset.UtcNow
        };
        if (!await commentRepository.CreateCommentAsync(comment))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        await notificationService.NotifyComment(comment, eventDivision, eventDivision.GetDisplay(), dto.Content);
        await notificationService.NotifyMentions(result.Item2, currentUser,
            resourceService.GetRichText<Comment>(comment.Id.ToString(), dto.Content));

        return StatusCode(StatusCodes.Status201Created,
            new ResponseDto<CreatedResponseDto<Guid>>
            {
                Status = ResponseStatus.Ok,
                Code = ResponseCodes.Ok,
                Data = new CreatedResponseDto<Guid> { Id = comment.Id }
            });
    }
}