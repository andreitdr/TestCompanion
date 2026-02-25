using System.Text.Json;
using TestCompanion.Models;

namespace TestCompanion.Services;

public class SettingsService
{
    private readonly string _settingsFilePath;
    private AppSettings _settings;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public SettingsService()
    {
        var localFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(localFolder, "TestCompanion");
        Directory.CreateDirectory(appFolder);
        _settingsFilePath = Path.Combine(appFolder, "settings.json");
        _settings = Load();
    }

    public AppSettings Settings => _settings;

    public string GetExportPath()
    {
        if (!string.IsNullOrWhiteSpace(_settings.ExportPath) && Directory.Exists(_settings.ExportPath))
            return _settings.ExportPath;

        // Default to Documents/TestingSessionReports
        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var defaultPath = Path.Combine(documentsPath, "TestingSessionReports");
        Directory.CreateDirectory(defaultPath);
        return defaultPath;
    }

    public void SetExportPath(string path)
    {
        _settings.ExportPath = path;
        Save();
    }

    public ExportFormat GetExportFormat()
    {
        return _settings.LastExportFormat;
    }

    public void SetExportFormat(ExportFormat format)
    {
        _settings.LastExportFormat = format;
        Save();
    }

    public string GetFileExtension(ExportFormat format)
    {
        return format switch
        {
            ExportFormat.PlainText => ".txt",
            ExportFormat.Markdown => ".md",
            ExportFormat.Html => ".html",
            ExportFormat.Json => ".json",
            _ => ".txt"
        };
    }

    public string GetFormatDisplayName(ExportFormat format)
    {
        return format switch
        {
            ExportFormat.PlainText => "Plain Text (.txt)",
            ExportFormat.Markdown => "Markdown (.md)",
            ExportFormat.Html => "HTML (.html)",
            ExportFormat.Json => "JSON (.json)",
            _ => "Plain Text (.txt)"
        };
    }

    private void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_settings, _jsonOptions);
            File.WriteAllText(_settingsFilePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Settings save failed: {ex.Message}");
        }
    }

    private AppSettings Load()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                return JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions) ?? new AppSettings();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Settings load failed: {ex.Message}");
        }
        return new AppSettings();
    }
}

