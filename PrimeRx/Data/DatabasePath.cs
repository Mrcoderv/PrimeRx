namespace PrimeRx.Data;

public static class DatabasePath
{
    public static string ResolveSqliteConnectionString(string? connectionString, string contentRootPath)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        const string prefix = "Data Source=";
        if (!connectionString.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return connectionString;

        var dataSource = connectionString[prefix.Length..].Trim().Trim('"');
        var fullPath = Path.IsPathRooted(dataSource)
            ? dataSource
            : Path.Combine(contentRootPath, dataSource);

        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        return $"{prefix}{fullPath}";
    }
}
