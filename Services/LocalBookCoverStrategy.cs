using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Book_Shelf.Models;

namespace Book_Shelf.Services;

public sealed class LocalBookCoverStrategy : IBookCoverStrategy
{
    private static readonly string[] ImageExtensions = [".jpg", ".jpeg", ".png"];

    public async Task<string?> TryResolveAsync(
        Book book,
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(book.FilePath);
        var name = Path.GetFileNameWithoutExtension(book.FilePath);
        if (directory is null)
        {
            return null;
        }

        var coverPath = ImageExtensions
            .Select(extension => Path.Combine(directory, name + extension))
            .Concat(ImageExtensions.Select(extension => Path.Combine(directory, "cover" + extension)))
            .FirstOrDefault(File.Exists);

        if (coverPath is null)
        {
            var imagePaths = Directory.EnumerateFiles(directory)
                .Where(path => ImageExtensions.Contains(
                    Path.GetExtension(path),
                    System.StringComparer.OrdinalIgnoreCase))
                .Take(2)
                .ToArray();
            coverPath = imagePaths.Length == 1 ? imagePaths[0] : null;
        }

        return coverPath is null
            ? null
            : await CoverFileCache.CopyAsync(coverPath, destinationPath, cancellationToken);
    }
}
