using System.Collections.Generic;
using Book_Shelf.Models;
using Book_Shelf.UI;
using Xunit;

namespace Book_Shelf.Tests;

/// <summary>Verifies book card presentation state exposed by the book view model.</summary>
public sealed class BookItemViewModelTests
{
    /// <summary>Verifies page-count visibility follows whether the book has a page count.</summary>
    [Fact]
    public void HasPageCount_TracksWhetherPageCountIsPresent()
    {
        using var viewModel = new BookItemViewModel(CreateBook());
        var changedProperties = new List<string?>();
        viewModel.PropertyChanged += (_, eventArgs) => changedProperties.Add(eventArgs.PropertyName);

        Assert.False(viewModel.HasPageCount);

        viewModel.PageCount = 240;
        Assert.True(viewModel.HasPageCount);
        Assert.Contains(nameof(BookItemViewModel.HasPageCount), changedProperties);

        viewModel.PageCount = null;
        Assert.False(viewModel.HasPageCount);
    }

    /// <summary>Creates a book record with the required fields for view model tests.</summary>
    private static Book CreateBook() => new()
    {
        Title = "Test book",
        FilePath = "test-book.epub",
        Format = "EPUB",
        ContentHash = string.Empty
    };
}
