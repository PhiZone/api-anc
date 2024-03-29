namespace PhiZoneApi.Dtos.Deliverers;

public class RemoteUserDto
{
    public string Id { get; set; } = null!;

    public string UserName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public byte[]? Avatar { get; set; }
}