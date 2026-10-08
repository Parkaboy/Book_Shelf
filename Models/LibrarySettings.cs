namespace Book_Shelf.Models;

/// <summary>Stores persisted application preferences and the selected library folder.</summary>
public sealed class LibrarySettings
{
    public string? LibraryFolderPath { get; set; }

    public string? LanguageCode { get; set; }
}