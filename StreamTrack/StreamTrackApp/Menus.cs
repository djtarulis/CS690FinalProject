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
                        "Watch History",
                        "Suggest Something to Watch",
                        "Search Titles",
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
                case "Watch History":
                    RunWatchHistory(entries);
                    break;
                case "Suggest Something to Watch":
                    RunSuggest(entries);
                    break;
                case "Search Titles":
                    RunSearch(entries);
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
    // Watchlist browser (FR-3.1, FR-3.2, FR-4.2)
    // ─────────────────────────────────────────────
    private static void RunWatchlistMenu(List<WatchlistEntry> all)
    {
        WatchStatus? statusFilter = null;
        string?      tagFilter    = null;
        string       sortMode     = "Date Added";

        while (true)
        {
            Display.Header();
            AnsiConsole.MarkupLine("[bold]Watchlist[/]\n");

            var view = WatchlistService.Filter(all, statusFilter);
            view = WatchlistService.FilterByTag(view, tagFilter);
            view = WatchlistService.Sort(view, sortMode);

            var filterLabel = statusFilter != null
                ? Display.StatusMarkup(statusFilter.Value)
                : "[grey]All[/]";
            var tagLabel = tagFilter != null
                ? $"[teal]{Markup.Escape(tagFilter)}[/]"
                : "[grey]All[/]";
            AnsiConsole.MarkupLine($"[grey]Status:[/] {filterLabel}  [grey]Tag:[/] {tagLabel}  [grey]Sort:[/] {sortMode}\n");

            Display.RenderWatchlist(view);
            AnsiConsole.WriteLine();

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold]Choose an action[/]")
                    .HighlightStyle(Style.Parse("steelblue1"))
                    .AddChoices(
                        "Open a title",
                        "Filter by status",
                        "Filter by tag",
                        "Change sort order",
                        "Clear filters",
                        "← Back"
                    )
            );

            switch (choice)
            {
                case "Open a title":
                    OpenTitle(view, all);
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

                case "Filter by tag":
                    var allTags = WatchlistService.AllTags(all);
                    if (allTags.Count == 0)
                    {
                        Display.Warn("No tags found. Add tags to titles first.");
                        Display.PressAnyKey();
                    }
                    else
                    {
                        tagFilter = AnsiConsole.Prompt(
                            new SelectionPrompt<string>()
                                .Title("Filter by tag:")
                                .HighlightStyle(Style.Parse("steelblue1"))
                                .AddChoices(allTags)
                        );
                    }
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
                    tagFilter    = null;
                    sortMode     = "Date Added";
                    break;

                case "← Back":
                    return;
            }
        }
    }

    // ─────────────────────────────────────────────
    // FR-1.3 — Watch History
    // ─────────────────────────────────────────────
    private static void RunWatchHistory(List<WatchlistEntry> all)
    {
        Display.Header();
        AnsiConsole.MarkupLine("[bold]Watch History[/]\n");

        var watched = WatchlistService.Filter(all, WatchStatus.Watched);
        watched = WatchlistService.Sort(watched, "Watch Date");

        if (watched.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]You haven't marked anything as Watched yet.[/]");
            Display.PressAnyKey();
            return;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn(new TableColumn("#").Centered())
            .AddColumn(new TableColumn("[bold]Title[/]"))
            .AddColumn(new TableColumn("[bold]Type[/]").Centered())
            .AddColumn(new TableColumn("[bold]Platform[/]").Centered())
            .AddColumn(new TableColumn("[bold]Date Watched[/]").Centered());

        for (int i = 0; i < watched.Count; i++)
        {
            var e = watched[i];
            table.AddRow(
                $"[grey]{i + 1}[/]",
                Markup.Escape(e.Title),
                Display.TypeMarkup(e.Type),
                string.IsNullOrWhiteSpace(e.Platform) ? "[grey]-[/]" : $"[steelblue1]{Markup.Escape(e.Platform)}[/]",
                e.WatchedAt.HasValue
                    ? $"[green]{e.WatchedAt.Value:MMM d, yyyy}[/]"
                    : "[grey]Unknown[/]"
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[grey]{watched.Count} title(s) watched[/]");
        Display.PressAnyKey();
    }

    // ─────────────────────────────────────────────
    // FR-4.3 — Suggest what to watch
    // ─────────────────────────────────────────────
    private static void RunSuggest(List<WatchlistEntry> all)
    {
        Display.Header();
        AnsiConsole.MarkupLine("[bold]Suggest Something to Watch[/]\n");

        var suggestion = WatchlistService.Suggest(all);

        if (suggestion == null)
        {
            AnsiConsole.MarkupLine("[grey]Nothing left on your Want to Watch list![/]");
            Display.PressAnyKey();
            return;
        }

        var panel = new Panel(
            $"[bold]{Markup.Escape(suggestion.Title)}[/]\n\n" +
            $"[grey]Type:[/]     {Display.TypeMarkup(suggestion.Type)}\n" +
            $"[grey]Priority:[/] {Display.PriorityMarkup(suggestion.Priority)}\n" +
            (string.IsNullOrWhiteSpace(suggestion.Platform) ? "" : $"[grey]Platform:[/] [steelblue1]{Markup.Escape(suggestion.Platform)}[/]\n") +
            (string.IsNullOrWhiteSpace(suggestion.Notes) ? "" : $"\n[grey]Notes:[/] {Markup.Escape(suggestion.Notes)}")
        )
        {
            Header      = new PanelHeader("[bold] ▶ Watch This Next [/]"),
            Border      = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Green)
        };

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();

        if (AnsiConsole.Confirm("Start watching this now?", defaultValue: false))
        {
            WatchlistService.SetStatus(suggestion, WatchStatus.Watching);
            StorageService.Save(all);
            Display.Success($"[bold]{Markup.Escape(suggestion.Title)}[/] marked as Watching.");
        }

        Display.PressAnyKey();
    }

    // ─────────────────────────────────────────────
    // FR-6.2 — Search titles
    // ─────────────────────────────────────────────
    private static void RunSearch(List<WatchlistEntry> all)
    {
        Display.Header();
        AnsiConsole.MarkupLine("[bold]Search Titles[/]\n");

        var term = AnsiConsole.Ask<string>("Search:");
        if (string.IsNullOrWhiteSpace(term)) return;

        var results = WatchlistService.Search(all, term);

        Display.Header();
        AnsiConsole.MarkupLine($"[bold]Search results for:[/] [steelblue1]{Markup.Escape(term)}[/]\n");

        if (results.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]No titles found.[/]");
            Display.PressAnyKey();
            return;
        }

        Display.RenderWatchlist(results);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[grey]{results.Count} result(s)[/]\n");

        if (AnsiConsole.Confirm("Open a title from these results?", defaultValue: false))
            OpenTitle(results, all);
        else
            Display.PressAnyKey();
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
        var picked  = AnsiConsole.Prompt(
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
    // Entry detail (FR-1.1, FR-1.2, FR-3.2, FR-5.1)
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
            choices.Insert(choices.Count - 1, "Manage tags");
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
                case "Change status":
                    ChangeStatus(entry);
                    StorageService.Save(all);
                    Display.Success("Status updated.");
                    Display.PressAnyKey();
                    break;

                case "Update progress":
                    UpdateProgress(entry);
                    StorageService.Save(all);
                    Display.Success("Progress saved.");
                    Display.PressAnyKey();
                    break;

                case "Manage tags":
                    ManageTags(entry, all);
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
        var status = picked switch
        {
            "Want to Watch" => WatchStatus.WantToWatch,
            "Watching"      => WatchStatus.Watching,
            _               => WatchStatus.Watched
        };
        WatchlistService.SetStatus(entry, status);
    }

    // ─────────────────────────────────────────────
    // FR-1.2 — update season / episode progress
    // ─────────────────────────────────────────────
    private static void UpdateProgress(WatchlistEntry entry)
    {
        AnsiConsole.MarkupLine("\n[bold]Update Progress[/]");

        entry.CurrentSeason  = AnsiConsole.Ask<int>("Current season:",  entry.CurrentSeason  ?? 1);
        entry.CurrentEpisode = AnsiConsole.Ask<int>("Current episode:", entry.CurrentEpisode ?? 1);

        if (entry.TotalSeasons == null &&
            AnsiConsole.Confirm("Add total seasons?", defaultValue: false))
            entry.TotalSeasons = AnsiConsole.Ask<int>("Total seasons:");

        if (entry.TotalEpisodes == null &&
            AnsiConsole.Confirm("Add episodes per season?", defaultValue: false))
            entry.TotalEpisodes = AnsiConsole.Ask<int>("Episodes this season:");

        if (entry.Status == WatchStatus.WantToWatch)
            entry.Status = WatchStatus.Watching;
    }

    // ─────────────────────────────────────────────
    // FR-3.2 — manage tags
    // ─────────────────────────────────────────────
    private static void ManageTags(WatchlistEntry entry, List<WatchlistEntry> all)
    {
        while (true)
        {
            Display.Header();
            var tagDisplay = entry.Tags.Count > 0
                ? string.Join(", ", entry.Tags.Select(t => $"[teal]{Markup.Escape(t)}[/]"))
                : "[grey]No tags[/]";
            AnsiConsole.MarkupLine($"[bold]Tags for:[/] {Markup.Escape(entry.Title)}");
            AnsiConsole.MarkupLine($"[grey]Current tags:[/] {tagDisplay}\n");

            var choices = new List<string> { "Add tag", "← Back" };
            if (entry.Tags.Count > 0)
                choices.Insert(1, "Remove tag");

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold]Choose an action[/]")
                    .HighlightStyle(Style.Parse("steelblue1"))
                    .AddChoices(choices)
            );

            switch (choice)
            {
                case "Add tag":
                    var newTag = AnsiConsole.Ask<string>("Tag name:");
                    if (!string.IsNullOrWhiteSpace(newTag))
                    {
                        WatchlistService.AddTag(entry, newTag);
                        StorageService.Save(all);
                        Display.Success($"Tag [teal]{Markup.Escape(newTag.Trim())}[/] added.");
                    }
                    break;

                case "Remove tag":
                    var removeTag = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("Remove which tag?")
                            .HighlightStyle(Style.Parse("steelblue1"))
                            .AddChoices(entry.Tags)
                    );
                    WatchlistService.RemoveTag(entry, removeTag);
                    StorageService.Save(all);
                    Display.Success($"Tag [teal]{Markup.Escape(removeTag)}[/] removed.");
                    break;

                case "← Back":
                    return;
            }
        }
    }

    // ─────────────────────────────────────────────
    // Edit details (FR-5.1 platform field)
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

        entry.Platform = AnsiConsole.Ask("Platform [grey](Netflix, Hulu, etc. — press Enter to skip)[/]:", entry.Platform);
        entry.Notes    = AnsiConsole.Ask("Notes [grey](press Enter to clear)[/]:",  entry.Notes);
        entry.Source   = AnsiConsole.Ask("Source [grey](press Enter to clear)[/]:", entry.Source);
    }

    // ─────────────────────────────────────────────
    // FR-2.1 full add / FR-6.1 quick add
    // ─────────────────────────────────────────────
    private static void AddTitle(List<WatchlistEntry> all, bool quick)
    {
        Display.Header();
        AnsiConsole.MarkupLine(quick
            ? "[bold]Quick Add[/] [grey]— just the name; fill details later[/]\n"
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
            if (entry.Status == WatchStatus.Watched)
                entry.WatchedAt = DateTime.Now;

            entry.Priority = AnsiConsole.Prompt(
                new SelectionPrompt<Priority>()
                    .Title("Priority:")
                    .HighlightStyle(Style.Parse("steelblue1"))
                    .AddChoices(Enum.GetValues<Priority>())
            );

            var platform = AnsiConsole.Ask<string>("Platform [grey](press Enter to skip)[/]:", string.Empty);
            if (!string.IsNullOrWhiteSpace(platform)) entry.Platform = platform;

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