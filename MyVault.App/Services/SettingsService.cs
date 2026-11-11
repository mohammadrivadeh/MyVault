using System.IO;
using System.Text.Json;

namespace MyVault.App.Services;

public sealed class SettingsService
{
    private readonly string _path;

    public AppSettings Current { get; private set; }

    public SettingsService()
    {
        _path = Path.Combine(VaultPaths.GetVaultDirectory(), "settings.json");
        Current = Load();
    }

    private AppSettings Load()
    {
        try
        {
            if (!File.Exists(_path)) return new AppSettings();
            string json = File.ReadAllText(_path);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save()
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
        }
        catch
        {
            // Settings are best-effort; never fail the app over this.
        }
    }
}