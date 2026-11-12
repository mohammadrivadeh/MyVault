using MyVault.Core.Models;
using MyVault.App.Services;
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
                return LocalizationService.Text("No entries in your vault yet.");
            if (Report.AllGood)
                return LocalizationService.Format("All {0} entries look healthy.",Report.TotalEntries);

            var parts = new List<string>();
            if (Report.WeakCount > 0) parts.Add(LocalizationService.Format("{0} weak",Report.WeakCount));
            if (Report.ReusedCount > 0) parts.Add(LocalizationService.Format("{0} reused",Report.ReusedCount));
            if (Report.OldCount > 0) parts.Add(LocalizationService.Format("{0} old",Report.OldCount));
            return LocalizationService.Format("{0} of {1} entries are healthy. {2} need attention.",Report.HealthyCount,Report.TotalEntries,string.Join(", ",parts));
        }
    }
}