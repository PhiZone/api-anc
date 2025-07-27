using PhiZoneApi.Dtos.Requests;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IVoteService
{
    Task<bool> CreateVoteAsync(VoteRequestDto dto, Chart chart, User user);

    Task<bool> RemoveVoteAsync(Chart chart, int userId);

    Task<bool> UpdateChartAsync(Chart chart);

    double GetRating(double sum, double amount, double reliability, double defaultValue = 2.5);

    double GetReliability(double amount);
}