using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Book_Shelf.Resources;

namespace Book_Shelf.UI;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
    }

    private void BuyMeACoffee_OnClick(object? sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = Strings.BuyMeACoffeeUrl,
            UseShellExecute = true
        });
    }

    private void Close_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}