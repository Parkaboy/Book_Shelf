using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.ComponentModel.DataAnnotations.Schema;

namespace Book_Shelf.Models;

public sealed class Book : INotifyPropertyChanged
{
    private bool isSelected;
    private bool isEditing;

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

    public DateTime ImportedUtc { get; set; }

    public DateTime UpdatedUtc { get; set; }

    [NotMapped]
    public bool IsSelected
    {
        get => isSelected;
        set => SetField(ref isSelected, value);
    }

    [NotMapped]
    public bool IsEditing
    {
        get => isEditing;
        set => SetField(ref isEditing, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}