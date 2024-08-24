namespace PhiZoneApi.Dtos.Deliverers;

public class ProxyRequestDto
{
    public string Uri { get; set; } = null!;

    public string Method { get; set; } = null!;

    public IEnumerable<HeaderDto> Headers { get; set; } = [];

    public string? ContentType { get; set; }

    public string? Body { get; set; }
}

public class HeaderDto
{
    public string Key { get; set; } = null!;

    public IEnumerable<string> Values { get; set; } = null!;
}