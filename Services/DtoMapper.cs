using AutoMapper;
using Microsoft.AspNetCore.Identity;
using PhiZoneApi.Dtos.Responses;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;

namespace PhiZoneApi.Services;

public class DtoMapper : IDtoMapper
{
    private readonly IMapper _mapper;
    private readonly IRegionRepository _regionRepository;
    private readonly UserManager<User> _userManager;
    private readonly IUserRelationRepository _userRelationRepository;

    public DtoMapper(IUserRelationRepository userRelationRepository, IRegionRepository regionRepository,
        UserManager<User> userManager, IMapper mapper)
    {
        _userRelationRepository = userRelationRepository;
        _regionRepository = regionRepository;
        _userManager = userManager;
        _mapper = mapper;
    }

    public async Task<T> MapUserAsync<T>(User user, User? currentUser = null) where T : UserDto
    {
        var dto = _mapper.Map<T>(user);
        dto.Roles = await _userManager.GetRolesAsync(user);
        dto.FollowerCount = await _userRelationRepository.CountFollowersAsync(user);
        dto.FolloweeCount = await _userRelationRepository.CountFolloweesAsync(user);
        if (user.RegionId != null) dto.Region = (await _regionRepository.GetRegionByIdAsync((int)user.RegionId)).Code;
        if (currentUser != null && await _userRelationRepository.RelationExistsAsync(currentUser.Id, user.Id))
            dto.DateFollowed = (await _userRelationRepository.GetRelationAsync(currentUser.Id, user.Id)).Time;
        return dto;
    }

    public async Task<T> MapFollowerAsync<T>(UserRelation userRelation, User? currentUser = null) where T : UserDto
    {
        var user = await _userManager.FindByIdAsync(userRelation.FollowerId.ToString());
        return await MapUserAsync<T>(user!, currentUser);
    }

    public async Task<T> MapFolloweeAsync<T>(UserRelation userRelation, User? currentUser = null) where T : UserDto
    {
        var user = await _userManager.FindByIdAsync(userRelation.FolloweeId.ToString());
        return await MapUserAsync<T>(user!, currentUser);
    }

    public async Task<T> MapUserRelationAsync<T>(UserRelation userRelation, User? currentUser = null)
        where T : UserRelationDto
    {
        var dto = _mapper.Map<T>(userRelation);
        dto.Follower = await MapFollowerAsync<UserDto>(userRelation, currentUser);
        dto.Followee = await MapFolloweeAsync<UserDto>(userRelation, currentUser);
        return dto;
    }
}