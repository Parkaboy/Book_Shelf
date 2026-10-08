using System;

namespace Book_Shelf.Models;

/// <summary>Represents a book file and its editable catalog metadata.</summary>
public sealed class Book
{
    public int Id { get; set; }

    public required string Title { get; set; }

    public string? Isbn { get; set; }

    public string? Author { get; set; }

    public int? PageCount { get; set; }

    public required string FilePath { get; set; }

    public required string Format { get; set; }

    public long FileSize { get; set; }

    public DateTime FileLastModifiedUtc { get; set; }

    public required string ContentHash { get; set; }

    public string? CoverPath { get; set; }

    public DateTime ImportedUtc { get; set; }

    public DateTime UpdatedUtc { get; set; }
}