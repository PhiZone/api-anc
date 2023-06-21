namespace PhiZoneApi.Dtos;

public class TokenDto
{
    public required DateTimeOffset Expiration;

    public required string Token;
}