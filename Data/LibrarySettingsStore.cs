using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using Book_Shelf.Models;

namespace Book_Shelf.Data;

public sealed class LibrarySettingsStore
{
    private readonly string settingsPath;

    public LibrarySettingsStore()
    {
        var applicationDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var bookShelfPath = Path.Combine(applicationDataPath, "Book Shelf");
        Directory.CreateDirectory(bookShelfPath);
        settingsPath = Path.Combine(bookShelfPath, "settings.json");
    }

    public async Task<LibrarySettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(settingsPath))
        {
            return new LibrarySettings();
        }

        await using var stream = File.OpenRead(settingsPath);
        return await JsonSerializer.DeserializeAsync<LibrarySettings>(stream, cancellationToken: cancellationToken)
            ?? new LibrarySettings();
    }

    public async Task SaveAsync(LibrarySettings settings, CancellationToken cancellationToken = default)
    {
        await using var stream = File.Create(settingsPath);
        await JsonSerializer.SerializeAsync(stream, settings, cancellationToken: cancellationToken);
    }
}