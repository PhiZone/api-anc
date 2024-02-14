namespace PhiZoneApi.Dtos.Responses;

public class UserDetailedDto : UserDto
{
    public string Email { get; set; } = null!;

    public bool EmailConfirmed { get; set; }

    public string? PhoneNumber { get; set; }

    public bool PhoneNumberConfirmed { get; set; }

    public bool TwoFactorEnabled { get; set; }

    public int Notifications { get; set; }
}