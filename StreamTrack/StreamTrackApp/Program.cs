using Spectre.Console;
using StreamTrack;

var entries = StorageService.Load();

try
{
    Menus.RunMainMenu(entries);
}
catch (Exception ex)
{
    AnsiConsole.WriteException(ex);
}