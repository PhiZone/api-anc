using AutoMapper;
using Microsoft.AspNetCore.Identity;
using PhiZoneApi.Dtos;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;

namespace PhiZoneApi.Services;

public class DtoMapper : IDtoMapper
{
    private readonly IMapper _mapper;
    private readonly UserManager<User> _userManager;

    private readonly IUserRepository _userRepository;

    public DtoMapper(IUserRepository userRepository, UserManager<User> userManager, IMapper mapper)
    {
        _userRepository = userRepository;
        _userManager = userManager;
        _mapper = mapper;
    }

    public async Task<T> MapUserAsync<T>(User user) where T : UserDto
    {
        var dto = _mapper.Map<T>(user);
        dto.Roles = await _userManager.GetRolesAsync(user);
        dto.FollowerCount = await _userRepository.CountFollowersAsync(user);
        dto.FolloweeCount = await _userRepository.CountFolloweesAsync(user);
        if (user.Region != null) dto.Region = user.Region.Code;
        return dto;
    }
}