using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Book_Shelf.Data;
using Book_Shelf.Models;
using Book_Shelf.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Book_Shelf.Tests;

/// <summary>Verifies library synchronization, book management, and cover resolution behavior.</summary>
public sealed class LibrarySyncServiceTests : IDisposable
{
    private readonly string testRoot = Path.Combine(Path.GetTempPath(), "BookShelfTests", Guid.NewGuid().ToString("N"));
    private readonly string libraryFolder;
    private readonly string databasePath;

    /// <summary>Creates a unique temporary library folder and database for each test.</summary>
    public LibrarySyncServiceTests()
    {
        libraryFolder = Path.Combine(testRoot, "Library");
        databasePath = Path.Combine(testRoot, "books.db");
        Directory.CreateDirectory(libraryFolder);
    }

    /// <summary>Verifies synchronization imports supported book files and ignores unrelated files.</summary>
    [Fact]
    public async Task SynchronizeAsync_ImportsSupportedBookFiles()
    {
        await File.WriteAllTextAsync(Path.Combine(libraryFolder, "The Odyssey.epub"), "epub content");
        await File.WriteAllTextAsync(Path.Combine(libraryFolder, "cover.jpg"), "not a book");

        var service = CreateService();
        var changedBooks = await service.SynchronizeAsync(libraryFolder);
        var books = await service.SearchAsync();

        Assert.Equal(1, changedBooks);
        var book = Assert.Single(books);
        Assert.Equal("The Odyssey", book.Title);
        Assert.Equal("EPUB", book.Format);
    }

    /// <summary>Verifies synchronization imports each additional supported file format.</summary>
    [Theory]
    [InlineData(".mobi", "MOBI")]
    [InlineData(".rtf", "RTF")]
    [InlineData(".txt", "TXT")]
    public async Task SynchronizeAsync_ImportsAdditionalFormats(string extension, string expectedFormat)
    {
        await File.WriteAllTextAsync(Path.Combine(libraryFolder, "New format" + extension), "book content");
        var service = CreateService(strategies: Array.Empty<IBookCoverStrategy>());

        var changedBooks = await service.SynchronizeAsync(libraryFolder);
        var book = Assert.Single(await service.SearchAsync());

        Assert.Equal(1, changedBooks);
        Assert.Equal(expectedFormat, book.Format);
    }

    /// <summary>Verifies catalog search matches book titles without case sensitivity.</summary>
    [Fact]
    public async Task SearchAsync_IsCaseInsensitive()
    {
        await File.WriteAllTextAsync(Path.Combine(libraryFolder, "Hask.epub"), "epub content");
        var service = CreateService();

        await service.SynchronizeAsync(libraryFolder);

        var books = await service.SearchAsync("hask");

        Assert.Single(books);
        Assert.Equal("Hask", books[0].Title);
    }

    /// <summary>Verifies repeating an unchanged scan does not create duplicate records.</summary>
    [Fact]
    public async Task SynchronizeAsync_DoesNotDuplicateUnchangedBooks()
    {
        await File.WriteAllTextAsync(Path.Combine(libraryFolder, "Dune.pdf"), "pdf content");
        var service = CreateService();

        await service.SynchronizeAsync(libraryFolder);
        var changedBooks = await service.SynchronizeAsync(libraryFolder);
        var books = await service.SearchAsync();

        Assert.Equal(0, changedBooks);
        Assert.Single(books);
    }

    /// <summary>Verifies edited metadata updates its existing database record.</summary>
    [Fact]
    public async Task SaveBookAsync_UpdatesExistingBookWithoutCreatingDuplicate()
    {
        var bookPath = Path.Combine(libraryFolder, "Dune.pdf");
        await File.WriteAllTextAsync(bookPath, "pdf content");
        var service = CreateService(strategies: new IBookCoverStrategy[]
        {
            new LocalBookCoverStrategy()
        });

        await service.SynchronizeAsync(libraryFolder);
        var book = Assert.Single(await service.SearchAsync());
        book.Title = "Updated Dune";
        book.Author = "Frank Herbert";

        await service.SaveBookAsync(book);

        var books = await service.SearchAsync();
        var savedBook = Assert.Single(books);
        Assert.Equal(book.Id, savedBook.Id);
        Assert.Equal("Updated Dune", savedBook.Title);
        Assert.Equal("Frank Herbert", savedBook.Author);
    }

    /// <summary>Verifies synchronization finds and caches a cover matching the book filename.</summary>
    [Fact]
    public async Task SynchronizeAsync_CachesMatchingLocalCover()
    {
        var bookPath = Path.Combine(libraryFolder, "Dune.pdf");
        var coverPath = Path.Combine(libraryFolder, "Dune.jpg");
        await File.WriteAllTextAsync(bookPath, "pdf content");
        await File.WriteAllTextAsync(coverPath, "cover content");
        var service = CreateService();

        await service.SynchronizeAsync(libraryFolder);
        var book = Assert.Single(await service.SearchAsync());

        Assert.NotNull(book.CoverPath);
        Assert.True(File.Exists(book.CoverPath));
        Assert.Equal("cover content", await File.ReadAllTextAsync(book.CoverPath));
        Assert.NotEqual(coverPath, book.CoverPath);
    }

    /// <summary>Verifies the only image in a book directory is selected as its cover.</summary>
    [Fact]
    public async Task SynchronizeAsync_UsesTheOnlyImageInBookDirectory()
    {
        var bookPath = Path.Combine(libraryFolder, "Dune.pdf");
        var coverPath = Path.Combine(libraryFolder, "front-cover.jpg");
        await File.WriteAllTextAsync(bookPath, "pdf content");
        await File.WriteAllTextAsync(coverPath, "cover content");
        var service = CreateService(strategies: new IBookCoverStrategy[]
        {
            new LocalBookCoverStrategy()
        });

        await service.SynchronizeAsync(libraryFolder);

        var book = Assert.Single(await service.SearchAsync());
        Assert.NotNull(book.CoverPath);
        Assert.Equal("cover content", await File.ReadAllTextAsync(book.CoverPath));
    }

    /// <summary>Verifies a later scan finds a cover added after the book was imported.</summary>
    [Fact]
    public async Task SynchronizeAsync_FindsCoverAddedAfterBookWasImported()
    {
        var bookPath = Path.Combine(libraryFolder, "Dune.pdf");
        var coverPath = Path.Combine(libraryFolder, "Dune.jpg");
        await File.WriteAllTextAsync(bookPath, "pdf content");
        var service = CreateService(strategies: new IBookCoverStrategy[]
        {
            new LocalBookCoverStrategy()
        });

        await service.SynchronizeAsync(libraryFolder);
        await File.WriteAllTextAsync(coverPath, "cover content");
        await service.SynchronizeAsync(libraryFolder);

        var book = Assert.Single(await service.SearchAsync());
        Assert.NotNull(book.CoverPath);
        Assert.Equal("cover content", await File.ReadAllTextAsync(book.CoverPath));
    }

    /// <summary>Verifies an ISBN edit can resolve and cache a cover from Google Books.</summary>
    [Fact]
    public async Task SynchronizeAsync_UsesGoogleBooksCoverByIsbn()
    {
        var bookPath = Path.Combine(libraryFolder, "Dune.pdf");
        await File.WriteAllTextAsync(bookPath, "pdf content");
        var handler = new GoogleBooksHandler();
        var service = CreateService(handler);

        await service.SynchronizeAsync(libraryFolder);
        var book = Assert.Single(await service.SearchAsync());
        book.Isbn = "978-0-441-17271-9";
        await service.SaveBookAsync(book);

        Assert.NotNull(book.CoverPath);
        Assert.True(File.Exists(book.CoverPath));
        Assert.Equal("fake image", await File.ReadAllTextAsync(book.CoverPath));
        Assert.Contains("isbn", handler.LastRequestUri);
        Assert.Contains("9780441172719", handler.LastRequestUri);
    }

    /// <summary>Verifies synchronization removes records for supported files deleted from the folder.</summary>
    [Fact]
    public async Task SynchronizeAsync_RemovesBooksDeletedFromFolder()
    {
        var bookPath = Path.Combine(libraryFolder, "To remove.epub");
        await File.WriteAllTextAsync(bookPath, "epub content");
        var service = CreateService();

        await service.SynchronizeAsync(libraryFolder);
        File.Delete(bookPath);
        var changedBooks = await service.SynchronizeAsync(libraryFolder);

        Assert.Equal(1, changedBooks);
        Assert.Empty(await service.SearchAsync());
    }

    /// <summary>Verifies deleting a book removes its record and cover as well as its source file.</summary>
    [Fact]
    public async Task DeleteBookAsync_RemovesDatabaseEntryAndCachedCoverButKeepsBookFile()
    {
        var bookPath = Path.Combine(libraryFolder, "To remove.epub");
        var coverPath = Path.Combine(libraryFolder, "cover.jpg");
        await File.WriteAllTextAsync(bookPath, "epub content");
        await File.WriteAllTextAsync(coverPath, "cover content");
        var service = CreateService(strategies: new IBookCoverStrategy[]
        {
            new LocalBookCoverStrategy()
        });

        await service.SynchronizeAsync(libraryFolder);
        var book = Assert.Single(await service.SearchAsync());
        Assert.NotNull(book.CoverPath);
        Assert.True(File.Exists(book.CoverPath));

        await service.DeleteBookAsync(book);

        Assert.Empty(await service.SearchAsync());
        Assert.False(File.Exists(bookPath));
        Assert.False(File.Exists(book.CoverPath));
    }

    /// <summary>Verifies clearing the catalog removes records and covers but retains source files.</summary>
    [Fact]
    public async Task ClearDatabaseAsync_RemovesRecordsAndCachedCoversButKeepsBookFiles()
    {
        var bookPath = Path.Combine(libraryFolder, "Keep me.epub");
        var coverPath = Path.Combine(libraryFolder, "cover.jpg");
        await File.WriteAllTextAsync(bookPath, "epub content");
        await File.WriteAllTextAsync(coverPath, "cover content");
        var service = CreateService(strategies: new IBookCoverStrategy[]
        {
            new LocalBookCoverStrategy()
        });

        await service.SynchronizeAsync(libraryFolder);
        var book = Assert.Single(await service.SearchAsync());
        Assert.NotNull(book.CoverPath);
        Assert.True(File.Exists(book.CoverPath));

        await service.ClearDatabaseAsync();

        Assert.Empty(await service.SearchAsync());
        Assert.True(File.Exists(bookPath));
        Assert.False(File.Exists(book.CoverPath));
    }

    /// <summary>Removes the temporary files and database created for the current test.</summary>
    public void Dispose()
    {
        if (Directory.Exists(testRoot))
        {
            Directory.Delete(testRoot, recursive: true);
        }
    }

    /// <summary>Builds a sync service connected to this test's isolated database and cover cache.</summary>
    private LibrarySyncService CreateService(
        HttpMessageHandler? handler = null,
        IReadOnlyList<IBookCoverStrategy>? strategies = null)
    {
        handler ??= new GoogleBooksHandler();
        return new LibrarySyncService(() =>
        {
            var options = new DbContextOptionsBuilder<LibraryDbContext>()
                .UseSqlite($"Data Source={databasePath};Pooling=False")
                .Options;
            return new LibraryDbContext(options);
        }, strategies is null
            ? new BookCoverService(Path.Combine(testRoot, "covers"), new HttpClient(handler))
            : new BookCoverService(Path.Combine(testRoot, "covers"), strategies));
    }

    /// <summary>Returns deterministic Google Books API and image responses for cover tests.</summary>
    private sealed class GoogleBooksHandler : HttpMessageHandler
    {
        public string? LastRequestUri { get; private set; }

        /// <summary>Records API queries and returns stubbed metadata or cover image responses.</summary>
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            System.Threading.CancellationToken cancellationToken)
        {
            if (request.RequestUri?.AbsolutePath.EndsWith("/volumes", StringComparison.Ordinal) == true)
            {
                LastRequestUri = request.RequestUri.ToString();
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                    "{\"items\":[{\"volumeInfo\":{\"imageLinks\":{\"thumbnail\":\"https://books.google.test/cover.jpg\"}}}]}",
                    Encoding.UTF8,
                    "application/json")
                };
                return Task.FromResult(response);
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("fake image", Encoding.UTF8, "image/jpeg")
            });
        }
    }
}