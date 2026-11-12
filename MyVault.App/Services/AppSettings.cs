namespace MyVault.App.Services;

public sealed class AppSettings
{
    public string Accent { get; set; } = "Palette";
    public double TextSize { get; set; } = 13;
    public string Density { get; set; } = "Comfortable";
    public string Language { get; set; } = "en";
    public string BackupDirectory { get; set; } = "";
    public int BackupMinutes { get; set; } = 0;
    public int BackupKeep { get; set; } = 10;
    public List<string> RecentVaults { get; set; } = new();
    public Dictionary<string, DateTimeOffset> LastBackups { get; set; } = new();
    public string LastBackupStatus { get; set; } = "No scheduled backup yet";
    public int AutoLockMinutes { get; set; } = 5;
    public int ClearClipboardSeconds { get; set; } = 30;
    public string Motion { get; set; } = "System";
    public string Theme { get; set; } = "MoonlightGlow";

    /// <summary>
    /// Absolute path to the user's chosen vault. Null/empty = use default
    /// &lt;exe folder&gt;\MyVault\vault.dat.
    /// </summary>
    public string? LastVaultPath { get; set; }
}