namespace PhiZoneApi.Dtos.Requests;

public class TapTapRequestDto
{
    public Guid ApplicationId { get; set; }

    public string AccessToken { get; set; } = null!;

    public string MacKey { get; set; } = null!;
}