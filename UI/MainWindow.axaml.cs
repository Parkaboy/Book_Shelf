using System;
using System.Diagnostics;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Book_Shelf.Data;
using Book_Shelf.Resources;
using Book_Shelf.UI;

namespace Book_Shelf;

public partial class MainWindow : Window
{
    private readonly LibraryViewModel viewModel = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = viewModel;
        Opened += OnOpened;
        Closed += (_, _) => viewModel.Dispose();
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        Opened -= OnOpened;
        await viewModel.InitializeAsync();
    }

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

    private async void Rescan_OnClick(object? sender, RoutedEventArgs e)
    {
        await viewModel.SynchronizeAsync();
    }

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

    private async void CleanDatabase_OnClick(object? sender, RoutedEventArgs e)
    {
        await viewModel.ClearDatabaseAsync();
    }

    private void BookCard_DoubleTapped(object? sender, RoutedEventArgs e)
    {
        OpenBook(sender);
    }

    private void BookCard_GotFocus(object? sender, FocusChangedEventArgs e)
    {
        if (sender is Control { DataContext: BookItemViewModel book })
        {
            viewModel.SelectBook(book);
        }
    }

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

    private void BookCard_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (sender is Control card && card.DataContext is BookItemViewModel book)
        {
            card.Focus();
            viewModel.SelectBook(book);
        }
    }

    private void EditBook_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: BookItemViewModel book })
        {
            viewModel.BeginEditing(book);
        }
    }

    private async void SaveBook_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: BookItemViewModel book })
        {
            await viewModel.SaveBookAsync(book);
        }
    }

    private async void DeleteBook_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: BookItemViewModel book })
        {
            await viewModel.DeleteBookAsync(book);
        }
    }

    private async void About_OnClick(object? sender, RoutedEventArgs e)
    {
        var aboutWindow = new AboutWindow();
        await aboutWindow.ShowDialog(this);
    }

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

    private void Exit_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }


    private async void Configuration_OnClick(object? sender, RoutedEventArgs e)
{
    var configurationWindow = new ConfigurationWindow();
    await configurationWindow.ShowDialog(this);
}


}