namespace PhiZoneApi.Dtos.Responses;

public class TapTapResponseDto
{
    public string UserName { get; set; } = null!;

    public string Avatar { get; set; } = null!;

    public string OpenId { get; set; } = null!;

    public string UnionId { get; set; } = null!;

    public UserDetailedDto? User { get; set; }
}