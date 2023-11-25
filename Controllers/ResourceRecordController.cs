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

[Route("resourceRecords")]
[ApiVersion("2.0")]
[ApiController]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
    Policy = "AllowAnonymous")]
public class ResourceRecordController(IResourceRecordRepository resourceRecordRepository,
    IOptions<DataSettings> dataSettings, IMapper mapper, IFilterService filterService, UserManager<User> userManager,
    IResourceService resourceService) : Controller
{
    /// <summary>
    ///     Retrieves resource records.
    /// </summary>
    /// <returns>An array of resource records.</returns>
    /// <response code="200">Returns an array of resource records.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<ResourceRecordDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetResourceRecords([FromQuery] ArrayRequestDto dto,
        [FromQuery] ResourceRecordFilterDto? filterDto = null)
    {
        var currentUser = await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        dto.PerPage = dto.PerPage > 0 && dto.PerPage < dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? dataSettings.Value.PaginationPerPage : dataSettings.Value.PaginationMaxPerPage;
        dto.Page = dto.Page > 1 ? dto.Page : 1;
        var position = dto.PerPage * (dto.Page - 1);
        var predicateExpr = await filterService.Parse(filterDto, dto.Predicate, currentUser);
        var resourceRecords = await resourceRecordRepository.GetResourceRecordsAsync(dto.Order, dto.Desc, position,
            dto.PerPage, dto.Search, predicateExpr);
        var total = await resourceRecordRepository.CountResourceRecordsAsync(dto.Search, predicateExpr);
        var list = mapper.Map<List<ResourceRecordDto>>(resourceRecords);

        return Ok(new ResponseDto<IEnumerable<ResourceRecordDto>>
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
    ///     Retrieves a specific resource record.
    /// </summary>
    /// <param name="id">A resource record's ID.</param>
    /// <returns>A resource record.</returns>
    /// <response code="200">Returns a resource record.</response>
    /// <response code="304">
    ///     When the resource has not been updated since last retrieval. Requires <c>If-None-Match</c>.
    /// </response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified resourceRecord is not found.</response>
    [HttpGet("{id:guid}")]
    [ServiceFilter(typeof(ETagFilter))]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<ResourceRecordDto>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status304NotModified, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetResourceRecord([FromRoute] Guid id)
    {
        if (!await resourceRecordRepository.ResourceRecordExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });
        var resourceRecord = await resourceRecordRepository.GetResourceRecordAsync(id);
        var dto = mapper.Map<ResourceRecordDto>(resourceRecord);

        return Ok(
            new ResponseDto<ResourceRecordDto> { Status = ResponseStatus.Ok, Code = ResponseCodes.Ok, Data = dto });
    }

    /// <summary>
    ///     Creates a new resource record.
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
    [ProducesResponseType(typeof(void), StatusCodes.Status201Created, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> CreateResourceRecord([FromBody] ResourceRecordRequestDto dto)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!await resourceService.HasPermission(currentUser, Roles.Administrator))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        var resourceRecord = new ResourceRecord
        {
            Type = dto.Type,
            Title = dto.Title,
            EditionType = dto.EditionType,
            Edition = dto.Edition,
            AuthorName = dto.AuthorName,
            Description = dto.Description,
            Strategy = dto.Strategy,
            Source = dto.Source,
            CopyrightOwner = dto.CopyrightOwner,
            DateCreated = DateTimeOffset.UtcNow,
            DateUpdated = DateTimeOffset.UtcNow
        };
        if (!await resourceRecordRepository.CreateResourceRecordAsync(resourceRecord))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return StatusCode(StatusCodes.Status201Created);
    }

    /// <summary>
    ///     Creates new resource records.
    /// </summary>
    /// <returns>An empty body.</returns>
    /// <response code="201">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="500">When an internal server error has occurred.</response>
    [HttpPost("batch")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(void), StatusCodes.Status201Created, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, "text/plain")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> CreateResourceRecords([FromBody] List<ResourceRecordRequestDto> dtos)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!await resourceService.HasPermission(currentUser, Roles.Administrator))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        var resourceRecords = dtos.Select(dto => new ResourceRecord
        {
            Type = dto.Type,
            Title = dto.Title,
            EditionType = dto.EditionType,
            Edition = dto.Edition,
            AuthorName = dto.AuthorName,
            Description = dto.Description,
            Strategy = dto.Strategy,
            Source = dto.Source,
            CopyrightOwner = dto.CopyrightOwner,
            DateCreated = DateTimeOffset.UtcNow,
            DateUpdated = DateTimeOffset.UtcNow
        });
        if (!await resourceRecordRepository.CreateResourceRecordsAsync(resourceRecords))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return StatusCode(StatusCodes.Status201Created);
    }

    /// <summary>
    ///     Updates a resource record.
    /// </summary>
    /// <param name="id">A resource record's ID.</param>
    /// <param name="patchDocument">A JSON Patch Document.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified resourceRecord is not found.</response>
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
    public async Task<IActionResult> UpdateResourceRecord([FromRoute] Guid id,
        [FromBody] JsonPatchDocument<ResourceRecordRequestDto> patchDocument)
    {
        if (!await resourceRecordRepository.ResourceRecordExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        var resourceRecord = await resourceRecordRepository.GetResourceRecordAsync(id);

        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;
        if (!await resourceService.HasPermission(currentUser, Roles.Administrator))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        var dto = mapper.Map<ResourceRecordRequestDto>(resourceRecord);
        patchDocument.ApplyTo(dto, ModelState);

        if (!TryValidateModel(dto))
            return BadRequest(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorDetailed,
                Code = ResponseCodes.InvalidData,
                Errors = ModelErrorTranslator.Translate(ModelState)
            });

        resourceRecord.Type = dto.Type;
        resourceRecord.Title = dto.Title;
        resourceRecord.EditionType = dto.EditionType;
        resourceRecord.Edition = dto.Edition;
        resourceRecord.AuthorName = dto.AuthorName;
        resourceRecord.Description = dto.Description;
        resourceRecord.Strategy = dto.Strategy;
        resourceRecord.Source = dto.Source;
        resourceRecord.CopyrightOwner = dto.CopyrightOwner;
        resourceRecord.DateUpdated = DateTimeOffset.UtcNow;

        if (!await resourceRecordRepository.UpdateResourceRecordAsync(resourceRecord))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return NoContent();
    }

    /// <summary>
    ///     Removes a resource record.
    /// </summary>
    /// <param name="id">A resource record's ID.</param>
    /// <returns>An empty body.</returns>
    /// <response code="204">Returns an empty body.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="401">When the user is not authorized.</response>
    /// <response code="403">When the user does not have sufficient permission.</response>
    /// <response code="404">When the specified resourceRecord is not found.</response>
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
    public async Task<IActionResult> RemoveResourceRecord([FromRoute] Guid id)
    {
        var currentUser = (await userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!))!;

        if (!await resourceRecordRepository.ResourceRecordExistsAsync(id))
            return NotFound(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound
            });

        if (!await resourceService.HasPermission(currentUser, Roles.Moderator))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ResponseDto<object>
                {
                    Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InsufficientPermission
                });

        if (!await resourceRecordRepository.RemoveResourceRecordAsync(id))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDto<object> { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.InternalError });

        return NoContent();
    }
}