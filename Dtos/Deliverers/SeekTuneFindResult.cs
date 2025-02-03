namespace PhiZoneApi.Dtos.Deliverers;

public abstract class SeekTuneFindResult
{
    public Guid Id { get; set; }
    
    public double Score { get; set; }
}