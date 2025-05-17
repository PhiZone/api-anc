namespace PhiZoneApi.Dtos.Responses;

public class UserPersonalBestsDto
{
    public List<RecordDto> Phi3 { get; set; }

    public List<RecordDto> Best27 { get; set; } = null!;
}