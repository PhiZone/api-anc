namespace PhiZoneApi.Dtos.Requests;

public class TapTapRequestDto
{
    public Guid ApplicationId { get; set; }

    public string? AccessToken { get; set; }

    public string? MacKey { get; set; }

    public string? UnionId { get; set; }
}