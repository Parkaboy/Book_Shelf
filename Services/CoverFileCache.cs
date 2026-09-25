using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Book_Shelf.Services;

internal static class CoverFileCache
{
    public static async Task<string> CopyAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        var temporaryPath = destinationPath + ".tmp";
        try
        {
            await using (var source = File.OpenRead(sourcePath))
            await using (var destination = File.Create(temporaryPath))
            {
                await source.CopyToAsync(destination, cancellationToken);
            }

            File.Move(temporaryPath, destinationPath, overwrite: true);
            return destinationPath;
        }
        catch
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }

            throw;
        }
    }
}
