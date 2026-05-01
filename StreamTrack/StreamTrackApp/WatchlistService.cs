namespace StreamTrack;

/// <summary>
/// Pure business logic for watchlist operations.
/// No console I/O, no file I/O — fully unit-testable.
/// </summary>
public static class WatchlistService
{
    // ── Filtering ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns entries whose status matches the given filter.
    /// If filter is null, all entries are returned unchanged.
    /// </summary>
    public static List<WatchlistEntry> Filter(
        IEnumerable<WatchlistEntry> entries,
        WatchStatus? statusFilter)
    {
        if (statusFilter == null)
            return entries.ToList();

        return entries.Where(e => e.Status == statusFilter).ToList();
    }

    // ── Sorting ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a sorted copy of the list. Does not mutate the original.
    /// </summary>
    public static List<WatchlistEntry> Sort(
        IEnumerable<WatchlistEntry> entries,
        string sortMode)
    {
        return sortMode switch
        {
            "Priority"    => entries.OrderBy(e => e.Priority).ToList(),
            "Title (A-Z)" => entries.OrderBy(e => e.Title, StringComparer.OrdinalIgnoreCase).ToList(),
            "Status"      => entries.OrderBy(e => e.Status).ToList(),
            _             => entries.OrderByDescending(e => e.AddedAt).ToList() // "Date Added"
        };
    }

    // ── Progress ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Updates episode progress on an entry.
    /// Auto-promotes status from WantToWatch → Watching when progress is set.
    /// </summary>
    public static void UpdateProgress(
        WatchlistEntry entry,
        int season,
        int episode,
        int? totalSeasons = null,
        int? totalEpisodes = null)
    {
        entry.CurrentSeason  = season;
        entry.CurrentEpisode = episode;

        if (totalSeasons  != null) entry.TotalSeasons  = totalSeasons;
        if (totalEpisodes != null) entry.TotalEpisodes = totalEpisodes;

        if (entry.Status == WatchStatus.WantToWatch)
            entry.Status = WatchStatus.Watching;
    }

    // ── Status ───────────────────────────────────────────────────────────────

    /// <summary>Sets the watch status on an entry.</summary>
    public static void SetStatus(WatchlistEntry entry, WatchStatus status)
    {
        entry.Status = status;
    }

    // ── Add ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new WatchlistEntry and appends it to the list.
    /// Returns the newly created entry.
    /// </summary>
    public static WatchlistEntry Add(
        List<WatchlistEntry> entries,
        string title,
        TitleType type      = TitleType.Movie,
        WatchStatus status  = WatchStatus.WantToWatch,
        Priority priority   = Priority.Medium,
        string notes        = "",
        string source       = "")
    {
        var entry = new WatchlistEntry
        {
            Title    = title.Trim(),
            Type     = type,
            Status   = status,
            Priority = priority,
            Notes    = notes,
            Source   = source,
            AddedAt  = DateTime.Now
        };
        entries.Add(entry);
        return entry;
    }

    // ── Remove ───────────────────────────────────────────────────────────────

    /// <summary>Removes the given entry from the list. Returns true if removed.</summary>
    public static bool Remove(List<WatchlistEntry> entries, WatchlistEntry entry)
    {
        return entries.Remove(entry);
    }
}