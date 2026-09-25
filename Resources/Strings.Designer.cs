#nullable enable

using System.Globalization;
using System.Resources;

namespace Book_Shelf.Resources;

public static class Strings
{
    private static readonly ResourceManager resourceManager = new(
        "Book_Shelf.Resources.Strings",
        typeof(Strings).Assembly);

    public static ResourceManager ResourceManager => resourceManager;

    public static CultureInfo? Culture { get; set; }

    public static string AboutBookShelf => GetString(nameof(AboutBookShelf));
    public static string AboutCopyright => GetString(nameof(AboutCopyright));
    public static string AboutDescription => GetString(nameof(AboutDescription));
    public static string AboutLicense => GetString(nameof(AboutLicense));
    public static string AboutTechnology => GetString(nameof(AboutTechnology));
    public static string AboutVersion => GetString(nameof(AboutVersion));
    public static string Author => GetString(nameof(Author));
    public static string Book => GetString(nameof(Book));
    public static string BookShelf => GetString(nameof(BookShelf));
    public static string BuyMeACoffeeMessage => GetString(nameof(BuyMeACoffeeMessage));
    public static string BuyMeACoffeeUrl => GetString(nameof(BuyMeACoffeeUrl));
    public static string Close => GetString(nameof(Close));
    public static string ChooseExistingLibraryFolderFirst => GetString(nameof(ChooseExistingLibraryFolderFirst));
    public static string ChooseLibraryFolder => GetString(nameof(ChooseLibraryFolder));
    public static string ChooseYourBookLibrary => GetString(nameof(ChooseYourBookLibrary));
    public static string ChooseLibraryFolderToBegin => GetString(nameof(ChooseLibraryFolderToBegin));
    public static string EditDetails => GetString(nameof(EditDetails));
    public static string Exit => GetString(nameof(Exit));
    public static string Isbn => GetString(nameof(Isbn));
    public static string Library => GetString(nameof(Library));
    public static string LibraryIsUpToDate => GetString(nameof(LibraryIsUpToDate));
    public static string LibraryUpdatedFormat => GetString(nameof(LibraryUpdatedFormat));
    public static string OrderBy => GetString(nameof(OrderBy));
    public static string OrderByTitle => GetString(nameof(OrderByTitle));
    public static string OrderByAuthor => GetString(nameof(OrderByAuthor));
    public static string OrderByDateAdded => GetString(nameof(OrderByDateAdded));
    public static string Filter => GetString(nameof(Filter));
    public static string FilterAll => GetString(nameof(FilterAll));
    public static string FilterEpub => GetString(nameof(FilterEpub));
    public static string FilterPdf => GetString(nameof(FilterPdf));
    public static string Light => GetString(nameof(Light));
    public static string Dark => GetString(nameof(Dark));
    public static string System => GetString(nameof(System));
    public static string Delete => GetString(nameof(Delete));
    public static string DeletedBookFormat => GetString(nameof(DeletedBookFormat));
    public static string ExportDatabase => GetString(nameof(ExportDatabase));
    public static string SQLiteDatabase => GetString(nameof(SQLiteDatabase));
    public static string CleanDatabase => GetString(nameof(CleanDatabase));
    public static string DatabaseCleaned => GetString(nameof(DatabaseCleaned));
    public static string NoLibraryFolderSelected => GetString(nameof(NoLibraryFolderSelected));
    public static string Pages => GetString(nameof(Pages));
    public static string PageCountFormat => GetString(nameof(PageCountFormat));
    public static string RefreshBooks => GetString(nameof(RefreshBooks));
    public static string RescanLibrary => GetString(nameof(RescanLibrary));
    public static string Save => GetString(nameof(Save));
    public static string SavedLibraryFolderUnavailable => GetString(nameof(SavedLibraryFolderUnavailable));
    public static string ScanningLibrary => GetString(nameof(ScanningLibrary));
    public static string SearchTitleAuthorOrIsbn => GetString(nameof(SearchTitleAuthorOrIsbn));
    public static string Title => GetString(nameof(Title));
    public static string UpdatedBookFormat => GetString(nameof(UpdatedBookFormat));
    public static string Configuration => GetString(nameof(Configuration));
    public static string Help => GetString(nameof(Help));
    public static string YourLibrary => GetString(nameof(YourLibrary));

    private static string GetString(string name) => resourceManager.GetString(name, Culture) ?? name;
}