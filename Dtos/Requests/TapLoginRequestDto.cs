namespace PhiZoneApi.Dtos.Requests;

public class TapLoginRequestDto
{
    public string AccessToken { get; set; } = null!;

    public string MacKey { get; set; } = null!;
}