namespace StreamTrack;

/// <summary>
/// Pure text-formatting helpers extracted from Display.cs.
/// No Spectre.Console calls — no console output — fully unit-testable.
/// Display.cs uses these methods internally to build its markup strings.
/// </summary>
public static class DisplayService
{
    public static string StatusText(WatchStatus status) => status switch
    {
        WatchStatus.Watching    => "Watching",
        WatchStatus.Watched     => "Watched",
        WatchStatus.WantToWatch => "Want to Watch",
        _                       => status.ToString()
    };

    public static string PriorityText(Priority priority) => priority switch
    {
        Priority.High   => "High",
        Priority.Medium => "Medium",
        Priority.Low    => "Low",
        _               => priority.ToString()
    };

    public static string TypeText(TitleType type) => type switch
    {
        TitleType.Movie      => "Movie",
        TitleType.Series     => "Series",
        TitleType.MiniSeries => "Mini-Series",
        _                    => type.ToString()
    };

    public static string ProgressText(WatchlistEntry e)
    {
        if (e.Type == TitleType.Movie) return "-";
        if (e.CurrentSeason == null)  return "Not started";

        var ep    = e.CurrentEpisode != null ? $"Ep {e.CurrentEpisode}" : "";
        var total = e.TotalEpisodes  != null ? $"/{e.TotalEpisodes}"    : "";
        return $"S{e.CurrentSeason} {ep}{total}".Trim();
    }

    public static string FormatDate(DateTime date) =>
        date.ToString("MMM d, yyyy");

    /// <summary>
    /// Returns a single-line plain-text summary of an entry suitable for
    /// list views, reports, or plain-text export.
    /// </summary>
    public static string BuildSummaryLine(WatchlistEntry e) =>
        $"{e.Title} | {TypeText(e.Type)} | {StatusText(e.Status)} | {PriorityText(e.Priority)} | {ProgressText(e)}";
}