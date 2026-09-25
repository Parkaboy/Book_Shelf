using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Book_Shelf.Data;
using Book_Shelf.Services;
using Microsoft.EntityFrameworkCore;

namespace Book_Shelf;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var settings = new LibrarySettingsStore().LoadAsync().GetAwaiter().GetResult();
            LanguageService.Apply(settings.LanguageCode);
            desktop.MainWindow = new MainWindow();
            _ = InitializeLibraryAsync();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static async Task InitializeLibraryAsync()
    {
        await using (var database = LibraryDbContext.Create())
        {
            await database.Database.MigrateAsync();
        }

    }
}