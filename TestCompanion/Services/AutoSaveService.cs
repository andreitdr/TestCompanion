using System.Text.Json;
using TestCompanion.Models;

namespace TestCompanion.Services;

public class AutoSaveService
{
    private readonly string _cacheFilePath;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public AutoSaveService()
    {
        var localFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(localFolder, "TestCompanion", "Cache");
        Directory.CreateDirectory(appFolder);
        _cacheFilePath = Path.Combine(appFolder, "session_cache.json");
    }

    public void Save(SessionModel model)
    {
        try
        {
            var json = JsonSerializer.Serialize(model, _jsonOptions);
            File.WriteAllText(_cacheFilePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AutoSave failed: {ex.Message}");
        }
    }

    public SessionModel? Load()
    {
        try
        {
            if (!File.Exists(_cacheFilePath))
                return null;

            var json = File.ReadAllText(_cacheFilePath);
            return JsonSerializer.Deserialize<SessionModel>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AutoLoad failed: {ex.Message}");
            return null;
        }
    }

    public void ClearCache()
    {
        try
        {
            if (File.Exists(_cacheFilePath))
                File.Delete(_cacheFilePath);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ClearCache failed: {ex.Message}");
        }
    }
}

