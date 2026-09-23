using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Book_Shelf.Models;

namespace Book_Shelf.Services;

public sealed class EmbeddedEpubCoverStrategy : IBookCoverStrategy
{
    private static readonly string[] ImageExtensions = [".jpg", ".jpeg", ".png"];

    public async Task<string?> TryResolveAsync(
        Book book,
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(Path.GetExtension(book.FilePath), ".epub", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        try
        {
            using var archive = ZipFile.OpenRead(book.FilePath);
            var imageEntry = archive.Entries
                .Where(entry => ImageExtensions.Contains(Path.GetExtension(entry.FullName), StringComparer.OrdinalIgnoreCase))
                .OrderBy(entry => entry.FullName.Contains("cover", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(entry => entry.Length)
                .FirstOrDefault();

            if (imageEntry is null)
            {
                return null;
            }

            var temporaryPath = destinationPath + ".tmp";
            await using (var source = imageEntry.Open())
            await using (var destination = File.Create(temporaryPath))
            {
                await source.CopyToAsync(destination, cancellationToken);
            }

            File.Move(temporaryPath, destinationPath, overwrite: true);
            return destinationPath;
        }
        catch (InvalidDataException)
        {
            return null;
        }
    }
}
