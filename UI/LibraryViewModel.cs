using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Book_Shelf.Data;
using Book_Shelf.Models;
using Book_Shelf.Resources;
using Book_Shelf.Services;

namespace Book_Shelf.UI;

public sealed class LibraryViewModel : INotifyPropertyChanged
{
    private readonly LibrarySyncService syncService = new();
    private string? searchText;
    private string libraryFolder = Strings.NoLibraryFolderSelected;
    private string statusMessage = Strings.ChooseLibraryFolderToBegin;
    private bool isBusy;

    public ObservableCollection<Book> Books { get; } = new();

    public string? SearchText
    {
        get => searchText;
        set
        {
            if (searchText == value)
            {
                return;
            }

            searchText = value;
            OnPropertyChanged();
            _ = LoadBooksAsync();
        }
    }

    public string LibraryFolder
    {
        get => libraryFolder;
        private set => SetField(ref libraryFolder, value);
    }

    public string StatusMessage
    {
        get => statusMessage;
        private set => SetField(ref statusMessage, value);
    }

    public bool IsBusy
    {
        get => isBusy;
        private set => SetField(ref isBusy, value);
    }

    public async Task InitializeAsync()
    {
        var settings = await new LibrarySettingsStore().LoadAsync();
        if (string.IsNullOrWhiteSpace(settings.LibraryFolderPath))
        {
            await LoadBooksAsync();
            return;
        }

        LibraryFolder = settings.LibraryFolderPath;
        if (Directory.Exists(LibraryFolder))
        {
            await SynchronizeAsync();
        }
        else
        {
            StatusMessage = Strings.SavedLibraryFolderUnavailable;
            await LoadBooksAsync();
        }
    }

    public async Task SetLibraryFolderAsync(string folderPath)
    {
        var settings = new LibrarySettings { LibraryFolderPath = Path.GetFullPath(folderPath) };
        await new LibrarySettingsStore().SaveAsync(settings);
        LibraryFolder = settings.LibraryFolderPath;
        await SynchronizeAsync();
    }

    public async Task SynchronizeAsync()
    {
        if (LibraryFolder == Strings.NoLibraryFolderSelected || !Directory.Exists(LibraryFolder))
        {
            StatusMessage = Strings.ChooseExistingLibraryFolderFirst;
            return;
        }

        IsBusy = true;
        StatusMessage = Strings.ScanningLibrary;
        try
        {
            var changedBooks = await syncService.SynchronizeAsync(LibraryFolder);
            await LoadBooksAsync();
            StatusMessage = changedBooks == 0
                ? Strings.LibraryIsUpToDate
                : string.Format(Strings.LibraryUpdatedFormat, changedBooks);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void BeginEditing(Book book)
    {
        book.IsEditing = true;
    }

    public void SelectBook(Book selectedBook)
    {
        foreach (var book in Books)
        {
            book.IsSelected = ReferenceEquals(book, selectedBook);
        }
    }

    public async Task SaveBookAsync(Book book)
    {
        await syncService.SaveBookAsync(book);
        book.IsEditing = false;
        StatusMessage = string.Format(Strings.UpdatedBookFormat, book.Title);
    }

    private async Task LoadBooksAsync()
    {
        var books = await syncService.SearchAsync(SearchText);
        Books.Clear();
        foreach (var book in books)
        {
            Books.Add(book);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value))
        {
            return;
        }

        field = value;
        OnPropertyChanged(propertyName);
    }
}