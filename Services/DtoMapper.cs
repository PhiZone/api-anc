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
    private readonly ILikeRepository _likeRepository;
    private readonly UserManager<User> _userManager;
    private readonly IUserRelationRepository _userRelationRepository;

    public DtoMapper(IUserRelationRepository userRelationRepository, IRegionRepository regionRepository,
        ILikeRepository likeRepository, UserManager<User> userManager, IMapper mapper)
    {
        _userRelationRepository = userRelationRepository;
        _regionRepository = regionRepository;
        _likeRepository = likeRepository;
        _userManager = userManager;
        _mapper = mapper;
    }

    public async Task<T> MapUserAsync<T>(User user, User? currentUser = null) where T : UserDto
    {
        var dto = _mapper.Map<T>(user);
        dto.Roles = await _userManager.GetRolesAsync(user);
        dto.FollowerCount = await _userRelationRepository.CountFollowersAsync(user);
        dto.FolloweeCount = await _userRelationRepository.CountFolloweesAsync(user);
        if (user.RegionId != null) dto.Region = (await _regionRepository.GetRegionAsync((int)user.RegionId)).Code;
        if (currentUser != null && await _userRelationRepository.RelationExistsAsync(currentUser.Id, user.Id))
            dto.DateFollowed = (await _userRelationRepository.GetRelationAsync(currentUser.Id, user.Id)).DateCreated;
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

    public async Task<T> MapChapterAsync<T>(Chapter chapter, User? currentUser = null) where T : ChapterDto
    {
        var dto = _mapper.Map<T>(chapter);
        dto.DateLiked = currentUser != null && await _likeRepository.LikeExistsAsync(chapter.Id, currentUser.Id)
            ? (await _likeRepository.GetLikeAsync(chapter.Id, currentUser.Id)).DateCreated
            : null;
        return dto;
    }

    public async Task<T> MapSongAsync<T>(Song song, User? currentUser = null) where T : SongDto
    {
        var dto = _mapper.Map<T>(song);
        dto.DateLiked = currentUser != null && await _likeRepository.LikeExistsAsync(song.Id, currentUser.Id)
            ? (await _likeRepository.GetLikeAsync(song.Id, currentUser.Id)).DateCreated
            : null;
        return dto;
    }

    public async Task<T> MapChartAsync<T>(Chart chart, User? currentUser = null) where T : ChartDto
    {
        var dto = _mapper.Map<T>(chart);
        dto.DateLiked = currentUser != null && await _likeRepository.LikeExistsAsync(chart.Id, currentUser.Id)
            ? (await _likeRepository.GetLikeAsync(chart.Id, currentUser.Id)).DateCreated
            : null;
        return dto;
    }
}