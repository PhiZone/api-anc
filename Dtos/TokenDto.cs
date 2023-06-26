namespace PhiZoneApi.Dtos;

public class TokenDto
{
    public string Token { get; set; } = null!;

    public DateTimeOffset Expiration { get; set; }
}