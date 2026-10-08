using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia;
using Avalonia.Styling;
using System;
using Book_Shelf.Data;
using Book_Shelf.Services;

namespace Book_Shelf.UI;

/// <summary>Lets users select the application's theme and display language.</summary>
public partial class ConfigurationWindow : Window {
    private bool isLoadingLanguage;

    /// <summary>Initializes the configuration window and loads saved preferences when opened.</summary>
    public ConfigurationWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
    }

    /// <summary>Loads saved preferences and selects the configured language in the window.</summary>
    private async void OnOpened(object? sender, EventArgs e)
    {
        Opened -= OnOpened;
        var settings = await new LibrarySettingsStore().LoadAsync();
        isLoadingLanguage = true;
        SetLanguageSelection(settings.LanguageCode);
        isLoadingLanguage = false;
    }

    /// <summary>Applies the theme variant selected by the user.</summary>
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

    /// <summary>Saves the selected language and recreates the main window with updated strings.</summary>
    private async void Language_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (isLoadingLanguage || LanguageComboBox.SelectedItem is not ComboBoxItem selectedItem)
        {
            return;
        }

        var languageCode = selectedItem.Tag as string;
        languageCode = string.IsNullOrWhiteSpace(languageCode) ? null : languageCode;

        LanguageService.Apply(languageCode);
        var settings = await new LibrarySettingsStore().LoadAsync();
        settings.LanguageCode = languageCode;
        await new LibrarySettingsStore().SaveAsync(settings);
        ReloadMainWindow();
    }

    /// <summary>Selects the language option matching the saved language code.</summary>
    private void SetLanguageSelection(string? languageCode)
    {
        LanguageComboBox.SelectedIndex = languageCode switch
        {
            "en" => 1,
            "es" => 2,
            "de" => 3,
            "pt-BR" => 4,
            "it" => 5,
            _ => 0
        };
    }

    /// <summary>Replaces the main window so all static localized labels are refreshed.</summary>
    private void ReloadMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow is not MainWindow currentMainWindow)
        {
            return;
        }

        var replacementWindow = new MainWindow();
        desktop.MainWindow = replacementWindow;
        replacementWindow.Show();
        Close();
        currentMainWindow.Close();
    }

}