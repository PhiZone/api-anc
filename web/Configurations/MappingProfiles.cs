using AutoMapper;
using PhiZoneApi.Dtos.Deliverers;
using PhiZoneApi.Dtos.Requests;
using PhiZoneApi.Dtos.Responses;
using PhiZoneApi.Models;

namespace PhiZoneApi.Configurations;

public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        CreateMap<User, UserDto>();
        CreateMap<User, PositionalUserDto>();
        CreateMap<User, UserDetailedDto>();
        CreateMap<User, UserUpdateDto>();
        CreateMap<UserDetailedDto, UserDto>();
        CreateMap<UserRegistrationDto, User>();
        CreateMap<UserRelation, UserRelationDto>();
        CreateMap<TapGhost, UserDto>();
        CreateMap<TapGhost, UserDetailedDto>();
        CreateMap<Region, RegionDto>();
        CreateMap<Chapter, ChapterDto>();
        CreateMap<Chapter, ChapterAdmitterDto>();
        CreateMap<Chapter, ChapterUpdateDto>();
        CreateMap<Collection, CollectionDto>();
        CreateMap<Collection, CollectionAdmitterDto>();
        CreateMap<Collection, CollectionUpdateDto>();
        CreateMap<Song, SongDto>();
        CreateMap<Song, EventSongPromptDto>();
        CreateMap<Song, EventSongEntryDto>();
        CreateMap<Song, SongAdmitteeDto>();
        CreateMap<Song, SongUpdateDto>().ForMember(x => x.Tags, opt => opt.Ignore());
        CreateMap<Song, SongMatchDto>();
        CreateMap<Chart, ChartDto>();
        CreateMap<Chart, EventChartPromptDto>();
        CreateMap<Chart, EventChartEntryDto>();
        CreateMap<Chart, ChartDetailedDto>();
        CreateMap<Chart, ChartAdmitteeDto>();
        CreateMap<Chart, ChartUpdateDto>().ForMember(x => x.Tags, opt => opt.Ignore());
        CreateMap<ChartAsset, ChartAssetDto>();
        CreateMap<ChartAsset, ChartAssetUpdateDto>();
        CreateMap<ChartAssetSubmission, ChartAssetSubmissionDto>();
        CreateMap<ChartAssetSubmission, ChartAssetUpdateDto>();
        CreateMap<Record, RecordDto>();
        CreateMap<Record, EventRecordEntryDto>();
        CreateMap<Tag, TagDto>();
        CreateMap<Tag, EventTagDto>();
        CreateMap<TagRequestDto, Tag>().ReverseMap();
        CreateMap<Like, LikeDto>();
        CreateMap<Comment, CommentDto>();
        CreateMap<Reply, ReplyDto>();
        CreateMap<Application, ApplicationDto>();
        CreateMap<Application, ApplicationUpdateDto>();
        CreateMap<ServiceScript, ServiceScriptDto>();
        CreateMap<ServiceScript, ServiceScriptRequestDto>();
        CreateMap<ServiceRecord, ServiceRecordDto>();
        CreateMap<ApplicationUser, ApplicationUserDto>();
        CreateMap<Announcement, AnnouncementDto>();
        CreateMap<Announcement, AnnouncementRequestDto>();
        CreateMap<Event, EventDto>().ForMember(x => x.Divisions, opt => opt.Ignore());
        CreateMap<Event, EventUpdateDto>();
        CreateMap<EventDivision, EventDivisionDto>();
        CreateMap<EventDivision, EventDivisionUpdateDto>();
        CreateMap<EventTeam, EventTeamDto>();
        CreateMap<EventTeam, EventTeamUpdateDto>();
        CreateMap<EventTask, EventTaskDto>();
        CreateMap<EventTask, EventTaskRequestDto>();
        CreateMap<Hostship, HostshipDto>();
        CreateMap<Hostship, HostshipRequestDto>();
        CreateMap<EventResource, EventResourceDto>();
        CreateMap<EventResource, EventResourceRequestDto>();
        CreateMap<Participation, ParticipationUpdateDto>();
        CreateMap<PlayConfiguration, PlayConfigurationResponseDto>();
        CreateMap<PlayConfigurationRequestDto, PlayConfiguration>();
        CreateMap<PlayConfiguration, PlayConfigurationRequestDto>();
        CreateMap<Vote, VoteDto>();
        CreateMap<VolunteerVote, VolunteerVoteDto>();
        CreateMap<SongSubmission, SongSubmissionDto>();
        CreateMap<SongSubmission, SongSubmissionMatchDto>();
        CreateMap<ChartSubmission, ChartSubmissionDto>();
        CreateMap<SongSubmission, SongSubmissionUpdateDto>();
        CreateMap<ChartSubmission, ChartSubmissionUpdateDto>();
        CreateMap<Collaboration, CollaborationDto>();
        CreateMap<Collaboration, CollaborationUpdateDto>();
        CreateMap<Notification, NotificationDto>();
        CreateMap<ResourceRecord, ResourceRecordDto>();
        CreateMap<ResourceRecord, ResourceRecordMatchDto>();
        CreateMap<PetAnswer, PetAnswerDto>()
            .ForMember(x => x.Question1, opt => opt.Ignore())
            .ForMember(x => x.Question2, opt => opt.Ignore())
            .ForMember(x => x.Question3, opt => opt.Ignore());
    }
}