using System.Threading;
using System.Threading.Tasks;
using Book_Shelf.Models;

namespace Book_Shelf.Services;

public interface IBookCoverStrategy
{
    Task<string?> TryResolveAsync(Book book, string destinationPath, CancellationToken cancellationToken = default);
}
