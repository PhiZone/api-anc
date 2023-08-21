using Microsoft.AspNetCore.Identity;
using PhiZoneApi.Enums;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;

namespace PhiZoneApi.Services;

public class UserService : IUserService
{
    private readonly IServiceProvider _provider;
    private readonly ITemplateService _templateService;

    public UserService(IServiceProvider provider, ITemplateService templateService)
    {
        _provider = provider;
        _templateService = templateService;
    }

    public async Task CreateUser(User user)
    {
        using var scope = _provider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var playConfigurationRepository = scope.ServiceProvider.GetRequiredService<IPlayConfigurationRepository>();
        await userManager.CreateAsync(user);
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
        await playConfigurationRepository.CreatePlayConfigurationAsync(configuration);
    }

    public async Task<bool> IsBlacklisted(int user1, int user2)
    {
        using var scope = _provider.CreateScope();
        var userRelationRepository = scope.ServiceProvider.GetRequiredService<IUserRelationRepository>();
        if (await userRelationRepository.RelationExistsAsync(user1, user2))
            if ((await userRelationRepository.GetRelationAsync(user1, user2)).Type == UserRelationType.Blacklisted)
                return true;
        if (!await userRelationRepository.RelationExistsAsync(user2, user1)) return false;
        return (await userRelationRepository.GetRelationAsync(user2, user1)).Type == UserRelationType.Blacklisted;
    }
}