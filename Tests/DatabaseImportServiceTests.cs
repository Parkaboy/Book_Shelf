using System;
using System.IO;
using System.Threading.Tasks;
using Book_Shelf.Data;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Book_Shelf.Tests;

/// <summary>Verifies that database imports replace only compatible, validated library databases.</summary>
public sealed class DatabaseImportServiceTests
{
    /// <summary>Verifies a valid imported database replaces the destination and retains its records.</summary>
    [Fact]
    public async Task ImportAsync_ReplacesDestinationWithValidatedLibraryDatabase()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var sourcePath = Path.Combine(directory, "source.db");
            var destinationPath = Path.Combine(directory, "active.db");
            await CreateLibraryDatabaseAsync(sourcePath, "Imported book");
            await File.WriteAllTextAsync(destinationPath, "old database");

            await DatabaseImportService.ImportAsync(sourcePath, destinationPath);

            await using (var connection = new SqliteConnection(
                             new SqliteConnectionStringBuilder
                             {
                                 DataSource = destinationPath,
                                 Mode = SqliteOpenMode.ReadOnly
                             }.ToString()))
            {
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT \"Title\" FROM \"Books\";";
                Assert.Equal("Imported book", await command.ExecuteScalarAsync());
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>Verifies an incompatible database is rejected without changing the destination.</summary>
    [Fact]
    public async Task ImportAsync_RejectsNonLibraryDatabaseWithoutReplacingDestination()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var sourcePath = Path.Combine(directory, "source.db");
            var destinationPath = Path.Combine(directory, "active.db");
            await using (var connection = new SqliteConnection($"Data Source={sourcePath}"))
            {
                await connection.OpenAsync();
            }

            await File.WriteAllTextAsync(destinationPath, "keep this database");

            await Assert.ThrowsAsync<InvalidDataException>(
                () => DatabaseImportService.ImportAsync(sourcePath, destinationPath));

            Assert.Equal("keep this database", await File.ReadAllTextAsync(destinationPath));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>Creates a SQLite database with the current Book Shelf schema and one book.</summary>
    private static async Task CreateLibraryDatabaseAsync(string path, string title)
    {
        await using var connection = new SqliteConnection($"Data Source={path}");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE "__EFMigrationsHistory" (
                "MigrationId" TEXT NOT NULL PRIMARY KEY,
                "ProductVersion" TEXT NOT NULL
            );
            INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
            VALUES ('20260923132739_AddBookCoverGenerated', '10.0.12');
            CREATE TABLE "Books" (
                "Id" INTEGER NOT NULL PRIMARY KEY,
                "Title" TEXT NOT NULL,
                "Isbn" TEXT NULL,
                "Author" TEXT NULL,
                "PageCount" INTEGER NULL,
                "FilePath" TEXT NOT NULL,
                "Format" TEXT NOT NULL,
                "FileSize" INTEGER NOT NULL,
                "FileLastModifiedUtc" TEXT NOT NULL,
                "ContentHash" TEXT NOT NULL,
                "CoverPath" TEXT NULL,
                "ImportedUtc" TEXT NOT NULL,
                "UpdatedUtc" TEXT NOT NULL
            );
            INSERT INTO "Books" ("Title", "FilePath", "Format", "FileSize",
                "FileLastModifiedUtc", "ContentHash", "ImportedUtc", "UpdatedUtc")
            VALUES ($title, 'book.epub', 'EPUB', 0, '2026-01-01', 'hash', '2026-01-01', '2026-01-01');
            """;
        command.Parameters.AddWithValue("$title", title);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>Creates and returns a unique temporary directory for an import test.</summary>
    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"BookShelf-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }
}
