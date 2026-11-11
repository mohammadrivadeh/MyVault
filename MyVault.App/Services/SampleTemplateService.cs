using System.IO;

namespace MyVault.App.Services;

public static class SampleTemplateService
{
    /// <summary>Writes a CSV template with the expected column headers and one example row.</summary>
    public static void WriteTemplate(string path)
    {
        string csv =
            "Title,Username,Password,URL,Notes,Tags\r\n" +
            "Example entry,user@example.com,replace-me,https://example.com,Optional note,\"personal, imported\"\r\n";
        File.WriteAllText(path, csv);
    }
}