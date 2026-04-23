using Spectre.Console;

namespace StreamTrack;

public static class Display
{
    public static string StatusMarkup(WatchStatus status) => status switch
    {
        WatchStatus.Watching     => "[cyan]Watching[/]",
        WatchStatus.Watched      => "[green]Watched[/]",
        WatchStatus.WantToWatch  => "[grey]Want to Watch[/]",
        _                        => status.ToString()
    };

    public static string PriorityMarkup(Priority priority) => priority switch
    {
        Priority.High   => "[red]High[/]",
        Priority.Medium => "[yellow]Medium[/]",
        Priority.Low    => "[grey]Low[/]",
        _               => priority.ToString()
    };

    public static string TypeMarkup(TitleType type) => type switch
    {
        TitleType.Movie      => "[blue]Movie[/]",
        TitleType.Series     => "[purple]Series[/]",
        TitleType.MiniSeries => "[purple]Mini-Series[/]",
        _                    => type.ToString()
    };

    public static string ProgressText(WatchlistEntry e)
    {
        if (e.Type == TitleType.Movie) return "-";
        if (e.CurrentSeason == null) return "[grey]Not started[/]";
        var ep = e.CurrentEpisode != null ? $"Ep {e.CurrentEpisode}" : "";
        var total = e.TotalEpisodes != null ? $"/{e.TotalEpisodes}" : "";
        return $"S{e.CurrentSeason} {ep}{total}";
    }

    public static void RenderWatchlist(List<WatchlistEntry> entries)
    {
        if (entries.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]No entries found.[/]");
            return;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn(new TableColumn("#").Centered())
            .AddColumn(new TableColumn("[bold]Title[/]"))
            .AddColumn(new TableColumn("[bold]Type[/]").Centered())
            .AddColumn(new TableColumn("[bold]Status[/]").Centered())
            .AddColumn(new TableColumn("[bold]Priority[/]").Centered())
            .AddColumn(new TableColumn("[bold]Progress[/]").Centered());

        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            table.AddRow(
                $"[grey]{i + 1}[/]",
                Markup.Escape(e.Title),
                TypeMarkup(e.Type),
                StatusMarkup(e.Status),
                PriorityMarkup(e.Priority),
                ProgressText(e)
            );
        }

        AnsiConsole.Write(table);
    }

    public static void RenderEntryDetail(WatchlistEntry e)
    {
        var panel = new Panel(BuildDetailMarkup(e))
        {
            Header = new PanelHeader($"[bold] {Markup.Escape(e.Title)} [/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.SteelBlue1)
        };
        AnsiConsole.Write(panel);
    }

    private static string BuildDetailMarkup(WatchlistEntry e)
    {
        var lines = new List<string>
        {
            $"[grey]Type:[/]     {TypeMarkup(e.Type)}",
            $"[grey]Status:[/]   {StatusMarkup(e.Status)}",
            $"[grey]Priority:[/] {PriorityMarkup(e.Priority)}",
            $"[grey]Added:[/]    {e.AddedAt:MMM d, yyyy}"
        };

        if (e.Type != TitleType.Movie)
        {
            lines.Add("");
            lines.Add("[bold]Progress[/]");
            if (e.CurrentSeason != null)
            {
                lines.Add($"  Season {e.CurrentSeason}, Episode {e.CurrentEpisode ?? 0}" +
                          (e.TotalEpisodes != null ? $" / {e.TotalEpisodes}" : ""));
            }
            else
            {
                lines.Add("  [grey]Not started[/]");
            }
            if (e.TotalSeasons != null)
                lines.Add($"  [grey]Total seasons: {e.TotalSeasons}[/]");
        }

        if (!string.IsNullOrWhiteSpace(e.Notes))
        {
            lines.Add("");
            lines.Add($"[grey]Notes:[/]  {Markup.Escape(e.Notes)}");
        }

        if (!string.IsNullOrWhiteSpace(e.Source))
            lines.Add($"[grey]Source:[/] {Markup.Escape(e.Source)}");

        return string.Join("\n", lines);
    }

    public static void Header()
    {
        AnsiConsole.Clear();
        var rule = new Rule("[bold steelblue1]StreamTrack[/]")
            .RuleStyle(Style.Parse("grey"))
            .LeftJustified();
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();
    }

    public static void Success(string msg) =>
        AnsiConsole.MarkupLine($"[green]✓[/] {msg}");

    public static void Info(string msg) =>
        AnsiConsole.MarkupLine($"[steelblue1]→[/] {msg}");

    public static void Warn(string msg) =>
        AnsiConsole.MarkupLine($"[yellow]![/] {msg}");

    public static void PressAnyKey()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
        Console.ReadKey(true);
    }
}