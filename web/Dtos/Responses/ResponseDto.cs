using PhiZoneApi.Enums;

namespace PhiZoneApi.Dtos.Responses;

public class ResponseDto<T>
{
    public ResponseStatus Status { get; set; }

    public string Code { get; set; } = null!;

    public string? Message { get; set; }

    public List<ModelErrorDto>? Errors { get; set; }

    public DateTimeOffset? DateAvailable { get; set; }

    public int? Total { get; set; }

    public int? PerPage { get; set; }

    public bool? HasPrevious { get; set; }

    public bool? HasNext { get; set; }

    public T? Data { get; set; }
}