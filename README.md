Book shelf is an e-book management system, I have used Calibre for a while and it is AWESOME, but I wanted a beter looking interface and really didn't needed most of the features so I decided to develop my own way to manage my book files.

# how to install

To build the Windows installer, install the .NET 10 SDK and Inno Setup, then run these commands from the repository root:

```powershell
dotnet publish Book_Shelf.csproj -c Release -r win-x64 --self-contained true -o artifacts\publish\win-x64
& "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe" installer_script.iss
```

The installer is created as `Output\BookShelfSetup.exe`. It includes the complete self-contained publish output, so the target computer does not need the .NET runtime installed.

# how to use

## Choose a book library

1. Start Book Shelf and open **Library > Choose library folder...**.
2. Select the folder containing your books. Book Shelf scans that folder and its subfolders for EPUB, PDF, MOBI, RTF, and TXT files.
3. The folder is remembered, so the library is scanned again when you next start the app. Use **Library > Rescan library** after adding, changing, or removing files.

Book Shelf catalogs the files where they are; it does not copy them into a separate library. Keep the selected folder and its files available. If files are moved or removed, a rescan updates the catalog accordingly.

## Browse and manage books

- Search by title, author, or ISBN using the search box.
- Use the **Order by** and **Filter** lists to sort the displayed books by title, author, or date added, or show only one format.
- Double-click a book card (or select it and press **Enter** or **Space**) to open the book with the application associated with its file type.
- Choose **Edit details** on a card to change its title, author, ISBN, or page count, then select **Save**.
- Choose **Delete** to remove a book from the catalog and permanently delete its original book file from disk. This cannot be undone.

## Database and settings

Open **Configuration** to change the app theme or language. Your preferences and catalog are stored in your local application data; the catalog database is at `%LOCALAPPDATA%\Book Shelf\books.db`.

Use the **Library** menu to:

- **Import database...** to replace the current catalog with a compatible Book Shelf SQLite database. This cannot be undone. Importing a database does not import the book files themselves; their recorded paths must still be accessible.
- **Export database...** to save a copy of the catalog database. Keep separate backups of your book files as well.
- **Clean database data** to remove all book records and cached covers from the database without deleting the original book files.

Cover images may be read from local files or embedded EPUB covers, or looked up online using the book's ISBN. If a cover cannot be found, the book remains in the catalog with a text cover.
