using PhiZoneApi.Models;

namespace PhiZoneApi.Constants;

public static class HostshipPermissions
{
    private const ushort Operation = 26;
    private const ushort Scope = 28;

    public const uint Retrieve = 0b00u;
    public const uint Create = 0b01u;
    public const uint Update = 0b10u;
    public const uint Remove = 0b11u;

    public const uint Division = 0b0000u;
    public const uint Team = 0b0001u;
    public const uint Resource = 0b0010u;
    public const uint Hostship = 0b0011u;
    public const uint PreservedField = 0b0100u;

    public static bool HasPermission(this Hostship hostship, uint permission) =>
        hostship.Permissions.Contains(permission);

    public static uint Gen(uint operation, uint scope, int? index = null)
    {
        if (index != null && (index & (1 << Operation)) != 0)
            throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");
        return (operation << Operation) | (scope << Scope) | ((uint?)index ?? 0u);
    }

    public static int GetIndex(uint permission) => (int)(permission - ((permission >> Operation) << Operation));
    
    public static bool SameAs(this uint permission, uint other) =>
        permission >> Operation == other >> Operation;
}