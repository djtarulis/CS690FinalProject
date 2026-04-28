using System.Text.Json;

namespace StreamTrack;

public static class StorageService
{
    private static readonly string DataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "StreamTrack"
    );
    private static readonly string DataFile = Path.Combine(DataDir, "watchlist.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public static List<WatchlistEntry> Load()
    {
        if (!Directory.Exists(DataDir))
            Directory.CreateDirectory(DataDir);

        if (!File.Exists(DataFile))
            return [];

        var json = File.ReadAllText(DataFile);
        return JsonSerializer.Deserialize<List<WatchlistEntry>>(json, JsonOptions) ?? [];
    }

    public static void Save(List<WatchlistEntry> entries)
    {
        if (!Directory.Exists(DataDir))
            Directory.CreateDirectory(DataDir);

        var json = JsonSerializer.Serialize(entries, JsonOptions);
        File.WriteAllText(DataFile, json);
    }
}