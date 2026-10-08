using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Book_Shelf.UI;

public partial class ConfirmationWindow : Window
{
    public ConfirmationWindow(string title, string message, string confirmText, string cancelText)
    {
        InitializeComponent();
        Title = title;
        MessageTextBlock.Text = message;
        ConfirmButton.Content = confirmText;
        CancelButton.Content = cancelText;
    }

    private void Confirm_OnClick(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }

    private void Cancel_OnClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}