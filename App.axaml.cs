using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Book_Shelf.Data;
using Book_Shelf.Services;

namespace Book_Shelf;

/// <summary>Initializes application resources and configures the desktop startup window.</summary>
public partial class App : Application
{
    /// <summary>Loads the Avalonia application resources.</summary>
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>Applies saved preferences and creates the main application window.</summary>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var settings = new LibrarySettingsStore().Load();
            LanguageService.Apply(settings.LanguageCode);
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}