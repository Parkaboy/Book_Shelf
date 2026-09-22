using System;
using System.Diagnostics;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Book_Shelf.Models;
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
            Title = "Choose your book library"
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

    private void Exit_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}