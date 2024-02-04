namespace PhiZoneApi.Dtos.Responses;

public class UserPersonalBestsDto
{
    public RecordDto? Phi1 { get; set; }

    public List<RecordDto> Best19 { get; set; } = null!;
}