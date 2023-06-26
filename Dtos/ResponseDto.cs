using PhiZoneApi.Enums;

namespace PhiZoneApi.Dtos;

public class ResponseDto<T>
{
    public ResponseStatus Status { get; set; }

    public string Code { get; set; } = null!;

    public string? Message { get; set; }

    public object? Errors { get; set; }

    public DateTimeOffset? DateAvailable { get; set; }

    public int? PerPage { get; set; }

    public string? PreviousPage { get; set; }

    public string? NextPage { get; set; }

    public T? Data { get; set; }
}