using System.Globalization;
using Book_Shelf.Resources;

namespace Book_Shelf.Services;

/// <summary>Applies a selected or system-default UI culture to localized resources.</summary>
public static class LanguageService
{
    private static readonly CultureInfo systemCulture = CultureInfo.CurrentUICulture;

    /// <summary>Sets the current UI culture from a language code, or restores the system culture.</summary>
    public static void Apply(string? languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            Strings.Culture = null;
            CultureInfo.DefaultThreadCurrentUICulture = null;
            CultureInfo.CurrentUICulture = systemCulture;
            return;
        }

        var culture = CultureInfo.GetCultureInfo(languageCode);
        Strings.Culture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }
}