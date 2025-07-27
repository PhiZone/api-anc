namespace PhiZoneApi.Enums;

public enum EventTaskType
{
    Scheduled, // EventDivision

    PreRegistration, // EventTeam
    PreInvitation, // EventTeam
    PreParticipation, // Participation
    PreSubmission, // SongSubmission / ChartSubmission / Record
    PreUpdateTeam, // EventTeam
    PreUpdateParticipant, // Participation
    PreUpdateSubmission, // SongSubmission / ChartSubmission

    PostRegistration, // EventTeam
    PostInvitation, // EventTeamInviteDelivererDto
    PostParticipation, // Participation
    PostSubmission, // SongSubmission / ChartSubmission / Record
    PostUpdateTeam, // EventTeam
    PostUpdateParticipant, // Participation
    PostUpdateSubmission, // SongSubmission / ChartSubmission

    OnApproval, // EventResource
    OnDeletion, // SongSubmission / ChartSubmission
    OnDisbandment, // EventTeam
    OnWithdrawal, // Participation
    OnEntryEvaluation, // ValueTuple<EventResource, int>
    OnTeamEvaluation // ValueTuple<EventTeam, int>
}