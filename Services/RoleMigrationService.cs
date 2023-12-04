using Microsoft.AspNetCore.Identity;
using PhiZoneApi.Constants;
using PhiZoneApi.Enums;
using PhiZoneApi.Models;

namespace PhiZoneApi.Services;

public class RoleMigrationService(IServiceProvider serviceProvider) : IHostedService
{
    private ILogger<RoleMigrationService> _logger = null!;
    private UserManager<User> _userManager = null!;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        _logger = scope.ServiceProvider.GetRequiredService<ILogger<RoleMigrationService>>();
        _userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        _logger.LogInformation(LogEvents.RoleMigration, "Role migration started");
        try
        {
            await MigrateRolesAsync();
            _logger.LogInformation(LogEvents.RoleMigration, "Role migration finished");
        }
        catch (Exception ex)
        {
            _logger.LogError(LogEvents.RoleMigration, ex, "Role migration failed");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task MigrateRolesAsync()
    {
        foreach (var role in UserRoles.List)
        {
            if (!Enum.TryParse(role.Name, out UserRole userRole))
            {
                _logger.LogCritical(LogEvents.RoleMigration, "Failed to parse Role {Name}", role.Name);
            }
            _logger.LogInformation(LogEvents.RoleMigration, "Migrating roles for users in Role {Name}", role.Name);
            var users = await _userManager.GetUsersInRoleAsync(role.Name);
            foreach (var user in users)
            {
                if (user.Role > 0)
                {
                    _logger.LogWarning(LogEvents.RoleMigration, "User already has a UserRole of {Name}", user.Role);
                }
                user.Role = userRole;
                await _userManager.UpdateAsync(user);
            }

            foreach (var user in users)
            {
                await _userManager.RemoveFromRoleAsync(user, role.Name);
            }
        }
    }
}