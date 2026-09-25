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

public sealed class BookCoverService
{
    private readonly string cacheDirectory;
    private readonly IReadOnlyList<IBookCoverStrategy> strategies;

    public BookCoverService()
        : this(
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Book Shelf",
                "covers"),
            CreateHttpClient())
    {
    }

    public BookCoverService(string cacheDirectory)
        : this(cacheDirectory, CreateHttpClient())
    {
    }

    public BookCoverService(string cacheDirectory, HttpClient httpClient)
        : this(
            cacheDirectory,
            new IBookCoverStrategy[]
            {
                new LocalBookCoverStrategy(),
                new EmbeddedEpubCoverStrategy(),
                new GoogleBooksCoverStrategy(httpClient)
            })
    {
    }

    public BookCoverService(string cacheDirectory, IReadOnlyList<IBookCoverStrategy> strategies)
    {
        this.cacheDirectory = cacheDirectory;
        this.strategies = strategies;
    }

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

    public void Delete(string? coverPath)
    {
        if (!string.IsNullOrWhiteSpace(coverPath) && File.Exists(coverPath))
        {
            File.Delete(coverPath);
        }
    }

    private static string BuildCacheKey(Book book)
    {
        var lookupIdentity = NormalizeIsbn(book.Isbn) ??
            (book.Title.Trim() + "|" + (book.Author ?? string.Empty).Trim()).ToUpperInvariant();
        var bytes = Encoding.UTF8.GetBytes(book.ContentHash + "|" + lookupIdentity);
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    private static string? NormalizeIsbn(string? isbn)
    {
        if (string.IsNullOrWhiteSpace(isbn))
        {
            return null;
        }

        var normalized = new string(isbn.Where(character => char.IsDigit(character) || character is 'X' or 'x').ToArray());
        return normalized.Length is 10 or 13 ? normalized.ToUpperInvariant() : null;
    }

    private static HttpClient CreateHttpClient() => new()
    {
        Timeout = TimeSpan.FromSeconds(8)
    };
}
