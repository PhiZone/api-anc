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

[Route("userRelations")]
[ApiVersion("2.0")]
[ApiController]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
    Policy = "AllowAnonymous")]
public class UserRelationController : Controller
{
    private readonly IOptions<DataSettings> _dataSettings;
    private readonly IDtoMapper _dtoMapper;
    private readonly IFilterService _filterService;
    private readonly UserManager<User> _userManager;
    private readonly IUserRelationRepository _userRelationRepository;

    public UserRelationController(IUserRelationRepository userRelationRepository, IFilterService filterService,
        UserManager<User> userManager, IOptions<DataSettings> dataSettings, IDtoMapper dtoMapper)
    {
        _userRelationRepository = userRelationRepository;
        _filterService = filterService;
        _userManager = userManager;
        _dataSettings = dataSettings;
        _dtoMapper = dtoMapper;
    }

    /// <summary>
    ///     Retrieves user relations.
    /// </summary>
    /// <returns>An array of user relations.</returns>
    /// <response code="200">Returns an array of user relations.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<UserRelationDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetUserRelations([FromQuery] ArrayWithTimeRequestDto dto,
        [FromQuery] UserRelationFilterDto? filterDto = null)
    {
        var currentUser = await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        dto.PerPage = dto.PerPage > 0 && dto.PerPage < _dataSettings.Value.PaginationMaxPerPage ? dto.PerPage :
            dto.PerPage == 0 ? _dataSettings.Value.PaginationPerPage : _dataSettings.Value.PaginationMaxPerPage;
        var position = dto.PerPage * (dto.Page - 1);
        var predicateExpr = await _filterService.Parse(filterDto, dto.Predicate, currentUser);
        var userRelations =
            await _userRelationRepository.GetRelationsAsync(dto.Order, dto.Desc, position, dto.PerPage, predicateExpr);
        var total = await _userRelationRepository.CountRelationsAsync(predicateExpr);
        var list = new List<UserRelationDto>();

        foreach (var userRelation in userRelations)
            list.Add(await _dtoMapper.MapUserRelationAsync<UserRelationDto>(userRelation, currentUser));

        return Ok(new ResponseDto<IEnumerable<UserRelationDto>>
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
    ///     Retrieves a specific user relation.
    /// </summary>
    /// <returns>A user relation.</returns>
    /// <response code="200">Returns a user relation.</response>
    /// <response code="304">
    ///     When the resource has not been updated since last retrieval. Requires <c>If-None-Match</c>.
    /// </response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified user relation is not found.</response>
    [HttpGet("{followerId:int}/{followeeId:int}")]
    [ServiceFilter(typeof(ETagFilter))]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<UserRelationDto>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status304NotModified, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetUserRelation([FromRoute] int followerId, [FromRoute] int followeeId)
    {
        var currentUser = await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        if (!await _userRelationRepository.RelationExistsAsync(followerId, followeeId))
            return NotFound(new ResponseDto<object>
                { Status = ResponseStatus.ErrorBrief, Code = ResponseCodes.ResourceNotFound });

        var userRelation = await _userRelationRepository.GetRelationAsync(followerId, followeeId);
        var dto = await _dtoMapper.MapUserRelationAsync<UserRelationDto>(userRelation, currentUser);

        return Ok(new ResponseDto<UserRelationDto> { Status = ResponseStatus.Ok, Code = ResponseCodes.Ok, Data = dto });
    }
}