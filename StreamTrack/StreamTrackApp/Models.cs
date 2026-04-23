namespace StreamTrack;

public enum WatchStatus
{
    WantToWatch,
    Watching,
    Watched
}

public enum TitleType
{
    Movie,
    Series,
    MiniSeries
}

public enum Priority
{
    High,
    Medium,
    Low
}

public class WatchlistEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public TitleType Type { get; set; } = TitleType.Movie;
    public WatchStatus Status { get; set; } = WatchStatus.WantToWatch;
    public Priority Priority { get; set; } = Priority.Medium;
    public string Notes { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTime AddedAt { get; set; } = DateTime.Now;

    // Progress tracking (for series)
    public int? CurrentSeason { get; set; }
    public int? CurrentEpisode { get; set; }
    public int? TotalSeasons { get; set; }
    public int? TotalEpisodes { get; set; }
}