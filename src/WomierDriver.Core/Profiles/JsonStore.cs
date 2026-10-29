using System.Text.Json;
using System.Text.Json.Serialization;

namespace WomierDriver.Core.Profiles;

/// <summary>Minimal JSON persistence with atomic writes (write to a temp file, then replace).</summary>
public static class JsonStore
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.General)
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static T? Load<T>(string path) where T : class
    {
        if (!File.Exists(path))
        {
            return null;
        }

        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<T>(stream, Options);
    }

    public static void Save<T>(string path, T value)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var temp = path + ".tmp";
        using (var stream = File.Create(temp))
        {
            JsonSerializer.Serialize(stream, value, Options);
        }

        File.Move(temp, path, overwrite: true);
    }

    /// <summary>Writes already serialized JSON atomically (temp file + replace).</summary>
    public static void SaveText(string path, string json)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var temp = path + ".tmp";
        File.WriteAllText(temp, json);
        File.Move(temp, path, overwrite: true);
    }

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);

    public static T? Deserialize<T>(string json) where T : class => JsonSerializer.Deserialize<T>(json, Options);
}

/// <summary>Where the driver keeps its data: %APPDATA%\WomierDriver unless WOMIER_DRIVER_DATA overrides it.</summary>
public static class AppPaths
{
    private const string OverrideVariable = "WOMIER_DRIVER_DATA";

    static AppPaths()
    {
        var overridePath = Environment.GetEnvironmentVariable(OverrideVariable);
        IsRedirected = !string.IsNullOrWhiteSpace(overridePath);
        DataDirectory = IsRedirected
            ? Path.GetFullPath(overridePath!)
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WomierDriver");

        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(DataDirectory.ToUpperInvariant()));
        InstanceKey = Convert.ToHexString(hash, 0, 6);
    }

    public static string DataDirectory { get; }

    /// <summary>True when <c>WOMIER_DRIVER_DATA</c> redirects the data folder (a test or secondary instance).</summary>
    public static bool IsRedirected { get; }

    /// <summary>Short, stable key derived from the data folder; keeps instances with different folders apart.</summary>
    public static string InstanceKey { get; }

    public static string ProfilesFile => Path.Combine(DataDirectory, "profiles.json");

    public static string SettingsFile => Path.Combine(DataDirectory, "settings.json");

    public static string LogFile => Path.Combine(DataDirectory, "driver.log");
}
