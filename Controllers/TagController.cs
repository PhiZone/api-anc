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

[Route("tags")]
[ApiVersion("2.0")]
[ApiController]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
    Policy = "AllowAnonymous")]
public class TagController(
    ITagRepository tagRepository,
    ISongRepository songRepository,
    IChartRepository chartRepository,
    IEventResourceRepository eventResourceRepository,
    IEventDivisionRepository eventDivisionRepository,
    IEventRepository eventRepository,
    IDtoMapper dtoMapper,
    IOptions<DataSettings> dataSettings,
    IFilterService filterService,
    UserManager<User> userManager,
    IMapper mapper,
    IResourceService resourceService,
    IMeilisearchService meilisearchService) : Controller
{
    /// <summary>
    ///     Retrieves tags.
    /// </summary>
    /// <returns>An array of tags.</returns>
    /// <response code="200">Returns an array of tags.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<TagDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetTags([FromQuery] ArrayRequestDto dto,
        [FromQuery] TagFilterDto? filterDto = null)
    {
        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        dto.PerPage = dto.PerPage > 0 && dto.PerPage < dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? dataSettings.Value.PaginationPerPage : dataSettings.Value.PaginationMaxPerPage;
        dto.Page = dto.Page > 1 ? dto.Page : 1;
        var position = dto.PerPage * (dto.Page - 1);
        var predicateExpr = await filterService.Parse(filterDto, dto.Predicate, currentUser);
        IEnumerable<Tag> tags;
        int total;
        if (dto.Search != null)
        {
            var result = await meilisearchService.SearchAsync<Tag>(dto.Search, dto.PerPage, dto.Page);
            var idList = result.Hits.Select(item => item.Id).ToList();
            tags = (await tagRepository.GetTagsAsync(predicate: e => idList.Contains(e.Id)))
                .OrderBy(e => idList.IndexOf(e.Id));
            total = result.TotalHits;
        }
        else
        {
            tags = await tagRepository.GetTagsAsync(dto.Order, dto.Desc, position, dto.PerPage, predicateExpr);
            total = await tagRepository.CountTagsAsync(predicateExpr);
        }

        var list = mapper.Map<List<TagDto>>(tags);

        return Ok(new ResponseDto<IEnumerable<TagDto>>
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
    ///     Retrieves a specific tag.
    /// </summary>
    /// <param name="id">A tag's ID.</param>
    /// <returns>A tag.</returns>
    /// <response code="200">Returns a tag.</response>
    /// <response code="304">
    ///     When the resource has not been updated since last retrieval. Requires <c>If-None-Match</c>.
    /// </response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified tag is not found.</response>
    [HttpGet("{id:guid}")]
    [ServiceFilter(typeof(ETagFilter))]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<TagDto>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status304NotModified, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetTag([FromRoute] Guid id)
    {
        if (!await tagRepository.TagExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var tag = await tagRepository.GetTagAsync(id);
        var dto = mapper.Map<TagDto>(tag);

        return Ok(new ResponseDto<TagDto> { Status = ResponseStatus.Ok, Code = ResponseCodes.Ok, Data = dto });
    }

    /// <summary>
    ///     Retrieves a specific tag by its name.
    /// </summary>
    /// <param name="name">A tag's name.</param>
    /// <returns>A tag.</returns>
    /// <response code="200">Returns a tag.</response>
    /// <response code="304">
    ///     When the resource has not been updated since last retrieval. Requires <c>If-None-Match</c>.
    /// </response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified tag is not found.</response>
    [HttpGet("{name}")]
    [ServiceFilter(typeof(ETagFilter))]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<TagDto>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status304NotModified, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetTag([FromRoute] string name)
    {
        var normalizedName = resourceService.Normalize(name);
        if (!await tagRepository.TagExistsAsync(normalizedName))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var tag = await tagRepository.GetTagAsync(normalizedName);
        var dto = mapper.Map<TagDto>(tag);

        return Ok(new ResponseDto<TagDto> { Status = ResponseStatus.Ok, Code = ResponseCodes.Ok, Data = dto });
    }

    /// <summary>
    ///     Creates a new tag.
    /// </summary>
    /// <returns>An empty body.</returns>
    /// <response code="201">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(ResponseDto<CreatedResponseDto<Guid>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> CreateTag([FromBody] TagRequestDto dto)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!resourceService.HasPermission(currentUser, UserRole.Administrator))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        var tag = new Tag
        {
            Name = dto.Name,
            NormalizedName = resourceService.Normalize(dto.Name),
            Description = dto.Description,
            DateCreated = DateTimeOffset.UtcNow
        };
        if (!await tagRepository.CreateTagAsync(tag))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return StatusCode(StatusCodes.Status201Created,
            new ResponseDto<CreatedResponseDto<Guid>>
            {
                Status = ResponseStatus.Ok,
                Code = ResponseCodes.Ok,
                Data = new CreatedResponseDto<Guid> { Id = tag.Id }
            });
    }

    /// <summary>
    ///     Updates a tag.
    /// </summary>
    /// <param name="id">A tag's ID.</param>
    /// <param name="patchDocument">A JSON Patch Document.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified tag is not found.</response>
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
    public async Task<IActionResult> UpdateTag([FromRoute] Guid id,
        [FromBody] JsonPatchDocument<TagRequestDto> patchDocument)
    {
        if (!await tagRepository.TagExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var tag = await tagRepository.GetTagAsync(id);

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!resourceService.HasPermission(currentUser, UserRole.Moderator))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        var dto = mapper.Map<TagRequestDto>(tag);
        patchDocument.ApplyTo(dto, ModelState);

        if (!TryValidateModel(dto))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorDetailed,
                Code = ResponseCodes.InvalidData,
                Errors = ModelErrorTranslator.Translate(ModelState)
            });

        tag.Name = dto.Name;
        tag.NormalizedName = resourceService.Normalize(dto.Name);
        tag.Description = dto.Description;

        if (!await tagRepository.UpdateTagAsync(tag))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return NoContent();
    }

    /// <summary>
    ///     Removes a tag.
    /// </summary>
    /// <param name="id">A tag's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified tag is not found.</response>
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
    public async Task<IActionResult> RemoveTag([FromRoute] Guid id)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;

        if (!await tagRepository.TagExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        if (!resourceService.HasPermission(currentUser, UserRole.Administrator))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (!await tagRepository.RemoveTagAsync(id))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return NoContent();
    }

    /// <summary>
    ///     Retrieves songs from a specific tag.
    /// </summary>
    /// <param name="id">A tag's ID.</param>
    /// <returns>An array of songs.</returns>
    /// <response code="200">Returns an array of songs.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified tag is not found.</response>
    [HttpGet("{id:guid}/songs")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<SongDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetTagSongs([FromRoute] Guid id, [FromQuery] ArrayRequestDto dto)
    {
        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        dto.PerPage = dto.PerPage > 0 && dto.PerPage < dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? dataSettings.Value.PaginationPerPage : dataSettings.Value.PaginationMaxPerPage;
        dto.Page = dto.Page > 1 ? dto.Page : 1;
        var position = dto.PerPage * (dto.Page - 1);
        if (!await tagRepository.TagExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var songs = await songRepository.GetSongsAsync(dto.Order, dto.Desc, position, dto.PerPage,
            e => e.Tags.Any(tag => tag.Id == id), currentUser?.Id);
        var total = await songRepository.CountSongsAsync(e => e.Tags.Any(tag => tag.Id == id));
        var list = songs.Select(e => dtoMapper.MapSong<SongDto>(e)).ToList();

        return Ok(new ResponseDto<IEnumerable<SongDto>>
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
    ///     Retrieves charts from a specific tag.
    /// </summary>
    /// <param name="id">A tag's ID.</param>
    /// <returns>An array of charts.</returns>
    /// <response code="200">Returns an array of charts.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified tag is not found.</response>
    [HttpGet("{id:guid}/charts")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<ChartDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetTagCharts([FromRoute] Guid id, [FromQuery] ArrayRequestDto dto)
    {
        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        dto.PerPage = dto.PerPage > 0 && dto.PerPage < dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? dataSettings.Value.PaginationPerPage : dataSettings.Value.PaginationMaxPerPage;
        dto.Page = dto.Page > 1 ? dto.Page : 1;
        var position = dto.PerPage * (dto.Page - 1);
        if (!await tagRepository.TagExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var charts = await chartRepository.GetChartsAsync(dto.Order, dto.Desc, position, dto.PerPage,
            e => e.Tags.Any(tag => tag.Id == id), currentUser?.Id);
        var total = await chartRepository.CountChartsAsync(e => e.Tags.Any(tag => tag.Id == id));
        var list = charts.Select(e => dtoMapper.MapChart<ChartDto>(e)).ToList();

        return Ok(new ResponseDto<IEnumerable<ChartDto>>
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
    ///     Links a tag to a specific event division.
    /// </summary>
    /// <returns>An empty body.</returns>
    /// <response code="201">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPost("{id:guid}/event")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(void), StatusCodes.Status201Created, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> CreateEventResource([FromRoute] Guid id, [FromBody] EventResourceRequestDto dto)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!await tagRepository.TagExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var tag = await tagRepository.GetTagAsync(id);

        if (!await eventDivisionRepository.EventDivisionExistsAsync(dto.DivisionId))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ParentNotFound
            });

        var eventDivision = await eventDivisionRepository.GetEventDivisionAsync(dto.DivisionId);
        var eventEntity = await eventRepository.GetEventAsync(eventDivision.EventId);
        var permission = HP.Gen(HP.Create, HP.Resource);

        if (!(resourceService.HasPermission(currentUser, UserRole.Administrator) || eventEntity.Hostships.Any(f =>
                f.UserId == currentUser.Id && (f.IsAdmin || f.Permissions.Contains(permission)))))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        var eventResource = new EventResource
        {
            DivisionId = dto.DivisionId,
            ResourceId = tag.Id,
            TagId = tag.Id,
            Type = dto.Type,
            Label = dto.Label,
            Description = dto.Description,
            IsAnonymous = dto.IsAnonymous,
            TeamId = dto.TeamId,
            DateCreated = DateTimeOffset.UtcNow
        };

        if (!await eventResourceRepository.CreateEventResourceAsync(eventResource))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return StatusCode(StatusCodes.Status201Created);
    }
}