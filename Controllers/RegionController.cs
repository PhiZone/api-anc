using AutoMapper;
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

[Route("regions")]
[ApiVersion("2.0")]
[ApiController]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
    Policy = "AllowAnonymous")]
public class RegionController : Controller
{
    private readonly IOptions<DataSettings> _dataSettings;
    private readonly IDtoMapper _dtoMapper;
    private readonly IFilterService _filterService;
    private readonly IMapper _mapper;
    private readonly IRegionRepository _regionRepository;
    private readonly UserManager<User> _userManager;

    public RegionController(IRegionRepository regionRepository, IOptions<DataSettings> dataSettings,
        UserManager<User> userManager, IFilterService filterService, IMapper mapper, IDtoMapper dtoMapper)
    {
        _regionRepository = regionRepository;
        _dataSettings = dataSettings;
        _userManager = userManager;
        _filterService = filterService;
        _mapper = mapper;
        _dtoMapper = dtoMapper;
    }

    /// <summary>
    ///     Retrieves regions.
    /// </summary>
    /// <returns>An array of regions.</returns>
    /// <response code="200">Returns an array of regions.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<RegionDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetRegions([FromQuery] ArrayRequestDto dto,
        [FromQuery] RegionFilterDto? filterDto = null)
    {
        var currentUser = await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        dto.PerPage = dto.PerPage > 0
            ? dto.PerPage <= _dataSettings.Value.PaginationMaxPerPage
                ? dto.PerPage
                : _dataSettings.Value.PaginationMaxPerPage
            : _dataSettings.Value.PaginationPerPage;
        var position = dto.PerPage * (dto.Page - 1);
        var predicateExpr = await _filterService.Parse(filterDto, dto.Predicate, currentUser);
        var list = _mapper.Map<List<RegionDto>>(await _regionRepository.GetRegionsAsync(dto.Order, dto.Desc, position,
            dto.PerPage, dto.Search, predicateExpr));
        var total = await _regionRepository.CountRegionsAsync(dto.Search, predicateExpr);

        return Ok(new ResponseDto<IEnumerable<RegionDto>>
        {
            Status = ResponseStatus.Ok,
            Code = ResponseCodes.Ok,
            Total = total,
            PerPage = dto.PerPage,
            HasPrevious = position > 0,
            HasNext = position < total - total % dto.PerPage,
            Data = list
        });
    }

    /// <summary>
    ///     Retrieves a specific region.
    /// </summary>
    /// <param name="code">A region's code</param>
    /// <returns>A region.</returns>
    /// <response code="200">Returns a region.</response>
    /// <response code="304">
    ///     When the resource has not been updated since last retrieval. Requires <c>If-None-Match</c>.
    /// </response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified region is not found.</response>
    [HttpGet("{code}")]
    [ServiceFilter(typeof(ETagFilter))]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<RegionDto>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status304NotModified, "text/plain")]
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
    /// <param name="id">Region's ID.</param>
    /// <returns>A region.</returns>
    /// <response code="200">Returns a region.</response>
    /// <response code="304">
    ///     When the resource has not been updated since last retrieval. Requires <c>If-None-Match</c>.
    /// </response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified region is not found.</response>
    [HttpGet("{id:int}")]
    [ServiceFilter(typeof(ETagFilter))]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<RegionDto>))]
    [ProducesResponseType(typeof(void), StatusCodes.Status304NotModified, "text/plain")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetRegion([FromRoute] int id)
    {
        if (!await _regionRepository.RegionExistsAsync(id)) return NotFound();
        var region = await _regionRepository.GetRegionAsync(id);
        var dto = _mapper.Map<RegionDto>(region);

        return Ok(new ResponseDto<RegionDto> { Status = ResponseStatus.Ok, Code = ResponseCodes.Ok, Data = dto });
    }

    /// <summary>
    ///     Retrieves users from a specific region.
    /// </summary>
    /// <param name="code">A region's code</param>
    /// <response code="200">Returns an array of users.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified region is not found.</response>
    [HttpGet("{code}/users")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<UserDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetRegionUsers([FromRoute] string code, [FromQuery] ArrayRequestDto dto,
        [FromQuery] UserFilterDto? filterDto = null)
    {
        var currentUser = await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        dto.PerPage = dto.PerPage > 0
            ? dto.PerPage <= _dataSettings.Value.PaginationMaxPerPage
                ? dto.PerPage
                : _dataSettings.Value.PaginationMaxPerPage
            : _dataSettings.Value.PaginationPerPage;
        var position = dto.PerPage * (dto.Page - 1);
        var predicateExpr = await _filterService.Parse(filterDto, dto.Predicate, currentUser);
        if (!await _regionRepository.RegionExistsAsync(code)) return NotFound();
        var users = await _regionRepository.GetRegionUsersAsync(code, dto.Order, dto.Desc, position, dto.PerPage,
            dto.Search, predicateExpr);
        var list = new List<UserDto>();
        var total = await _regionRepository.CountRegionUsersAsync(code, dto.Search, predicateExpr);

        foreach (var user in users) list.Add(await _dtoMapper.MapUserAsync<UserDto>(user, currentUser));

        return Ok(new ResponseDto<IEnumerable<UserDto>>
        {
            Status = ResponseStatus.Ok,
            Code = ResponseCodes.Ok,
            Total = total,
            PerPage = dto.PerPage,
            HasPrevious = position > 0,
            HasNext = position < total - total % dto.PerPage,
            Data = list
        });
    }

    /// <summary>
    ///     Retrieves users from a specific region by its ID.
    /// </summary>
    /// <param name="id">Region's ID.</param>
    /// <returns>An array of users.</returns>
    /// <response code="200">Returns an array of users.</response>
    /// <response code="400">When any of the parameters is invalid.</response>
    /// <response code="404">When the specified region is not found.</response>
    [HttpGet("{id:int}/users")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto<IEnumerable<UserDto>>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseDto<object>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseDto<object>))]
    public async Task<IActionResult> GetRegionUsers([FromRoute] int id, [FromQuery] ArrayRequestDto dto,
        [FromQuery] UserFilterDto? filterDto = null)
    {
        var currentUser = await _userManager.FindByIdAsync(User.GetClaim(OpenIddictConstants.Claims.Subject)!);
        dto.PerPage = dto.PerPage > 0
            ? dto.PerPage <= _dataSettings.Value.PaginationMaxPerPage
                ? dto.PerPage
                : _dataSettings.Value.PaginationMaxPerPage
            : _dataSettings.Value.PaginationPerPage;
        var position = dto.PerPage * (dto.Page - 1);
        var predicateExpr = await _filterService.Parse(filterDto, dto.Predicate, currentUser);
        if (!await _regionRepository.RegionExistsAsync(id)) return NotFound();
        var users = await _regionRepository.GetRegionUsersAsync(id, dto.Order, dto.Desc, position, dto.PerPage,
            dto.Search, predicateExpr);
        var list = new List<UserDto>();
        var total = await _regionRepository.CountRegionUsersAsync(id, dto.Search, predicateExpr);

        foreach (var user in users) list.Add(await _dtoMapper.MapUserAsync<UserDto>(user, currentUser));

        return Ok(new ResponseDto<IEnumerable<UserDto>>
        {
            Status = ResponseStatus.Ok,
            Code = ResponseCodes.Ok,
            Total = total,
            PerPage = dto.PerPage,
            HasPrevious = position > 0,
            HasNext = position < total - total % dto.PerPage,
            Data = list
        });
    }
}