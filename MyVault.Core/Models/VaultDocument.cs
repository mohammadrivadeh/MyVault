namespace MyVault.Core.Models;

public sealed class VaultDocument
{
    public int Version { get; set; } = 1;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastSavedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<VaultEntry> Entries { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public void Validate()
    {
        if (Version != 1 || Entries is null || Categories is null || Entries.Count > 100000 || Categories.Count > 10000 ||
            Entries.Any(e => e is null || e.Title is null || e.Username is null || e.Password is null || e.Url is null || e.Notes is null || e.Tags is null || e.Tags.Any(t => t is null)) ||
            Categories.Any(c => c is null || string.IsNullOrWhiteSpace(c.Name)) ||
            Entries.Select(e => e.Id).Distinct().Count() != Entries.Count || Categories.Select(c => c.Id).Distinct().Count() != Categories.Count)
            throw new MyVault.Core.Exceptions.VaultException(MyVault.Core.Exceptions.VaultError.Corrupted, "Invalid vault document.");
    }
}