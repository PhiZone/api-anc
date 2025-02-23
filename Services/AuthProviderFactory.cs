using PhiZoneApi.Enums;
using PhiZoneApi.Interfaces;

namespace PhiZoneApi.Services;

public class AuthProviderFactory(IEnumerable<IAuthProvider> authProviders)
{
    public async Task InitializeAsync()
    {
        foreach (var authProvider in authProviders) await authProvider.InitializeAsync();
    }

    public IAuthProvider? GetAuthProvider(AuthProvider? provider)
    {
        return authProviders.SingleOrDefault(e => e.GetType().Name == $"{provider}AuthProvider");
    }
}