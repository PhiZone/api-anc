using Microsoft.AspNetCore.Identity;
using PhiZoneApi.Constants;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;

namespace PhiZoneApi.Services;

public class QualificationMigrationService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private IAuthorshipRepository _authorshipRepository = null!;
    private IChartRepository _chartRepository = null!;
    private ILogger<QualificationMigrationService> _logger = null!;
    private IResourceService _resourceService = null!;
    private UserManager<User> _userManager = null!;

    public QualificationMigrationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        _logger = scope.ServiceProvider.GetRequiredService<ILogger<QualificationMigrationService>>();
        _userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        _chartRepository = scope.ServiceProvider.GetRequiredService<IChartRepository>();
        _authorshipRepository = scope.ServiceProvider.GetRequiredService<IAuthorshipRepository>();
        _resourceService = scope.ServiceProvider.GetRequiredService<IResourceService>();

        _logger.LogInformation(LogEvents.QualificationMigration, "Qualification migration started");
        try
        {
            await MigrateQualificationsAsync();
            _logger.LogInformation(LogEvents.QualificationMigration, "Qualification migration finished");
        }
        catch (Exception ex)
        {
            _logger.LogError(LogEvents.QualificationMigration, ex, "Qualification migration failed");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task MigrateQualificationsAsync()
    {
        var charts =
            await _chartRepository.GetChartsAsync(new List<string> { "DateCreated" }, new List<bool> { false }, 0, -1);
        var charters = new HashSet<int>();
        foreach (var chart in charts)
        {
            _logger.LogInformation(LogEvents.QualificationMigration, "Migrating authorships for Chart #{Id}", chart.Id);
            var ids = _resourceService.GetAuthorIds(chart.AuthorName);
            if (!ids.Contains(chart.OwnerId)) ids.Add(chart.OwnerId);
            foreach (var id in ids)
            {
                charters.Add(id);
                if (await _authorshipRepository.AuthorshipExistsAsync(chart.Id, id)) continue;
                var authorship = new Authorship
                {
                    ResourceId = chart.Id, AuthorId = id, DateCreated = chart.DateCreated
                };
                await _authorshipRepository.CreateAuthorshipAsync(authorship);
            }
        }

        foreach (var id in charters)
        {
            var charter = (await _userManager.FindByIdAsync(id.ToString()))!;
            _logger.LogInformation(LogEvents.QualificationMigration, "Granting permission to {UserName}",
                charter.UserName);
            var roles = await _userManager.GetRolesAsync(charter);
            if (roles.Count >= 1)
            {
                var realRole = roles.Select(role => Roles.List.FirstOrDefault(r => r.Name == role))
                    .OrderByDescending(role => role?.Priority ?? 0).First()!;
                foreach (var r in roles.Where(role => role != realRole.Name))
                    await _userManager.RemoveFromRoleAsync(charter, r);
            }

            if (await _resourceService.HasPermission(charter, Roles.Qualified)) continue;
            var role = await _resourceService.GetRole(charter);
            if (role != null) await _userManager.RemoveFromRoleAsync(charter, role.Name);
            await _userManager.AddToRoleAsync(charter, Roles.Qualified.Name);
        }
    }
}