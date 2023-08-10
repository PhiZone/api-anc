using System.Data;
using MySqlConnector;

namespace PhiZoneApi.Utils;

public static class DataMigrationUtil
{
    public static async Task<string?> GetStr(this MySqlDataReader reader, string name)
    {
        return await reader.IsDBNullAsync(name) ? null : reader.GetString(name);
    }

    public static async Task<int?> GetInt(this MySqlDataReader reader, string name)
    {
        return await reader.IsDBNullAsync(name) ? null : reader.GetInt32(name);
    }

    public static async Task<bool?> GetBool(this MySqlDataReader reader, string name)
    {
        return await reader.IsDBNullAsync(name) ? null : reader.GetBoolean(name);
    }

    public static async Task<DateTimeOffset?> GetTime(this MySqlDataReader reader, string name)
    {
        return await reader.IsDBNullAsync(name) ? null : reader.GetDateTimeOffset(name);
    }
}