namespace PhiZoneApi.Dtos.Deliverers;

public class TapTapDelivererDto
{
    public TapTapDataDto Data { get; set; } = null!;
    
    public long Now { get; set; }
    
    public bool Success { get; set; }
}

public class TapTapDataDto
{
    public string Name { get; set; } = null!;
    
    public string Avatar { get; set; } = null!;
    
    public string Openid { get; set; } = null!;
    
    public string Unionid { get; set; } = null!;
}