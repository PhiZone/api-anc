using System.Data;
using MySqlConnector;

namespace PhiZoneApi.Utils;

public static class DataUtil
{
    public static bool IsPrimitive(this Type type)
    {
        var types = new[]
        {
            typeof(bool), typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint),
            typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal), typeof(string),
            typeof(char), typeof(DateTime), typeof(DateTimeOffset), typeof(TimeSpan), typeof(Guid), typeof(bool?),
            typeof(byte?), typeof(sbyte?), typeof(short?), typeof(ushort?), typeof(int?), typeof(uint?),
            typeof(long?), typeof(ulong?), typeof(float?), typeof(double?), typeof(decimal?), typeof(char?),
            typeof(DateTime?), typeof(DateTimeOffset?), typeof(TimeSpan?), typeof(Guid?)
        };

        return types.Contains(type) || type.IsEnum;
    }
    
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