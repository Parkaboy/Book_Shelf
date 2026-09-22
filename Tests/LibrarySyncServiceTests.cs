using System;
using System.IO;
using System.Threading.Tasks;
using Book_Shelf.Data;
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

    private LibrarySyncService CreateService()
    {
        return new LibrarySyncService(() =>
        {
            var options = new DbContextOptionsBuilder<LibraryDbContext>()
                .UseSqlite($"Data Source={databasePath};Pooling=False")
                .Options;
            return new LibraryDbContext(options);
        });
    }
}