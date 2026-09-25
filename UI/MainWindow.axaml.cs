using System;
using System.Diagnostics;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Book_Shelf.Data;
using Book_Shelf.Models;
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
        if (sender is not Control { DataContext: Book book } || !File.Exists(book.FilePath))
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
        if (sender is Control card && card.DataContext is Book book)
        {
            card.Focus();
            viewModel.SelectBook(book);
        }
    }

    private void EditBook_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: Book book })
        {
            viewModel.BeginEditing(book);
        }
    }

    private async void SaveBook_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: Book book })
        {
            await viewModel.SaveBookAsync(book);
        }
    }

    private async void DeleteBook_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: Book book })
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