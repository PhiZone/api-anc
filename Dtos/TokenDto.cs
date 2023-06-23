namespace PhiZoneApi.Dtos;

public class TokenDto
{
    public DateTimeOffset Expiration { get; set; }

    public string Token { get; set; } = null!;
}