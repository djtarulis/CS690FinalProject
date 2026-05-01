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

    public static List<WatchlistEntry> Load(string? filePath = null)
    {
        var path = filePath ?? DataFile;
        var dir  = Path.GetDirectoryName(path)!;

        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        if (!File.Exists(path))
            return [];

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<WatchlistEntry>>(json, JsonOptions) ?? [];
    }

    public static void Save(List<WatchlistEntry> entries, string? filePath = null)
    {
        var path = filePath ?? DataFile;
        var dir  = Path.GetDirectoryName(path)!;

        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(entries, JsonOptions);
        File.WriteAllText(path, json);
    }
}