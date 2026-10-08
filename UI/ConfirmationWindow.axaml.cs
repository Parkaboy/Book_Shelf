using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Book_Shelf.UI;

/// <summary>Displays a modal confirmation prompt and returns the chosen response.</summary>
public partial class ConfirmationWindow : Window
{
    /// <summary>Initializes the dialog with its title, message, and button labels.</summary>
    public ConfirmationWindow(string title, string message, string confirmText, string cancelText)
    {
        InitializeComponent();
        Title = title;
        MessageTextBlock.Text = message;
        ConfirmButton.Content = confirmText;
        CancelButton.Content = cancelText;
    }

    /// <summary>Closes the dialog with a positive confirmation result.</summary>
    private void Confirm_OnClick(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }

    /// <summary>Closes the dialog with a negative confirmation result.</summary>
    private void Cancel_OnClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}