using System.IO;
using System.Text.Json;

namespace MyVault.App.Services;

public sealed class SettingsService
{
    private readonly string _path;

    public AppSettings Current { get; private set; }

    public SettingsService(string? settingsPath = null)
    {
        _path = settingsPath ?? Path.Combine(VaultPaths.GetVaultDirectory(), "settings.json");
        Current = Load();
    }

    private AppSettings Load()
    {
        try
        {
            if (!File.Exists(_path)) return new AppSettings();
            string json = File.ReadAllText(_path);
            var settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            settings.AutoLockMinutes = Math.Clamp(settings.AutoLockMinutes, 0, 1440);
            settings.ClearClipboardSeconds = Math.Clamp(settings.ClearClipboardSeconds, 0, 3600);
            settings.Theme = ThemeService.Normalize(settings.Theme);
            if (settings.Motion is not ("System" or "Full" or "Off")) settings.Motion = "System";
            settings.TextSize = double.IsFinite(settings.TextSize) ? Math.Clamp(settings.TextSize, 12, 18) : 13;
            if (!AppearanceService.Accents.Contains(settings.Accent)) settings.Accent = "Palette";
            if (settings.Density is not ("Compact" or "Comfortable")) settings.Density = "Comfortable";
            if (settings.Language is not ("en" or "fa")) settings.Language = "en";
            settings.RecentVaults = (settings.RecentVaults ?? new()).Where(p => !string.IsNullOrWhiteSpace(p)).Distinct(StringComparer.OrdinalIgnoreCase).Take(8).ToList();
            settings.LastBackups ??= new();
            settings.BackupDirectory ??= "";
            settings.BackupMinutes = Math.Clamp(settings.BackupMinutes, 0, 1440);
            settings.BackupKeep = Math.Clamp(settings.BackupKeep, 1, 50);
            return settings;
        }
        catch
        {
            return new AppSettings();
        }
    }

    public bool Update(Action<AppSettings> edit)
    {
        var before = AppearanceService.Copy(Current);
        edit(Current);
        if (Save()) return true;
        Current = before;
        return false;
    }
    public bool Save()
    {
        try
        {
            string? dir = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            string json = JsonSerializer.Serialize(
                Current,
                new JsonSerializerOptions { WriteIndented = true });

            string tmp = _path + ".tmp";
            File.WriteAllText(tmp, json);
            File.Move(tmp, _path, overwrite: true);
            return true;
        }
        catch
        {
            return false;
        }
    }
}