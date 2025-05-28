namespace PhiZoneApi.Dtos.Responses;

public class CreatedResponseDto<T>
{
    public T Id { get; set; } = default!;
}