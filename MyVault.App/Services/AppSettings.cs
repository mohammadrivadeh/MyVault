namespace MyVault.App.Services;

public sealed class AppSettings
{
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