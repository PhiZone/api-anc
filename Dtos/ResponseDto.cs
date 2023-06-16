namespace PhiZoneApi.Dtos
{
    public class ResponseDto<T>
    {
        public required int Status { get; set; }
        public required string Code { get; set; }
        public object? Errors { get; set; }
        public T? Data { get; set; }
    }
}
