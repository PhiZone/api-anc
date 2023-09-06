using Microsoft.AspNetCore.Identity;
using PhiZoneApi.Enums;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;

namespace PhiZoneApi.Services;

public class UserService : IUserService
{
    private readonly IPlayConfigurationRepository _playConfigurationRepository;
    private readonly ITemplateService _templateService;
    private readonly UserManager<User> _userManager;
    private readonly IUserRelationRepository _userRelationRepository;

    public UserService(UserManager<User> userManager, ITemplateService templateService,
        IPlayConfigurationRepository playConfigurationRepository, IUserRelationRepository userRelationRepository)
    {
        _userManager = userManager;
        _templateService = templateService;
        _playConfigurationRepository = playConfigurationRepository;
        _userRelationRepository = userRelationRepository;
    }

    public async Task CreateUser(User user)
    {
        await _userManager.CreateAsync(user);
        var configuration = new PlayConfiguration
        {
            Name = _templateService.GetMessage("default", user.Language),
            PerfectJudgment = 80,
            GoodJudgment = 160,
            AspectRatio = null,
            NoteSize = 1,
            ChartMirroring = ChartMirroringMode.Off,
            BackgroundLuminance = 0.5,
            BackgroundBlur = 1,
            SimultaneousNoteHint = true,
            FcApIndicator = true,
            ChartOffset = 0,
            HitSoundVolume = 0,
            MusicVolume = 0,
            OwnerId = user.Id,
            DateCreated = DateTimeOffset.UtcNow
        };
        await _playConfigurationRepository.CreatePlayConfigurationAsync(configuration);
    }

    public async Task<bool> IsBlacklisted(int user1, int user2)
    {
        if (await _userRelationRepository.RelationExistsAsync(user1, user2))
            if ((await _userRelationRepository.GetRelationAsync(user1, user2)).Type == UserRelationType.Blacklisted)
                return true;
        if (!await _userRelationRepository.RelationExistsAsync(user2, user1)) return false;
        return (await _userRelationRepository.GetRelationAsync(user2, user1)).Type == UserRelationType.Blacklisted;
    }
}