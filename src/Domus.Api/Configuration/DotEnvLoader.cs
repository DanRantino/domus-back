using System.Diagnostics;

namespace Domus.Api.Configuration;

internal static class DotEnvLoader
{
    public static void Load()
    {
        if (IsTestHost())
        {
            return;
        }

        if (TryLoadFromAncestors(Directory.GetCurrentDirectory()))
        {
            return;
        }

        TryLoadFromAncestors(AppContext.BaseDirectory);
    }

    private static bool TryLoadFromAncestors(string startDirectory)
    {
        var directory = startDirectory;
        while (!string.IsNullOrEmpty(directory))
        {
            var envPath = Path.Combine(directory, ".env");
            var localPath = Path.Combine(directory, ".env.local");
            var hasEnv = File.Exists(envPath);
            var hasLocal = File.Exists(localPath);

            if (hasEnv || hasLocal)
            {
                var preexistingKeys = SnapshotProcessKeys();
                if (hasEnv)
                {
                    Apply(envPath, preexistingKeys);
                }

                if (hasLocal)
                {
                    Apply(localPath, preexistingKeys);
                }

                return true;
            }

            directory = Directory.GetParent(directory)?.FullName;
        }

        return false;
    }

    private static HashSet<string> SnapshotProcessKeys()
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var key in Environment.GetEnvironmentVariables().Keys)
        {
            if (key is string name)
            {
                keys.Add(name);
            }
        }

        return keys;
    }

    private static void Apply(string path, HashSet<string> preexistingKeys)
    {
        foreach (var rawLine in File.ReadAllLines(path))
        {
            if (!TryParseLine(rawLine, out var key, out var value))
            {
                continue;
            }

            if (preexistingKeys.Contains(key))
            {
                continue;
            }

            Environment.SetEnvironmentVariable(key, value);
        }
    }

    private static bool TryParseLine(string line, out string key, out string value)
    {
        key = string.Empty;
        value = string.Empty;

        var trimmed = line.Trim();
        if (trimmed.Length == 0 || trimmed.StartsWith('#'))
        {
            return false;
        }

        var separator = trimmed.IndexOf('=');
        if (separator <= 0)
        {
            return false;
        }

        key = trimmed[..separator].Trim();
        if (key.Length == 0)
        {
            return false;
        }

        value = Unquote(trimmed[(separator + 1)..].Trim());
        return true;
    }

    private static string Unquote(string value)
    {
        if (value.Length >= 2
            && ((value[0] == '"' && value[^1] == '"')
                || (value[0] == '\'' && value[^1] == '\'')))
        {
            return value[1..^1];
        }

        return value;
    }

    private static bool IsTestHost()
    {
        var processName = Process.GetCurrentProcess().ProcessName;
        return processName.Contains("testhost", StringComparison.OrdinalIgnoreCase)
            || processName.Contains("vstest", StringComparison.OrdinalIgnoreCase);
    }
}
