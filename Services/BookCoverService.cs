using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Book_Shelf.Models;

namespace Book_Shelf.Services;

/// <summary>Resolves, caches, and removes book cover images using configured strategies.</summary>
public sealed class BookCoverService
{
    private readonly string cacheDirectory;
    private readonly IReadOnlyList<IBookCoverStrategy> strategies;

    /// <summary>Creates a cover service using the user's local cover cache and default strategies.</summary>
    public BookCoverService()
        : this(
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Book Shelf",
                "covers"),
            CreateHttpClient())
    {
    }

    /// <summary>Creates a cover service using the specified cache directory.</summary>
    public BookCoverService(string cacheDirectory)
        : this(cacheDirectory, CreateHttpClient())
    {
    }

    /// <summary>Creates a cover service with an HTTP client for online cover lookup.</summary>
    public BookCoverService(string cacheDirectory, HttpClient httpClient)
        : this(
            cacheDirectory,
            [
                new LocalBookCoverStrategy(),
                new EmbeddedEpubCoverStrategy(),
                new GoogleBooksCoverStrategy(httpClient)
            ])
    {
    }

    /// <summary>Creates a cover service with the specified cache directory and lookup strategies.</summary>
    public BookCoverService(string cacheDirectory, IReadOnlyList<IBookCoverStrategy> strategies)
    {
        this.cacheDirectory = cacheDirectory;
        this.strategies = strategies;
    }

    /// <summary>Returns a cached or newly resolved cover image path for the book.</summary>
    public async Task<string?> ResolveAsync(
        Book book,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(cacheDirectory);
        var cachedPath = Path.Combine(cacheDirectory, BuildCacheKey(book) + ".cover");
        if (File.Exists(cachedPath))
        {
            return cachedPath;
        }

        foreach (var strategy in strategies)
        {
            var coverPath = await strategy.TryResolveAsync(book, cachedPath, cancellationToken);
            if (coverPath is not null)
            {
                return coverPath;
            }
        }

        return null;
    }

    /// <summary>Deletes a cover file when a path is provided.</summary>
    public void Delete(string? coverPath)
    {
        if (!string.IsNullOrWhiteSpace(coverPath) && File.Exists(coverPath))
        {
            File.Delete(coverPath);
        }
    }

    /// <summary>Builds a stable cache key from the book's content and lookup identity.</summary>
    private static string BuildCacheKey(Book book)
    {
        var lookupIdentity = NormalizeIsbn(book.Isbn) ??
            (book.Title.Trim() + "|" + (book.Author ?? string.Empty).Trim()).ToUpperInvariant();
        var bytes = Encoding.UTF8.GetBytes(book.ContentHash + "|" + lookupIdentity);
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    /// <summary>Normalizes a valid ISBN for cover lookup and cache identity.</summary>
    private static string? NormalizeIsbn(string? isbn)
    {
        if (string.IsNullOrWhiteSpace(isbn))
        {
            return null;
        }

        var normalized = new string(isbn.Where(character => char.IsDigit(character) || character is 'X' or 'x').ToArray());
        return normalized.Length is 10 or 13 ? normalized.ToUpperInvariant() : null;
    }

    /// <summary>Creates the HTTP client used to retrieve online book covers.</summary>
    private static HttpClient CreateHttpClient() => new()
    {
        Timeout = TimeSpan.FromSeconds(8)
    };
}
