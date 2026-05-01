using StreamTrack;
using Xunit;

namespace StreamTrack.Tests;

/// <summary>
/// Tests for WatchlistService — the pure business logic module.
/// Covers filtering, sorting, progress tracking, status changes, add, and remove.
/// </summary>
public class WatchlistServiceTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static WatchlistEntry MakeEntry(
        string title,
        WatchStatus status   = WatchStatus.WantToWatch,
        Priority priority    = Priority.Medium,
        TitleType type       = TitleType.Movie,
        DateTime? addedAt    = null) => new()
    {
        Title    = title,
        Status   = status,
        Priority = priority,
        Type     = type,
        AddedAt  = addedAt ?? DateTime.Now
    };

    // ── Filter tests ─────────────────────────────────────────────────────────

    [Fact]
    public void Filter_NullFilter_ReturnsAllEntries()
    {
        var entries = new List<WatchlistEntry>
        {
            MakeEntry("A", WatchStatus.Watching),
            MakeEntry("B", WatchStatus.Watched),
            MakeEntry("C", WatchStatus.WantToWatch)
        };

        var result = WatchlistService.Filter(entries, null);

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void Filter_WatchingStatus_ReturnsOnlyWatchingEntries()
    {
        var entries = new List<WatchlistEntry>
        {
            MakeEntry("The Bear",   WatchStatus.Watching),
            MakeEntry("Severance",  WatchStatus.WantToWatch),
            MakeEntry("Succession", WatchStatus.Watched),
            MakeEntry("Slow Horses",WatchStatus.Watching)
        };

        var result = WatchlistService.Filter(entries, WatchStatus.Watching);

        Assert.Equal(2, result.Count);
        Assert.All(result, e => Assert.Equal(WatchStatus.Watching, e.Status));
    }

    [Fact]
    public void Filter_NoMatchingEntries_ReturnsEmptyList()
    {
        var entries = new List<WatchlistEntry>
        {
            MakeEntry("A", WatchStatus.Watching),
            MakeEntry("B", WatchStatus.Watching)
        };

        var result = WatchlistService.Filter(entries, WatchStatus.Watched);

        Assert.Empty(result);
    }

    [Fact]
    public void Filter_EmptyList_ReturnsEmptyList()
    {
        var result = WatchlistService.Filter([], WatchStatus.Watching);
        Assert.Empty(result);
    }

    // ── Sort tests ───────────────────────────────────────────────────────────

    [Fact]
    public void Sort_ByTitle_ReturnsAlphabeticalOrder()
    {
        var entries = new List<WatchlistEntry>
        {
            MakeEntry("Severance"),
            MakeEntry("Arcane"),
            MakeEntry("The Bear")
        };

        var result = WatchlistService.Sort(entries, "Title (A-Z)");

        Assert.Equal("Arcane",    result[0].Title);
        Assert.Equal("Severance", result[1].Title);
        Assert.Equal("The Bear",  result[2].Title);
    }

    [Fact]
    public void Sort_ByPriority_ReturnsHighBeforeLow()
    {
        var entries = new List<WatchlistEntry>
        {
            MakeEntry("Low one",    priority: Priority.Low),
            MakeEntry("High one",   priority: Priority.High),
            MakeEntry("Medium one", priority: Priority.Medium)
        };

        var result = WatchlistService.Sort(entries, "Priority");

        Assert.Equal(Priority.High,   result[0].Priority);
        Assert.Equal(Priority.Medium, result[1].Priority);
        Assert.Equal(Priority.Low,    result[2].Priority);
    }

    [Fact]
    public void Sort_ByDateAdded_ReturnsMostRecentFirst()
    {
        var now = DateTime.Now;
        var entries = new List<WatchlistEntry>
        {
            MakeEntry("Oldest",  addedAt: now.AddDays(-10)),
            MakeEntry("Newest",  addedAt: now.AddDays(-1)),
            MakeEntry("Middle",  addedAt: now.AddDays(-5))
        };

        var result = WatchlistService.Sort(entries, "Date Added");

        Assert.Equal("Newest",  result[0].Title);
        Assert.Equal("Middle",  result[1].Title);
        Assert.Equal("Oldest",  result[2].Title);
    }

    [Fact]
    public void Sort_UnknownMode_DefaultsToDateAdded()
    {
        var now = DateTime.Now;
        var entries = new List<WatchlistEntry>
        {
            MakeEntry("Old", addedAt: now.AddDays(-3)),
            MakeEntry("New", addedAt: now)
        };

        var result = WatchlistService.Sort(entries, "some unknown mode");

        Assert.Equal("New", result[0].Title);
    }

    [Fact]
    public void Sort_DoesNotMutateOriginalList()
    {
        var entries = new List<WatchlistEntry>
        {
            MakeEntry("B"),
            MakeEntry("A")
        };

        _ = WatchlistService.Sort(entries, "Title (A-Z)");

        Assert.Equal("B", entries[0].Title); // original unchanged
    }

    // ── UpdateProgress tests ─────────────────────────────────────────────────

    [Fact]
    public void UpdateProgress_SetsSeason_AndEpisode()
    {
        var entry = MakeEntry("The Bear", type: TitleType.Series);

        WatchlistService.UpdateProgress(entry, season: 2, episode: 5);

        Assert.Equal(2, entry.CurrentSeason);
        Assert.Equal(5, entry.CurrentEpisode);
    }

    [Fact]
    public void UpdateProgress_AutoPromotes_WantToWatch_ToWatching()
    {
        var entry = MakeEntry("Severance",
            status: WatchStatus.WantToWatch,
            type:   TitleType.Series);

        WatchlistService.UpdateProgress(entry, season: 1, episode: 1);

        Assert.Equal(WatchStatus.Watching, entry.Status);
    }

    [Fact]
    public void UpdateProgress_DoesNotDowngrade_Watched_Status()
    {
        var entry = MakeEntry("Arcane",
            status: WatchStatus.Watched,
            type:   TitleType.Series);

        WatchlistService.UpdateProgress(entry, season: 2, episode: 3);

        // Status should stay Watched — user already finished it
        Assert.Equal(WatchStatus.Watched, entry.Status);
    }

    [Fact]
    public void UpdateProgress_SetsOptionalTotals_WhenProvided()
    {
        var entry = MakeEntry("Shogun", type: TitleType.Series);

        WatchlistService.UpdateProgress(entry, 1, 4, totalSeasons: 1, totalEpisodes: 10);

        Assert.Equal(1,  entry.TotalSeasons);
        Assert.Equal(10, entry.TotalEpisodes);
    }

    [Fact]
    public void UpdateProgress_LeavesTotalsNull_WhenNotProvided()
    {
        var entry = MakeEntry("Slow Horses", type: TitleType.Series);

        WatchlistService.UpdateProgress(entry, 3, 2);

        Assert.Null(entry.TotalSeasons);
        Assert.Null(entry.TotalEpisodes);
    }

    // ── SetStatus tests ──────────────────────────────────────────────────────

    [Fact]
    public void SetStatus_UpdatesEntryStatus()
    {
        var entry = MakeEntry("Dune", status: WatchStatus.WantToWatch);

        WatchlistService.SetStatus(entry, WatchStatus.Watched);

        Assert.Equal(WatchStatus.Watched, entry.Status);
    }

    // ── Add tests ────────────────────────────────────────────────────────────

    [Fact]
    public void Add_AppendsEntryToList()
    {
        var list = new List<WatchlistEntry>();

        WatchlistService.Add(list, "Severance");

        Assert.Single(list);
        Assert.Equal("Severance", list[0].Title);
    }

    [Fact]
    public void Add_TrimsWhitespace_FromTitle()
    {
        var list = new List<WatchlistEntry>();

        WatchlistService.Add(list, "  The Bear  ");

        Assert.Equal("The Bear", list[0].Title);
    }

    [Fact]
    public void Add_QuickAdd_UsesDefaultValues()
    {
        var list = new List<WatchlistEntry>();

        var entry = WatchlistService.Add(list, "Arcane");

        Assert.Equal(TitleType.Movie,           entry.Type);
        Assert.Equal(WatchStatus.WantToWatch,   entry.Status);
        Assert.Equal(Priority.Medium,           entry.Priority);
        Assert.Equal(string.Empty,              entry.Notes);
        Assert.Equal(string.Empty,              entry.Source);
    }

    [Fact]
    public void Add_FullAdd_StoresAllFields()
    {
        var list = new List<WatchlistEntry>();

        var entry = WatchlistService.Add(
            list,
            title:    "Shogun",
            type:     TitleType.Series,
            status:   WatchStatus.Watching,
            priority: Priority.High,
            notes:    "Incredible show",
            source:   "Coworker Sarah"
        );

        Assert.Equal(TitleType.Series,       entry.Type);
        Assert.Equal(WatchStatus.Watching,   entry.Status);
        Assert.Equal(Priority.High,          entry.Priority);
        Assert.Equal("Incredible show",      entry.Notes);
        Assert.Equal("Coworker Sarah",       entry.Source);
    }

    [Fact]
    public void Add_AssignsUniqueIds_ForEachEntry()
    {
        var list = new List<WatchlistEntry>();

        WatchlistService.Add(list, "Show A");
        WatchlistService.Add(list, "Show B");

        Assert.NotEqual(list[0].Id, list[1].Id);
    }

    // ── Remove tests ─────────────────────────────────────────────────────────

    [Fact]
    public void Remove_RemovesEntryFromList()
    {
        var entry = MakeEntry("The Bear");
        var list  = new List<WatchlistEntry> { entry };

        var removed = WatchlistService.Remove(list, entry);

        Assert.True(removed);
        Assert.Empty(list);
    }

    [Fact]
    public void Remove_ReturnsFalse_WhenEntryNotInList()
    {
        var list  = new List<WatchlistEntry> { MakeEntry("Arcane") };
        var other = MakeEntry("Severance");

        var removed = WatchlistService.Remove(list, other);

        Assert.False(removed);
        Assert.Single(list);
    }
}