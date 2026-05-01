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

    /// <summary>
    /// Returns entries that contain the given tag (case-insensitive).
    /// If tag is null or empty, all entries are returned.
    /// </summary>
    public static List<WatchlistEntry> FilterByTag(
        IEnumerable<WatchlistEntry> entries,
        string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return entries.ToList();

        return entries
            .Where(e => e.Tags.Any(t => t.Equals(tag, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    /// <summary>
    /// FR-6.2 — Returns entries whose title contains the search term (case-insensitive).
    /// If term is null or empty, all entries are returned.
    /// </summary>
    public static List<WatchlistEntry> Search(
        IEnumerable<WatchlistEntry> entries,
        string? term)
    {
        if (string.IsNullOrWhiteSpace(term))
            return entries.ToList();

        return entries
            .Where(e => e.Title.Contains(term, StringComparison.OrdinalIgnoreCase))
            .ToList();
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
            "Watch Date"  => entries.OrderByDescending(e => e.WatchedAt).ToList(),
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
        int? totalSeasons   = null,
        int? totalEpisodes  = null)
    {
        entry.CurrentSeason  = season;
        entry.CurrentEpisode = episode;

        if (totalSeasons  != null) entry.TotalSeasons  = totalSeasons;
        if (totalEpisodes != null) entry.TotalEpisodes = totalEpisodes;

        if (entry.Status == WatchStatus.WantToWatch)
            entry.Status = WatchStatus.Watching;
    }

    // ── Status ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Sets the watch status on an entry.
    /// Automatically stamps WatchedAt when marked Watched (FR-1.3).
    /// Clears WatchedAt if moved back to Watching or WantToWatch.
    /// </summary>
    public static void SetStatus(WatchlistEntry entry, WatchStatus status)
    {
        entry.Status = status;
        if (status == WatchStatus.Watched)
            entry.WatchedAt ??= DateTime.Now;
        else
            entry.WatchedAt = null;
    }

    // ── Tags ─────────────────────────────────────────────────────────────────

    /// <summary>FR-3.2 — Adds a tag to an entry if it isn't already present.</summary>
    public static void AddTag(WatchlistEntry entry, string tag)
    {
        var trimmed = tag.Trim();
        if (!string.IsNullOrEmpty(trimmed) &&
            !entry.Tags.Any(t => t.Equals(trimmed, StringComparison.OrdinalIgnoreCase)))
            entry.Tags.Add(trimmed);
    }

    /// <summary>FR-3.2 — Removes a tag from an entry.</summary>
    public static void RemoveTag(WatchlistEntry entry, string tag)
    {
        entry.Tags.RemoveAll(t => t.Equals(tag.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Returns all unique tags across all entries, sorted alphabetically.</summary>
    public static List<string> AllTags(IEnumerable<WatchlistEntry> entries)
    {
        return entries
            .SelectMany(e => e.Tags)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(t => t)
            .ToList();
    }

    // ── Suggest ──────────────────────────────────────────────────────────────

    /// <summary>
    /// FR-4.3 — Returns the highest-priority WantToWatch entry.
    /// Ties broken by most recently added.
    /// Returns null if the watchlist has no WantToWatch entries.
    /// </summary>
    public static WatchlistEntry? Suggest(IEnumerable<WatchlistEntry> entries)
    {
        return entries
            .Where(e => e.Status == WatchStatus.WantToWatch)
            .OrderBy(e => e.Priority)
            .ThenByDescending(e => e.AddedAt)
            .FirstOrDefault();
    }

    // ── Add ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new WatchlistEntry and appends it to the list.
    /// Returns the newly created entry.
    /// </summary>
    public static WatchlistEntry Add(
        List<WatchlistEntry> entries,
        string title,
        TitleType   type     = TitleType.Movie,
        WatchStatus status   = WatchStatus.WantToWatch,
        Priority    priority = Priority.Medium,
        string      notes    = "",
        string      source   = "",
        string      platform = "")
    {
        var entry = new WatchlistEntry
        {
            Title    = title.Trim(),
            Type     = type,
            Status   = status,
            Priority = priority,
            Notes    = notes,
            Source   = source,
            Platform = platform,
            AddedAt  = DateTime.Now
        };
        if (status == WatchStatus.Watched)
            entry.WatchedAt = DateTime.Now;

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