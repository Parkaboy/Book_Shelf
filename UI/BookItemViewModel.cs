using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using Avalonia.Media.Imaging;
using Book_Shelf.Models;

namespace Book_Shelf.UI;

public sealed class BookItemViewModel : INotifyPropertyChanged, IDisposable
{
    private bool isSelected;
    private bool isEditing;
    private Bitmap? coverImage;

    public BookItemViewModel(Book book)
    {
        Book = book;
        ReloadCoverImage();
    }

    public Book Book { get; }

    public string Title
    {
        get => Book.Title;
        set
        {
            if (Book.Title == value)
            {
                return;
            }

            Book.Title = value;
            OnPropertyChanged();
        }
    }

    public string? Author
    {
        get => Book.Author;
        set
        {
            if (Book.Author == value)
            {
                return;
            }

            Book.Author = value;
            OnPropertyChanged();
        }
    }

    public string? Isbn
    {
        get => Book.Isbn;
        set
        {
            if (Book.Isbn == value)
            {
                return;
            }

            Book.Isbn = value;
            OnPropertyChanged();
        }
    }

    public int? PageCount
    {
        get => Book.PageCount;
        set
        {
            if (Book.PageCount == value)
            {
                return;
            }

            Book.PageCount = value;
            OnPropertyChanged();
        }
    }

    public string Format => Book.Format;

    public string FilePath => Book.FilePath;

    public bool IsSelected
    {
        get => isSelected;
        set => SetField(ref isSelected, value);
    }

    public bool IsEditing
    {
        get => isEditing;
        set => SetField(ref isEditing, value);
    }

    public Bitmap? CoverImage
    {
        get => coverImage;
        private set
        {
            if (ReferenceEquals(coverImage, value))
            {
                return;
            }

            coverImage?.Dispose();
            coverImage = value;
            OnPropertyChanged();
        }
    }

    public void ReloadCoverImage()
    {
        CoverImage = null;
        if (string.IsNullOrWhiteSpace(Book.CoverPath) || !File.Exists(Book.CoverPath))
        {
            return;
        }

        try
        {
            using var coverStream = File.OpenRead(Book.CoverPath);
            CoverImage = Bitmap.DecodeToWidth(coverStream, 480, BitmapInterpolationMode.HighQuality);
        }
        catch (IOException)
        {
            CoverImage = null;
        }
        catch (ArgumentException)
        {
            CoverImage = null;
        }
    }

    public void Dispose()
    {
        CoverImage = null;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value))
        {
            return;
        }

        field = value;
        OnPropertyChanged(propertyName);
    }
}