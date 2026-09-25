using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia;
using Avalonia.Styling;

namespace Book_Shelf.UI;

public partial class ConfigurationWindow : Window {

    public ConfigurationWindow()
    {
        InitializeComponent();
    }

    private void Theme_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender == LightThemeRadioButton)
        {
            Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
        }
        else if (sender == DarkThemeRadioButton)
        {
            Application.Current!.RequestedThemeVariant = ThemeVariant.Dark;
        }
        else if (sender == SystemThemeRadioButton)
        {
            Application.Current!.RequestedThemeVariant = ThemeVariant.Default;
        }
    }

}