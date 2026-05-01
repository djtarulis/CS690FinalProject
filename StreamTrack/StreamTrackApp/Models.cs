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
    public Guid        Id             { get; set; } = Guid.NewGuid();
    public string      Title          { get; set; } = string.Empty;
    public TitleType   Type           { get; set; } = TitleType.Movie;
    public WatchStatus Status         { get; set; } = WatchStatus.WantToWatch;
    public Priority    Priority       { get; set; } = Priority.Medium;
    public string      Notes          { get; set; } = string.Empty;
    public string      Source         { get; set; } = string.Empty;
    public string      Platform       { get; set; } = string.Empty;  // FR-5.1 streaming platform
    public List<string> Tags          { get; set; } = [];             // FR-3.2 tags/categories
    public DateTime    AddedAt        { get; set; } = DateTime.Now;
    public DateTime?   WatchedAt      { get; set; }                   // FR-1.3 watch history date

    // Progress tracking (for series)
    public int?        CurrentSeason  { get; set; }
    public int?        CurrentEpisode { get; set; }
    public int?        TotalSeasons   { get; set; }
    public int?        TotalEpisodes  { get; set; }
}