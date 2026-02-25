using TestCompanion.Models;
using TestCompanion.Services;
using Windows.Storage.Pickers;

namespace TestCompanion;

public sealed partial class SettingsWindow : Page
{
    private readonly SettingsService _settingsService;
    private bool _isInitializing = true;

    public SettingsWindow()
    {
        this.InitializeComponent();
        _settingsService = new SettingsService();
        LoadSettings();
        _isInitializing = false;
    }

    private void LoadSettings()
    {
        // Populate format options
        var formats = new[]
        {
            _settingsService.GetFormatDisplayName(ExportFormat.PlainText),
            _settingsService.GetFormatDisplayName(ExportFormat.Markdown),
            _settingsService.GetFormatDisplayName(ExportFormat.Html),
            _settingsService.GetFormatDisplayName(ExportFormat.Json)
        };
        foreach (var format in formats)
            FormatComboBox.Items.Add(format);

        FormatComboBox.SelectedIndex = (int)_settingsService.GetExportFormat();
        ExportPathBox.Text = _settingsService.GetExportPath();
    }

    private void FormatComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;
        _settingsService.SetExportFormat((ExportFormat)FormatComboBox.SelectedIndex);
    }

    private void ExportPathBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isInitializing) return;
        _settingsService.SetExportPath(ExportPathBox.Text);
    }

    private async void BrowseExportPath_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new FolderPicker();
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add("*");

            var folder = await picker.PickSingleFolderAsync();
            if (folder != null)
            {
                ExportPathBox.Text = folder.Path;
                _settingsService.SetExportPath(folder.Path);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Folder picker error: {ex.Message}");
        }
    }
}

