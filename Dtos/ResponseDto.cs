using PhiZoneApi.Data;

namespace PhiZoneApi.Dtos;

public class ResponseDto<T>
{
    public required ResponseStatus Status { get; set; }

    public required string Code { get; set; }

    public object? Errors { get; set; }

    public DateTimeOffset? DateAvailable { get; set; }

    public T? Data { get; set; }
}