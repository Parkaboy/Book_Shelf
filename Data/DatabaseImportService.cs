using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace Book_Shelf.Data;

/// <summary>Validates and safely replaces the active SQLite library database.</summary>
public static class DatabaseImportService
{
    /// <summary>Copies a compatible Book Shelf database into the destination atomically.</summary>
    public static async Task ImportAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        sourcePath = Path.GetFullPath(sourcePath);
        destinationPath = Path.GetFullPath(destinationPath);
        if (string.Equals(sourcePath, destinationPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The source database cannot be the active database.");
        }

        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException("The selected database file does not exist.", sourcePath);
        }

        var destinationDirectory = Path.GetDirectoryName(destinationPath)
            ?? throw new InvalidOperationException("The destination database path is invalid.");
        Directory.CreateDirectory(destinationDirectory);

        var temporaryPath = $"{destinationPath}.{Guid.NewGuid():N}.import";
        try
        {
            var sourceConnectionString = new SqliteConnectionStringBuilder
            {
                DataSource = sourcePath,
                Mode = SqliteOpenMode.ReadOnly
            }.ToString();

            var temporaryConnectionString = new SqliteConnectionStringBuilder
            {
                DataSource = temporaryPath,
                Mode = SqliteOpenMode.ReadWriteCreate
            }.ToString();

            await using (var source = new SqliteConnection(sourceConnectionString))
            {
                await source.OpenAsync(cancellationToken);
                await ValidateDatabaseAsync(source, cancellationToken);

                await using var temporary = new SqliteConnection(temporaryConnectionString);
                await temporary.OpenAsync(cancellationToken);
                source.BackupDatabase(temporary);
            }

            SqliteConnection.ClearAllPools();
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaryPath, destinationPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    /// <summary>Checks database integrity, schema, and Book Shelf migration compatibility.</summary>
    private static async Task ValidateDatabaseAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using (var integrityCheck = connection.CreateCommand())
        {
            integrityCheck.CommandText = "PRAGMA quick_check;";
            var result = await integrityCheck.ExecuteScalarAsync(cancellationToken);
            if (!string.Equals(result as string, "ok", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("The selected file is not a valid SQLite database.");
            }
        }

        await using (var booksTableCheck = connection.CreateCommand())
        {
            booksTableCheck.CommandText =
                "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'Books';";
            var result = await booksTableCheck.ExecuteScalarAsync(cancellationToken);
            if (Convert.ToInt32(result) == 0)
            {
                throw new InvalidDataException("The selected database does not contain the Book Shelf library.");
            }
        }

        await using (var migrationCheck = connection.CreateCommand())
        {
            migrationCheck.CommandText = """
                SELECT COUNT(*) FROM "__EFMigrationsHistory"
                WHERE "MigrationId" = '20260923132739_AddBookCoverGenerated';
                """;
            try
            {
                var result = await migrationCheck.ExecuteScalarAsync(cancellationToken);
                if (Convert.ToInt32(result) == 0)
                {
                    throw new InvalidDataException("The selected database is not compatible with this version of Book Shelf.");
                }
            }
            catch (SqliteException exception)
            {
                throw new InvalidDataException("The selected database is not compatible with this version of Book Shelf.", exception);
            }
        }

        await using var schemaCheck = connection.CreateCommand();
        schemaCheck.CommandText = """
            SELECT "Id", "Title", "Isbn", "Author", "PageCount", "FilePath", "Format",
                   "FileSize", "FileLastModifiedUtc", "ContentHash", "CoverPath",
                   "ImportedUtc", "UpdatedUtc"
            FROM "Books"
            LIMIT 0;
            """;
        await schemaCheck.ExecuteNonQueryAsync(cancellationToken);
    }
}
