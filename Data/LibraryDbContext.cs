using System;
using System.IO;
using Book_Shelf.Models;
using Microsoft.EntityFrameworkCore;

namespace Book_Shelf.Data;

/// <summary>Defines the Entity Framework model and SQLite connection for the book library.</summary>
public sealed class LibraryDbContext : DbContext
{
    /// <summary>Creates a context with the supplied Entity Framework options.</summary>
    public LibraryDbContext(DbContextOptions<LibraryDbContext> options)
        : base(options)
    {
    }

    public DbSet<Book> Books => Set<Book>();

    /// <summary>Gets the per-user path of the active library database.</summary>
    public static string GetDatabasePath()
    {
        var applicationDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var bookShelfPath = Path.Combine(applicationDataPath, "Book Shelf");
        Directory.CreateDirectory(bookShelfPath);
        return Path.Combine(bookShelfPath, "books.db");
    }

    /// <summary>Creates a context connected to the active local library database.</summary>
    public static LibraryDbContext Create()
    {
        var options = new DbContextOptionsBuilder<LibraryDbContext>()
            .UseSqlite($"Data Source={GetDatabasePath()}")
            .Options;

        return new LibraryDbContext(options);
    }

    /// <summary>Configures the book entity constraints and database indexes.</summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>(entity =>
        {
            entity.HasKey(book => book.Id);
            entity.Property(book => book.Title).HasMaxLength(500).IsRequired();
            entity.Property(book => book.Isbn).HasMaxLength(32);
            entity.Property(book => book.Author).HasMaxLength(500);
            entity.Property(book => book.FilePath).HasMaxLength(4096).IsRequired();
            entity.Property(book => book.Format).HasMaxLength(16).IsRequired();
            entity.Property(book => book.ContentHash).HasMaxLength(64).IsRequired();
            entity.Property(book => book.CoverPath).HasMaxLength(4096);
            entity.HasIndex(book => book.FilePath).IsUnique();
            entity.HasIndex(book => book.ContentHash);
        });
    }
}