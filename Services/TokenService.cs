using PhiZoneApi.Interfaces;

namespace PhiZoneApi.Services;

public class TokenService : ITokenService
{
    private readonly Dictionary<int, ValueTuple<string, DateTimeOffset>> _dict = new();

    public string? GetToken(int service, TimeSpan validFor)
    {
        var value = _dict.GetValueOrDefault(service);
        return DateTimeOffset.UtcNow - value.Item2 <= validFor ? value.Item1 : null;
    }

    public void UpdateToken(int service, string token)
    {
        _dict[service] = (token, DateTimeOffset.UtcNow);
    }
}