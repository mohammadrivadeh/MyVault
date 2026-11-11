using System.IO;
using System.Text;
using Microsoft.Win32;
using MyVault.App.Services;
using MyVault.Core.Models;
using MyVault.Core.Services;
using MyVault.Core.Storage;

namespace MyVault.App.ViewModels;

public sealed class ImportExportViewModel : ObservableObject
{
    private readonly VaultRepository _repository;
    private readonly BackupService _backup;

    public ImportExportViewModel(VaultRepository repository, BackupService backup)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _backup = backup ?? throw new ArgumentNullException(nameof(backup));

        RestoreBackupCommand = new RelayCommand(_ => RestoreBackup());
        SaveBackupCommand = new RelayCommand(_ => SaveBackup());
        ExportCsvCommand = new RelayCommand(_ => ExportCsv());
        ImportCsvCommand = new RelayCommand(_ => ImportCsv());
        DownloadTemplateCommand = new RelayCommand(_ => DownloadTemplate());
    }

    public event EventHandler? CloseRequested;

    public IReadOnlyList<VaultEntry>? ImportedEntries { get; private set; }
    public string? FinalStatusMessage { get; private set; }

    public bool Restored { get; private set; }
    public RelayCommand RestoreBackupCommand { get; }
    public RelayCommand SaveBackupCommand { get; }
    public RelayCommand ExportCsvCommand { get; }
    public RelayCommand ImportCsvCommand { get; }
    public RelayCommand DownloadTemplateCommand { get; }

    // --------------------------------------------------------------------

    private void RestoreBackup()
    {
        var dialog = new OpenFileDialog { Title = "Restore encrypted backup", Filter = "Vault and backups|*.vault;*.dat;*.bak.*|All files|*.*" };
        if (dialog.ShowDialog() != true) return;
        if (!Dialogs.Confirm("Replace current entries with this backup? The current vault will be rotated into a backup first. Backups from before a master-password change require their original password and must be opened separately.", "Restore backup")) return;
        byte[]? blob = null;
        try
        {
            blob = VaultFile.Read(dialog.FileName);
            _repository.RestoreBackup(blob);
            Restored = true; FinalStatusMessage = "Backup restored."; CloseRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex) { Dialogs.Error("Restore failed. Current vault was preserved.\n\n" + ex.Message, "Restore failed"); }
        finally { if (blob is not null) Array.Clear(blob); }
    }

    private void SaveBackup()
    {
        var dlg = new SaveFileDialog
        {
            Filter = "MyVault backup (*.vault)|*.vault|All files (*.*)|*.*",
            FileName = $"myvault-backup-{DateTime.Now:yyyy-MM-dd-HHmm}.vault",
            Title = "Save encrypted backup"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            _backup.SaveEncryptedCopy(dlg.FileName);
            FinalStatusMessage = "Encrypted backup saved.";
            Dialogs.Success(
                "Encrypted backup saved.\n\n" +
                "The file is fully encrypted and can only be opened with your master password.",
                "Backup complete");
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            Dialogs.Error("Backup failed:\n\n" + ex.Message, "Backup error");
        }
    }

    private void ExportCsv()
    {
        if (_repository.Document.Entries.Count == 0)
        {
            Dialogs.Info("There are no entries to export.", "Nothing to export");
            return;
        }

        if (!Dialogs.Confirm(
                "This will create a PLAINTEXT CSV file with every username and password in your vault.\n\n" +
                "Anyone who gets that file can read all your passwords.\n\nCSV excludes Trash, categories, favorites, TOTP and timestamps. Use an encrypted backup for a complete copy. Opening untrusted CSV in a spreadsheet may interpret formulas.\n\n" +
                "Delete it as soon as you've imported it into whatever tool you need.\n\n" +
                "Continue?",
                "Export to CSV"))
            return;

        var dlg = new SaveFileDialog
        {
            Filter = "CSV file (*.csv)|*.csv",
            FileName = $"myvault-export-{DateTime.Now:yyyy-MM-dd}.csv",
            Title = "Export to CSV (plaintext)"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            var header = new[] { "Title", "Username", "Password", "URL", "Notes", "Tags" };
            var rows = new List<IEnumerable<string?>> { header };

            foreach (var e in _repository.Document.Entries.Where(e => e.DeletedAt is null))
            {
                rows.Add(new[]
                {
                    e.Title,
                    e.Username,
                    e.Password,
                    e.Url,
                    e.Notes,
                    string.Join(", ", e.Tags)
                });
            }

            string csv = CsvWriter.Write(rows);
            File.WriteAllText(dlg.FileName, csv, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            FinalStatusMessage = $"Exported {_repository.Document.Entries.Count} entries to CSV.";
            Dialogs.Success(
                $"Exported {_repository.Document.Entries.Count} entries to:\n{dlg.FileName}\n\n" +
                "Remember to delete the file when you no longer need it.",
                "Export complete");
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            Dialogs.Error("Export failed:\n\n" + ex.Message, "Export error");
        }
    }

    private void ImportCsv()
    {
        var dlg = new OpenFileDialog
        {
            Filter = "CSV file (*.csv)|*.csv|All files (*.*)|*.*",
            Title = "Import from CSV"
        };
        if (dlg.ShowDialog() != true) return;

        CsvImportPreview preview;
        try
        {
            string text = File.ReadAllText(dlg.FileName, Encoding.UTF8);
            preview = CsvImporter.Analyze(text);
        }
        catch (Exception ex)
        {
            Dialogs.Error("Could not read the CSV file:\n\n" + ex.Message, "Import error");
            return;
        }

        if (preview.Entries.Count == 0)
        {
            Dialogs.Info(
                "No importable entries were found in the CSV.\n\n" +
                "The file must contain a header row with at least a 'title' column.",
                "Nothing to import");
            return;
        }

        string message = $"Found {preview.Entries.Count} entries to import.";
        if (preview.Skipped > 0)
            message += $"\n{preview.Skipped} rows skipped (missing title).";
        message += "\n\nExact matches by title, username and URL will be skipped. Existing entries will not be overwritten. Add the remaining entries?";

        if (!Dialogs.Confirm(message, "Import from CSV"))
            return;

        ImportedEntries = preview.Entries.DistinctBy(e => (e.Title.ToUpperInvariant(), e.Username, e.Url)).Where(entry => !_repository.Document.Entries.Any(e => e.DeletedAt is null && string.Equals(e.Title, entry.Title, StringComparison.OrdinalIgnoreCase) && e.Username == entry.Username && e.Url == entry.Url)).ToList();
        FinalStatusMessage = $"{ImportedEntries.Count} entries imported; {preview.Entries.Count - ImportedEntries.Count} duplicates skipped.";
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void DownloadTemplate()
    {
        var dlg = new SaveFileDialog
        {
            Filter = "CSV template (*.csv)|*.csv",
            FileName = "myvault-import-template.csv",
            Title = "Save CSV template"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            SampleTemplateService.WriteTemplate(dlg.FileName);
            Dialogs.Success(
                "CSV template saved. Fill it in and import it back with 'Import from CSV…'.",
                "Template ready");
        }
        catch (Exception ex)
        {
            Dialogs.Error("Could not write template:\n" + ex.Message, "Template error");
        }
    }
}