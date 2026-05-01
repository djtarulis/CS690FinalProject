using StreamTrack;
using Xunit;

namespace StreamTrack.Tests;

public class WatchlistServiceTests
{
    private static WatchlistEntry MakeEntry(
        string title,
        WatchStatus status   = WatchStatus.WantToWatch,
        Priority priority    = Priority.Medium,
        TitleType type       = TitleType.Movie,
        DateTime? addedAt    = null) => new()
    {
        Title   = title,
        Status  = status,
        Priority = priority,
        Type    = type,
        AddedAt = addedAt ?? DateTime.Now
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
        Assert.Equal(3, WatchlistService.Filter(entries, null).Count);
    }

    [Fact]
    public void Filter_WatchingStatus_ReturnsOnlyWatching()
    {
        var entries = new List<WatchlistEntry>
        {
            MakeEntry("A", WatchStatus.Watching),
            MakeEntry("B", WatchStatus.WantToWatch),
            MakeEntry("C", WatchStatus.Watching)
        };
        var result = WatchlistService.Filter(entries, WatchStatus.Watching);
        Assert.Equal(2, result.Count);
        Assert.All(result, e => Assert.Equal(WatchStatus.Watching, e.Status));
    }

    [Fact]
    public void Filter_NoMatch_ReturnsEmpty()
    {
        var entries = new List<WatchlistEntry> { MakeEntry("A", WatchStatus.Watching) };
        Assert.Empty(WatchlistService.Filter(entries, WatchStatus.Watched));
    }

    // ── FilterByTag tests ─────────────────────────────────────────────────────

    [Fact]
    public void FilterByTag_NullTag_ReturnsAll()
    {
        var entries = new List<WatchlistEntry> { MakeEntry("A"), MakeEntry("B") };
        Assert.Equal(2, WatchlistService.FilterByTag(entries, null).Count);
    }

    [Fact]
    public void FilterByTag_MatchesTag_CaseInsensitive()
    {
        var e1 = MakeEntry("A"); e1.Tags.Add("Action");
        var e2 = MakeEntry("B"); e2.Tags.Add("Comedy");
        var entries = new List<WatchlistEntry> { e1, e2 };

        var result = WatchlistService.FilterByTag(entries, "action");
        Assert.Single(result);
        Assert.Equal("A", result[0].Title);
    }

    [Fact]
    public void FilterByTag_NoMatch_ReturnsEmpty()
    {
        var e = MakeEntry("A"); e.Tags.Add("Drama");
        Assert.Empty(WatchlistService.FilterByTag([e], "Action"));
    }

    // ── Search tests ──────────────────────────────────────────────────────────

    [Fact]
    public void Search_NullTerm_ReturnsAll()
    {
        var entries = new List<WatchlistEntry> { MakeEntry("A"), MakeEntry("B") };
        Assert.Equal(2, WatchlistService.Search(entries, null).Count);
    }

    [Fact]
    public void Search_PartialMatch_CaseInsensitive()
    {
        var entries = new List<WatchlistEntry>
        {
            MakeEntry("The Bear"),
            MakeEntry("Severance"),
            MakeEntry("Bear Country")
        };
        var result = WatchlistService.Search(entries, "bear");
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Search_NoMatch_ReturnsEmpty()
    {
        var entries = new List<WatchlistEntry> { MakeEntry("Severance") };
        Assert.Empty(WatchlistService.Search(entries, "bear"));
    }

    // ── Sort tests ────────────────────────────────────────────────────────────

    [Fact]
    public void Sort_ByTitle_Alphabetical()
    {
        var entries = new List<WatchlistEntry>
        {
            MakeEntry("Severance"), MakeEntry("Arcane"), MakeEntry("The Bear")
        };
        var result = WatchlistService.Sort(entries, "Title (A-Z)");
        Assert.Equal("Arcane",    result[0].Title);
        Assert.Equal("Severance", result[1].Title);
        Assert.Equal("The Bear",  result[2].Title);
    }

    [Fact]
    public void Sort_ByPriority_HighFirst()
    {
        var entries = new List<WatchlistEntry>
        {
            MakeEntry("Low",    priority: Priority.Low),
            MakeEntry("High",   priority: Priority.High),
            MakeEntry("Medium", priority: Priority.Medium)
        };
        var result = WatchlistService.Sort(entries, "Priority");
        Assert.Equal(Priority.High,   result[0].Priority);
        Assert.Equal(Priority.Medium, result[1].Priority);
        Assert.Equal(Priority.Low,    result[2].Priority);
    }

    [Fact]
    public void Sort_ByDateAdded_MostRecentFirst()
    {
        var now = DateTime.Now;
        var entries = new List<WatchlistEntry>
        {
            MakeEntry("Old",    addedAt: now.AddDays(-10)),
            MakeEntry("New",    addedAt: now),
            MakeEntry("Middle", addedAt: now.AddDays(-5))
        };
        var result = WatchlistService.Sort(entries, "Date Added");
        Assert.Equal("New",    result[0].Title);
        Assert.Equal("Middle", result[1].Title);
        Assert.Equal("Old",    result[2].Title);
    }

    [Fact]
    public void Sort_DoesNotMutateOriginal()
    {
        var entries = new List<WatchlistEntry> { MakeEntry("B"), MakeEntry("A") };
        _ = WatchlistService.Sort(entries, "Title (A-Z)");
        Assert.Equal("B", entries[0].Title);
    }

    // ── UpdateProgress tests ──────────────────────────────────────────────────

    [Fact]
    public void UpdateProgress_SetsSeasonAndEpisode()
    {
        var entry = MakeEntry("Bear", type: TitleType.Series);
        WatchlistService.UpdateProgress(entry, 2, 5);
        Assert.Equal(2, entry.CurrentSeason);
        Assert.Equal(5, entry.CurrentEpisode);
    }

    [Fact]
    public void UpdateProgress_AutoPromotes_WantToWatch_ToWatching()
    {
        var entry = MakeEntry("Bear", status: WatchStatus.WantToWatch, type: TitleType.Series);
        WatchlistService.UpdateProgress(entry, 1, 1);
        Assert.Equal(WatchStatus.Watching, entry.Status);
    }

    [Fact]
    public void UpdateProgress_DoesNotDowngrade_WatchedStatus()
    {
        var entry = MakeEntry("Bear", status: WatchStatus.Watched, type: TitleType.Series);
        WatchlistService.UpdateProgress(entry, 2, 3);
        Assert.Equal(WatchStatus.Watched, entry.Status);
    }

    // ── SetStatus tests ───────────────────────────────────────────────────────

    [Fact]
    public void SetStatus_Watched_StampsWatchedAt()
    {
        var entry = MakeEntry("Bear");
        WatchlistService.SetStatus(entry, WatchStatus.Watched);
        Assert.Equal(WatchStatus.Watched, entry.Status);
        Assert.NotNull(entry.WatchedAt);
    }

    [Fact]
    public void SetStatus_BackToWatching_ClearsWatchedAt()
    {
        var entry = MakeEntry("Bear");
        WatchlistService.SetStatus(entry, WatchStatus.Watched);
        WatchlistService.SetStatus(entry, WatchStatus.Watching);
        Assert.Null(entry.WatchedAt);
    }

    [Fact]
    public void SetStatus_Watched_DoesNotOverwrite_ExistingWatchedAt()
    {
        var entry     = MakeEntry("Bear");
        var original  = new DateTime(2026, 1, 1);
        entry.WatchedAt = original;
        WatchlistService.SetStatus(entry, WatchStatus.Watched);
        Assert.Equal(original, entry.WatchedAt);
    }

    // ── Tag tests ─────────────────────────────────────────────────────────────

    [Fact]
    public void AddTag_AddsTagToEntry()
    {
        var entry = MakeEntry("Bear");
        WatchlistService.AddTag(entry, "Drama");
        Assert.True(entry.Tags.Contains("Drama"));
    }

    [Fact]
    public void AddTag_NoDuplicates_CaseInsensitive()
    {
        var entry = MakeEntry("Bear");
        WatchlistService.AddTag(entry, "Drama");
        WatchlistService.AddTag(entry, "drama");
        Assert.Single(entry.Tags);
    }

    [Fact]
    public void RemoveTag_RemovesTagFromEntry()
    {
        var entry = MakeEntry("Bear");
        WatchlistService.AddTag(entry, "Drama");
        WatchlistService.RemoveTag(entry, "Drama");
        Assert.Empty(entry.Tags);
    }

    [Fact]
    public void AllTags_ReturnsUniqueSortedTags()
    {
        var e1 = MakeEntry("A"); e1.Tags.AddRange(["Drama", "Action"]);
        var e2 = MakeEntry("B"); e2.Tags.AddRange(["Drama", "Comedy"]);
        var tags = WatchlistService.AllTags([e1, e2]);
        Assert.Equal(["Action", "Comedy", "Drama"], tags);
    }

    // ── Suggest tests ─────────────────────────────────────────────────────────

    [Fact]
    public void Suggest_ReturnsHighestPriorityWantToWatch()
    {
        var entries = new List<WatchlistEntry>
        {
            MakeEntry("Low",  WatchStatus.WantToWatch, Priority.Low),
            MakeEntry("High", WatchStatus.WantToWatch, Priority.High),
            MakeEntry("Med",  WatchStatus.WantToWatch, Priority.Medium)
        };
        Assert.Equal("High", WatchlistService.Suggest(entries)!.Title);
    }

    [Fact]
    public void Suggest_IgnoresWatchingAndWatched()
    {
        var entries = new List<WatchlistEntry>
        {
            MakeEntry("Watching", WatchStatus.Watching, Priority.High),
            MakeEntry("Watched",  WatchStatus.Watched,  Priority.High),
            MakeEntry("Want",     WatchStatus.WantToWatch, Priority.Low)
        };
        Assert.Equal("Want", WatchlistService.Suggest(entries)!.Title);
    }

    [Fact]
    public void Suggest_ReturnsNull_WhenNoWantToWatch()
    {
        var entries = new List<WatchlistEntry>
        {
            MakeEntry("A", WatchStatus.Watching),
            MakeEntry("B", WatchStatus.Watched)
        };
        Assert.Null(WatchlistService.Suggest(entries));
    }

    // ── Add tests ─────────────────────────────────────────────────────────────

    [Fact]
    public void Add_AppendsEntry()
    {
        var list = new List<WatchlistEntry>();
        WatchlistService.Add(list, "Severance");
        Assert.Single(list);
    }

    [Fact]
    public void Add_TrimsTitle()
    {
        var list = new List<WatchlistEntry>();
        WatchlistService.Add(list, "  The Bear  ");
        Assert.Equal("The Bear", list[0].Title);
    }

    [Fact]
    public void Add_StorePlatform()
    {
        var list = new List<WatchlistEntry>();
        var e    = WatchlistService.Add(list, "Severance", platform: "Apple TV+");
        Assert.Equal("Apple TV+", e.Platform);
    }

    // ── Remove tests ──────────────────────────────────────────────────────────

    [Fact]
    public void Remove_RemovesEntry()
    {
        var entry = MakeEntry("Bear");
        var list  = new List<WatchlistEntry> { entry };
        Assert.True(WatchlistService.Remove(list, entry));
        Assert.Empty(list);
    }

    [Fact]
    public void Remove_ReturnsFalse_WhenNotFound()
    {
        var list  = new List<WatchlistEntry> { MakeEntry("Arcane") };
        var other = MakeEntry("Bear");
        Assert.False(WatchlistService.Remove(list, other));
    }
}