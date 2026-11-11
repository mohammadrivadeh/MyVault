using System.Text;

namespace MyVault.Core.Services;

public static class CsvWriter
{
    /// <summary>Writes rows to RFC 4180 CSV with CRLF line endings.</summary>
    public static string Write(IEnumerable<IEnumerable<string?>> rows)
    {
        var sb = new StringBuilder();
        bool firstRow = true;

        foreach (var row in rows)
        {
            if (!firstRow) sb.Append("\r\n");
            firstRow = false;

            bool firstField = true;
            foreach (var field in row)
            {
                if (!firstField) sb.Append(',');
                firstField = false;
                sb.Append(Escape(field));
            }
        }

        return sb.ToString();
    }

    public static string Escape(string? field)
    {
        if (string.IsNullOrEmpty(field)) return string.Empty;

        bool needsQuotes =
            field.Contains(',') ||
            field.Contains('"') ||
            field.Contains('\r') ||
            field.Contains('\n');

        if (!needsQuotes) return field;
        return "\"" + field.Replace("\"", "\"\"") + "\"";
    }
}