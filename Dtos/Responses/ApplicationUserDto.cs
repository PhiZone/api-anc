namespace PhiZoneApi.Dtos.Responses;

public class ApplicationUserDto
{
    public int UserId { get; set; }

    public Guid ApplicationId { get; set; }

    public ApplicationDto Application { get; set; } = null!;

    public string? RemoteUserId { get; set; }

    public DateTimeOffset DateCreated { get; set; }

    public DateTimeOffset DateUpdated { get; set; }
}