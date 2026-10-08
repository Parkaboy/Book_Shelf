using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Book_Shelf.Models;

namespace Book_Shelf.Services;

/// <summary>Looks up book covers through the Google Books API.</summary>
public sealed class GoogleBooksCoverStrategy : IBookCoverStrategy
{
    private readonly HttpClient httpClient;

    /// <summary>Creates the strategy with the HTTP client used for API and image requests.</summary>
    public GoogleBooksCoverStrategy(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    /// <summary>Finds and downloads a matching cover image for a book.</summary>
    public async Task<string?> TryResolveAsync(
        Book book,
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(book);
        if (query is null)
        {
            return null;
        }

        try
        {
            var requestUri = "https://www.googleapis.com/books/v1/volumes?q=" + Uri.EscapeDataString(query) + "&maxResults=1";
            var response = await httpClient.GetFromJsonAsync<GoogleBooksResponse>(requestUri, cancellationToken);
            var imageUrl = response?.Items?.FirstOrDefault()?.VolumeInfo?.ImageLinks?.SelectBest();
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return null;
            }

            using var imageResponse = await httpClient.GetAsync(NormalizeImageUrl(imageUrl), cancellationToken);
            imageResponse.EnsureSuccessStatusCode();
            var temporaryPath = destinationPath + ".tmp";
            try
            {
                await using (var source = await imageResponse.Content.ReadAsStreamAsync(cancellationToken))
                await using (var destination = System.IO.File.Create(temporaryPath))
                {
                    await source.CopyToAsync(destination, cancellationToken);
                }

                System.IO.File.Move(temporaryPath, destinationPath, overwrite: true);
                return destinationPath;
            }
            catch
            {
                if (System.IO.File.Exists(temporaryPath))
                {
                    System.IO.File.Delete(temporaryPath);
                }

                throw;
            }
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Builds an ISBN- or title-based Google Books search query.</summary>
    private static string? BuildQuery(Book book)
    {
        var isbn = NormalizeIsbn(book.Isbn);
        if (!string.IsNullOrWhiteSpace(isbn))
        {
            return "isbn:" + isbn;
        }

        var title = book.Title.Trim();
        if (title.Length == 0)
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(book.Author)
            ? "intitle:" + title
            : "intitle:" + title + " inauthor:" + book.Author.Trim();
    }

    /// <summary>Normalizes a valid ISBN for use in a Google Books query.</summary>
    private static string? NormalizeIsbn(string? isbn)
    {
        if (string.IsNullOrWhiteSpace(isbn))
        {
            return null;
        }

        var normalized = new string(isbn.Where(character => char.IsDigit(character) || character is 'X' or 'x').ToArray());
        return normalized.Length is 10 or 13 ? normalized.ToUpperInvariant() : null;
    }

    /// <summary>Upgrades insecure HTTP image URLs to HTTPS.</summary>
    private static string NormalizeImageUrl(string imageUrl) =>
        imageUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            ? "https://" + imageUrl[7..]
            : imageUrl;

    /// <summary>Represents the top-level response returned by Google Books.</summary>
    private sealed class GoogleBooksResponse
    {
        [JsonPropertyName("items")]
        public GoogleBookItem[]? Items { get; set; }
    }

    /// <summary>Represents one book item in a Google Books response.</summary>
    private sealed class GoogleBookItem
    {
        [JsonPropertyName("volumeInfo")]
        public GoogleVolumeInfo? VolumeInfo { get; set; }
    }

    /// <summary>Contains volume metadata relevant to locating a cover.</summary>
    private sealed class GoogleVolumeInfo
    {
        [JsonPropertyName("imageLinks")]
        public GoogleImageLinks? ImageLinks { get; set; }
    }

    /// <summary>Contains the available cover image URLs for a volume.</summary>
    private sealed class GoogleImageLinks
    {
        [JsonPropertyName("extraLarge")]
        public string? ExtraLarge { get; set; }

        [JsonPropertyName("large")]
        public string? Large { get; set; }

        [JsonPropertyName("medium")]
        public string? Medium { get; set; }

        [JsonPropertyName("thumbnail")]
        public string? Thumbnail { get; set; }

        [JsonPropertyName("smallThumbnail")]
        public string? SmallThumbnail { get; set; }

        /// <summary>Returns the largest available cover image URL.</summary>
        public string? SelectBest() => ExtraLarge ?? Large ?? Medium ?? Thumbnail ?? SmallThumbnail;
    }
}
