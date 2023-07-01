using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using PhiZoneApi.Configurations;
using PhiZoneApi.Constants;
using PhiZoneApi.Dtos;
using PhiZoneApi.Enums;
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
    private readonly UserManager<User> _userManager;
    private readonly IUserRelationRepository _userRelationRepository;

    public UserRelationController(IUserRelationRepository userRelationRepository, UserManager<User> userManager,
        IOptions<DataSettings> dataSettings, IDtoMapper dtoMapper)
    {
        _userRelationRepository = userRelationRepository;
        _userManager = userManager;
        _dataSettings = dataSettings;
        _dtoMapper = dtoMapper;
    }

    /// <summary>
    ///     Retrieves user relations.
    /// </summary>
    /// <param name="order">The field by which the result is sorted. Defaults to <c>time</c>.</param>
    /// <param name="desc">Whether or not the result is sorted in descending order. Defaults to <c>false</c>.</param>
    /// <param name="page">The page number. Defaults to 1.</param>
    /// <param name="perPage">How many entries are present in one page. Defaults to DataSettings:PaginationPerPage.</param>
    /// <returns>An array of user relations.</returns>
    /// <response code="200">Returns an array of user relations.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<UserRelationDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetUserRelations(string order = "time", bool desc = false, int page = 1,
        int perPage = 0)
    {
        var currentUser = await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        perPage = perPage > 0 ? perPage : _dataSettings.Value.PaginationPerPage;
        var position = perPage * (page - 1);
        var userRelations = await _userRelationRepository.GetRelationsAsync(order, desc, position, perPage);
        var total = await _userRelationRepository.CountAsync();
        var list = new List<UserRelationDto>();

        foreach (var userRelation in userRelations)
            list.Add(await _dtoMapper.MapUserRelationAsync<UserRelationDto>(userRelation, currentUser));

        return Ok(new ResponseDto<IEnumerable<UserRelationDto>>
        {
            Status = ResponseStatus.Ok,
            Code = ResponseCodes.Ok,
            Total = total,
            PerPage = perPage,
            HasPrevious = position > 0,
            HasNext = position < total - total % perPage,
            Data = list
        });
    }

    /// <summary>
    ///     Retrieves a specific user relation.
    /// </summary>
    /// <returns>A user relation.</returns>
    /// <response code="200">Returns a user relation.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified user relation is not found.</response>
    [HttpGet("{followerId:int}/{followeeId:int}")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<UserRelationDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetUserRelation([FromRoute] int followerId, [FromRoute] int followeeId)
    {
        var currentUser = await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        if (!await _userRelationRepository.RelationExistsAsync(followerId, followeeId)) return NotFound();

        var userRelation = await _userRelationRepository.GetRelationAsync(followerId, followeeId);
        var dto = await _dtoMapper.MapUserRelationAsync<UserRelationDto>(userRelation, currentUser);

        return Ok(new ResponseDto<UserRelationDto> { Status = ResponseStatus.Ok, Code = ResponseCodes.Ok, Data = dto });
    }
}