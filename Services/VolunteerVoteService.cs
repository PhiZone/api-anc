using PhiZoneApi.Dtos.Requests;
using PhiZoneApi.Enums;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;

// ReSharper disable InvertIf

namespace PhiZoneApi.Services;

public class VolunteerVoteService(
    IVolunteerVoteRepository volunteerVoteRepository,
    IChartSubmissionRepository chartSubmissionRepository,
    ISubmissionService submissionService) : IVolunteerVoteService
{
    private readonly Dictionary<int, (double, double, double)> _voteScoreDictionary = new()
    {
        { 2, (-2.4, 1.5, 2.5) },
        { 3, (-1.2, 0.9, 2.1) },
        { 4, (-0.5, 0.45, 1.8) },
        { 5, (-0.2, 0.15, 1.6) },
        { 6, (0, 0, 1.5) }
    };

    public async Task<bool> CreateVolunteerVoteAsync(VolunteerVoteRequestDto dto, ChartSubmission chartSubmission,
        User user)
    {
        bool result;
        if (await volunteerVoteRepository.VolunteerVoteExistsAsync(chartSubmission.Id, user.Id))
        {
            var volunteerVote =
                await volunteerVoteRepository.GetVolunteerVoteAsync(chartSubmission.Id, user.Id);
            volunteerVote.Score = dto.Score;
            volunteerVote.SuggestedDifficulty = dto.SuggestedDifficulty;
            volunteerVote.Message = dto.Message;
            volunteerVote.DateCreated = DateTimeOffset.UtcNow;
            result = await volunteerVoteRepository.UpdateVolunteerVoteAsync(volunteerVote);
        }
        else
        {
            var volunteerVote = new VolunteerVote
            {
                ChartId = chartSubmission.Id,
                Score = dto.Score,
                SuggestedDifficulty = dto.SuggestedDifficulty,
                Message = dto.Message,
                OwnerId = user.Id,
                DateCreated = DateTimeOffset.UtcNow
            };
            result = await volunteerVoteRepository.CreateVolunteerVoteAsync(volunteerVote);
        }

        return result && await UpdateChartSubmissionAsync(chartSubmission);
    }

    public async Task<bool> RemoveVolunteerVoteAsync(ChartSubmission chartSubmission, int userId)
    {
        if (!await volunteerVoteRepository.VolunteerVoteExistsAsync(chartSubmission.Id, userId)) return false;
        var volunteerVote = await volunteerVoteRepository.GetVolunteerVoteAsync(chartSubmission.Id, userId);
        var result = await volunteerVoteRepository.RemoveVolunteerVoteAsync(volunteerVote.Id);
        return result && await UpdateChartSubmissionAsync(chartSubmission);
    }

    private async Task<bool> UpdateChartSubmissionAsync(ChartSubmission chartSubmission)
    {
        var votes = await volunteerVoteRepository.GetVolunteerVotesAsync(predicate: vote =>
            vote.ChartId == chartSubmission.Id && vote.DateCreated > chartSubmission.DateFileUpdated);
        var score = votes.Average(vote => vote.Score);
        var suggestedDifficulty = votes.Average(vote => vote.SuggestedDifficulty);
        if (_voteScoreDictionary.TryGetValue(votes.Count, out var scoreRange))
        {
            if (score < scoreRange.Item1 || Math.Abs(chartSubmission.Difficulty - suggestedDifficulty) >= 0.4)
            {
                chartSubmission.VolunteerStatus = RequestStatus.Rejected;
                chartSubmission.Status = RequestStatus.Rejected;
                await submissionService.RejectChart(chartSubmission);
            }
            else if (score >= scoreRange.Item2 && (!chartSubmission.IsRanked || score >= scoreRange.Item3 ||
                                                   votes.Count == _voteScoreDictionary.Last().Key))
            {
                chartSubmission.IsRanked = chartSubmission.IsRanked && score >= scoreRange.Item3;
                chartSubmission.Difficulty = Math.Round(suggestedDifficulty * 10, MidpointRounding.AwayFromZero) / 10;
                chartSubmission.VolunteerStatus = RequestStatus.Approved;
                if (chartSubmission.AdmissionStatus == RequestStatus.Approved)
                {
                    chartSubmission.Status = RequestStatus.Approved;
                    await submissionService.ApproveChart(chartSubmission);
                }
            }
        }

        return await chartSubmissionRepository.UpdateChartSubmissionAsync(chartSubmission);
    }
}