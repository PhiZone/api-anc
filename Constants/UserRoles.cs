namespace PhiZoneApi.Constants;

public static class UserRoles
{
    public static readonly Role Member = new() { Name = "Member", Priority = 1 };
    public static readonly Role Sponsor = new() { Name = "Sponsor", Priority = 2 };
    public static readonly Role Qualified = new() { Name = "Qualified", Priority = 3 };
    public static readonly Role Volunteer = new() { Name = "Volunteer", Priority = 4 };
    public static readonly Role Moderator = new() { Name = "Moderator", Priority = 5 };
    public static readonly Role Administrator = new() { Name = "Administrator", Priority = 6 };

    public static readonly List<Role> List = new()
    {
        Member, Sponsor, Qualified, Volunteer, Moderator, Administrator
    };
}

public class Role
{
    public string Name { get; set; } = null!;

    public int Priority { get; set; }
}