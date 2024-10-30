using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.JsonPatch;
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
using PhiZoneApi.Utils;
using StackExchange.Redis;
using HP = PhiZoneApi.Constants.HostshipPermissions;

// ReSharper disable RouteTemplates.ActionRoutePrefixCanBeExtractedToControllerRoute

namespace PhiZoneApi.Controllers;

[Route("events/teams")]
[ApiVersion("2.0")]
[ApiController]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
    Policy = "AllowAnonymous")]
public class EventTeamController(
    IEventTeamRepository eventTeamRepository,
    IEventDivisionRepository eventDivisionRepository,
    IEventRepository eventRepository,
    IOptions<DataSettings> dataSettings,
    IFilterService filterService,
    UserManager<User> userManager,
    IMapper mapper,
    IDtoMapper dtoMapper,
    IResourceService resourceService,
    IFileStorageService fileStorageService,
    ILeaderboardService leaderboardService,
    ILikeService likeService,
    ILikeRepository likeRepository,
    IParticipationRepository participationRepository,
    IUserRepository userRepository,
    IConnectionMultiplexer redis,
    IScriptService scriptService,
    INotificationService notificationService,
    IMeilisearchService meilisearchService) : Controller
{
    /// <summary>
    ///     Retrieves event teams.
    /// </summary>
    /// <returns>An array of event teams.</returns>
    /// <response code="200">Returns an array of event teams.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<EventTeamDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetEventTeams([FromQuery] ArrayRequestDto dto,
        [FromQuery] EventTeamFilterDto? filterDto = null)
    {
        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        var permission = HP.Gen(HP.Retrieve, HP.Team);
        dto.PerPage = dto.PerPage > 0 && dto.PerPage < dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? dataSettings.Value.PaginationPerPage : dataSettings.Value.PaginationMaxPerPage;
        dto.Page = dto.Page > 1 ? dto.Page : 1;
        var position = dto.PerPage * (dto.Page - 1);
        var predicateExpr = await filterService.Parse(filterDto, dto.Predicate, currentUser,
            e =>
                (((currentUser != null && e.Participations.Any(f => f.ParticipantId == currentUser.Id)) ||
                  e.IsUnveiled) && e.Division.DateUnveiled <= DateTimeOffset.UtcNow) || (currentUser != null &&
                    (resourceService.HasPermission(currentUser, UserRole.Administrator) ||
                     e.Division.Event.Hostships.Any(f => f.UserId == currentUser.Id &&
                                                         (f.IsAdmin || f.Permissions.Contains(permission))))));
        IEnumerable<EventTeam> eventTeams;
        int total;
        if (dto.Search != null)
        {
            var result = await meilisearchService.SearchAsync<EventTeam>(dto.Search, dto.PerPage, dto.Page);
            var idList = result.Hits.Select(item => item.Id).ToList();
            eventTeams =
                (await eventTeamRepository.GetEventTeamsAsync(predicate: e => idList.Contains(e.Id),
                    currentUserId: currentUser?.Id)).OrderBy(e => idList.IndexOf(e.Id));
            total = result.TotalHits;
        }
        else
        {
            eventTeams = await eventTeamRepository.GetEventTeamsAsync(dto.Order, dto.Desc, position, dto.PerPage,
                predicateExpr, currentUser?.Id);
            total = await eventTeamRepository.CountEventTeamsAsync(predicateExpr);
        }

        var list = eventTeams.Select(dtoMapper.MapEventTeam<EventTeamDto>).ToList();

        return Ok(new ResponseDto<IEnumerable<EventTeamDto>>
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
    ///     Retrieves a specific event team.
    /// </summary>
    /// <param name="id">An event team's ID.</param>
    /// <returns>An event team.</returns>
    /// <response code="200">Returns an event team.</response>
    /// <response code="304">
    ///     When the resource has not been updated since last retrieval. Requires <c>If-None-Match</c>.
    /// </response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified event team is not found.</response>
    [HttpGet("{id:guid}")]
    [ServiceFilter(typeof(ETagFilter))]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<EventTeamDto>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status304NotModified, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetEventTeam([FromRoute] Guid id)
    {
        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        if (!await eventTeamRepository.EventTeamExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var eventTeam = await eventTeamRepository.GetEventTeamAsync(id);
        var eventDivision = await eventDivisionRepository.GetEventDivisionAsync(eventTeam.DivisionId);
        var eventEntity = await eventRepository.GetEventAsync(eventDivision.EventId);
        var permission = HP.Gen(HP.Retrieve, HP.Team);
        if ((currentUser == null || !(resourceService.HasPermission(currentUser, UserRole.Administrator) ||
                                      eventEntity.Hostships.Any(f =>
                                          f.UserId == currentUser.Id &&
                                          (f.IsAdmin || f.Permissions.Contains(permission))) ||
                                      await participationRepository.ParticipationExistsAsync(id, currentUser.Id))) &&
            (eventDivision.DateUnveiled >= DateTimeOffset.UtcNow || !eventTeam.IsUnveiled))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var dto = dtoMapper.MapEventTeam<EventTeamDto>(eventTeam);
        var leaderboard = leaderboardService.ObtainEventDivisionLeaderboard(eventDivision.Id);
        dto.Position = leaderboard.GetRank(eventTeam);

        return Ok(new ResponseDto<EventTeamDto> { Status = ResponseStatus.Ok, Code = ResponseCodes.Ok, Data = dto });
    }

    /// <summary>
    ///     Retrieves reserved fields of event teams.
    /// </summary>
    /// <returns>A matrix of reserved fields.</returns>
    /// <response code="200">Returns an array of reserved fields.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    [HttpGet("reservedFields")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK,
        Type = typeof(ResponseDto<IEnumerable<IEnumerable<ReservedFieldDto?>>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetReservedFields([FromQuery] ArrayRequestDto dto,
        [FromQuery] EventTeamFilterDto? filterDto = null)
    {
        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        var permission = HP.Gen(HP.Retrieve, HP.Team);
        dto.PerPage = dto.PerPage > 0 && dto.PerPage < dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? dataSettings.Value.PaginationPerPage : dataSettings.Value.PaginationMaxPerPage;
        dto.Page = dto.Page > 1 ? dto.Page : 1;
        var position = dto.PerPage * (dto.Page - 1);
        var predicateExpr = await filterService.Parse(filterDto, dto.Predicate, currentUser,
            e => currentUser != null && (resourceService.HasPermission(currentUser, UserRole.Administrator) ||
                                         e.Division.Event.Hostships.Any(f =>
                                             f.UserId == currentUser.Id &&
                                             (f.IsAdmin || f.Permissions.Contains(permission)))));
        var eventTeams = await eventTeamRepository.GetEventTeamsAsync(dto.Order, dto.Desc, position,
            dto.PerPage, predicateExpr);
        var total = await eventTeamRepository.CountEventTeamsAsync(predicateExpr);

        List<IEnumerable<ReservedFieldDto?>> matrix = [];
        Dictionary<Guid, Hostship?> cache = [];
        permission = HP.Gen(HP.Retrieve, HP.ReservedField);

        // ReSharper disable once InvertIf
        if (currentUser == null || !resourceService.HasPermission(currentUser, UserRole.Administrator))
            foreach (var eventTeam in eventTeams)
            {
                Hostship? hostship;
                if (cache.TryGetValue(eventTeam.DivisionId, out var value))
                {
                    hostship = value;
                }
                else
                {
                    var eventDivision = await eventDivisionRepository.GetEventDivisionAsync(eventTeam.DivisionId);
                    var eventEntity = await eventRepository.GetEventAsync(eventDivision.EventId);
                    hostship = eventEntity.Hostships.FirstOrDefault(f =>
                        currentUser != null && f.UserId == currentUser.Id &&
                        (f.IsAdmin || f.Permissions.Any(e => e.SameAs(permission))));
                    cache.Add(eventTeam.DivisionId, hostship);
                }

                if (hostship == null)
                {
                    matrix.Add([]);
                }
                else
                {
                    IEnumerable<ReservedFieldDto?> list =
                        eventTeam.Reserved.Select((e, i) => new ReservedFieldDto { Index = i + 1, Content = e });

                    if (hostship.Permissions.All(e => e != permission))
                        matrix.Add(hostship.Permissions.Where(e => e.SameAs(permission))
                            .Select(HP.GetIndex)
                            .Select(index => list.ElementAtOrDefault(index - 1))
                            .ToList());
                    else
                        matrix.Add(list);
                }
            }
        else
            matrix = eventTeams.Select(e =>
                    e.Reserved.Select((f, i) => new ReservedFieldDto { Index = i + 1, Content = f }))
                .ToList()!;

        return Ok(new ResponseDto<IEnumerable<IEnumerable<ReservedFieldDto?>>>
        {
            Status = ResponseStatus.Ok,
            Code = ResponseCodes.Ok,
            Total = total,
            PerPage = dto.PerPage,
            HasPrevious = position > 0,
            HasNext = dto.PerPage > 0 && dto.PerPage * dto.Page < total,
            Data = matrix
        });
    }

    /// <summary>
    ///     Retrieves reserved fields of a specified event team.
    /// </summary>
    /// <param name="teamId">An event team's ID.</param>
    /// <returns>An array of reserved fields.</returns>
    /// <response code="200">Returns an array of reserved fields.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    [HttpGet("{teamId:guid}/reservedFields")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<ReservedFieldDto?>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetReservedFields([FromRoute] Guid teamId)
    {
        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        if (!await eventTeamRepository.EventTeamExistsAsync(teamId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var eventTeam = await eventTeamRepository.GetEventTeamAsync(teamId);
        var eventDivision = await eventDivisionRepository.GetEventDivisionAsync(eventTeam.DivisionId);
        var eventEntity = await eventRepository.GetEventAsync(eventDivision.EventId);
        var permission = HP.Gen(HP.Retrieve, HP.Team);
        if (currentUser == null ||
            (!(eventEntity.Hostships.Any(f =>
                   f.UserId == currentUser.Id && (f.IsAdmin || f.Permissions.Contains(permission))) ||
               resourceService.HasPermission(currentUser, UserRole.Administrator)) &&
             eventDivision.DateUnveiled >= DateTimeOffset.UtcNow))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        IEnumerable<ReservedFieldDto?> list = eventTeam.Reserved.Select((e, i) => new ReservedFieldDto
        {
            Index = i + 1, Content = e
        });

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
    ///     Updates a specific reserved field of the specified event team.
    /// </summary>
    /// <param name="teamId">An event team's ID.</param>
    /// <param name="index">A 1-based index.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified event team is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPost("{teamId:guid}/reservedFields/{index:int}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> UpdateReservedField([FromRoute] Guid teamId, [FromRoute] int index,
        [FromBody] StringDto dto)
    {
        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        if (!await eventTeamRepository.EventTeamExistsAsync(teamId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var eventTeam = await eventTeamRepository.GetEventTeamAsync(teamId);
        var eventDivision = await eventDivisionRepository.GetEventDivisionAsync(eventTeam.DivisionId);
        var eventEntity = await eventRepository.GetEventAsync(eventDivision.EventId);
        var permission = HP.Gen(HP.Update, HP.Team);
        if (currentUser == null ||
            (!(eventEntity.Hostships.Any(f =>
                   f.UserId == currentUser.Id && (f.IsAdmin || f.Permissions.Contains(permission))) ||
               resourceService.HasPermission(currentUser, UserRole.Administrator)) &&
             eventDivision.DateUnveiled >= DateTimeOffset.UtcNow))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        // ReSharper disable once InvertIf
        if (!(eventEntity.Hostships.Any(f => f.UserId == currentUser.Id && f.IsAdmin) ||
              resourceService.HasPermission(currentUser, UserRole.Administrator)))
        {
            var hostship = eventEntity.Hostships.FirstOrDefault(f =>
                f.UserId == currentUser.Id && (f.IsAdmin || f.Permissions.Contains(permission)));
            permission = HP.Gen(HP.Update, HP.ReservedField);
            var permissionWithIndex = HP.Gen(HP.Update, HP.ReservedField, index);
            if (hostship == null || (hostship.Permissions.All(e => e != permission) &&
                                     !hostship.HasPermission(permissionWithIndex)))
                return StatusCode(StatusCodes.Status403Forbidden,
                    new ResponseDto<object>
                    {
                        Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                    });
        }

        while (index > eventTeam.Reserved.Count) eventTeam.Reserved.Add(null);

        eventTeam.Reserved[index - 1] = dto.Content;

        if (!await eventTeamRepository.UpdateEventTeamAsync(eventTeam))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        await scriptService.RunEventTaskAsync(eventTeam.DivisionId, (eventTeam, index), eventTeam.Id, currentUser,
            [EventTaskType.OnTeamEvaluation]);

        if (eventTeam.IsUnveiled) leaderboardService.Add(eventTeam);

        return NoContent();
    }

    /// <summary>
    ///     Creates a new event team.
    /// </summary>
    /// <returns>The ID of the event team.</returns>
    /// <response code="201">Returns the ID of the event team.</response>
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
    public async Task<IActionResult> CreateEventTeam([FromForm] EventTeamCreationDto dto)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;

        if (!await eventDivisionRepository.EventDivisionExistsAsync(dto.DivisionId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentNotFound
            });

        var eventDivision = await eventDivisionRepository.GetEventDivisionAsync(dto.DivisionId);
        var eventEntity = await eventRepository.GetEventAsync(eventDivision.EventId);

        var permission = HP.Gen(HP.Create, HP.Team);
        if (!(resourceService.HasPermission(currentUser, UserRole.Administrator) || eventEntity.Hostships.Any(f =>
                f.UserId == currentUser.Id && (f.IsAdmin || f.Permissions.Contains(permission)))) &&
            eventDivision.DateUnveiled >= DateTimeOffset.UtcNow)
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (await participationRepository.CountParticipationsAsync(e =>
                e.ParticipantId == currentUser.Id && e.Team.DivisionId == dto.DivisionId) > 0)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidOperation
            });

        if (await eventTeamRepository.CountEventTeamsAsync(e =>
                e.Status != ParticipationStatus.Banned && e.DivisionId == dto.DivisionId) >= eventDivision.MaxTeamCount)
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorWithMessage,
                    Code = ResponseCodes.InvalidOperation,
                    Message = "No new teams can be registered for this division."
                });

        if (dto.ClaimedParticipantCount < eventDivision.MinParticipantPerTeamCount ||
            dto.ClaimedParticipantCount > eventDivision.MaxParticipantPerTeamCount)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorWithMessage,
                Code = ResponseCodes.InvalidData,
                Message = "The claimed number of participants is not within the allowed range."
            });

        if (dto.ClaimedSubmissionCount < eventDivision.MinSubmissionCount ||
            dto.ClaimedSubmissionCount > eventDivision.MaxSubmissionCount)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorWithMessage,
                Code = ResponseCodes.InvalidData,
                Message = "The claimed number of submissions is not within the allowed range."
            });

        var iconUrl = currentUser.Avatar;
        if (dto.Icon != null)
        {
            iconUrl = (await fileStorageService.UploadImage<EventTeam>(dto.Name, dto.Icon, (1, 1))).Item1;
            await fileStorageService.SendUserInput(iconUrl, "Icon", Request, currentUser);
        }

        Guid teamId;
        do
        {
            teamId = Guid.NewGuid();
        } while (await eventTeamRepository.EventTeamExistsAsync(teamId));

        var eventTeam = new EventTeam
        {
            Id = teamId,
            Name = dto.Name,
            Icon = iconUrl,
            Description = dto.Description,
            Status = dto.ClaimedParticipantCount == 1 ? ParticipationStatus.Prepared : ParticipationStatus.Registered,
            ClaimedParticipantCount = dto.ClaimedParticipantCount,
            ClaimedSubmissionCount = dto.ClaimedSubmissionCount,
            DivisionId = dto.DivisionId,
            OwnerId = currentUser.Id,
            DateCreated = DateTimeOffset.UtcNow
        };

        var firstFailure = await scriptService.RunEventTaskAsync(eventTeam.DivisionId, eventTeam, eventTeam.Id,
            currentUser, [EventTaskType.PreRegistration]);

        if (firstFailure != null)
            return BadRequest(new ResponseDto<object>
            {
                Status = firstFailure.Status, Code = firstFailure.Code, Message = firstFailure.Message
            });

        var participation = new Participation
        {
            TeamId = eventTeam.Id,
            ParticipantId = currentUser.Id,
            Position = "Leader",
            DateCreated = DateTimeOffset.UtcNow
        };

        firstFailure = await scriptService.RunEventTaskAsync(eventTeam.DivisionId, participation, eventTeam.Id,
            currentUser, [EventTaskType.PreParticipation]);

        if (firstFailure != null)
            return BadRequest(new ResponseDto<object>
            {
                Status = firstFailure.Status, Code = firstFailure.Code, Message = firstFailure.Message
            });

        if (!await eventTeamRepository.CreateEventTeamAsync(eventTeam))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        await participationRepository.CreateParticipationAsync(participation);

        await scriptService.RunEventTaskAsync(eventTeam.DivisionId, eventTeam, eventTeam.Id, currentUser,
            [EventTaskType.PostRegistration]);

        await scriptService.RunEventTaskAsync(eventTeam.DivisionId, participation, eventTeam.Id, currentUser,
            [EventTaskType.PostParticipation]);

        return StatusCode(StatusCodes.Status201Created,
            new ResponseDto<CreatedResponseDto<Guid>>
            {
                Status = ResponseStatus.Ok,
                Code = ResponseCodes.Ok,
                Data = new CreatedResponseDto<Guid> { Id = eventTeam.Id }
            });
    }

    /// <summary>
    ///     Updates an event team.
    /// </summary>
    /// <param name="id">An event team's ID.</param>
    /// <param name="patchDocument">A JSON Patch Document.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified event team is not found.</response>
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
    public async Task<IActionResult> UpdateEventTeam([FromRoute] Guid id,
        [FromBody] JsonPatchDocument<EventTeamUpdateDto> patchDocument)
    {
        if (!await eventTeamRepository.EventTeamExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var eventTeam = await eventTeamRepository.GetEventTeamAsync(id);
        var eventDivision = await eventDivisionRepository.GetEventDivisionAsync(eventTeam.DivisionId);
        var eventEntity = await eventRepository.GetEventAsync(eventDivision.EventId);

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        var permission = HP.Gen(HP.Update, HP.Team);
        var isAdmin = resourceService.HasPermission(currentUser, UserRole.Administrator) ||
                      eventEntity.Hostships.Any(f =>
                          f.UserId == currentUser.Id && (f.IsAdmin || f.Permissions.Contains(permission)));
        if (currentUser.Id != eventTeam.OwnerId && !isAdmin)
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (!isAdmin && !eventDivision.IsAvailable())
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.DivisionUnavailable
            });

        var dto = mapper.Map<EventTeamUpdateDto>(eventTeam);
        patchDocument.ApplyTo(dto, ModelState);

        if (!TryValidateModel(dto))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorDetailed,
                Code = ResponseCodes.InvalidData,
                Errors = ModelErrorTranslator.Translate(ModelState)
            });

        if (dto.ClaimedParticipantCount < eventDivision.MinParticipantPerTeamCount ||
            dto.ClaimedParticipantCount > eventDivision.MaxParticipantPerTeamCount)
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorWithMessage,
                    Code = ResponseCodes.InvalidData,
                    Message = "The claimed number of participants is not within the allowed range."
                });

        if (dto.ClaimedSubmissionCount < eventDivision.MinSubmissionCount ||
            dto.ClaimedSubmissionCount > eventDivision.MaxSubmissionCount)
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorWithMessage,
                    Code = ResponseCodes.InvalidData,
                    Message = "The claimed number of submissions is not within the allowed range."
                });

        eventTeam.Name = dto.Name;
        eventTeam.Description = dto.Description;
        eventTeam.ClaimedParticipantCount = dto.ClaimedParticipantCount;
        eventTeam.ClaimedSubmissionCount = dto.ClaimedSubmissionCount;

        var status = await resourceService.IsPreparedOrFinished(eventTeam);
        if (status.Item2)
            eventTeam.Status = ParticipationStatus.Finished;
        else if (status.Item1) eventTeam.Status = ParticipationStatus.Prepared;

        var firstFailure = await scriptService.RunEventTaskAsync(eventTeam.DivisionId, eventTeam, eventTeam.Id,
            currentUser, [EventTaskType.PreUpdateTeam]);

        if (firstFailure != null)
            return BadRequest(new ResponseDto<object>
            {
                Status = firstFailure.Status, Code = firstFailure.Code, Message = firstFailure.Message
            });

        if (!await eventTeamRepository.UpdateEventTeamAsync(eventTeam))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        await scriptService.RunEventTaskAsync(eventTeam.DivisionId, eventTeam, eventTeam.Id, currentUser,
            [EventTaskType.PostUpdateTeam]);

        if (eventTeam.IsUnveiled) leaderboardService.Add(eventTeam);

        return NoContent();
    }

    /// <summary>
    ///     Updates an event team's icon.
    /// </summary>
    /// <param name="id">An event team's ID.</param>
    /// <param name="dto">The new icon.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified event team is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPatch("{id:guid}/icon")]
    [Consumes("multipart/form-data")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> UpdateEventTeamIcon([FromRoute] Guid id, [FromForm] FileDto dto)
    {
        if (!await eventTeamRepository.EventTeamExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var eventTeam = await eventTeamRepository.GetEventTeamAsync(id);
        var eventDivision = await eventDivisionRepository.GetEventDivisionAsync(eventTeam.DivisionId);
        var eventEntity = await eventRepository.GetEventAsync(eventDivision.EventId);

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        var permission = HP.Gen(HP.Update, HP.Team);
        var isAdmin = resourceService.HasPermission(currentUser, UserRole.Administrator) ||
                      eventEntity.Hostships.Any(f =>
                          f.UserId == currentUser.Id && (f.IsAdmin || f.Permissions.Contains(permission)));
        if (currentUser.Id != eventTeam.OwnerId && !isAdmin)
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (!isAdmin && !eventDivision.IsAvailable())
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.DivisionUnavailable
            });

        if (dto.File != null)
        {
            eventTeam.Icon = (await fileStorageService.UploadImage<EventTeam>(eventTeam.Name, dto.File, (1, 1))).Item1;
            await fileStorageService.SendUserInput(eventTeam.Icon, "Icon", Request, currentUser);
        }

        var firstFailure = await scriptService.RunEventTaskAsync(eventTeam.DivisionId, eventTeam, eventTeam.Id,
            currentUser, [EventTaskType.PreUpdateTeam]);

        if (firstFailure != null)
            return BadRequest(new ResponseDto<object>
            {
                Status = firstFailure.Status, Code = firstFailure.Code, Message = firstFailure.Message
            });

        if (!await eventTeamRepository.UpdateEventTeamAsync(eventTeam))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        await scriptService.RunEventTaskAsync(eventTeam.DivisionId, eventTeam, eventTeam.Id, currentUser,
            [EventTaskType.PostUpdateTeam]);

        if (eventTeam.IsUnveiled) leaderboardService.Add(eventTeam);

        return NoContent();
    }

    /// <summary>
    ///     Removes an event team's icon.
    /// </summary>
    /// <param name="id">A chart submission's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified eventTeam is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpDelete("{id:guid}/icon")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> RemoveEventTeamIcon([FromRoute] Guid id)
    {
        if (!await eventTeamRepository.EventTeamExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var eventTeam = await eventTeamRepository.GetEventTeamAsync(id);
        var eventDivision = await eventDivisionRepository.GetEventDivisionAsync(eventTeam.DivisionId);
        var eventEntity = await eventRepository.GetEventAsync(eventDivision.EventId);

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        var permission = HP.Gen(HP.Update, HP.Team);
        var isAdmin = resourceService.HasPermission(currentUser, UserRole.Administrator) ||
                      eventEntity.Hostships.Any(f =>
                          f.UserId == currentUser.Id && (f.IsAdmin || f.Permissions.Contains(permission)));
        if (currentUser.Id != eventTeam.OwnerId && !isAdmin)
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (!isAdmin && !eventDivision.IsAvailable())
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.DivisionUnavailable
            });

        eventTeam.Icon = null;

        var firstFailure = await scriptService.RunEventTaskAsync(eventTeam.DivisionId, eventTeam, eventTeam.Id,
            currentUser, [EventTaskType.PreUpdateTeam]);

        if (firstFailure != null)
            return BadRequest(new ResponseDto<object>
            {
                Status = firstFailure.Status, Code = firstFailure.Code, Message = firstFailure.Message
            });

        if (!await eventTeamRepository.UpdateEventTeamAsync(eventTeam))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        await scriptService.RunEventTaskAsync(eventTeam.DivisionId, eventTeam, eventTeam.Id, currentUser,
            [EventTaskType.PostUpdateTeam]);

        if (eventTeam.IsUnveiled) leaderboardService.Add(eventTeam);

        return NoContent();
    }

    /// <summary>
    ///     Removes an event team.
    /// </summary>
    /// <param name="id">An event team's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified event team is not found.</response>
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
    public async Task<IActionResult> RemoveEventTeam([FromRoute] Guid id)
    {
        if (!await eventTeamRepository.EventTeamExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var eventTeam = await eventTeamRepository.GetEventTeamAsync(id);
        var eventDivision = await eventDivisionRepository.GetEventDivisionAsync(eventTeam.DivisionId);
        var eventEntity = await eventRepository.GetEventAsync(eventDivision.EventId);

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        var permission = HP.Gen(HP.Remove, HP.Team);
        var isAdmin = resourceService.HasPermission(currentUser, UserRole.Administrator) ||
                      eventEntity.Hostships.Any(f =>
                          f.UserId == currentUser.Id && (f.IsAdmin || f.Permissions.Contains(permission)));
        if (currentUser.Id != eventTeam.OwnerId && !isAdmin)
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (!await eventTeamRepository.RemoveEventTeamAsync(id))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        var participants =
            (await participationRepository.GetParticipantsAsync(eventTeam.Id)).Select(e => e.ParticipantId);
        foreach (var user in
                 await userRepository.GetUsersAsync(["Id"], [false], 0, -1,
                     e => participants.Contains(e.Id) && e.Id != currentUser.Id))
            await notificationService.Notify(user, currentUser, NotificationType.System, "event-team-disband",
                new Dictionary<string, string>
                {
                    { "User", resourceService.GetRichText<User>(currentUser.Id.ToString(), currentUser.UserName!) },
                    {
                        "Event",
                        resourceService.GetRichText<Event>(eventEntity.Id.ToString(), eventEntity.GetDisplay())
                    },
                    {
                        "EventDivision",
                        resourceService.GetRichText<EventDivision>(eventDivision.Id.ToString(),
                            eventDivision.GetDisplay())
                    },
                    {
                        "EventTeam",
                        resourceService.GetRichText<EventTeam>(eventTeam.Id.ToString(), eventTeam.GetDisplay())
                    }
                });

        await scriptService.RunEventTaskAsync(eventTeam.DivisionId, eventTeam, eventTeam.Id, currentUser,
            [EventTaskType.OnDisbandment]);

        leaderboardService.Remove(eventTeam);

        return NoContent();
    }

    /// <summary>
    ///     Retrieves likes from a specified event team.
    /// </summary>
    /// <param name="id">An event team's ID.</param>
    /// <returns>An array of likes.</returns>
    /// <response code="200">Returns an array of likes.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified event team is not found.</response>
    [HttpGet("{id:guid}/likes")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<LikeDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetEventTeamLikes([FromRoute] Guid id, [FromQuery] ArrayRequestDto dto)
    {
        dto.PerPage = dto.PerPage > 0 && dto.PerPage < dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? dataSettings.Value.PaginationPerPage : dataSettings.Value.PaginationMaxPerPage;
        dto.Page = dto.Page > 1 ? dto.Page : 1;
        var position = dto.PerPage * (dto.Page - 1);
        if (!await eventTeamRepository.EventTeamExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var eventTeam = await eventTeamRepository.GetEventTeamAsync(id);
        var eventDivision = await eventDivisionRepository.GetEventDivisionAsync(eventTeam.DivisionId);
        var eventEntity = await eventRepository.GetEventAsync(eventDivision.EventId);

        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        var permission = HP.Gen(HP.Retrieve, HP.Team);
        if ((currentUser == null || !(resourceService.HasPermission(currentUser, UserRole.Administrator) ||
                                      eventEntity.Hostships.Any(f =>
                                          f.UserId == currentUser.Id &&
                                          (f.IsAdmin || f.Permissions.Contains(permission))) ||
                                      await participationRepository.ParticipationExistsAsync(id, currentUser.Id))) &&
            (eventDivision.DateUnveiled >= DateTimeOffset.UtcNow || !eventTeam.IsUnveiled))
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
    ///     Likes a specific event team.
    /// </summary>
    /// <param name="id">An event team's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="201">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="404">When the specified event team is not found.</response>
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
        if (!await eventTeamRepository.EventTeamExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var eventTeam = await eventTeamRepository.GetEventTeamAsync(id);
        var eventDivision = await eventDivisionRepository.GetEventDivisionAsync(eventTeam.DivisionId);
        var eventEntity = await eventRepository.GetEventAsync(eventDivision.EventId);
        var permission = HP.Gen(HP.Retrieve, HP.Team);
        if (!(resourceService.HasPermission(currentUser, UserRole.Administrator) ||
              eventEntity.Hostships.Any(f =>
                  f.UserId == currentUser.Id && (f.IsAdmin || f.Permissions.Contains(permission))) ||
              await participationRepository.ParticipationExistsAsync(id, currentUser.Id)) &&
            (eventDivision.DateUnveiled >= DateTimeOffset.UtcNow || !eventTeam.IsUnveiled))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        if (await resourceService.IsBlacklisted(eventTeam.OwnerId, currentUser.Id))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.Blacklisted
            });
        if (!await likeService.CreateLikeAsync(eventTeam, currentUser.Id))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.AlreadyDone
            });

        return StatusCode(StatusCodes.Status201Created);
    }

    /// <summary>
    ///     Removes the like from a specified event team.
    /// </summary>
    /// <param name="id">An event team's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="404">When the specified event team is not found.</response>
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
        if (!await eventTeamRepository.EventTeamExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var eventTeam = await eventTeamRepository.GetEventTeamAsync(id);
        var eventDivision = await eventDivisionRepository.GetEventDivisionAsync(eventTeam.DivisionId);
        var eventEntity = await eventRepository.GetEventAsync(eventDivision.EventId);
        var permission = HP.Gen(HP.Retrieve, HP.Team);
        if (!(resourceService.HasPermission(currentUser, UserRole.Administrator) ||
              eventEntity.Hostships.Any(f =>
                  f.UserId == currentUser.Id && (f.IsAdmin || f.Permissions.Contains(permission))) ||
              await participationRepository.ParticipationExistsAsync(id, currentUser.Id)) &&
            (eventDivision.DateUnveiled >= DateTimeOffset.UtcNow || !eventTeam.IsUnveiled))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        if (!await likeService.RemoveLikeAsync(eventTeam, currentUser.Id))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.AlreadyDone
            });

        return NoContent();
    }

    /// <summary>
    ///     Creates an invite code for a specific event team.
    /// </summary>
    /// <param name="id">An event team's ID.</param>
    /// <returns>An invite code.</returns>
    /// <response code="204">Returns an invite code.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified event team is not found.</response>
    [HttpPost("{id:guid}/invite")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(ResponseDto<CodeDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> CreateInvitation([FromRoute] Guid id)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!await eventTeamRepository.EventTeamExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var eventTeam = await eventTeamRepository.GetEventTeamAsync(id);
        var eventDivision = await eventDivisionRepository.GetEventDivisionAsync(eventTeam.DivisionId);
        var eventEntity = await eventRepository.GetEventAsync(eventDivision.EventId);

        var permission = HP.Gen(HP.Update, HP.Team);
        if (currentUser.Id != eventTeam.OwnerId &&
            !(resourceService.HasPermission(currentUser, UserRole.Administrator) || eventEntity.Hostships.Any(f =>
                f.UserId == currentUser.Id && (f.IsAdmin || f.Permissions.Contains(permission)))))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        var firstFailure = await scriptService.RunEventTaskAsync(eventTeam.DivisionId, eventTeam, eventTeam.Id,
            currentUser, [EventTaskType.PreInvitation]);

        if (firstFailure != null)
            return BadRequest(new ResponseDto<object>
            {
                Status = firstFailure.Status, Code = firstFailure.Code, Message = firstFailure.Message
            });

        var db = redis.GetDatabase();
        string key, code;
        do
        {
            code = resourceService.GenerateCode(6);
            key = $"phizone:eventteam:{code}";
        } while (await db.KeyExistsAsync(key));

        var dto = new EventTeamInviteDelivererDto
        {
            TeamId = id, InviterId = currentUser.Id, Code = code, DateExpired = DateTimeOffset.UtcNow.AddHours(12)
        };

        await db.StringSetAsync(key, JsonConvert.SerializeObject(dto), TimeSpan.FromHours(12));

        await scriptService.RunEventTaskAsync(eventTeam.DivisionId, dto, eventTeam.Id, currentUser,
            [EventTaskType.PostInvitation]);

        return StatusCode(StatusCodes.Status201Created,
            new ResponseDto<CodeDto>
            {
                Status = ResponseStatus.Ok, Code = ResponseCodes.Ok, Data = new CodeDto { Code = code }
            });
    }

    /// <summary>
    ///     Retrieves details of the specified invitation.
    /// </summary>
    /// <param name="id">An invite code.</param>
    /// <returns>Details of an invitation.</returns>
    /// <response code="204">Returns details of an invitation.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified code is invalid.</response>
    [HttpGet("invites/{code}")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<EventTeamInviteDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetInvitation([FromRoute] string code)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        var db = redis.GetDatabase();
        var key = $"phizone:eventteam:{code.ToUpper()}";
        if (!await db.KeyExistsAsync(key))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var invite = JsonConvert.DeserializeObject<EventTeamInviteDelivererDto>((await db.StringGetAsync(key))!)!;

        var inviter = (await userRepository.GetUserByIdAsync(invite.InviterId))!;
        var eventTeam = await eventTeamRepository.GetEventTeamAsync(invite.TeamId);
        var eventDivision = await eventDivisionRepository.GetEventDivisionAsync(eventTeam.DivisionId);
        var eventEntity = await eventRepository.GetEventAsync(eventDivision.EventId);

        var permission = HP.Gen(HP.Retrieve, HP.Team);
        if (currentUser.Id != eventTeam.OwnerId &&
            !(resourceService.HasPermission(currentUser, UserRole.Administrator) || eventEntity.Hostships.Any(f =>
                f.UserId == currentUser.Id && (f.IsAdmin || f.Permissions.Contains(permission)))) &&
            eventDivision.DateUnveiled >= DateTimeOffset.UtcNow)
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        return Ok(new ResponseDto<EventTeamInviteDto>
        {
            Status = ResponseStatus.Ok,
            Code = ResponseCodes.Ok,
            Data = new EventTeamInviteDto
            {
                Team = dtoMapper.MapEventTeam<EventTeamDto>(eventTeam),
                Inviter = dtoMapper.MapUser<UserDto>(inviter),
                DateExpired = invite.DateExpired
            }
        });
    }

    /// <summary>
    ///     Accepts an invitation.
    /// </summary>
    /// <param name="id">An invitation's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified invitation is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPost("invites/{code}/accept")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> AcceptInvitation([FromRoute] string code, [FromQuery] string? position = null)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        var db = redis.GetDatabase();
        var key = $"phizone:eventteam:{code}";
        if (!await db.KeyExistsAsync(key))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var invite = JsonConvert.DeserializeObject<EventTeamInviteDelivererDto>((await db.StringGetAsync(key))!)!;

        var eventTeam = await eventTeamRepository.GetEventTeamAsync(invite.TeamId);
        var eventDivision = await eventDivisionRepository.GetEventDivisionAsync(eventTeam.DivisionId);
        var eventEntity = await eventRepository.GetEventAsync(eventDivision.EventId);

        var permission = HP.Gen(HP.Retrieve, HP.Team);
        if (currentUser.Id != eventTeam.OwnerId &&
            !(resourceService.HasPermission(currentUser, UserRole.Administrator) || eventEntity.Hostships.Any(f =>
                f.UserId == currentUser.Id && (f.IsAdmin || f.Permissions.Contains(permission)))) &&
            eventDivision.DateUnveiled >= DateTimeOffset.UtcNow)
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (await participationRepository.CountParticipationsAsync(e =>
                e.ParticipantId == currentUser.Id && e.Team.DivisionId == eventDivision.Id) > 0)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidOperation
            });

        var participation = new Participation
        {
            TeamId = eventTeam.Id,
            ParticipantId = currentUser.Id,
            Position = position,
            DateCreated = DateTimeOffset.UtcNow
        };

        var firstFailure = await scriptService.RunEventTaskAsync(eventTeam.DivisionId, participation, eventTeam.Id,
            currentUser, [EventTaskType.PreParticipation]);

        if (firstFailure != null)
            return BadRequest(new ResponseDto<object>
            {
                Status = firstFailure.Status, Code = firstFailure.Code, Message = firstFailure.Message
            });

        if (!await participationRepository.CreateParticipationAsync(participation))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        var status = await resourceService.IsPreparedOrFinished(eventTeam);
        if (status.Item2)
        {
            eventTeam.Status = ParticipationStatus.Finished;
            await eventTeamRepository.UpdateEventTeamAsync(eventTeam);
        }
        else if (status.Item1)
        {
            eventTeam.Status = ParticipationStatus.Prepared;
            await eventTeamRepository.UpdateEventTeamAsync(eventTeam);
        }

        var participants =
            (await participationRepository.GetParticipantsAsync(eventTeam.Id)).Select(e => e.ParticipantId);
        foreach (var user in
                 await userRepository.GetUsersAsync(["Id"], [false], 0, -1,
                     e => participants.Contains(e.Id) && e.Id != currentUser.Id))
            await notificationService.Notify(user, currentUser, NotificationType.System, "event-team-join",
                new Dictionary<string, string>
                {
                    { "User", resourceService.GetRichText<User>(currentUser.Id.ToString(), currentUser.UserName!) },
                    {
                        "Event",
                        resourceService.GetRichText<Event>(eventEntity.Id.ToString(), eventEntity.GetDisplay())
                    },
                    {
                        "EventDivision",
                        resourceService.GetRichText<EventDivision>(eventDivision.Id.ToString(),
                            eventDivision.GetDisplay())
                    },
                    {
                        "EventTeam",
                        resourceService.GetRichText<EventTeam>(eventTeam.Id.ToString(), eventTeam.GetDisplay())
                    }
                });

        await scriptService.RunEventTaskAsync(eventTeam.DivisionId, participation, eventTeam.Id, currentUser,
            [EventTaskType.PostParticipation]);

        return NoContent();
    }

    /// <summary>
    ///     Updates a participation.
    /// </summary>
    /// <param name="teamId">An event team's ID.</param>
    /// <param name="participantId">A participant's ID.</param>
    /// <param name="patchDocument">A JSON Patch Document.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified event team is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPatch("{teamId:guid}/participants/{participantId:int}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> UpdateParticipation([FromRoute] Guid teamId, [FromRoute] int participantId,
        [FromBody] JsonPatchDocument<ParticipationUpdateDto> patchDocument)
    {
        if (!await participationRepository.ParticipationExistsAsync(teamId, participantId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var participation = await participationRepository.GetParticipationAsync(teamId, participantId);
        var eventTeam = await eventTeamRepository.GetEventTeamAsync(teamId);
        var eventDivision = await eventDivisionRepository.GetEventDivisionAsync(eventTeam.DivisionId);
        var eventEntity = await eventRepository.GetEventAsync(eventDivision.EventId);

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        var permission = HP.Gen(HP.Update, HP.Team);
        if (currentUser.Id != participantId && currentUser.Id != eventTeam.OwnerId &&
            !(resourceService.HasPermission(currentUser, UserRole.Administrator) || eventEntity.Hostships.Any(f =>
                f.UserId == currentUser.Id && (f.IsAdmin || f.Permissions.Contains(permission)))))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        var dto = mapper.Map<ParticipationUpdateDto>(participation);
        patchDocument.ApplyTo(dto, ModelState);

        if (!TryValidateModel(dto))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorDetailed,
                Code = ResponseCodes.InvalidData,
                Errors = ModelErrorTranslator.Translate(ModelState)
            });

        participation.Position = dto.Position;

        var firstFailure = await scriptService.RunEventTaskAsync(eventTeam.DivisionId, participation, eventTeam.Id,
            currentUser, [EventTaskType.PreUpdateParticipant]);

        if (firstFailure != null)
            return BadRequest(new ResponseDto<object>
            {
                Status = firstFailure.Status, Code = firstFailure.Code, Message = firstFailure.Message
            });

        if (!await participationRepository.UpdateParticipationAsync(participation))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        await scriptService.RunEventTaskAsync(eventTeam.DivisionId, participation, eventTeam.Id, currentUser,
            [EventTaskType.PostUpdateParticipant]);

        return NoContent();
    }

    /// <summary>
    ///     Removes a participation.
    /// </summary>
    /// <param name="teamId">An event team's ID.</param>
    /// <param name="participantId">A participant's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified event team is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpDelete("{teamId:guid}/participants/{participantId:int}")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> RemoveParticipation([FromRoute] Guid teamId, [FromRoute] int participantId)
    {
        if (!await participationRepository.ParticipationExistsAsync(teamId, participantId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var participation = await participationRepository.GetParticipationAsync(teamId, participantId);
        var eventTeam = await eventTeamRepository.GetEventTeamAsync(teamId);
        var eventDivision = await eventDivisionRepository.GetEventDivisionAsync(eventTeam.DivisionId);
        var eventEntity = await eventRepository.GetEventAsync(eventDivision.EventId);

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        var permission = HP.Gen(HP.Update, HP.Team);
        if (currentUser.Id != participantId && currentUser.Id != eventTeam.OwnerId &&
            !(resourceService.HasPermission(currentUser, UserRole.Administrator) || eventEntity.Hostships.Any(f =>
                f.UserId == currentUser.Id && (f.IsAdmin || f.Permissions.Contains(permission)))))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (participantId == eventTeam.OwnerId)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidOperation
            });

        if (!await participationRepository.RemoveParticipationAsync(teamId, participantId))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        var participants =
            (await participationRepository.GetParticipantsAsync(eventTeam.Id)).Select(e => e.ParticipantId);

        var target = (await userRepository.GetUserByIdAsync(participantId))!;

        foreach (var user in
                 await userRepository.GetUsersAsync(["Id"], [false], 0, -1,
                     e => participants.Contains(e.Id) && e.Id != currentUser.Id))
            await notificationService.Notify(user, user.Id == participantId ? currentUser : target,
                NotificationType.System, user.Id == participantId ? "event-team-remove" : "event-team-leave",
                new Dictionary<string, string>
                {
                    {
                        "User",
                        resourceService.GetRichText<User>(
                            (user.Id == participantId ? currentUser : target).Id.ToString(),
                            (user.Id == participantId ? currentUser : target).UserName!)
                    },
                    {
                        "Event", resourceService.GetRichText<Event>(eventEntity.Id.ToString(), eventEntity.GetDisplay())
                    },
                    {
                        "EventDivision",
                        resourceService.GetRichText<EventDivision>(eventDivision.Id.ToString(),
                            eventDivision.GetDisplay())
                    },
                    {
                        "EventTeam",
                        resourceService.GetRichText<EventTeam>(eventTeam.Id.ToString(), eventTeam.GetDisplay())
                    }
                });

        await scriptService.RunEventTaskAsync(eventTeam.DivisionId, participation, eventTeam.Id, currentUser,
            [EventTaskType.OnWithdrawal]);

        return NoContent();
    }
}