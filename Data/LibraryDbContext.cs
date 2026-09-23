using System;
using System.IO;
using Book_Shelf.Models;
using Microsoft.EntityFrameworkCore;

namespace Book_Shelf.Data;

public sealed class LibraryDbContext : DbContext
{
    public LibraryDbContext(DbContextOptions<LibraryDbContext> options)
        : base(options)
    {
    }

    public DbSet<Book> Books => Set<Book>();

    public static string GetDatabasePath()
    {
        var applicationDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var bookShelfPath = Path.Combine(applicationDataPath, "Book Shelf");
        Directory.CreateDirectory(bookShelfPath);
        return Path.Combine(bookShelfPath, "books.db");
    }

    public static LibraryDbContext Create()
    {
        var options = new DbContextOptionsBuilder<LibraryDbContext>()
            .UseSqlite($"Data Source={GetDatabasePath()}")
            .Options;

        return new LibraryDbContext(options);
    }

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