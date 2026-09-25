using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    private string selectedOrder = Strings.OrderByTitle;
    private string selectedFilter = Strings.FilterAll;
    private string libraryFolder = Strings.NoLibraryFolderSelected;
    private string statusMessage = Strings.ChooseLibraryFolderToBegin;
    private bool isBusy;

    public ObservableCollection<Book> Books { get; } = new();

    public int TotalBookCount { get; private set; }

    public IReadOnlyList<string> OrderOptions { get; } =
    [
        Strings.OrderByTitle,
        Strings.OrderByAuthor,
        Strings.OrderByDateAdded
    ];

    public IReadOnlyList<string> FilterOptions { get; } =
    [
        Strings.FilterAll,
        Strings.FilterEpub,
        Strings.FilterPdf
    ];

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

    public string SelectedOrder
    {
        get => selectedOrder;
        set
        {
            if (selectedOrder == value)
            {
                return;
            }

            selectedOrder = value;
            OnPropertyChanged();
            _ = LoadBooksAsync();
        }
    }

    public string SelectedFilter
    {
        get => selectedFilter;
        set
        {
            if (selectedFilter == value)
            {
                return;
            }

            selectedFilter = value;
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

    public void SetStatusMessage(string message) => StatusMessage = message;

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
                ? string.Format(Strings.LibraryIsUpToDateWithCount, TotalBookCount)
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

    public async Task DeleteBookAsync(Book book)
    {
        await syncService.DeleteBookAsync(book);
        await LoadBooksAsync();
        StatusMessage = string.Format(Strings.DeletedBookFormat, book.Title);
    }

    public async Task ClearDatabaseAsync()
    {
        await syncService.ClearDatabaseAsync();
        await LoadBooksAsync();
        StatusMessage = Strings.DatabaseCleaned;
    }

    private async Task LoadBooksAsync()
    {
        var books = await syncService.SearchAsync(SearchText);
        TotalBookCount = string.IsNullOrWhiteSpace(SearchText)
            ? books.Count
            : (await syncService.SearchAsync()).Count;
        IEnumerable<Book> filteredBooks = SelectedFilter switch
        {
            var filter when filter == Strings.FilterEpub => books.Where(book => book.Format == "EPUB"),
            var filter when filter == Strings.FilterPdf => books.Where(book => book.Format == "PDF"),
            _ => books
        };

        filteredBooks = SelectedOrder switch
        {
            var order when order == Strings.OrderByAuthor => filteredBooks.OrderBy(book => book.Author ?? book.Title),
            var order when order == Strings.OrderByDateAdded => filteredBooks.OrderByDescending(book => book.ImportedUtc),
            _ => filteredBooks.OrderBy(book => book.Title)
        };

        Books.Clear();
        foreach (var book in filteredBooks)
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