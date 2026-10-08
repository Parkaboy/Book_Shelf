using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using Book_Shelf.Models;

namespace Book_Shelf.Data;

/// <summary>Loads and saves user preferences in the local application data folder.</summary>
public sealed class LibrarySettingsStore
{
    private readonly string settingsPath;

    /// <summary>Creates the settings store and ensures its parent directory exists.</summary>
    public LibrarySettingsStore()
    {
        var applicationDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var bookShelfPath = Path.Combine(applicationDataPath, "Book Shelf");
        Directory.CreateDirectory(bookShelfPath);
        settingsPath = Path.Combine(bookShelfPath, "settings.json");
    }

    /// <summary>Asynchronously reads saved preferences or returns defaults if none exist.</summary>
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

    /// <summary>Synchronously reads saved preferences or returns defaults if none exist.</summary>
    public LibrarySettings Load()
    {
        if (!File.Exists(settingsPath))
        {
            return new LibrarySettings();
        }

        return JsonSerializer.Deserialize<LibrarySettings>(File.ReadAllText(settingsPath))
            ?? new LibrarySettings();
    }

    /// <summary>Asynchronously persists the supplied user preferences.</summary>
    public async Task SaveAsync(LibrarySettings settings, CancellationToken cancellationToken = default)
    {
        await using var stream = File.Create(settingsPath);
        await JsonSerializer.SerializeAsync(stream, settings, cancellationToken: cancellationToken);
    }
}