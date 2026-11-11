using System.IO;

namespace MyVault.App.Services;

internal static class VaultPaths
{
    public const string FolderName = "MyVault";
    public const string VaultFileName = "vault.dat";

    /// <summary>Folder next to the running .exe — portable by default.</summary>
    public static string GetVaultDirectory()
    {
        string exeDir = Path.GetDirectoryName(Environment.ProcessPath)
                        ?? AppContext.BaseDirectory;
        return Path.Combine(exeDir, FolderName);
    }

    public static string GetVaultFile()
        => Path.Combine(GetVaultDirectory(), VaultFileName);

    public static bool VaultExists()
        => File.Exists(GetVaultFile());

    /// <summary>
    /// One-time migration: if the new (portable) location has no vault but the old
    /// %APPDATA%\MyVault\vault.dat does, copy it over along with settings.json.
    /// Runs at most once per install. Best-effort — failures are silent.
    /// </summary>
    public static void MigrateFromAppDataIfNeeded()
    {
        try
        {
            string newVault = GetVaultFile();
            if (File.Exists(newVault)) return;

            string oldDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                FolderName);

            string oldVault = Path.Combine(oldDir, VaultFileName);
            if (!File.Exists(oldVault)) return;

            Directory.CreateDirectory(GetVaultDirectory());
            File.Copy(oldVault, newVault, overwrite: false);

            string oldSettings = Path.Combine(oldDir, "settings.json");
            string newSettings = Path.Combine(GetVaultDirectory(), "settings.json");
            if (File.Exists(oldSettings) && !File.Exists(newSettings))
                File.Copy(oldSettings, newSettings, overwrite: false);

            // Copy rotated backups too, so "restore from .bak.1" still works.
            for (int i = 1; i <= 5; i++)
            {
                string oldBak = $"{oldVault}.bak.{i}";
                string newBak = $"{newVault}.bak.{i}";
                if (File.Exists(oldBak) && !File.Exists(newBak))
                    File.Copy(oldBak, newBak, overwrite: false);
            }
        }
        catch
        {
            // Migration is best-effort. If it fails, user just starts fresh at the new location.
        }
    }
}