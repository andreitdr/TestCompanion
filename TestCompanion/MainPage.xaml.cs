using TestCompanion.Models;
using TestCompanion.Services;
using TestCompanion.ViewModels;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Pickers;

namespace TestCompanion;

public sealed partial class MainPage : Page
{
    private readonly SessionViewModel _viewModel;

    public MainPage()
    {
        this.InitializeComponent();
        _viewModel = new SessionViewModel();
        this.DataContext = _viewModel;

        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SessionViewModel.HasValidationError) ||
            e.PropertyName == nameof(SessionViewModel.ValidationMessage) ||
            string.IsNullOrEmpty(e.PropertyName))
        {
            ValidationBorder.Visibility = _viewModel.HasValidationError
                ? Visibility.Visible
                : Visibility.Collapsed;
            ValidationText.Text = _viewModel.ValidationMessage;
        }

        if (e.PropertyName == nameof(SessionViewModel.StatusMessage) ||
            string.IsNullOrEmpty(e.PropertyName))
        {
            StatusText.Text = _viewModel.StatusMessage;
        }

        if (e.PropertyName == nameof(SessionViewModel.IsReadOnly) ||
            string.IsNullOrEmpty(e.PropertyName))
        {
            ReadOnlyBanner.Visibility = _viewModel.IsReadOnly
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }

    public void OnWindowActivated()
    {
        _viewModel.OnAppActivated();
    }

    public void OnWindowDeactivated()
    {
        _viewModel.OnAppDeactivated();
    }

    private void DropArea_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Copy;
        if (e.DragUIOverride != null)
        {
            e.DragUIOverride.Caption = "Drop to attach";
            e.DragUIOverride.IsGlyphVisible = true;
        }
    }

    private async void DropArea_Drop(object sender, DragEventArgs e)
    {
        if (e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            var items = await e.DataView.GetStorageItemsAsync();
            var paths = new List<string>();
            foreach (var item in items)
            {
                paths.Add(item.Path);
            }
            _viewModel.AddFiles(paths);
        }
    }

    private async void BrowseFiles_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new FileOpenPicker();
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add("*");

            var files = await picker.PickMultipleFilesAsync();
            if (files != null && files.Count > 0)
            {
                var paths = files.Select(f => f.Path).ToList();
                _viewModel.AddFiles(paths);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"File picker error: {ex.Message}");
        }
    }

    private async void Submit_Click(object sender, RoutedEventArgs e)
    {
        if (!_viewModel.RunValidation()) return;

        // Build the format picker dialog
        var settingsService = new SettingsService();
        var defaultFormat = settingsService.GetExportFormat();

        var formatCombo = new ComboBox { MinWidth = 250 };
        formatCombo.Items.Add(settingsService.GetFormatDisplayName(ExportFormat.PlainText));
        formatCombo.Items.Add(settingsService.GetFormatDisplayName(ExportFormat.Markdown));
        formatCombo.Items.Add(settingsService.GetFormatDisplayName(ExportFormat.Html));
        formatCombo.Items.Add(settingsService.GetFormatDisplayName(ExportFormat.Json));
        formatCombo.SelectedIndex = (int)defaultFormat;

        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(new TextBlock { Text = "Select the export format:" });
        panel.Children.Add(formatCombo);

        var dialog = new ContentDialog
        {
            Title = "Export Report",
            Content = panel,
            PrimaryButtonText = "Export",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            var selectedFormat = (ExportFormat)formatCombo.SelectedIndex;
            await _viewModel.ExportWithFormat(selectedFormat);
        }
    }

    private async void OpenReport_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new FileOpenPicker();
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add(".json");

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                var content = await File.ReadAllTextAsync(file.Path);
                var importer = new ReportImportService();
                var model = importer.ImportFromJson(content);

                if (model != null)
                {
                    _viewModel.LoadFromReport(model, file.Path);
                }
                else
                {
                    var dialog = new ContentDialog
                    {
                        Title = "Import Failed",
                        Content = "The selected file could not be parsed. Only JSON reports exported by Test Companion are supported.",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    await dialog.ShowAsync();
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Open report error: {ex.Message}");
        }
    }

    private void OpenSettings_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new Window();
        settingsWindow.Content = new SettingsWindow();
        settingsWindow.Title = "Settings";
        settingsWindow.Activate();
    }

    private async void About_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "About Test Companion",
            Content = "Test Companion v1.0\n\nA testing session management tool for structured exploratory testing.\n\nBuilt with Uno Platform.",
            CloseButtonText = "OK",
            XamlRoot = this.XamlRoot
        };
        await dialog.ShowAsync();
    }
}
