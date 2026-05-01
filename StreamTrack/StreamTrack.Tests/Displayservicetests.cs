using StreamTrack;
using Xunit;

namespace StreamTrack.Tests;

/// <summary>
/// Tests for DisplayService — the formatting/presentation logic module.
/// We extract the pure formatting methods from Display.cs into a
/// separate static class so they can be tested without Spectre.Console.
/// </summary>
public class DisplayServiceTests
{
    // ── StatusText tests ─────────────────────────────────────────────────────

    [Theory]
    [InlineData(WatchStatus.Watching,    "Watching")]
    [InlineData(WatchStatus.Watched,     "Watched")]
    [InlineData(WatchStatus.WantToWatch, "Want to Watch")]
    public void StatusText_ReturnsCorrectLabel(WatchStatus status, string expected)
    {
        var result = DisplayService.StatusText(status);
        Assert.Equal(expected, result);
    }

    // ── PriorityText tests ───────────────────────────────────────────────────

    [Theory]
    [InlineData(Priority.High,   "High")]
    [InlineData(Priority.Medium, "Medium")]
    [InlineData(Priority.Low,    "Low")]
    public void PriorityText_ReturnsCorrectLabel(Priority priority, string expected)
    {
        var result = DisplayService.PriorityText(priority);
        Assert.Equal(expected, result);
    }

    // ── TypeText tests ───────────────────────────────────────────────────────

    [Theory]
    [InlineData(TitleType.Movie,      "Movie")]
    [InlineData(TitleType.Series,     "Series")]
    [InlineData(TitleType.MiniSeries, "Mini-Series")]
    public void TypeText_ReturnsCorrectLabel(TitleType type, string expected)
    {
        var result = DisplayService.TypeText(type);
        Assert.Equal(expected, result);
    }

    // ── ProgressText tests ───────────────────────────────────────────────────

    [Fact]
    public void ProgressText_Movie_ReturnsDash()
    {
        var entry = new WatchlistEntry { Type = TitleType.Movie };
        Assert.Equal("-", DisplayService.ProgressText(entry));
    }

    [Fact]
    public void ProgressText_SeriesNotStarted_ReturnsNotStarted()
    {
        var entry = new WatchlistEntry
        {
            Type          = TitleType.Series,
            CurrentSeason = null
        };
        Assert.Equal("Not started", DisplayService.ProgressText(entry));
    }

    [Fact]
    public void ProgressText_SeriesWithSeasonAndEpisode_FormatsCorrectly()
    {
        var entry = new WatchlistEntry
        {
            Type           = TitleType.Series,
            CurrentSeason  = 2,
            CurrentEpisode = 4
        };
        Assert.Equal("S2 Ep 4", DisplayService.ProgressText(entry));
    }

    [Fact]
    public void ProgressText_SeriesWithEpisodeTotal_IncludesTotal()
    {
        var entry = new WatchlistEntry
        {
            Type           = TitleType.Series,
            CurrentSeason  = 1,
            CurrentEpisode = 3,
            TotalEpisodes  = 10
        };
        Assert.Equal("S1 Ep 3/10", DisplayService.ProgressText(entry));
    }

    [Fact]
    public void ProgressText_MiniSeries_BehavesLikeSeries()
    {
        var entry = new WatchlistEntry
        {
            Type           = TitleType.MiniSeries,
            CurrentSeason  = 1,
            CurrentEpisode = 2,
            TotalEpisodes  = 6
        };
        Assert.Equal("S1 Ep 2/6", DisplayService.ProgressText(entry));
    }

    // ── FormatDate tests ─────────────────────────────────────────────────────

    [Fact]
    public void FormatDate_ReturnsHumanReadableDate()
    {
        var date   = new DateTime(2026, 3, 15);
        var result = DisplayService.FormatDate(date);
        Assert.Equal("Mar 15, 2026", result);
    }

    [Fact]
    public void FormatDate_SingleDigitDay_FormatsWithoutPadding()
    {
        var date   = new DateTime(2026, 1, 5);
        var result = DisplayService.FormatDate(date);
        Assert.Equal("Jan 5, 2026", result);
    }

    // ── BuildSummaryLine tests ────────────────────────────────────────────────

    [Fact]
    public void BuildSummaryLine_IncludesTitle()
    {
        var entry  = new WatchlistEntry { Title = "The Bear", Type = TitleType.Series };
        var result = DisplayService.BuildSummaryLine(entry);
        Assert.Contains("The Bear", result);
    }

    [Fact]
    public void BuildSummaryLine_IncludesStatus()
    {
        var entry  = new WatchlistEntry { Title = "X", Status = WatchStatus.Watching };
        var result = DisplayService.BuildSummaryLine(entry);
        Assert.Contains("Watching", result);
    }

    [Fact]
    public void BuildSummaryLine_IncludesPriority()
    {
        var entry  = new WatchlistEntry { Title = "X", Priority = Priority.High };
        var result = DisplayService.BuildSummaryLine(entry);
        Assert.Contains("High", result);
    }

    [Fact]
    public void BuildSummaryLine_SpecialCharactersInTitle_DoNotThrow()
    {
        var entry  = new WatchlistEntry { Title = "Movie [2026]" };
        var result = DisplayService.BuildSummaryLine(entry);
        Assert.Contains("Movie [2026]", result);
    }
}