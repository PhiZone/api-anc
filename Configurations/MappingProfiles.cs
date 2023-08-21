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
        CreateMap<Song, SongDto>();
        CreateMap<Song, SongAdmitteeDto>();
        CreateMap<Song, SongUpdateDto>();
        CreateMap<Chart, ChartDto>();
        CreateMap<Chart, ChartDetailedDto>();
        CreateMap<Chart, ChartUpdateDto>();
        CreateMap<Record, RecordDto>();
        CreateMap<Like, LikeDto>();
        CreateMap<Comment, CommentDto>();
        CreateMap<Reply, ReplyDto>();
        CreateMap<Application, ApplicationDto>();
        CreateMap<Application, ApplicationUpdateDto>();
        CreateMap<Announcement, AnnouncementDto>();
        CreateMap<Announcement, AnnouncementRequestDto>();
        CreateMap<PlayConfiguration, PlayConfigurationResponseDto>();
        CreateMap<PlayConfigurationRequestDto, PlayConfiguration>();
        CreateMap<Vote, VoteDto>();
        CreateMap<VolunteerVote, VolunteerVoteDto>();
        CreateMap<SongSubmission, SongSubmissionDto>();
        CreateMap<ChartSubmission, ChartSubmissionDto>();
        CreateMap<Collaboration, CollaborationDto>();
        CreateMap<Collaboration, CollaborationUpdateDto>();
        CreateMap<Notification, NotificationDto>();
    }
}