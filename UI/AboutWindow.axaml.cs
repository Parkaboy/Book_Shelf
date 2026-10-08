using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Book_Shelf.Resources;

namespace Book_Shelf.UI;

/// <summary>Displays application information and project links.</summary>
public partial class AboutWindow : Window
{
    /// <summary>Initializes the application information window.</summary>
    public AboutWindow()
    {
        InitializeComponent();
    }

    /// <summary>Opens the configured donation page in the default browser.</summary>
    private void BuyMeACoffee_OnClick(object? sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = Strings.BuyMeACoffeeUrl,
            UseShellExecute = true
        });
    }

    /// <summary>Closes the application information window.</summary>
    private void Close_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}