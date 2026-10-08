using System.Threading;
using System.Threading.Tasks;
using Book_Shelf.Models;

namespace Book_Shelf.Services;

/// <summary>Defines a strategy for resolving a book's cover image.</summary>
public interface IBookCoverStrategy
{
    /// <summary>Attempts to resolve the book cover and cache it at the destination path.</summary>
    Task<string?> TryResolveAsync(Book book, string destinationPath, CancellationToken cancellationToken = default);
}
