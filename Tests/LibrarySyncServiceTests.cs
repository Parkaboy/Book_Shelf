using System;
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

public sealed class LibrarySyncServiceTests : IDisposable
{
    private readonly string testRoot = Path.Combine(Path.GetTempPath(), "BookShelfTests", Guid.NewGuid().ToString("N"));
    private readonly string libraryFolder;
    private readonly string databasePath;

    public LibrarySyncServiceTests()
    {
        libraryFolder = Path.Combine(testRoot, "Library");
        databasePath = Path.Combine(testRoot, "books.db");
        Directory.CreateDirectory(libraryFolder);
    }

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

    public void Dispose()
    {
        if (Directory.Exists(testRoot))
        {
            Directory.Delete(testRoot, recursive: true);
        }
    }

    private LibrarySyncService CreateService(HttpMessageHandler? handler = null)
    {
        handler ??= new GoogleBooksHandler();
        return new LibrarySyncService(() =>
        {
            var options = new DbContextOptionsBuilder<LibraryDbContext>()
                .UseSqlite($"Data Source={databasePath};Pooling=False")
                .Options;
            return new LibraryDbContext(options);
        }, new BookCoverService(
            Path.Combine(testRoot, "covers"),
            new HttpClient(handler)));
    }

    private sealed class GoogleBooksHandler : HttpMessageHandler
    {
        public string? LastRequestUri { get; private set; }

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