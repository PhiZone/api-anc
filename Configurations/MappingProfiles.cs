using AutoMapper;
using PhiZoneApi.Dtos;
using PhiZoneApi.Models;

namespace PhiZoneApi.Configurations;

public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        CreateMap<User, UserDto>();
        CreateMap<UserRegistrationDto, User>();
    }
}