namespace PhiZoneApi.Dtos.Requests;

public class ArrayTagDto
{
    public List<string>? TagsToInclude { get; set; }

    public List<string>? TagsToExclude { get; set; }
}