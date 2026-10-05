using System;
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
using Microsoft.EntityFrameworkCore;

namespace Book_Shelf.UI;

public sealed class LibraryViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly LibrarySyncService syncService = new();
    private readonly List<BookItemViewModel> allBooks = new();
    private BookItemViewModel? selectedBook;
    private string? searchText;
    private string selectedOrder = Strings.OrderByTitle;
    private string selectedFilter = Strings.FilterAll;
    private string libraryFolder = Strings.NoLibraryFolderSelected;
    private string statusMessage = Strings.ChooseLibraryFolderToBegin;
    private bool isBusy;

    public ObservableCollection<BookItemViewModel> Books { get; } = new();

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
        Strings.FilterPdf,
        Strings.FilterMobi,
        Strings.FilterRtf,
        Strings.FilterTxt
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
            ApplyBookView();
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
            ApplyBookView();
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
            ApplyBookView();
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
        await using (var database = LibraryDbContext.Create())
        {
            await database.Database.MigrateAsync();
        }

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

    public void BeginEditing(BookItemViewModel book)
    {
        book.IsEditing = true;
    }

    public void SelectBook(BookItemViewModel selectedBook)
    {
        if (ReferenceEquals(this.selectedBook, selectedBook))
        {
            return;
        }

        if (this.selectedBook is not null)
        {
            this.selectedBook.IsSelected = false;
        }

        this.selectedBook = selectedBook;
        selectedBook.IsSelected = true;
    }

    public async Task SaveBookAsync(BookItemViewModel book)
    {
        await syncService.SaveBookAsync(book.Book);
        book.IsEditing = false;
        book.ReloadCoverImage();
        ApplyBookView();
        StatusMessage = string.Format(Strings.UpdatedBookFormat, book.Title);
    }

    public async Task DeleteBookAsync(BookItemViewModel book)
    {
        await syncService.DeleteBookAsync(book.Book);
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
        var books = await syncService.SearchAsync();
        TotalBookCount = books.Count;

        foreach (var book in allBooks)
        {
            book.Dispose();
        }

        allBooks.Clear();
        allBooks.AddRange(books.Select(book => new BookItemViewModel(book)));
        selectedBook = null;
        ApplyBookView();
    }

    private void ApplyBookView()
    {
        IEnumerable<BookItemViewModel> filteredBooks = allBooks;
        var search = SearchText?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            filteredBooks = filteredBooks.Where(book =>
                book.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (book.Author?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (book.Isbn?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        filteredBooks = SelectedFilter switch
        {
            var filter when filter == Strings.FilterEpub => filteredBooks.Where(book => book.Format == "EPUB"),
            var filter when filter == Strings.FilterPdf => filteredBooks.Where(book => book.Format == "PDF"),
            var filter when filter == Strings.FilterMobi => filteredBooks.Where(book => book.Format == "MOBI"),
            var filter when filter == Strings.FilterRtf => filteredBooks.Where(book => book.Format == "RTF"),
            var filter when filter == Strings.FilterTxt => filteredBooks.Where(book => book.Format == "TXT"),
            _ => filteredBooks
        };

        filteredBooks = SelectedOrder switch
        {
            var order when order == Strings.OrderByAuthor => filteredBooks.OrderBy(book => book.Author ?? book.Title),
            var order when order == Strings.OrderByDateAdded => filteredBooks.OrderByDescending(book => book.Book.ImportedUtc),
            _ => filteredBooks.OrderBy(book => book.Title)
        };

        var desiredBooks = filteredBooks.ToList();
        var visibleBooks = desiredBooks.ToHashSet();

        for (var index = Books.Count - 1; index >= 0; index--)
        {
            if (!visibleBooks.Contains(Books[index]))
            {
                if (ReferenceEquals(selectedBook, Books[index]))
                {
                    selectedBook.IsSelected = false;
                    selectedBook = null;
                }

                Books.RemoveAt(index);
            }
        }

        for (var targetIndex = 0; targetIndex < desiredBooks.Count; targetIndex++)
        {
            var currentIndex = Books.IndexOf(desiredBooks[targetIndex]);
            if (currentIndex < 0)
            {
                Books.Insert(targetIndex, desiredBooks[targetIndex]);
            }
            else if (currentIndex != targetIndex)
            {
                Books.Move(currentIndex, targetIndex);
            }
        }
    }

    public void Dispose()
    {
        foreach (var book in allBooks)
        {
            book.Dispose();
        }

        allBooks.Clear();
        Books.Clear();
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