using PhiZoneApi.Dtos.Requests;
using PhiZoneApi.Enums;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;

// ReSharper disable InvertIf

namespace PhiZoneApi.Services;

public class VolunteerVoteService : IVolunteerVoteService
{
    private readonly IChartSubmissionRepository _chartSubmissionRepository;
    private readonly ISubmissionService _submissionService;
    private readonly IVolunteerVoteRepository _volunteerVolunteerVoteRepository;
    private readonly Dictionary<int, (double, double)> _voteScoreDictionary;

    public VolunteerVoteService(IVolunteerVoteRepository volunteerVolunteerVoteRepository,
        IChartSubmissionRepository chartSubmissionRepository, ISubmissionService submissionService)
    {
        _volunteerVolunteerVoteRepository = volunteerVolunteerVoteRepository;
        _chartSubmissionRepository = chartSubmissionRepository;
        _submissionService = submissionService;
        _voteScoreDictionary = new Dictionary<int, (double, double)>
        {
            { 2, (-2, 2.5) },
            { 3, (-1.6, 1.6) },
            { 4, (-1.5, 1) },
            { 5, (-0.8, 0.5) },
            { 6, (0, 0) }
        };
    }

    public async Task<bool> CreateVolunteerVoteAsync(VolunteerVoteRequestDto dto, ChartSubmission chartSubmission,
        User user)
    {
        if (await _volunteerVolunteerVoteRepository.VolunteerVoteExistsAsync(chartSubmission.Id, user.Id)) return false;
        var volunteerVolunteerVote = new VolunteerVote
        {
            ChartId = chartSubmission.Id,
            Score = dto.Score,
            Message = dto.Message,
            OwnerId = user.Id,
            DateCreated = DateTimeOffset.UtcNow
        };
        var result = await _volunteerVolunteerVoteRepository.CreateVolunteerVoteAsync(volunteerVolunteerVote);
        return result && await UpdateChartSubmissionAsync(chartSubmission);
    }

    public async Task<bool> RemoveVolunteerVoteAsync(ChartSubmission chartSubmission, int userId)
    {
        if (!await _volunteerVolunteerVoteRepository.VolunteerVoteExistsAsync(chartSubmission.Id, userId)) return false;
        var volunteerVolunteerVote =
            await _volunteerVolunteerVoteRepository.GetVolunteerVoteAsync(chartSubmission.Id, userId);
        var result = await _volunteerVolunteerVoteRepository.RemoveVolunteerVoteAsync(volunteerVolunteerVote.Id);
        return result && await UpdateChartSubmissionAsync(chartSubmission);
    }

    private async Task<bool> UpdateChartSubmissionAsync(ChartSubmission chartSubmission)
    {
        var votes = await _volunteerVolunteerVoteRepository.GetVolunteerVotesAsync("DateCreated", false, 0, -1,
            vote => vote.ChartId == chartSubmission.Id && vote.DateCreated > chartSubmission.DateUpdated);
        if (_voteScoreDictionary.TryGetValue(votes.Count, out var scoreRange))
        {
            var score = votes.Average(vote => vote.Score);
            if (score < scoreRange.Item1)
            {
                chartSubmission.VolunteerStatus = RequestStatus.Rejected;
                chartSubmission.Status = RequestStatus.Rejected;
                await _submissionService.RejectChart(chartSubmission);
            }
            else if (score >= scoreRange.Item2)
            {
                chartSubmission.VolunteerStatus = RequestStatus.Approved;
                if (chartSubmission.AdmissionStatus == RequestStatus.Approved)
                {
                    chartSubmission.Status = RequestStatus.Approved;
                    await _submissionService.ApproveChart(chartSubmission);
                }
            }
        }

        return await _chartSubmissionRepository.UpdateChartSubmissionAsync(chartSubmission);
    }
}