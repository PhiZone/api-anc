namespace PhiZoneApi.Dtos.Responses;

public class TapLoginResponseDto
{
    public bool CanLogin { get; set; }

    public string? AccessToken { get; set; }

    public string? RefreshToken { get; set; }

    public string UserName { get; set; } = null!;

    public string Avatar { get; set; } = null!;

    public string OpenId { get; set; } = null!;

    public string UnionId { get; set; } = null!;
}