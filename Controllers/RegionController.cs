using AutoMapper;
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

[Route("regions")]
[ApiVersion("2.0")]
[ApiController]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
    Policy = "AllowAnonymous")]
public class RegionController : Controller
{
    private readonly IOptions<DataSettings> _dataSettings;
    private readonly IDtoMapper _dtoMapper;
    private readonly IMapper _mapper;
    private readonly IRegionRepository _regionRepository;
    private readonly UserManager<User> _userManager;

    public RegionController(IRegionRepository regionRepository, IOptions<DataSettings> dataSettings,
        UserManager<User> userManager, IMapper mapper, IDtoMapper dtoMapper)
    {
        _regionRepository = regionRepository;
        _dataSettings = dataSettings;
        _userManager = userManager;
        _mapper = mapper;
        _dtoMapper = dtoMapper;
    }

    /// <summary>
    ///     Retrieves regions.
    /// </summary>
    /// <param name="order">The field by which the result is sorted. Defaults to <c>id</c>.</param>
    /// <param name="desc">Whether or not the result is sorted in descending order. Defaults to <c>false</c>.</param>
    /// <param name="page">The page number. Defaults to 1.</param>
    /// <param name="perPage">How many entries are present in one page. Defaults to DataSettings:PaginationPerPage.</param>
    /// <returns>An array of regions.</returns>
    /// <response code="200">Returns an array of regions.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<RegionDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetRegions(string order = "id", bool desc = false, int page = 1, int perPage = 0)
    {
        perPage = perPage > 0 ? perPage : _dataSettings.Value.PaginationPerPage;
        var position = perPage * (page - 1);
        var list = _mapper.Map<List<RegionDto>>(
            await _regionRepository.GetRegionsAsync(order, desc, position, perPage));
        var total = await _regionRepository.CountAsync();

        return Ok(new ResponseDto<IEnumerable<RegionDto>>
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
    ///     Retrieves a specific region.
    /// </summary>
    /// <param name="code">A region's code</param>
    /// <returns>A region.</returns>
    /// <response code="200">Returns a region.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified region is not found.</response>
    [HttpGet("{code}")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<RegionDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetRegion([FromRoute] string code)
    {
        if (!await _regionRepository.RegionExistsAsync(code)) return NotFound();
        var region = await _regionRepository.GetRegionAsync(code);
        var dto = _mapper.Map<RegionDto>(region);

        return Ok(new ResponseDto<RegionDto> { Status = ResponseStatus.Ok, Code = ResponseCodes.Ok, Data = dto });
    }

    /// <summary>
    ///     Retrieves a specific region by its ID.
    /// </summary>
    /// <param name="id">A region's ID.</param>
    /// <returns>A region.</returns>
    /// <response code="200">Returns a region.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified region is not found.</response>
    [HttpGet("{id:int}")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<RegionDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetRegion([FromRoute] int id)
    {
        if (!await _regionRepository.RegionExistsByIdAsync(id)) return NotFound();
        var region = await _regionRepository.GetRegionByIdAsync(id);
        var dto = _mapper.Map<RegionDto>(region);

        return Ok(new ResponseDto<RegionDto> { Status = ResponseStatus.Ok, Code = ResponseCodes.Ok, Data = dto });
    }

    /// <summary>
    ///     Retrieves users from a specific region.
    /// </summary>
    /// <param name="code">A region's code</param>
    /// <param name="order">The field by which the result is sorted. Defaults to <c>id</c>.</param>
    /// <param name="desc">Whether or not the result is sorted in descending order. Defaults to <c>false</c>.</param>
    /// <param name="page">The page number. Defaults to 1.</param>
    /// <param name="perPage">How many entries are present in one page. Defaults to DataSettings:PaginationPerPage.</param>
    /// <returns>An array of users.</returns>
    /// <response code="200">Returns an array of users.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified region is not found.</response>
    [HttpGet("{code}/users")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<UserDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetRegionUsers([FromRoute] string code, string order = "id", bool desc = false,
        int page = 1, int perPage = 0)
    {
        var currentUser = await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        perPage = perPage > 0 ? perPage : _dataSettings.Value.PaginationPerPage;
        var position = perPage * (page - 1);
        if (!await _regionRepository.RegionExistsAsync(code)) return NotFound();
        var users = await _regionRepository.GetRegionUsersAsync(code, order, desc, position, perPage);
        var list = new List<UserDto>();
        var total = await _regionRepository.CountUsersAsync(code);

        foreach (var user in users) list.Add(await _dtoMapper.MapUserAsync<UserDto>(user, currentUser: currentUser));

        return Ok(new ResponseDto<IEnumerable<UserDto>>
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
    ///     Retrieves users from a specific region by its ID.
    /// </summary>
    /// <param name="id">A region's ID.</param>
    /// <param name="order">The field by which the result is sorted. Defaults to <c>id</c>.</param>
    /// <param name="desc">Whether or not the result is sorted in descending order. Defaults to <c>false</c>.</param>
    /// <param name="page">The page number. Defaults to 1.</param>
    /// <param name="perPage">How many entries are present in one page. Defaults to DataSettings:PaginationPerPage.</param>
    /// <returns>An array of users.</returns>
    /// <response code="200">Returns an array of users.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified region is not found.</response>
    [HttpGet("{id:int}/users")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<UserDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetRegionUsers([FromRoute] int id, string order = "id", bool desc = false,
        int page = 1, int perPage = 0)
    {
        var currentUser = await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        perPage = perPage > 0 ? perPage : _dataSettings.Value.PaginationPerPage;
        var position = perPage * (page - 1);
        if (!await _regionRepository.RegionExistsByIdAsync(id)) return NotFound();
        var users = await _regionRepository.GetRegionUsersByIdAsync(id, order, desc, position, perPage);
        var list = new List<UserDto>();
        var total = await _regionRepository.CountUsersByIdAsync(id);

        foreach (var user in users) list.Add(await _dtoMapper.MapUserAsync<UserDto>(user, currentUser: currentUser));

        return Ok(new ResponseDto<IEnumerable<UserDto>>
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
}