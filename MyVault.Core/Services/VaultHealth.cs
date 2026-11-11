using MyVault.Core.Models;

namespace MyVault.Core.Services;

public enum HealthIssue
{
    Weak,
    Reused,
    Old
}

public sealed class HealthFinding
{
    public VaultEntry Entry { get; }
    public IReadOnlyList<HealthIssue> Issues { get; }

    public HealthFinding(VaultEntry entry, IReadOnlyList<HealthIssue> issues)
    {
        Entry = entry;
        Issues = issues;
    }

    public string IssuesText => string.Join(", ", Issues.Select(Format));

    private static string Format(HealthIssue issue) => issue switch
    {
        HealthIssue.Weak => "weak",
        HealthIssue.Reused => "reused",
        HealthIssue.Old => "old",
        _ => issue.ToString().ToLowerInvariant()
    };
}

public sealed class HealthReport
{
    public IReadOnlyList<HealthFinding> Findings { get; }
    public int TotalEntries { get; }

    public HealthReport(IReadOnlyList<HealthFinding> findings, int totalEntries)
    {
        Findings = findings;
        TotalEntries = totalEntries;
    }

    public int HealthyCount => TotalEntries - Findings.Count;
    public bool AllGood => Findings.Count == 0;
    public int WeakCount => Findings.Count(f => f.Issues.Contains(HealthIssue.Weak));
    public int ReusedCount => Findings.Count(f => f.Issues.Contains(HealthIssue.Reused));
    public int OldCount => Findings.Count(f => f.Issues.Contains(HealthIssue.Old));
}

public static class VaultHealth
{
    /// <summary>
    /// Looks for weak / reused / old passwords. Skips entries with no password
    /// (notes-only entries don't count as "unhealthy").
    /// </summary>
    public static HealthReport Analyze(
        IReadOnlyList<VaultEntry> entries,
        TimeSpan? oldThreshold = null)
    {
        TimeSpan threshold = oldThreshold ?? TimeSpan.FromDays(365);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        var passwordCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var e in entries)
        {
            if (string.IsNullOrEmpty(e.Password)) continue;
            passwordCounts[e.Password] = passwordCounts.GetValueOrDefault(e.Password) + 1;
        }

        var findings = new List<HealthFinding>();

        foreach (var entry in entries)
        {
            if (string.IsNullOrEmpty(entry.Password)) continue;

            var issues = new List<HealthIssue>();

            var level = PasswordStrength.Categorize(entry.Password);
            if (level == PasswordStrengthLevel.Weak)
                issues.Add(HealthIssue.Weak);

            if (passwordCounts.TryGetValue(entry.Password, out int n) && n > 1)
                issues.Add(HealthIssue.Reused);

            DateTimeOffset lastChanged = entry.PasswordChangedAt ?? entry.CreatedAt;
            if ((now - lastChanged) > threshold)
                issues.Add(HealthIssue.Old);

            if (issues.Count > 0)
                findings.Add(new HealthFinding(entry, issues));
        }

        // Most-issues-first, then by title
        findings = findings
            .OrderByDescending(f => f.Issues.Count)
            .ThenBy(f => f.Entry.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new HealthReport(findings, entries.Count);
    }
}