namespace PhiZoneApi.Dtos.Deliverers;

public class PhiraTokenDto
{
    public string AccessToken { get; set; } = null!;

    public string TokenType { get; set; } = null!;

    public string ExpiresIn { get; set; } = null!;

    public string RefreshToken { get; set; } = null!;
}