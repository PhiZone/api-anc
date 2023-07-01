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
    private readonly IUserRelationRepository _userRelationRepository;

    public DtoMapper(IUserRelationRepository userRelationRepository, UserManager<User> userManager, IMapper mapper)
    {
        _userRelationRepository = userRelationRepository;
        _userManager = userManager;
        _mapper = mapper;
    }

    public async Task<T> MapUserAsync<T>(User user, T? dto = null, User? currentUser = null) where T : UserDto
    {
        dto ??= _mapper.Map<T>(user);
        dto.Roles = await _userManager.GetRolesAsync(user);
        dto.FollowerCount = await _userRelationRepository.CountFollowersAsync(user);
        dto.FolloweeCount = await _userRelationRepository.CountFolloweesAsync(user);
        if (user.Region != null) dto.Region = user.Region.Code;
        if (currentUser != null && await _userRelationRepository.RelationExistsAsync(currentUser.Id, user.Id))
            dto.DateFollowed = (await _userRelationRepository.GetRelationAsync(currentUser.Id, user.Id)).Time;

        return dto;
    }

    public async Task<T> MapFollowerAsync<T>(UserRelation userRelation, User? currentUser = null) where T : UserDto
    {
        var user = await _userManager.FindByIdAsync(userRelation.FollowerId.ToString());
        var dto = _mapper.Map<T>(user);
        dto.Roles = await _userManager.GetRolesAsync(user!);
        dto.FollowerCount = await _userRelationRepository.CountFollowersAsync(user!);
        dto.FolloweeCount = await _userRelationRepository.CountFolloweesAsync(user!);
        if (user!.Region != null) dto.Region = user.Region.Code;
        if (currentUser != null && await _userRelationRepository.RelationExistsAsync(currentUser.Id, user.Id))
            dto.DateFollowed = (await _userRelationRepository.GetRelationAsync(currentUser.Id, user.Id)).Time;
        return dto;
    }

    public async Task<T> MapFolloweeAsync<T>(UserRelation userRelation, User? currentUser = null) where T : UserDto
    {
        var user = await _userManager.FindByIdAsync(userRelation.FolloweeId.ToString());
        var dto = _mapper.Map<T>(user);
        dto.Roles = await _userManager.GetRolesAsync(user!);
        dto.FollowerCount = await _userRelationRepository.CountFollowersAsync(user!);
        dto.FolloweeCount = await _userRelationRepository.CountFolloweesAsync(user!);
        if (user!.Region != null) dto.Region = user.Region.Code;
        if (currentUser != null && await _userRelationRepository.RelationExistsAsync(currentUser.Id, user.Id))
            dto.DateFollowed = (await _userRelationRepository.GetRelationAsync(currentUser.Id, user.Id)).Time;
        return dto;
    }

    public async Task<T> MapUserRelationAsync<T>(UserRelation userRelation, User? currentUser = null)
        where T : UserRelationDto
    {
        var dto = _mapper.Map<T>(userRelation);
        var followee = await _userManager.FindByIdAsync(userRelation.FollowerId.ToString());
        dto.Followee = await MapUserAsync(followee!, dto.Followee, currentUser);
        var follower = await _userManager.FindByIdAsync(userRelation.FollowerId.ToString());
        dto.Follower = await MapUserAsync(follower!, dto.Follower, currentUser);
        return dto;
    }
}