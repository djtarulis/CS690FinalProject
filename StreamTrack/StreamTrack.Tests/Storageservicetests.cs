using StreamTrack;
using Xunit;

namespace StreamTrack.Tests;

public class StorageServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _tempFile;

    public StorageServiceTests()
    {
        _tempDir  = Path.Combine(Path.GetTempPath(), "StreamTrackTests", Guid.NewGuid().ToString());
        _tempFile = Path.Combine(_tempDir, "watchlist.json");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // ── Load tests ───────────────────────────────────────────────────────────

    [Fact]
    public void Load_WhenFileDoesNotExist_ReturnsEmptyList()
    {
        var result = StorageService.Load(_tempFile);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void Load_AfterSave_ReturnsTheSameEntries()
    {
        var entries = new List<WatchlistEntry>
        {
            new() { Title = "Severance",   Status = WatchStatus.Watching, Type = TitleType.Series },
            new() { Title = "Dune Part 2", Status = WatchStatus.Watched,  Type = TitleType.Movie  }
        };

        StorageService.Save(entries, _tempFile);
        var loaded = StorageService.Load(_tempFile);

        Assert.Equal(2, loaded.Count);
        Assert.Equal("Severance",   loaded[0].Title);
        Assert.Equal("Dune Part 2", loaded[1].Title);
    }

    [Fact]
    public void Load_PreservesAllFields()
    {
        var original = new WatchlistEntry
        {
            Title          = "The Bear",
            Type           = TitleType.Series,
            Status         = WatchStatus.Watching,
            Priority       = Priority.High,
            Notes          = "Amazing show",
            Source         = "Coworker",
            CurrentSeason  = 2,
            CurrentEpisode = 4,
            TotalSeasons   = 3,
            TotalEpisodes  = 10
        };

        StorageService.Save([original], _tempFile);
        var loaded = StorageService.Load(_tempFile)[0];

        Assert.Equal(original.Title,          loaded.Title);
        Assert.Equal(original.Type,           loaded.Type);
        Assert.Equal(original.Status,         loaded.Status);
        Assert.Equal(original.Priority,       loaded.Priority);
        Assert.Equal(original.Notes,          loaded.Notes);
        Assert.Equal(original.Source,         loaded.Source);
        Assert.Equal(original.CurrentSeason,  loaded.CurrentSeason);
        Assert.Equal(original.CurrentEpisode, loaded.CurrentEpisode);
        Assert.Equal(original.TotalSeasons,   loaded.TotalSeasons);
        Assert.Equal(original.TotalEpisodes,  loaded.TotalEpisodes);
    }

    [Fact]
    public void Load_PreservesEnumValues()
    {
        var entry = new WatchlistEntry
        {
            Title    = "Arcane",
            Status   = WatchStatus.WantToWatch,
            Priority = Priority.Low,
            Type     = TitleType.MiniSeries
        };

        StorageService.Save([entry], _tempFile);
        var loaded = StorageService.Load(_tempFile)[0];

        Assert.Equal(WatchStatus.WantToWatch, loaded.Status);
        Assert.Equal(Priority.Low,            loaded.Priority);
        Assert.Equal(TitleType.MiniSeries,    loaded.Type);
    }

    [Fact]
    public void Load_PreservesNullProgressFields()
    {
        var entry = new WatchlistEntry
        {
            Title          = "Shogun",
            CurrentSeason  = null,
            CurrentEpisode = null,
            TotalSeasons   = null,
            TotalEpisodes  = null
        };

        StorageService.Save([entry], _tempFile);
        var loaded = StorageService.Load(_tempFile)[0];

        Assert.Null(loaded.CurrentSeason);
        Assert.Null(loaded.CurrentEpisode);
        Assert.Null(loaded.TotalSeasons);
        Assert.Null(loaded.TotalEpisodes);
    }

    // ── Save tests ───────────────────────────────────────────────────────────

    [Fact]
    public void Save_CreatesFileIfItDoesNotExist()
    {
        Assert.False(File.Exists(_tempFile));

        StorageService.Save([], _tempFile);

        Assert.True(File.Exists(_tempFile));
    }

    [Fact]
    public void Save_CreatesDirectoryIfItDoesNotExist()
    {
        var deepFile = Path.Combine(_tempDir, "deep", "nested", "watchlist.json");

        StorageService.Save([], deepFile);

        Assert.True(File.Exists(deepFile));
    }

    [Fact]
    public void Save_WritesValidJson()
    {
        StorageService.Save([new WatchlistEntry { Title = "Test" }], _tempFile);

        var raw = File.ReadAllText(_tempFile);
        Assert.StartsWith("[", raw.TrimStart());
        Assert.Contains("Test", raw);
    }

    [Fact]
    public void Save_OverwritesPreviousData()
    {
        StorageService.Save([new WatchlistEntry { Title = "First" }], _tempFile);
        StorageService.Save(
        [
            new() { Title = "A" },
            new() { Title = "B" },
            new() { Title = "C" }
        ], _tempFile);

        var loaded = StorageService.Load(_tempFile);
        Assert.Equal(3, loaded.Count);
        Assert.DoesNotContain(loaded, e => e.Title == "First");
    }

    [Fact]
    public void Save_EmptyList_WritesEmptyJsonArray()
    {
        StorageService.Save([], _tempFile);

        var loaded = StorageService.Load(_tempFile);
        Assert.Empty(loaded);
    }

    [Fact]
    public void Save_PreservesGuid_AcrossRoundtrip()
    {
        var id    = Guid.NewGuid();
        var entry = new WatchlistEntry { Id = id, Title = "Slow Horses" };

        StorageService.Save([entry], _tempFile);
        var loaded = StorageService.Load(_tempFile)[0];

        Assert.Equal(id, loaded.Id);
    }
}