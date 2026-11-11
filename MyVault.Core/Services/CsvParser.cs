using System.Text;

namespace MyVault.Core.Services;

/// <summary>
/// Minimal RFC 4180 CSV parser. Handles quoted fields, embedded commas/quotes/newlines,
/// and both CRLF and LF line endings. Returns rows of raw field values.
/// </summary>
public static class CsvParser
{
    public static List<List<string>> Parse(string text)
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var field = new StringBuilder();
        bool inQuotes = false;
        int i = 0;

        void EndField()
        {
            row.Add(field.ToString());
            field.Clear();
        }

        void EndRow()
        {
            EndField();
            // Skip rows that are entirely one empty field (blank line)
            if (!(row.Count == 1 && row[0].Length == 0))
                rows.Add(row);
            row = new List<string>();
        }

        while (i < text.Length)
        {
            char c = text[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"')
                    {
                        field.Append('"');
                        i += 2;
                    }
                    else
                    {
                        inQuotes = false;
                        i++;
                    }
                }
                else
                {
                    field.Append(c);
                    i++;
                }
            }
            else if (c == '"') { inQuotes = true; i++; }
            else if (c == ',') { EndField(); i++; }
            else if (c == '\r') { i++; }
            else if (c == '\n') { EndRow(); i++; }
            else { field.Append(c); i++; }
        }

        if (inQuotes) throw new InvalidDataException("Unterminated quoted CSV field.");
        if (field.Length > 0 || row.Count > 0)
            EndRow();

        return rows;
    }
}