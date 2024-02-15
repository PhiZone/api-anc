using PhiZoneApi.Dtos.Requests;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;

namespace PhiZoneApi.Services;

public class VoteService(IVoteRepository voteRepository, IChartRepository chartRepository)
    : IVoteService
{
    private readonly List<int> _experienceList =
    [
        0,
        50,
        100,
        500,
        1000,
        3000,
        6000,
        10000,
        30000,
        60000,
        100000
    ];

    private readonly List<double> _multiplierList =
    [
        0.0,
        1.0,
        1.1,
        1.2,
        1.3,
        1.4,
        1.5,
        1.6,
        1.8,
        2.0,
        3.0
    ];

    public async Task<bool> CreateVoteAsync(VoteRequestDto dto, Chart chart, User user)
    {
        bool result;
        if (await voteRepository.VoteExistsAsync(chart.Id, user.Id))
        {
            var vote = await voteRepository.GetVoteAsync(chart.Id, user.Id);
            vote.Arrangement = dto.Arrangement;
            vote.Gameplay = dto.Gameplay;
            vote.VisualEffects = dto.VisualEffects;
            vote.Creativity = dto.Creativity;
            vote.Concord = dto.Concord;
            vote.Impression = dto.Impression;
            vote.Total = dto.Arrangement + dto.Gameplay + dto.VisualEffects + dto.Creativity + dto.Concord +
                         dto.Impression;
            vote.Multiplier = GetMultiplier(user);
            vote.DateCreated = DateTimeOffset.UtcNow;
            result = await voteRepository.UpdateVoteAsync(vote);
        }
        else
        {
            var vote = new Vote
            {
                ChartId = chart.Id,
                Arrangement = dto.Arrangement,
                Gameplay = dto.Gameplay,
                VisualEffects = dto.VisualEffects,
                Creativity = dto.Creativity,
                Concord = dto.Concord,
                Impression = dto.Impression,
                Total = dto.Arrangement + dto.Gameplay + dto.VisualEffects + dto.Creativity + dto.Concord +
                        dto.Impression,
                Multiplier = GetMultiplier(user),
                OwnerId = user.Id,
                DateCreated = DateTimeOffset.UtcNow
            };
            result = await voteRepository.CreateVoteAsync(vote);
        }

        return result && await UpdateChartAsync(chart);
    }

    public async Task<bool> RemoveVoteAsync(Chart chart, int userId)
    {
        if (!await voteRepository.VoteExistsAsync(chart.Id, userId)) return false;
        var vote = await voteRepository.GetVoteAsync(chart.Id, userId);
        var result = await voteRepository.RemoveVoteAsync(vote.Id);
        return result && await UpdateChartAsync(chart);
    }

    public async Task<bool> UpdateChartAsync(Chart chart)
    {
        var votes = await voteRepository.GetVotesAsync(["DateCreated"], [false], 0,
            -1, vote => vote.ChartId == chart.Id);
        var amount = votes.Sum(vote => vote.Multiplier);
        var r = GetReliability(amount);
        chart.Score = votes.Sum(vote => vote.Total * vote.Multiplier) / 6;
        chart.Rating = GetRating(chart.Score, amount, r);
        chart.RatingOnArrangement = GetRating(votes.Sum(vote => vote.Arrangement * vote.Multiplier), amount, r);
        chart.RatingOnGameplay = GetRating(votes.Sum(vote => vote.Gameplay * vote.Multiplier), amount, r);
        chart.RatingOnVisualEffects = GetRating(votes.Sum(vote => vote.VisualEffects * vote.Multiplier), amount, r);
        chart.RatingOnCreativity = GetRating(votes.Sum(vote => vote.Creativity * vote.Multiplier), amount, r);
        chart.RatingOnConcord = GetRating(votes.Sum(vote => vote.Concord * vote.Multiplier), amount, r);
        chart.RatingOnImpression = GetRating(votes.Sum(vote => vote.Impression * vote.Multiplier), amount, r);
        return await chartRepository.UpdateChartAsync(chart);
    }

    private double GetMultiplier(User user)
    {
        return _multiplierList[GetUserLevel(user)];
    }

    private int GetUserLevel(User user)
    {
        return _experienceList.FindLastIndex(exp => exp <= user.Experience);
    }

    private static double GetRating(double sum, double amount, double reliability, double defaultValue = 2.5)
    {
        if (sum == 0 || amount == 0) return defaultValue;
        return reliability * sum / amount + (1 - reliability) * defaultValue;
    }

    private static double GetReliability(double amount)
    {
        return 1 - Math.Pow(1.3, -amount);
    }
}