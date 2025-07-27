namespace PhiZoneApi.Interfaces;

public interface ITokenService
{
    string? GetToken(int service, TimeSpan validFor);

    public void UpdateToken(int service, string token);
}