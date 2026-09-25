using System.Globalization;
using Book_Shelf.Resources;

namespace Book_Shelf.Services;

public static class LanguageService
{
    private static readonly CultureInfo systemCulture = CultureInfo.CurrentUICulture;

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