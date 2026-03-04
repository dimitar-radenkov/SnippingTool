using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SnippingTool.Models;

namespace SnippingTool.Services;

public sealed class UserSettingsService : IUserSettingsService
{
    private readonly string _settingsPath;
    private readonly ILogger<UserSettingsService> _logger;

    public UserSettings Current { get; private set; }

    public UserSettingsService(ILogger<UserSettingsService> logger)
    {
        _logger = logger;
        _settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SnippingTool", "settings.json");

        Current = Load();
    }

    private UserSettings Load()
    {
        if (!File.Exists(_settingsPath))
        {
            _logger.LogDebug("No settings.json found — using defaults");
            return new UserSettings();
        }

        try
        {
            var json = File.ReadAllText(_settingsPath);
            var loaded = JsonSerializer.Deserialize<UserSettings>(json);
            if (loaded is not null)
            {
                _logger.LogInformation("Settings loaded from {Path}", _settingsPath);
                return loaded;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load settings.json — using defaults");
        }

        return new UserSettings();
    }

    public void Save(UserSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_settingsPath, json);
        Current = settings;
        _logger.LogInformation("Settings saved to {Path}", _settingsPath);
    }
}
