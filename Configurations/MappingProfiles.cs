using AutoMapper;
using PhiZoneApi.Dtos.Requests;
using PhiZoneApi.Dtos.Responses;
using PhiZoneApi.Models;

namespace PhiZoneApi.Configurations;

public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        CreateMap<User, UserDto>().ForMember(x => x.Role, opt => opt.Ignore());
        CreateMap<User, AuthorDto>().ForMember(x => x.Role, opt => opt.Ignore());
        CreateMap<User, UserDetailedDto>();
        CreateMap<User, UserUpdateDto>();
        CreateMap<UserRegistrationDto, User>();
        CreateMap<UserRelation, UserRelationDto>();
        CreateMap<Region, RegionDto>();
        CreateMap<Chapter, ChapterDto>();
        CreateMap<Chapter, ChapterAdmitterDto>();
        CreateMap<Chapter, ChapterUpdateDto>();
        CreateMap<Collection, CollectionDto>();
        CreateMap<Collection, CollectionAdmitterDto>();
        CreateMap<Collection, CollectionUpdateDto>();
        CreateMap<Song, SongDto>();
        CreateMap<Song, SongAdmitteeDto>();
        CreateMap<Song, SongUpdateDto>().ForMember(x => x.Tags, opt => opt.Ignore());
        CreateMap<Chart, ChartDto>();
        CreateMap<Chart, ChartDetailedDto>();
        CreateMap<Chart, ChartAdmitteeDto>();
        CreateMap<Chart, ChartUpdateDto>().ForMember(x => x.Tags, opt => opt.Ignore());
        CreateMap<ChartAsset, ChartAssetDto>();
        CreateMap<ChartAsset, ChartAssetUpdateDto>();
        CreateMap<ChartAssetSubmission, ChartAssetSubmissionDto>();
        CreateMap<ChartAssetSubmission, ChartAssetUpdateDto>();
        CreateMap<Record, RecordDto>();
        CreateMap<Tag, TagDto>();
        CreateMap<TagRequestDto, Tag>();
        CreateMap<Like, LikeDto>();
        CreateMap<Comment, CommentDto>();
        CreateMap<Reply, ReplyDto>();
        CreateMap<Application, ApplicationDto>();
        CreateMap<Application, ApplicationUpdateDto>();
        CreateMap<ApplicationService, ApplicationServiceDto>();
        CreateMap<ApplicationService, ApplicationServiceRequestDto>();
        CreateMap<ApplicationServiceRecord, ApplicationServiceRecordDto>();
        CreateMap<ApplicationUser, ApplicationUserDto>();
        CreateMap<Announcement, AnnouncementDto>();
        CreateMap<Announcement, AnnouncementRequestDto>();
        CreateMap<PlayConfiguration, PlayConfigurationResponseDto>();
        CreateMap<PlayConfigurationRequestDto, PlayConfiguration>();
        CreateMap<PlayConfiguration, PlayConfigurationRequestDto>();
        CreateMap<Vote, VoteDto>();
        CreateMap<VolunteerVote, VolunteerVoteDto>();
        CreateMap<SongSubmission, SongSubmissionDto>();
        CreateMap<ChartSubmission, ChartSubmissionDto>();
        CreateMap<SongSubmission, SongSubmissionUpdateDto>();
        CreateMap<ChartSubmission, ChartSubmissionUpdateDto>();
        CreateMap<Collaboration, CollaborationDto>();
        CreateMap<Collaboration, CollaborationUpdateDto>();
        CreateMap<Notification, NotificationDto>();
        CreateMap<ResourceRecord, ResourceRecordDto>();
        CreateMap<PetAnswer, PetAnswerDto>()
            .ForMember(x => x.Question1, opt => opt.Ignore())
            .ForMember(x => x.Question2, opt => opt.Ignore())
            .ForMember(x => x.Question3, opt => opt.Ignore());
    }
}