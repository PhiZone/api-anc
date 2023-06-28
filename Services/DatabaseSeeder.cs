using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using PhiZoneApi.Data;
using PhiZoneApi.Models;

namespace PhiZoneApi.Services;

public class DatabaseSeeder : IHostedService
{
    private readonly IServiceProvider _serviceProvider;

    public DatabaseSeeder(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }


    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync(cancellationToken);
        await PopulateRoles(scope);
        await PopulateScopes(scope, cancellationToken);
        await PopulateInternalApps(scope, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task PopulateRoles(IServiceScope scope)
    {
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
        var roles = new List<string>
        {
            "Member", "Qualified", "Volunteer", "Moderator", "Administrator"
        };
        foreach (var role in roles)
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new Role
                {
                    Name = role
                });
    }

    private async Task PopulateScopes(IServiceScope scope, CancellationToken cancellationToken)
    {
        var scopeManager = scope.ServiceProvider.GetRequiredService<IOpenIddictScopeManager>();
        var scopeDescriptor = new OpenIddictScopeDescriptor
        {
            Name = "basic_access"
        };
        var scopeInstance = await scopeManager.FindByNameAsync(scopeDescriptor.Name, cancellationToken);
        if (scopeInstance == null)
            await scopeManager.CreateAsync(scopeDescriptor, cancellationToken);
        else
            await scopeManager.UpdateAsync(scopeInstance, scopeDescriptor, cancellationToken);
    }

    private async Task PopulateInternalApps(IServiceScope scopeService, CancellationToken cancellationToken)
    {
        var appManager = scopeService.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        var appDescriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = "regular",
            ClientSecret = "c29b1587-80f9-475f-b97b-dca1884eb0e3",
            Type = OpenIddictConstants.ClientTypes.Confidential,
            Permissions =
            {
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.Endpoints.Introspection,
                OpenIddictConstants.Permissions.Endpoints.Revocation,
                OpenIddictConstants.Permissions.GrantTypes.Password,
                OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                OpenIddictConstants.Permissions.Prefixes.Scope + "basic_access"
            }
        };
        var client = await appManager.FindByClientIdAsync(appDescriptor.ClientId, cancellationToken);
        if (client == null)
            await appManager.CreateAsync(appDescriptor, cancellationToken);
        else
            await appManager.UpdateAsync(client, appDescriptor, cancellationToken);
    }
}