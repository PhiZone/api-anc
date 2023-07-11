using PhiZoneApi.Enums;

namespace PhiZoneApi.Models;

public class Application : LikeableResource
{
    public string Name { get; set; } = null!;

    public string Illustration { get; set; } = null!;

    public string Illustrator { get; set; } = null!;

    public string? Description { get; set; }

    public string Homepage { get; set; } = null!;

    public string? ApiEndpoint { get; set; }

    public ApplicationType Type { get; set; }
    
    public string Secret { get; set; } = null!;
}