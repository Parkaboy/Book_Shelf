using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Book_Shelf.Data;

/// <summary>Creates library contexts for Entity Framework design-time tooling.</summary>
public sealed class LibraryDbContextFactory : IDesignTimeDbContextFactory<LibraryDbContext>
{
    /// <summary>Creates a context connected to the user's library database.</summary>
    public LibraryDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<LibraryDbContext>()
            .UseSqlite($"Data Source={LibraryDbContext.GetDatabasePath()}")
            .Options;

        return new LibraryDbContext(options);
    }
}