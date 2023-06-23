namespace PhiZoneApi.Dtos;

public class UserDto
{
    public int Id { get; set; }

    public string UserName { get; set; } = null!;

    public string Avatar { get; set; } = null!;

    public int Gender { get; set; }

    public string? Biography { get; set; }

    public double Experience { get; set; }

    public string? Tag { get; set; }

    public double Rks { get; set; }

    public string Language { get; set; } = null!;

    public DateTimeOffset? DateLastLoggedIn { get; set; }

    public DateTimeOffset DateJoined { get; set; }

    public DateTimeOffset? DateOfBirth { get; set; }
}