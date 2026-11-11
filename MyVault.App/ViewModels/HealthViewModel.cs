using MyVault.Core.Models;
using MyVault.Core.Services;

namespace MyVault.App.ViewModels;

public sealed class HealthViewModel : ObservableObject
{
    public HealthReport Report { get; }

    public HealthViewModel(IReadOnlyList<VaultEntry> entries)
    {
        Report = VaultHealth.Analyze(entries.Where(e => e.DeletedAt is null).ToList());
    }

    public string SummaryText
    {
        get
        {
            if (Report.TotalEntries == 0)
                return "No entries in your vault yet.";
            if (Report.AllGood)
                return $"All {Report.TotalEntries} entries look healthy.";

            var parts = new List<string>();
            if (Report.WeakCount > 0) parts.Add($"{Report.WeakCount} weak");
            if (Report.ReusedCount > 0) parts.Add($"{Report.ReusedCount} reused");
            if (Report.OldCount > 0) parts.Add($"{Report.OldCount} old");
            return $"{Report.HealthyCount} of {Report.TotalEntries} entries are healthy. " +
                   string.Join(", ", parts) + " need attention.";
        }
    }
}