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

    public UserService(ITemplateService templateService, IServiceProvider serviceProvider)
    {
        _templateService = templateService;
        using var scope = serviceProvider.CreateScope();
        _userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        _playConfigurationRepository = scope.ServiceProvider.GetRequiredService<IPlayConfigurationRepository>();
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

    public async Task<User> GetOfficial()
    {
        return (await _userManager.FindByIdAsync("1"))!;
    }
}