using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Book_Shelf.Data;
using Book_Shelf.Models;
using Microsoft.EntityFrameworkCore;

namespace Book_Shelf.Services;

public sealed class LibrarySyncService
{
    private readonly Func<LibraryDbContext> contextFactory;
    private readonly BookCoverService coverService;

    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".epub",
        ".pdf"
    };

    public LibrarySyncService()
        : this(LibraryDbContext.Create)
    {
    }

    public LibrarySyncService(Func<LibraryDbContext> contextFactory)
        : this(contextFactory, new BookCoverService())
    {
    }

    public LibrarySyncService(Func<LibraryDbContext> contextFactory, BookCoverService coverService)
    {
        this.contextFactory = contextFactory;
        this.coverService = coverService;
    }

    public async Task<int> SynchronizeAsync(string folderPath, CancellationToken cancellationToken = default)
    {
        var fullFolderPath = Path.GetFullPath(folderPath);
        if (!Directory.Exists(fullFolderPath))
        {
            throw new DirectoryNotFoundException($"The library folder does not exist: {fullFolderPath}");
        }

        var files = Directory.EnumerateFiles(fullFolderPath, "*.*", SearchOption.AllDirectories)
            .Where(filePath => SupportedExtensions.Contains(Path.GetExtension(filePath)))
            .Select(Path.GetFullPath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        await using var database = contextFactory();
        await database.Database.MigrateAsync(cancellationToken);

        var folderPrefix = fullFolderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        var existingBooks = await database.Books
            .Where(book => book.FilePath.StartsWith(folderPrefix))
            .ToListAsync(cancellationToken);
        var existingByPath = existingBooks.ToDictionary(book => book.FilePath, StringComparer.OrdinalIgnoreCase);
        var changedBooks = 0;

        foreach (var filePath in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fileInfo = new FileInfo(filePath);
            var lastModifiedUtc = fileInfo.LastWriteTimeUtc;
            var book = existingByPath.GetValueOrDefault(filePath);
            var contentHash = await ComputeHashAsync(filePath, cancellationToken);

            if (book is not null && book.FileSize == fileInfo.Length &&
                book.FileLastModifiedUtc == lastModifiedUtc && book.ContentHash == contentHash &&
                book.CoverPath is not null && File.Exists(book.CoverPath))
            {
                continue;
            }

            var now = DateTime.UtcNow;

            if (book is null)
            {
                book = new Book
                {
                    Title = Path.GetFileNameWithoutExtension(filePath),
                    FilePath = filePath,
                    Format = Path.GetExtension(filePath).TrimStart('.').ToUpperInvariant(),
                    ImportedUtc = now,
                    UpdatedUtc = now,
                    ContentHash = contentHash,
                    FileSize = fileInfo.Length,
                    FileLastModifiedUtc = lastModifiedUtc
                };
                database.Books.Add(book);
            }
            else
            {
                coverService.Delete(book.CoverPath);
                book.FileSize = fileInfo.Length;
                book.FileLastModifiedUtc = lastModifiedUtc;
                book.ContentHash = contentHash;
                book.UpdatedUtc = now;
            }

            book.CoverPath = await coverService.ResolveAsync(book, cancellationToken);

            changedBooks++;
        }

        foreach (var book in existingBooks.Where(book => !files.Contains(book.FilePath)))
        {
            coverService.Delete(book.CoverPath);
            database.Books.Remove(book);
            changedBooks++;
        }

        if (changedBooks > 0)
        {
            await database.SaveChangesAsync(cancellationToken);
        }

        return changedBooks;
    }

    public async Task<IReadOnlyList<Book>> SearchAsync(string? searchText = null, CancellationToken cancellationToken = default)
    {
        await using var database = contextFactory();
        var query = database.Books.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var term = searchText.Trim();
            query = query.Where(book => book.Title.Contains(term) ||
                                        (book.Author != null && book.Author.Contains(term)) ||
                                        (book.Isbn != null && book.Isbn.Contains(term)));
        }

        return await query.OrderBy(book => book.Title).ToListAsync(cancellationToken);
    }

    public async Task SaveBookAsync(Book editedBook, CancellationToken cancellationToken = default)
    {
        await using var database = contextFactory();
        var book = await database.Books.SingleAsync(candidate => candidate.Id == editedBook.Id, cancellationToken);
        book.Title = editedBook.Title;
        book.Author = editedBook.Author;
        book.Isbn = editedBook.Isbn;
        book.PageCount = editedBook.PageCount;
        var previousCoverPath = book.CoverPath;
        book.CoverPath = await coverService.ResolveAsync(book, cancellationToken);
        if (!string.Equals(previousCoverPath, book.CoverPath, StringComparison.OrdinalIgnoreCase))
        {
            coverService.Delete(previousCoverPath);
        }
        book.UpdatedUtc = DateTime.UtcNow;
        await database.SaveChangesAsync(cancellationToken);
        editedBook.CoverPath = book.CoverPath;
    }

    public async Task DeleteBookAsync(Book bookToDelete, CancellationToken cancellationToken = default)
    {
        await using var database = contextFactory();
        var book = await database.Books.SingleAsync(candidate => candidate.Id == bookToDelete.Id, cancellationToken);
        coverService.Delete(book.CoverPath);
        database.Books.Remove(book);
        await database.SaveChangesAsync(cancellationToken);

        if (File.Exists(book.FilePath))
        {
            File.Delete(book.FilePath);
        }
    }

    public async Task ClearDatabaseAsync(CancellationToken cancellationToken = default)
    {
        await using var database = contextFactory();
        var books = await database.Books.ToListAsync(cancellationToken);
        foreach (var book in books)
        {
            coverService.Delete(book.CoverPath);
        }

        database.Books.RemoveRange(books);
        await database.SaveChangesAsync(cancellationToken);
    }

    private static async Task<string> ComputeHashAsync(string filePath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(filePath);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash);
    }
}