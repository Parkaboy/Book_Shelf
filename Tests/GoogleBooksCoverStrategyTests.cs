using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Book_Shelf.Models;
using Book_Shelf.Services;
using Xunit;

namespace Book_Shelf.Tests;

/// <summary>Verifies the Google Books strategy avoids repeated requests when connectivity fails.</summary>
public sealed class GoogleBooksCoverStrategyTests
{
    /// <summary>Verifies a connection failure disables subsequent Google Books requests.</summary>
    [Fact]
    public async Task TryResolveAsync_DoesNotRetryAfterConnectionFailure()
    {
        var handler = new UnavailableHandler();
        var strategy = new GoogleBooksCoverStrategy(new HttpClient(handler));
        var book = new Book
        {
            Title = "Test book",
            FilePath = "test-book.epub",
            Format = "EPUB",
            ContentHash = string.Empty
        };

        Assert.Null(await strategy.TryResolveAsync(book, "cover.cache"));
        Assert.Null(await strategy.TryResolveAsync(book, "cover.cache"));

        Assert.Equal(1, handler.RequestCount);
    }

    private sealed class UnavailableHandler : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            throw new HttpRequestException("No internet connection.");
        }
    }
}
