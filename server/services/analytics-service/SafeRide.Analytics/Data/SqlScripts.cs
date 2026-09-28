using System.Collections.Concurrent;
using System.Reflection;

namespace SafeRide.Analytics.Data;

/// Loads queries from embedded .sql files so no SQL string appears in C#.
/// Cached after first read — these never change at run time.
public static class SqlScripts
{
    private const string Prefix = "SafeRide.Analytics.Data.Sql.";

    private static readonly ConcurrentDictionary<string, string> Cache = new();

    public static string Get(string name) => Cache.GetOrAdd(name, Load);

    private static string Load(string name)
    {
        var resource = Prefix + name + ".sql";

        using var stream =
            Assembly.GetExecutingAssembly().GetManifestResourceStream(resource)
            // Fail loudly. A missing script would otherwise surface as an empty
            // command that silently does nothing.
            ?? throw new InvalidOperationException($"Embedded SQL script not found: {resource}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
