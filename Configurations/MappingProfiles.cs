using AutoMapper;
using PhiZoneApi.Dtos.Requests;
using PhiZoneApi.Dtos.Responses;
using PhiZoneApi.Models;

namespace PhiZoneApi.Configurations;

public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        CreateMap<User, UserDto>();
        CreateMap<User, UserDetailedDto>();
        CreateMap<User, UserUpdateDto>();
        CreateMap<UserRegistrationDto, User>();
        CreateMap<UserRelation, UserRelationDto>();
        CreateMap<Region, RegionDto>();
        CreateMap<Chapter, ChapterDto>();
        CreateMap<Chapter, ChapterUpdateDto>();
        CreateMap<ChapterUpdateDto, Chapter>();
        CreateMap<Song, SongDto>();
        CreateMap<Chart, ChartDto>();
        CreateMap<Record, RecordDto>();
    }
}