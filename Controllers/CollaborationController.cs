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

namespace PhiZoneApi.Controllers;

[Route("collaborations")]
[ApiVersion("2.0")]
[ApiController]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class CollaborationController(
    ICollaborationRepository collaborationRepository,
    UserManager<User> userManager,
    IMapper mapper,
    IResourceService resourceService,
    IOptions<DataSettings> dataSettings,
    IFilterService filterService,
    ITemplateService templateService,
    INotificationService notificationService,
    ISongSubmissionRepository songSubmissionRepository,
    IChartSubmissionRepository chartSubmissionRepository,
    IAuthorshipRepository authorshipRepository,
    ISongRepository songRepository,
    IChartRepository chartRepository)
    : Controller
{
    /// <summary>
    ///     Retrieves collaborations.
    /// </summary>
    /// <returns>An array of collaborations.</returns>
    /// <response code="200">Returns an array of collaborations.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<CollaborationDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetCollaborations([FromQuery] ArrayRequestDto dto,
        [FromQuery] CollaborationFilterDto? filterDto = null, [FromQuery] bool all = false)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        var isVolunteer = resourceService.HasPermission(currentUser, UserRole.Volunteer);
        if (!resourceService.HasPermission(currentUser, UserRole.Qualified))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        dto.PerPage = dto.PerPage > 0 && dto.PerPage < dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? dataSettings.Value.PaginationPerPage : dataSettings.Value.PaginationMaxPerPage;
        dto.Page = dto.Page > 1 ? dto.Page : 1;
        var position = dto.PerPage * (dto.Page - 1);
        var predicateExpr = await filterService.Parse(filterDto, dto.Predicate, currentUser,
            collaboration => (isVolunteer && all) || collaboration.InviteeId == currentUser.Id ||
                             collaboration.InviterId == currentUser.Id);
        var list = mapper.Map<List<CollaborationDto>>(
            await collaborationRepository.GetCollaborationsAsync(dto.Order, dto.Desc, position, dto.PerPage,
                predicateExpr));
        var total = await collaborationRepository.CountCollaborationsAsync(predicateExpr);

        return Ok(new ResponseDto<IEnumerable<CollaborationDto>>
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
    ///     Retrieves a specific collaboration.
    /// </summary>
    /// <param name="id">A collaboration's ID.</param>
    /// <returns>A collaboration.</returns>
    /// <response code="200">Returns a collaboration.</response>
    /// <response code="304">
    ///     When the resource has not been updated since last retrieval. Requires <c>If-None-Match</c>.
    /// </response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified collaboration is not found.</response>
    [HttpGet("{id:guid}")]
    [ServiceFilter(typeof(ETagFilter))]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<CollaborationDto>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status304NotModified, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetCollaboration([FromRoute] Guid id)
    {
        if (!await collaborationRepository.CollaborationExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var collaboration = await collaborationRepository.GetCollaborationAsync(id);
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (((collaboration.InviterId == currentUser.Id || collaboration.InviteeId == currentUser.Id) &&
             !resourceService.HasPermission(currentUser, UserRole.Qualified)) ||
            (collaboration.InviterId != currentUser.Id && collaboration.InviteeId != currentUser.Id &&
             !resourceService.HasPermission(currentUser, UserRole.Volunteer)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        var dto = mapper.Map<CollaborationDto>(collaboration);

        return Ok(new ResponseDto<CollaborationDto>
        {
            Status = ResponseStatus.Ok, Code = ResponseCodes.Ok, Data = dto
        });
    }

    /// <summary>
    ///     Updates a collaboration.
    /// </summary>
    /// <returns>An empty body.</returns>
    /// <response code="201">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified collaboration or invitee is not found.</response>
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
    public async Task<IActionResult> UpdateCollaboration([FromRoute] Guid id,
        [FromBody] JsonPatchDocument<CollaborationUpdateDto> patchDocument)
    {
        if (!await collaborationRepository.CollaborationExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var collaboration = await collaborationRepository.GetCollaborationAsync(id);
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if ((collaboration.InviterId == currentUser.Id &&
             !resourceService.HasPermission(currentUser, UserRole.Qualified)) ||
            (collaboration.InviterId != currentUser.Id &&
             !resourceService.HasPermission(currentUser, UserRole.Administrator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        var dto = mapper.Map<CollaborationUpdateDto>(collaboration);
        patchDocument.ApplyTo(dto, ModelState);

        if (!TryValidateModel(dto))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorDetailed,
                Code = ResponseCodes.InvalidData,
                Errors = ModelErrorTranslator.Translate(ModelState)
            });

        collaboration.Position = dto.Position;

        if (!await collaborationRepository.UpdateCollaborationAsync(collaboration))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return NoContent();
    }

    /// <summary>
    ///     Reviews a collaboration.
    /// </summary>
    /// <param name="id">A collaboration's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified collaboration is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPost("{id:guid}/review")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> ReviewCollaboration([FromRoute] Guid id, [FromBody] RequestReviewDto dto)
    {
        if (!await collaborationRepository.CollaborationExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var collaboration = await collaborationRepository.GetCollaborationAsync(id);
        if (collaboration.Status != RequestStatus.Waiting)
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InvalidOperation
            });
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if ((collaboration.InviteeId == currentUser.Id &&
             !resourceService.HasPermission(currentUser, UserRole.Qualified)) ||
            (collaboration.InviteeId != currentUser.Id &&
             !resourceService.HasPermission(currentUser, UserRole.Administrator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        string key;
        if (dto.Approve)
        {
            collaboration.Status = RequestStatus.Approved;
            key = "collab-approval";
            Submission submission;
            PublicResource? resource = null;
            if (await songSubmissionRepository.SongSubmissionExistsAsync(collaboration.SubmissionId))
            {
                submission = await songSubmissionRepository.GetSongSubmissionAsync(collaboration.SubmissionId);
                if (submission is { Status: RequestStatus.Approved, RepresentationId: not null })
                    resource = await songRepository.GetSongAsync(submission.RepresentationId.Value);
            }
            else
            {
                submission = await chartSubmissionRepository.GetChartSubmissionAsync(collaboration.SubmissionId);
                if (submission is { Status: RequestStatus.Approved, RepresentationId: not null })
                    resource = await chartRepository.GetChartAsync(submission.RepresentationId.Value);
            }

            if (resource != null)
            {
                if (await authorshipRepository.AuthorshipExistsAsync(resource.Id, collaboration.InviteeId))
                {
                    var authorship =
                        await authorshipRepository.GetAuthorshipAsync(resource.Id, collaboration.InviteeId);
                    authorship.Position = collaboration.Position;
                    await authorshipRepository.UpdateAuthorshipAsync(authorship);
                }
                else
                {
                    var authorship = new Authorship
                    {
                        ResourceId = resource.Id,
                        AuthorId = collaboration.InviteeId,
                        Position = collaboration.Position,
                        DateCreated = DateTimeOffset.UtcNow
                    };
                    await authorshipRepository.CreateAuthorshipAsync(authorship);
                }
            }
        }
        else
        {
            collaboration.Status = RequestStatus.Rejected;
            key = "collab-rejection";
        }

        if (!await collaborationRepository.UpdateCollaborationAsync(collaboration))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });
        var inviter = (await userManager.FindByIdAsync(collaboration.InviterId.ToString()))!;
        var invitee = (await userManager.FindByIdAsync(collaboration.InviteeId.ToString()))!;

        await notificationService.Notify(inviter, invitee, NotificationType.Requests, key,
            new Dictionary<string, string>
            {
                { "User", resourceService.GetRichText<User>(invitee.Id.ToString(), invitee.UserName!) },
                {
                    "Collaboration",
                    resourceService.GetRichText<Collaboration>(collaboration.Id.ToString(),
                        templateService.GetMessage("more-info", invitee.Language)!)
                }
            });

        return NoContent();
    }

    /// <summary>
    ///     Removes a collaboration.
    /// </summary>
    /// <param name="id">A collaboration's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified collaboration is not found.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpDelete("{id:guid}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> RemoveCollaboration([FromRoute] Guid id)
    {
        if (!await collaborationRepository.CollaborationExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var collaboration = await collaborationRepository.GetCollaborationAsync(id);
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if ((collaboration.InviterId == currentUser.Id &&
             !resourceService.HasPermission(currentUser, UserRole.Qualified)) ||
            (collaboration.InviterId != currentUser.Id &&
             !resourceService.HasPermission(currentUser, UserRole.Administrator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });
        if (!await collaborationRepository.RemoveCollaborationAsync(id))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return NoContent();
    }
}