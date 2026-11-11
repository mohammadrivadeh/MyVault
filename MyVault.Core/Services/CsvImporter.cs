using MyVault.Core.Models;
using System.Net.ServerSentEvents;

namespace MyVault.Core.Services;

public sealed class CsvImportPreview
{
    public IReadOnlyList<VaultEntry> Entries { get; }
    public int TotalDataRows { get; }
    public int Skipped { get; }

    public CsvImportPreview(IReadOnlyList<VaultEntry> entries, int totalDataRows, int skipped)
    {
        Entries = entries;
        TotalDataRows = totalDataRows;
        Skipped = skipped;
    }
}

public static class CsvImporter
{
    public static CsvImportPreview Analyze(string csvText)
    {
        var rows = CsvParser.Parse(csvText);
        if (rows.Count == 0)
            throw new InvalidDataException("CSV file is empty.");

        var header = rows[0];

        int titleIdx = FindColumn(header, "title", "name", "account", "site", "item");
        int userIdx = FindColumn(header, "username", "user", "login", "email", "user name");
        int passIdx = FindColumn(header, "password", "pass", "pwd");
        int urlIdx = FindColumn(header, "url", "website", "site_url", "login_uri", "link");
        int notesIdx = FindColumn(header, "notes", "note", "comment", "memo", "extra");
        int tagsIdx = FindColumn(header, "tags", "tag", "folder", "category", "group");

        if (titleIdx < 0)
            throw new InvalidDataException(
                "CSV must contain a 'title' column (or one of: name, account, site, item).");

        var entries = new List<VaultEntry>();
        int skipped = 0;
        int dataRows = rows.Count - 1;

        for (int r = 1; r < rows.Count; r++)
        {
            var row = rows[r];
            if (IsEmptyRow(row)) { dataRows--; continue; }

            string title = Get(row, titleIdx);
            if (string.IsNullOrWhiteSpace(title)) { skipped++; continue; }

            var entry = new VaultEntry
            {
                Title = title.Trim(),
                Username = Get(row, userIdx),
                Password = Get(row, passIdx),
                Url = Get(row, urlIdx),
                Notes = Get(row, notesIdx)
            };

            string tagsRaw = Get(row, tagsIdx);
            if (!string.IsNullOrEmpty(tagsRaw))
            {
                entry.Tags = tagsRaw
                    .Split(new[] { ',', ';', '|' },
                           StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            entries.Add(entry);
        }

        return new CsvImportPreview(entries, dataRows, skipped);
    }

    private static int FindColumn(IReadOnlyList<string> header, params string[] aliases)
    {
        for (int i = 0; i < header.Count; i++)
        {
            string h = header[i].Trim().ToLowerInvariant();
            if (aliases.Contains(h)) return i;
        }
        return -1;
    }

    private static string Get(IReadOnlyList<string> row, int idx)
        => idx >= 0 && idx < row.Count ? row[idx] : string.Empty;

    private static bool IsEmptyRow(IReadOnlyList<string> row)
        => row.All(string.IsNullOrWhiteSpace);
}