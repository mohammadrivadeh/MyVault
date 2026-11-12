using MyVault.Core.Models;


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

public enum CsvSource { Auto, Generic, Chrome, Bitwarden, OnePassword }

public static class CsvImporter
{
    public static CsvImportPreview Analyze(string csvText, CsvSource source = CsvSource.Auto)
    {
        var rows = CsvParser.Parse(csvText);
        if (rows.Count == 0)
            throw new InvalidDataException("CSV file is empty.");

        var header = rows[0];
        if (source == CsvSource.Auto)
            source = header.Any(h => h.Trim().Equals("login_username", StringComparison.OrdinalIgnoreCase)) ? CsvSource.Bitwarden : CsvSource.Generic;

        int titleIdx = FindColumn(header, "title", "name", "account", "site", "item");
        int userIdx = FindColumn(header, "username", "user", "login", "email", "user name", "login_username");
        int passIdx = FindColumn(header, "password", "pass", "pwd", "login_password");
        int urlIdx = FindColumn(header, "url", "website", "site_url", "login_uri", "link", "login_uri");
        int notesIdx = FindColumn(header, "notes", "note", "comment", "memo", "extra");
        int tagsIdx = FindColumn(header, "tags", "tag", "folder", "category", "group");

        if (titleIdx < 0)
            throw new InvalidDataException(
                "CSV must contain a 'title' column (or one of: name, account, site, item).");

        int totpIdx = FindColumn(header, "otpAuth", "login_totp", "totp");
        int typeIdx = FindColumn(header, "type");
        if (source == CsvSource.Bitwarden && (userIdx < 0 || passIdx < 0)) throw new InvalidDataException("Select a Bitwarden CSV with login_username and login_password columns.");
        if (source == CsvSource.Chrome && (userIdx < 0 || passIdx < 0 || urlIdx < 0)) throw new InvalidDataException("Select a Chrome CSV with name, url, username and password columns.");
        var entries = new List<VaultEntry>();
        int skipped = 0;
        int dataRows = rows.Count - 1;

        for (int r = 1; r < rows.Count; r++)
        {
            var row = rows[r];
            if (IsEmptyRow(row)) { dataRows--; continue; }

            if (source == CsvSource.Bitwarden && typeIdx >= 0 && Get(row, typeIdx) is not ("login" or "note" or "")) { skipped++; continue; }
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

            string setup = Get(row, totpIdx).Trim();
            if (setup.Length > 0)
            {
                // Reject unsupported authenticator parameters; never silently attach a wrong code generator.
                entry.TotpSecret = setup.StartsWith("otpauth://", StringComparison.OrdinalIgnoreCase) ? AuthenticatorSetup.Parse(setup).Secret :
                    Totp.IsValid(setup) ? setup.Replace(" ", "").ToUpperInvariant() : throw new InvalidDataException("Invalid authenticator key in CSV row " + (r + 1));
            }
            if (source == CsvSource.Bitwarden && Get(row, typeIdx) == "note") entry.Kind = EntryKind.SecureNote;
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