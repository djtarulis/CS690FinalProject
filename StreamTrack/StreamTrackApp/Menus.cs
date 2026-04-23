using Spectre.Console;
 
namespace StreamTrack;
 
public static class Menus
{
    // ─────────────────────────────────────────────
    // Main menu
    // ─────────────────────────────────────────────
    public static void RunMainMenu(List<WatchlistEntry> entries)
    {
        while (true)
        {
            Display.Header();
            AnsiConsole.MarkupLine($"[grey]{entries.Count} title(s) in your watchlist[/]\n");
 
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold]What would you like to do?[/]")
                    .HighlightStyle(Style.Parse("steelblue1"))
                    .AddChoices(
                        "View Watchlist",
                        "Add Title",
                        "Quick Add (name only)",
                        "Exit"
                    )
            );
 
            switch (choice)
            {
                case "View Watchlist":
                    RunWatchlistMenu(entries);
                    break;
                case "Add Title":
                    AddTitle(entries, quick: false);
                    break;
                case "Quick Add (name only)":
                    AddTitle(entries, quick: true);
                    break;
                case "Exit":
                    Display.Header();
                    AnsiConsole.MarkupLine("[grey]Goodbye![/]\n");
                    return;
            }
        }
    }
 
    // ─────────────────────────────────────────────
    // Watchlist browser  (FR-3.1 — filter & sort)
    // ─────────────────────────────────────────────
    private static void RunWatchlistMenu(List<WatchlistEntry> all)
    {
        // Filter state
        WatchStatus? statusFilter = null;
        string sortMode = "Date Added";
 
        while (true)
        {
            Display.Header();
            AnsiConsole.MarkupLine("[bold]Watchlist[/]\n");
 
            // Build filtered + sorted view
            IEnumerable<WatchlistEntry> view = all;
            if (statusFilter != null)
                view = view.Where(e => e.Status == statusFilter);
 
            view = sortMode switch
            {
                "Priority"   => view.OrderBy(e => e.Priority),
                "Title (A-Z)"=> view.OrderBy(e => e.Title),
                "Status"     => view.OrderBy(e => e.Status),
                _            => view.OrderByDescending(e => e.AddedAt)
            };
 
            var sorted = view.ToList();
 
            // Active filter badge
            if (statusFilter != null)
                AnsiConsole.MarkupLine($"[grey]Filtered by:[/] {Display.StatusMarkup(statusFilter.Value)}  [grey]Sort:[/] {sortMode}\n");
            else
                AnsiConsole.MarkupLine($"[grey]Filter: All   Sort:[/] {sortMode}\n");
 
            Display.RenderWatchlist(sorted);
            AnsiConsole.WriteLine();
 
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold]Choose an action[/]")
                    .HighlightStyle(Style.Parse("steelblue1"))
                    .AddChoices(
                        "Open a title",
                        "Filter by status",
                        "Change sort order",
                        "Clear filters",
                        "← Back"
                    )
            );
 
            switch (choice)
            {
                case "Open a title":
                    OpenTitle(sorted, all);
                    break;
 
                case "Filter by status":
                    statusFilter = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("Filter by status:")
                            .HighlightStyle(Style.Parse("steelblue1"))
                            .AddChoices("Want to Watch", "Watching", "Watched")
                    ) switch
                    {
                        "Want to Watch" => WatchStatus.WantToWatch,
                        "Watching"      => WatchStatus.Watching,
                        _               => WatchStatus.Watched
                    };
                    break;
 
                case "Change sort order":
                    sortMode = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("Sort by:")
                            .HighlightStyle(Style.Parse("steelblue1"))
                            .AddChoices("Date Added", "Priority", "Title (A-Z)", "Status")
                    );
                    break;
 
                case "Clear filters":
                    statusFilter = null;
                    sortMode = "Date Added";
                    break;
 
                case "← Back":
                    return;
            }
        }
    }
 
    // ─────────────────────────────────────────────
    // Open a title from the current view
    // ─────────────────────────────────────────────
    private static void OpenTitle(List<WatchlistEntry> view, List<WatchlistEntry> all)
    {
        if (view.Count == 0)
        {
            Display.Warn("No titles to display.");
            Display.PressAnyKey();
            return;
        }
 
        var options = view.Select(e => e.Title).Append("← Cancel").ToList();
        var picked = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select a title:")
                .HighlightStyle(Style.Parse("steelblue1"))
                .PageSize(12)
                .AddChoices(options)
        );
 
        if (picked == "← Cancel") return;
 
        var entry = view.First(e => e.Title == picked);
        RunEntryDetail(entry, all);
    }
 
    // ─────────────────────────────────────────────
    // Entry detail  (FR-1.1 status, FR-1.2 progress)
    // ─────────────────────────────────────────────
    private static void RunEntryDetail(WatchlistEntry entry, List<WatchlistEntry> all)
    {
        while (true)
        {
            Display.Header();
            Display.RenderEntryDetail(entry);
            AnsiConsole.WriteLine();
 
            var choices = new List<string> { "Change status", "← Back" };
            if (entry.Type != TitleType.Movie)
                choices.Insert(1, "Update progress");
            choices.Insert(choices.Count - 1, "Edit details");
            choices.Insert(choices.Count - 1, "Delete entry");
 
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold]Choose an action[/]")
                    .HighlightStyle(Style.Parse("steelblue1"))
                    .AddChoices(choices)
            );
 
            switch (choice)
            {
                // FR-1.1 — mark status
                case "Change status":
                    ChangeStatus(entry);
                    StorageService.Save(all);
                    Display.Success("Status updated.");
                    Display.PressAnyKey();
                    break;
 
                // FR-1.2 — log season / episode
                case "Update progress":
                    UpdateProgress(entry);
                    StorageService.Save(all);
                    Display.Success("Progress saved.");
                    Display.PressAnyKey();
                    break;
 
                case "Edit details":
                    EditDetails(entry);
                    StorageService.Save(all);
                    Display.Success("Details saved.");
                    Display.PressAnyKey();
                    break;
 
                case "Delete entry":
                    if (AnsiConsole.Confirm($"Delete [bold]{Markup.Escape(entry.Title)}[/]?", defaultValue: false))
                    {
                        all.Remove(entry);
                        StorageService.Save(all);
                        Display.Success("Entry deleted.");
                        Display.PressAnyKey();
                        return;
                    }
                    break;
 
                case "← Back":
                    return;
            }
        }
    }
 
    // ─────────────────────────────────────────────
    // FR-1.1 — change watch status
    // ─────────────────────────────────────────────
    private static void ChangeStatus(WatchlistEntry entry)
    {
        var picked = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select new status:")
                .HighlightStyle(Style.Parse("steelblue1"))
                .AddChoices("Want to Watch", "Watching", "Watched")
        );
        entry.Status = picked switch
        {
            "Want to Watch" => WatchStatus.WantToWatch,
            "Watching"      => WatchStatus.Watching,
            _               => WatchStatus.Watched
        };
    }
 
    // ─────────────────────────────────────────────
    // FR-1.2 — update season / episode progress
    // ─────────────────────────────────────────────
    private static void UpdateProgress(WatchlistEntry entry)
    {
        AnsiConsole.MarkupLine("\n[bold]Update Progress[/]");
 
        entry.CurrentSeason = AnsiConsole.Ask<int>(
            "Current season:",
            entry.CurrentSeason ?? 1
        );
        entry.CurrentEpisode = AnsiConsole.Ask<int>(
            "Current episode:",
            entry.CurrentEpisode ?? 1
        );
 
        if (entry.TotalSeasons == null &&
            AnsiConsole.Confirm("Add total seasons?", defaultValue: false))
            entry.TotalSeasons = AnsiConsole.Ask<int>("Total seasons:");
 
        if (entry.TotalEpisodes == null &&
            AnsiConsole.Confirm("Add episodes per season?", defaultValue: false))
            entry.TotalEpisodes = AnsiConsole.Ask<int>("Episodes this season:");
 
        // Auto-promote to Watching if still WantToWatch
        if (entry.Status == WatchStatus.WantToWatch)
            entry.Status = WatchStatus.Watching;
    }
 
    // ─────────────────────────────────────────────
    // Edit details
    // ─────────────────────────────────────────────
    private static void EditDetails(WatchlistEntry entry)
    {
        AnsiConsole.MarkupLine("\n[bold]Edit Details[/]");
 
        entry.Title = AnsiConsole.Ask("Title:", entry.Title);
 
        entry.Type = AnsiConsole.Prompt(
            new SelectionPrompt<TitleType>()
                .Title("Type:")
                .HighlightStyle(Style.Parse("steelblue1"))
                .AddChoices(Enum.GetValues<TitleType>())
        );
 
        entry.Priority = AnsiConsole.Prompt(
            new SelectionPrompt<Priority>()
                .Title("Priority:")
                .HighlightStyle(Style.Parse("steelblue1"))
                .AddChoices(Enum.GetValues<Priority>())
        );
 
        entry.Notes = AnsiConsole.Ask("Notes (leave blank to clear):", entry.Notes);
        entry.Source = AnsiConsole.Ask("Source (where you heard about it):", entry.Source);
    }
 
    // ─────────────────────────────────────────────
    // FR-2.1 full add  /  FR-6.1 quick add
    // ─────────────────────────────────────────────
    private static void AddTitle(List<WatchlistEntry> all, bool quick)
    {
        Display.Header();
        AnsiConsole.MarkupLine(quick
            ? "[bold]Quick Add[/] [grey]— just the name; you can fill details later[/]\n"
            : "[bold]Add Title[/]\n");
 
        var title = AnsiConsole.Ask<string>("Title name:");
        if (string.IsNullOrWhiteSpace(title)) return;
 
        var entry = new WatchlistEntry { Title = title.Trim() };
 
        if (!quick)
        {
            entry.Type = AnsiConsole.Prompt(
                new SelectionPrompt<TitleType>()
                    .Title("Type:")
                    .HighlightStyle(Style.Parse("steelblue1"))
                    .AddChoices(Enum.GetValues<TitleType>())
            );
 
            entry.Status = AnsiConsole.Prompt(
                new SelectionPrompt<WatchStatus>()
                    .Title("Status:")
                    .HighlightStyle(Style.Parse("steelblue1"))
                    .AddChoices(Enum.GetValues<WatchStatus>())
            );
 
            entry.Priority = AnsiConsole.Prompt(
                new SelectionPrompt<Priority>()
                    .Title("Priority:")
                    .HighlightStyle(Style.Parse("steelblue1"))
                    .AddChoices(Enum.GetValues<Priority>())
            );
 
            var notes = AnsiConsole.Ask<string>("Notes [grey](press Enter to skip)[/]:", string.Empty);
            if (!string.IsNullOrWhiteSpace(notes)) entry.Notes = notes;
 
            var source = AnsiConsole.Ask<string>("Source [grey](press Enter to skip)[/]:", string.Empty);
            if (!string.IsNullOrWhiteSpace(source)) entry.Source = source;
        }
 
        all.Add(entry);
        StorageService.Save(all);
 
        AnsiConsole.WriteLine();
        Display.Success($"[bold]{Markup.Escape(entry.Title)}[/] added to your watchlist.");
        Display.PressAnyKey();
    }
}