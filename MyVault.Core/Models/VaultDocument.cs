namespace MyVault.Core.Models;

public sealed class VaultDocument
{
    public int Version { get; set; } = 1;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastSavedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<VaultEntry> Entries { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
}