using AutoMapper;
using PhiZoneApi.Dtos;
using PhiZoneApi.Models;

namespace PhiZoneApi.Helpers
{
    public class MappingProfiles : Profile
    {
        public MappingProfiles()
        {
            CreateMap<User, UserDto>();
            CreateMap<UserRegistrationDto, User>();
        }
    }
}
