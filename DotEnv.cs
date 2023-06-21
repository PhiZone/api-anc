namespace PhiZoneApi;

public static class DotEnv
{
    public static void Load(string fileName)
    {
        var root = Directory.GetCurrentDirectory();
        var filePath = Path.Combine(root, fileName);

        if (!File.Exists(filePath))
            return;

        foreach (var line in File.ReadAllLines(filePath))
        {
            var parts = line.Split(
                '=',
                StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2)
                continue;

            Environment.SetEnvironmentVariable(parts[0], parts[1]);
        }
    }
}