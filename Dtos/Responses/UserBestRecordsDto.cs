namespace PhiZoneApi.Dtos.Responses;

public class UserBestRecordsDto
{
    public RecordDto? Phi1 { get; set; }

    public List<RecordDto> Best19 { get; set; } = null!;
}