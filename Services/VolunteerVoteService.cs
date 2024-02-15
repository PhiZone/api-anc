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
        { 2, (-2, 1.5, 2.5) },
        { 3, (-1.6, 1.3, 2.3) },
        { 4, (-1.5, 1, 1.75) },
        { 5, (-0.8, 0.5, 1.2) },
        { 6, (0, 0, 1.0) }
    };

    public async Task<bool> CreateVolunteerVoteAsync(VolunteerVoteRequestDto dto, ChartSubmission chartSubmission,
        User user)
    {
        bool result;
        if (await volunteerVoteRepository.VolunteerVoteExistsAsync(chartSubmission.Id, user.Id))
        {
            var volunteerVolunteerVote =
                await volunteerVoteRepository.GetVolunteerVoteAsync(chartSubmission.Id, user.Id);
            volunteerVolunteerVote.Score = dto.Score;
            volunteerVolunteerVote.SuggestedDifficulty = dto.SuggestedDifficulty;
            volunteerVolunteerVote.Message = dto.Message;
            volunteerVolunteerVote.DateCreated = DateTimeOffset.UtcNow;
            result = await volunteerVoteRepository.UpdateVolunteerVoteAsync(volunteerVolunteerVote);
        }
        else
        {
            var volunteerVolunteerVote = new VolunteerVote
            {
                ChartId = chartSubmission.Id,
                Score = dto.Score,
                SuggestedDifficulty = dto.SuggestedDifficulty,
                Message = dto.Message,
                OwnerId = user.Id,
                DateCreated = DateTimeOffset.UtcNow
            };
            result = await volunteerVoteRepository.CreateVolunteerVoteAsync(volunteerVolunteerVote);
        }

        return result && await UpdateChartSubmissionAsync(chartSubmission);
    }

    public async Task<bool> RemoveVolunteerVoteAsync(ChartSubmission chartSubmission, int userId)
    {
        if (!await volunteerVoteRepository.VolunteerVoteExistsAsync(chartSubmission.Id, userId)) return false;
        var volunteerVolunteerVote = await volunteerVoteRepository.GetVolunteerVoteAsync(chartSubmission.Id, userId);
        var result = await volunteerVoteRepository.RemoveVolunteerVoteAsync(volunteerVolunteerVote.Id);
        return result && await UpdateChartSubmissionAsync(chartSubmission);
    }

    private async Task<bool> UpdateChartSubmissionAsync(ChartSubmission chartSubmission)
    {
        var votes = await volunteerVoteRepository.GetVolunteerVotesAsync(["DateCreated"],
            [false], 0, -1,
            vote => vote.ChartId == chartSubmission.Id && vote.DateCreated > chartSubmission.DateUpdated);
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
            else if (score >= scoreRange.Item2 &&
                     (!chartSubmission.IsRanked || votes.Count == _voteScoreDictionary.Last().Key))
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