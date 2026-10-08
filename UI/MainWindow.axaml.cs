using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Book_Shelf.Data;
using Book_Shelf.Resources;
using Book_Shelf.UI;
using Serilog;

namespace Book_Shelf;

/// <summary>Hosts the library interface and routes user actions to the view model and services.</summary>
public partial class MainWindow : Window
{
    private readonly LibraryViewModel viewModel = new();

    /// <summary>Initializes the window, binds the library view model, and registers lifecycle handlers.</summary>
    public MainWindow()
    {
        InitializeComponent();
        DataContext = viewModel;
        Opened += OnOpened;
        Closed += (_, _) => viewModel.Dispose();
    }

    /// <summary>Loads the library after the splash screen is displayed and reports startup errors.</summary>
    private async void OnOpened(object? sender, EventArgs e)
    {
        Opened -= OnOpened;
        await Task.Yield();
        try
        {
            await viewModel.InitializeAsync();
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to initialize the library");
            viewModel.SetStatusMessage(Strings.LibraryInitializationFailed);
        }
        finally
        {
            SplashScreen.IsVisible = false;
            LibraryContent.IsVisible = true;
        }
    }

    /// <summary>Prompts for a book folder and synchronizes the library from the chosen location.</summary>
    private async void ChooseFolder_OnClick(object? sender, RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            AllowMultiple = false,
            Title = Strings.ChooseYourBookLibrary
        });

        if (folders.Count > 0 && folders[0].TryGetLocalPath() is string folderPath)
        {
            await viewModel.SetLibraryFolderAsync(folderPath);
        }
    }

    /// <summary>Rescans the currently selected book folder.</summary>
    private async void Rescan_OnClick(object? sender, RoutedEventArgs e)
    {
        await viewModel.SynchronizeAsync();
    }

    /// <summary>Prompts for a destination and exports a copy of the active database.</summary>
    private async void ExportDatabase_OnClick(object? sender, RoutedEventArgs e)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = Strings.ExportDatabase,
            SuggestedFileName = "books.db",
            FileTypeChoices =
            [
                new FilePickerFileType(Strings.SQLiteDatabase)
                {
                    Patterns = ["*.db"]
                }
            ]
        });

        if (file?.TryGetLocalPath() is string destinationPath)
        {
            File.Copy(LibraryDbContext.GetDatabasePath(), destinationPath, overwrite: true);
        }
    }

    /// <summary>Prompts for a database file, confirms replacement, and imports it into the library.</summary>
    private async void ImportDatabase_OnClick(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            Title = Strings.ImportDatabase,
            FileTypeFilter =
            [
                new FilePickerFileType(Strings.SQLiteDatabase)
                {
                    Patterns = ["*.db", "*.sqlite", "*.sqlite3"]
                }
            ]
        });

        if (files.Count == 0 || files[0].TryGetLocalPath() is not string sourcePath)
        {
            return;
        }

        var confirmed = await ConfirmAsync(
            Strings.ImportDatabaseConfirmationTitle,
            Strings.ImportDatabaseConfirmationMessage,
            Strings.ImportDatabase);
        if (!confirmed)
        {
            return;
        }

        try
        {
            await viewModel.ImportDatabaseAsync(sourcePath);
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to import the library database from {DatabasePath}", sourcePath);
            viewModel.SetStatusMessage(Strings.DatabaseImportFailed);
        }
    }

    /// <summary>Confirms and removes all catalog data while preserving original book files.</summary>
    private async void CleanDatabase_OnClick(object? sender, RoutedEventArgs e)
    {
        var confirmed = await ConfirmAsync(
            Strings.CleanDatabaseConfirmationTitle,
            Strings.CleanDatabaseConfirmationMessage,
            Strings.CleanDatabase);
        if (!confirmed)
        {
            return;
        }

        await viewModel.ClearDatabaseAsync();
    }

    /// <summary>Displays a confirmation dialog and returns the user's response.</summary>
    private async Task<bool> ConfirmAsync(string title, string message, string confirmText)
    {
        var dialog = new ConfirmationWindow(title, message, confirmText, Strings.Cancel);
        return await dialog.ShowDialog<bool>(this);
    }

    /// <summary>Opens the book associated with a card that was double-clicked.</summary>
    private void BookCard_DoubleTapped(object? sender, RoutedEventArgs e)
    {
        OpenBook(sender);
    }

    /// <summary>Selects the book card that receives keyboard focus.</summary>
    private void BookCard_GotFocus(object? sender, FocusChangedEventArgs e)
    {
        if (sender is Control { DataContext: BookItemViewModel book })
        {
            viewModel.SelectBook(book);
        }
    }

    /// <summary>Opens a focused book card when Enter or Space is pressed.</summary>
    private void BookCard_KeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not Control card ||
            !ReferenceEquals(e.Source, card) ||
            e.Key is not (Key.Enter or Key.Space))
        {
            return;
        }

        OpenBook(card);
        e.Handled = true;
    }

    /// <summary>Opens a book file with its registered system application.</summary>
    private void OpenBook(object? sender)
    {
        if (sender is not Control { DataContext: BookItemViewModel book } || !File.Exists(book.FilePath))
        {
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = book.FilePath,
            UseShellExecute = true
        });
    }

    /// <summary>Focuses and selects the book card under the pointer.</summary>
    private void BookCard_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (sender is Control card && card.DataContext is BookItemViewModel book)
        {
            card.Focus();
            viewModel.SelectBook(book);
        }
    }

    /// <summary>Enters edit mode for the book card whose edit action was invoked.</summary>
    private void EditBook_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: BookItemViewModel book })
        {
            viewModel.BeginEditing(book);
        }
    }

    /// <summary>Saves edited metadata for the book card whose save action was invoked.</summary>
    private async void SaveBook_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: BookItemViewModel book })
        {
            await viewModel.SaveBookAsync(book);
        }
    }

    /// <summary>Confirms deletion and removes the book associated with the invoked card.</summary>
    private async void DeleteBook_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: BookItemViewModel book } &&
            await ConfirmAsync(
                Strings.DeleteBookConfirmationTitle,
                string.Format(Strings.DeleteBookConfirmationMessageFormat, book.Title),
                Strings.Delete))
        {
            await viewModel.DeleteBookAsync(book);
        }
    }

    /// <summary>Displays the application information window.</summary>
    private async void About_OnClick(object? sender, RoutedEventArgs e)
    {
        var aboutWindow = new AboutWindow();
        await aboutWindow.ShowDialog(this);
    }

    /// <summary>Opens the application error log or displays a status when no log exists.</summary>
    private void ViewErrorLogs_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!File.Exists(Program.ErrorLogPath))
        {
            viewModel.SetStatusMessage(Strings.ErrorLogNotFound);
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = Program.ErrorLogPath,
            UseShellExecute = true
        });
    }

    /// <summary>Closes the main application window.</summary>
    private void Exit_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    /// <summary>Displays the configuration window.</summary>
    private async void Configuration_OnClick(object? sender, RoutedEventArgs e)
{
    var configurationWindow = new ConfigurationWindow();
    await configurationWindow.ShowDialog(this);
}


}