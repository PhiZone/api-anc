using PhiZoneApi.Dtos.Requests;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IVolunteerVoteService
{
    Task<bool> CreateVolunteerVoteAsync(VolunteerVoteRequestDto dto, ChartSubmission chartSubmission, User user);

    Task<bool> RemoveVolunteerVoteAsync(ChartSubmission chartSubmission, int userId);
}