using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using MyVault.App.Services;
using MyVault.App.Views;
using MyVault.Core.Models;
using MyVault.Core.Storage;

namespace MyVault.App.ViewModels;

public sealed class VaultViewModel : ObservableObject
{
    public enum FilterKind { All, Favorites, Category, Weak, Reused, Recent, Trash }

    private readonly VaultRepository _repository;
    private readonly ClipboardService _clipboard;
    private readonly SettingsService _settings;
    private readonly AutoLockService _autoLock;
    private readonly DispatcherTimer _statusTimer;
    private readonly DispatcherTimer _countdownTimer;

    private string _tagFilter = "";
    private bool _locked;
    private string _sortBy = "Title";
    private string _searchText = string.Empty;
    private VaultEntryViewModel? _selectedEntry;
    private FilterKind _currentFilter = FilterKind.All;
    private Guid? _selectedCategoryId;
    private string? _statusMessage;

    private string _countdownMessage = string.Empty;
    private int _countdownSeconds;
    private int _countdownTotal;
    private double _countdownProgress;
    private Stopwatch? _countdownWatch;
    private double _countdownDurationSeconds;
    private bool _isCountingDown;

    public VaultViewModel(
        VaultRepository repository,
        ClipboardService clipboard,
        SettingsService settings,
        AutoLockService autoLock)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _clipboard = clipboard ?? throw new ArgumentNullException(nameof(clipboard));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _autoLock = autoLock ?? throw new ArgumentNullException(nameof(autoLock));

        _autoLock.TimeToLock += OnTimeToLock;

        Entries = new ObservableCollection<VaultEntryViewModel>();
        foreach (var entry in _repository.Document.Entries)
            Entries.Add(WrapEntry(entry));

        Categories = new ObservableCollection<CategoryViewModel>();
        foreach (var cat in _repository.Document.Categories.OrderBy(c => c.Order))
            Categories.Add(new CategoryViewModel(cat));

        EntriesView = CollectionViewSource.GetDefaultView(Entries);
        EntriesView.Filter = FilterEntry;
        ApplySort();

        _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _statusTimer.Tick += (_, _) => { _statusTimer.Stop(); StatusMessage = null; };

        _countdownTimer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(16)   // ~60fps, runs regardless of focus
        };
        _countdownTimer.Tick += OnCountdownTick;

        RestoreCommand = new RelayCommand(p => RestoreEntry(Resolve(p)));
        CopyTotpCommand = new RelayCommand(p => CopyTotp(Resolve(p)));
        LockCommand = new RelayCommand(_ => Lock());
        AddCommand = new RelayCommand(_ => AddEntry());
        EditCommand = new RelayCommand(p => EditEntry(Resolve(p)), p => Resolve(p) is not null);
        DeleteCommand = new RelayCommand(p => DeleteEntry(Resolve(p)), p => Resolve(p) is not null);
        CopyPasswordCommand = new RelayCommand(p => CopyPassword(Resolve(p)), p => Resolve(p) is not null);
        CopyUsernameCommand = new RelayCommand(p => CopyUsername(Resolve(p)), p => Resolve(p) is not null);
        ToggleFavoriteCommand = new RelayCommand(p => ToggleFavorite(Resolve(p)), p => Resolve(p) is not null);
        SetFilterCommand = new RelayCommand(p => SetFilter(p));
        SettingsCommand = new RelayCommand(_ => OpenSettings());
        HealthCommand = new RelayCommand(_ => OpenHealth());
        ImportExportCommand = new RelayCommand(_ => OpenImportExport());
        HelpCommand = new RelayCommand(_ => OpenHelp());
        AboutCommand = new RelayCommand(_ => OpenAbout());

        AddCategoryCommand = new RelayCommand(_ => AddCategory());
        RenameCategoryCommand = new RelayCommand(p => RenameCategory(p as CategoryViewModel), p => p is CategoryViewModel);
        DeleteCategoryCommand = new RelayCommand(p => DeleteCategory(p as CategoryViewModel), p => p is CategoryViewModel);
        SelectCategoryCommand = new RelayCommand(p => SelectCategory(p as CategoryViewModel), p => p is CategoryViewModel);
    }

    public event EventHandler? Locked;

    public ObservableCollection<VaultEntryViewModel> Entries { get; }
    public ObservableCollection<CategoryViewModel> Categories { get; }
    public ICollectionView EntriesView { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                EntriesView.Refresh();
                OnPropertyChanged(nameof(IsEmpty));
                OnPropertyChanged(nameof(EmptyStateText));
            }
        }
    }

    public VaultEntryViewModel? SelectedEntry
    {
        get => _selectedEntry;
        set => SetProperty(ref _selectedEntry, value);
    }

    public FilterKind CurrentFilter
    {
        get => _currentFilter;
        private set
        {
            if (SetProperty(ref _currentFilter, value))
            {
                OnPropertyChanged(nameof(IsAllFilter));
                OnPropertyChanged(nameof(IsFavoritesFilter));
                OnPropertyChanged(nameof(IsWeakFilter)); OnPropertyChanged(nameof(IsReusedFilter));
                OnPropertyChanged(nameof(IsRecentFilter)); OnPropertyChanged(nameof(IsTrashFilter));
            }
        }
    }

    public Guid? SelectedCategoryId
    {
        get => _selectedCategoryId;
        private set => SetProperty(ref _selectedCategoryId, value);
    }

    public bool IsWeakFilter => _currentFilter == FilterKind.Weak;
    public bool IsReusedFilter => _currentFilter == FilterKind.Reused;
    public bool IsRecentFilter => _currentFilter == FilterKind.Recent;
    public bool IsTrashFilter => _currentFilter == FilterKind.Trash;
    public bool IsAllFilter => _currentFilter == FilterKind.All;
    public bool IsFavoritesFilter => _currentFilter == FilterKind.Favorites;

    public string? StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    // ---------- countdown ----------

    public string CountdownMessage
    {
        get => _countdownMessage;
        private set => SetProperty(ref _countdownMessage, value);
    }

    public int CountdownTotal
    {
        get => _countdownTotal;
        private set => SetProperty(ref _countdownTotal, value);
    }

    public int CountdownSeconds
    {
        get => _countdownSeconds;
        private set
        {
            if (SetProperty(ref _countdownSeconds, value))
            {
                OnPropertyChanged(nameof(ShowCountdown));
                OnPropertyChanged(nameof(CountdownLabel));
            }
        }
    }

    public double CountdownProgress
    {
        get => _countdownProgress;
        private set => SetProperty(ref _countdownProgress, value);
    }

    public bool ShowCountdown => _isCountingDown && _countdownSeconds > 0 && _countdownTotal > 0;
    public string CountdownLabel => $" · {_countdownSeconds}s";

    public bool IsEmpty => EntriesView.IsEmpty;
    public string EmptyStateText => LocalizationService.Text(
        _repository.Document.Entries.Count == 0
            ? "No entries yet — press Ctrl+N or click + New to add one."
            : "No entries match your filter or search.");

    public IReadOnlyList<string> SortOptions { get; } = new[] { "Title", "Updated", "Created" };
    public string SortBy { get => _sortBy; set { if (SetProperty(ref _sortBy, value)) ApplySort(); } }
    private void ApplySort()
    {
        EntriesView.SortDescriptions.Clear();
        EntriesView.SortDescriptions.Add(new SortDescription(_sortBy == "Title" ? "Title" : _sortBy == "Updated" ? "Model.UpdatedAt" : "Model.CreatedAt", _sortBy == "Title" ? ListSortDirection.Ascending : ListSortDirection.Descending));
    }
    public int SelectionCount => Entries.Count(e => e.IsSelected);
    public string SelectionSummary=>LocalizationService.Format("{0} selected",SelectionCount);
    public string TagFilter { get => _tagFilter; set { if (SetProperty(ref _tagFilter, value)) { EntriesView.Refresh(); RefreshEmptyState(); } } }
    private VaultEntryViewModel WrapEntry(VaultEntry entry)
    {
        var vm = new VaultEntryViewModel(entry);
        vm.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(VaultEntryViewModel.IsSelected)) { OnPropertyChanged(nameof(SelectionCount)); OnPropertyChanged(nameof(SelectionSummary)); CommandManager.InvalidateRequerySuggested(); } };
        return vm;
    }
    public ICommand RestoreCommand { get; }
    public ICommand CopyTotpCommand { get; }
    public ICommand LockCommand { get; }
    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand CopyPasswordCommand { get; }
    public ICommand CopyUsernameCommand { get; }
    public ICommand ToggleFavoriteCommand { get; }
    public ICommand SetFilterCommand { get; }
    public ICommand SettingsCommand { get; }
    public ICommand HealthCommand { get; }
    public ICommand ImportExportCommand { get; }
    public ICommand HelpCommand { get; }
    public ICommand AboutCommand { get; }

    public ICommand AddCategoryCommand { get; }
    public ICommand RenameCategoryCommand { get; }
    public ICommand DeleteCategoryCommand { get; }
    public ICommand SelectCategoryCommand { get; }

    // ---------- category ops ----------

    private void AddCategory()
    {
        var existing = Categories.Select(c => c.Name).ToList();
        var dialog = new CategoryNameDialog("New Category", "", existing)
        {
            Owner = Application.Current.MainWindow
        };
        if (dialog.ShowDialog() != true || _locked) return;

        var category = new Category
        {
            Name = dialog.CategoryName.Trim(),
            Order = Categories.Count == 0 ? 0 : Categories.Max(c => c.Model.Order) + 1
        };
        Commit(d => d.Categories.Add(category));
    }

    private void RenameCategory(CategoryViewModel? catVm)
    {
        if (catVm is null) return;

        var existing = Categories.Where(c => c.Id != catVm.Id).Select(c => c.Name).ToList();
        var dialog = new CategoryNameDialog("Rename Category", catVm.Name, existing)
        {
            Owner = Application.Current.MainWindow
        };
        if (dialog.ShowDialog() != true || _locked) return;

        Commit(d => d.Categories.Single(c => c.Id == catVm.Id).Name = dialog.CategoryName.Trim());
    }

    private void DeleteCategory(CategoryViewModel? catVm)
    {
        if (catVm is null) return;

        int entryCount = _repository.Document.Entries.Count(e => e.CategoryId == catVm.Id);
        string msg = entryCount > 0
            ? $"Delete category \"{catVm.Name}\"?\n\n{entryCount} {(entryCount == 1 ? "entry" : "entries")} will be moved to (None)."
            : $"Delete category \"{catVm.Name}\"?";

        if (!Dialogs.Confirm(msg, "Delete category"))
            return;

        if (!Commit(d =>
        {
            foreach (var entry in d.Entries.Where(e => e.CategoryId == catVm.Id)) entry.CategoryId = null;
            d.Categories.RemoveAll(c => c.Id == catVm.Id);
        })) return;

        if (SelectedCategoryId == catVm.Id)
            ApplyFilter(FilterKind.All, null);
    }

    private void SelectCategory(CategoryViewModel? catVm)
    {
        if (catVm is null) return;
        ApplyFilter(FilterKind.Category, catVm.Id);
    }

    private void SetFilter(object? param)
    {
        if (param is string s && Enum.TryParse<FilterKind>(s, out var kind) && kind != FilterKind.Category)
            ApplyFilter(kind, null);
    }

    private void ApplyFilter(FilterKind kind, Guid? categoryId)
    {
        CurrentFilter = kind;
        SelectedCategoryId = categoryId;

        foreach (var cat in Categories)
            cat.IsSelected = kind == FilterKind.Category && cat.Id == categoryId;

        EntriesView.Refresh();
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(EmptyStateText));
    }

    private bool FilterEntry(object obj)
    {
        if (obj is not VaultEntryViewModel vm) return false;
        if ((_currentFilter == FilterKind.Trash) != (vm.Model.DeletedAt is not null)) return false;
        if (_currentFilter == FilterKind.Weak && MyVault.Core.Services.PasswordStrength.Categorize(vm.Password) != MyVault.Core.Services.PasswordStrengthLevel.Weak) return false;
        if (_currentFilter == FilterKind.Reused && (string.IsNullOrEmpty(vm.Password) || _repository.Document.Entries.Count(e => e.DeletedAt is null && e.Password == vm.Password) < 2)) return false;
        if (_currentFilter == FilterKind.Recent && vm.Model.UpdatedAt < DateTimeOffset.UtcNow.AddDays(-30)) return false;

        if (_currentFilter == FilterKind.Favorites && !vm.IsFavorite) return false;

        if (_currentFilter == FilterKind.Category)
        {
            if (_selectedCategoryId is null) return false;
            if (vm.Model.CategoryId != _selectedCategoryId) return false;
        }

        if (!string.IsNullOrWhiteSpace(_tagFilter) && !vm.Model.Tags.Contains(_tagFilter.Trim(), StringComparer.OrdinalIgnoreCase)) return false;
        return vm.Matches(_searchText);
    }

    // ---------- entry ops ----------

    private VaultEntryViewModel? Resolve(object? param) =>
        param as VaultEntryViewModel ?? _selectedEntry;

    private void ShowStatus(string message)
    {
        StatusMessage = LocalizationService.Text(message);
        _statusTimer.Stop();
        _statusTimer.Start();
    }

    private void RefreshEmptyState()
    {
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(EmptyStateText));
    }

    private List<Category> SnapshotCategories() =>
        Categories.Select(c => c.Model).ToList();

    private void AddEntry()
    {
        var editor = new EntryEditorViewModel(null, SnapshotCategories());
        if (!ShowEditor(editor)) return;

        var entry = new VaultEntry();
        editor.ApplyTo(entry);
        if (!Commit(d => d.Entries.Add(entry))) return;
        SelectedEntry = Entries.FirstOrDefault(e => e.Id == entry.Id);
        RefreshEmptyState();
        ShowStatus("Entry added.");
    }

    private void EditEntry(VaultEntryViewModel? vm)
    {
        if (vm is null || vm.Model.DeletedAt is not null) return;

        var editor = new EntryEditorViewModel(vm.Model, SnapshotCategories());
        if (!ShowEditor(editor)) return;

        if (!Commit(d => editor.ApplyTo(d.Entries.Single(e => e.Id == vm.Id)))) return;
        EntriesView.Refresh();
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(EmptyStateText));
        ShowStatus("Entry updated.");
    }

    private void DeleteEntry(VaultEntryViewModel? vm)
    {
        if (vm is null) return;

        if (!Dialogs.Confirm(
                LocalizationService.Format("Delete \"{0}\"?\n\nThis moves the entry to Trash. Deleting it again from Trash is permanent.", vm.Title),
                "Delete entry"))
            return;

        if (!Commit(d =>
        {
            var entry = d.Entries.Single(e => e.Id == vm.Id);
            if (entry.DeletedAt is not null) d.Entries.Remove(entry);
            else entry.DeletedAt = DateTimeOffset.UtcNow;
        })) return;
        if (SelectedEntry == vm) SelectedEntry = null;
        RefreshEmptyState();
        ShowStatus("Entry deleted.");
    }

    private void CopyPassword(VaultEntryViewModel? vm)
    {
        if (vm is null || vm.Model.DeletedAt is not null || string.IsNullOrEmpty(vm.Password)) return;
        if (!_clipboard.SetSecret(vm.Password)) { Dialogs.Error("Clipboard is busy. Try again.", "Copy failed"); return; }

        int secs = _settings.Current.ClearClipboardSeconds;
        if (secs > 0) StartCountdown("Password copied", secs);
        else ShowStatus("Password copied to clipboard.");
    }

    private void CopyUsername(VaultEntryViewModel? vm)
    {
        if (vm is null || vm.Model.DeletedAt is not null || string.IsNullOrEmpty(vm.Model.Username)) return;
        if (!_clipboard.SetSecret(vm.Model.Username)) { Dialogs.Error("Clipboard is busy. Try again.", "Copy failed"); return; }

        int secs = _settings.Current.ClearClipboardSeconds;
        if (secs > 0) StartCountdown("Username copied", secs);
        else ShowStatus("Username copied to clipboard.");
    }

    private void ToggleFavorite(VaultEntryViewModel? vm)
    {
        if (vm is null) return;
        if (!Commit(d => { var e = d.Entries.Single(e => e.Id == vm.Id); e.IsFavorite = !e.IsFavorite; e.UpdatedAt = DateTimeOffset.UtcNow; })) return;
        EntriesView.Refresh();
    }

    private bool Commit(Action<VaultDocument> edit)
    {
        if (_locked) return false;
        bool success = false;
        try { _repository.Update(edit); success = true; }
        catch (Exception ex) when (ex is MyVault.Core.Exceptions.VaultException or System.IO.IOException or UnauthorizedAccessException or InvalidOperationException)
        { Dialogs.Error("Changes were not saved. The previous vault state was preserved.\n\n" + ex.Message, "Save failed"); }
        finally
        {
            if (!_locked) RefreshCollections();
        }
        return success;
    }
    private void RefreshCollections()
    {
        Guid? selected = SelectedEntry?.Id;
        Entries.Clear();
        foreach (var entry in _repository.Document.Entries) Entries.Add(WrapEntry(entry));
        Categories.Clear();
        foreach (var category in _repository.Document.Categories.OrderBy(c => c.Order))
            Categories.Add(new CategoryViewModel(category) { IsSelected = _currentFilter == FilterKind.Category && category.Id == SelectedCategoryId });
        SelectedEntry = Entries.FirstOrDefault(e => e.Id == selected);
        EntriesView.Refresh(); RefreshEmptyState();
        OnPropertyChanged(nameof(SelectionCount)); OnPropertyChanged(nameof(SelectionSummary));
        CommandManager.InvalidateRequerySuggested();
    }
    private void RestoreEntry(VaultEntryViewModel? vm)
    {
        if (vm is null || vm.Model.DeletedAt is null) return;
        if (Commit(d => d.Entries.Single(e => e.Id == vm.Id).DeletedAt = null)) ShowStatus("Entry restored.");
    }
    private void CopyTotp(VaultEntryViewModel? vm)
    {
        if (vm is null || vm.Model.DeletedAt is not null || !MyVault.Core.Services.Totp.IsValid(vm.Model.TotpSecret)) { Dialogs.Info("Add a valid Base32 secret in the entry editor.", "No TOTP secret"); return; }
        string code = MyVault.Core.Services.Totp.Generate(vm.Model.TotpSecret!, DateTimeOffset.UtcNow);
        if (_clipboard.SetSecret(code)) ShowStatus(LocalizationService.Format("One-time code copied. Valid for {0}s.", 30 - DateTimeOffset.UtcNow.ToUnixTimeSeconds() % 30));
        else Dialogs.Error("Clipboard is busy. Try again.", "Copy failed");
    }

    // ---------- countdown ----------

    private void StartCountdown(string message, int seconds)
    {
        StopCountdown();

        CountdownMessage = LocalizationService.Text(message);
        CountdownTotal = Math.Max(1, seconds);
        _countdownDurationSeconds = CountdownTotal;

        CountdownProgress = 1.0;
        CountdownSeconds = CountdownTotal;
        _isCountingDown = true;
        OnPropertyChanged(nameof(ShowCountdown));

        _countdownWatch = Stopwatch.StartNew();
        _countdownTimer.Start();
    }

    private void OnCountdownTick(object? sender, EventArgs e)
    {
        if (_countdownWatch is null) return;

        double elapsed = _countdownWatch.Elapsed.TotalSeconds;
        double remaining = _countdownDurationSeconds - elapsed;

        if (remaining <= 0)
        {
            StopCountdown();
            return;
        }

        CountdownProgress = remaining / _countdownDurationSeconds;

        int secs = (int)Math.Ceiling(remaining);
        if (secs != _countdownSeconds)
            CountdownSeconds = secs;
    }

    private void StopCountdown()
    {
        _countdownTimer.Stop();
        _countdownWatch = null;
        _isCountingDown = false;
        _countdownProgress = 0;
        CountdownProgress = 0;
        CountdownSeconds = 0;
        OnPropertyChanged(nameof(ShowCountdown));
    }

    private void OnTimeToLock(object? sender, EventArgs e) => Lock();

    // ---------- dialogs ----------

    private bool ShowEditor(EntryEditorViewModel editorVm)
    {
        var window = new EntryEditorWindow
        {
            DataContext = editorVm,
            Owner = Application.Current.MainWindow
        };
        return window.ShowDialog() == true && !_locked;
    }

    private void OpenSettings()
    {
        var vm = new SettingsViewModel(_settings, _repository);
        var window = new SettingsWindow { DataContext = vm, Owner = Application.Current.MainWindow };
        if (window.ShowDialog() == true && !_locked)
        {
            _autoLock.SetIdleTimeout(_settings.Current.AutoLockMinutes);
            _clipboard.ClearSeconds = _settings.Current.ClearClipboardSeconds;
            RefreshEmptyState();OnPropertyChanged(nameof(SelectionSummary));
            ShowStatus("Settings saved.");
        }
    }

    private void OpenHealth()
    {
        var window = new HealthWindow
        {
            DataContext = new HealthViewModel(_repository.Document.Entries),
            Owner = Application.Current.MainWindow
        };
        if (window.ShowDialog() == true && !_locked && window.SelectedEntry is not null)
        {
            var vm = Entries.FirstOrDefault(e => e.Id == window.SelectedEntry.Id);
            if (vm is not null) { SelectedEntry = vm; EditEntry(vm); }
        }
    }

    private void OpenImportExport()
    {
        var backup = new BackupService(_repository);
        var vm = new ImportExportViewModel(_repository, backup);
        var window = new ImportExportWindow { DataContext = vm, Owner = Application.Current.MainWindow };
        window.ShowDialog();
        if (_locked) return;
        if (window.Restored) RefreshCollections();

        if (window.ImportedEntries is { Count: > 0 } imported)
        {
            if (!Commit(d =>
            {
                foreach (var entry in imported)
                    if (!d.Entries.Any(e => e.DeletedAt is null && string.Equals(e.Title, entry.Title, StringComparison.OrdinalIgnoreCase) && e.Username == entry.Username && e.Url == entry.Url)) d.Entries.Add(entry);
            })) return;
            RefreshEmptyState();
            EntriesView.Refresh();
        }

        if (!string.IsNullOrEmpty(window.FinalStatusMessage))
            ShowStatus(window.FinalStatusMessage);
    }

    private void OpenHelp() => new HelpWindow { Owner = Application.Current.MainWindow }.ShowDialog();
    private void OpenAbout() => new AboutWindow { Owner = Application.Current.MainWindow }.ShowDialog();

    private void Lock()
    {
        if (_locked) return;
        _locked = true;
        // Hide secrets before any animation or potentially blocking repository disposal.
        if (Application.Current.MainWindow is { } main && main.Content is UIElement content) content.Visibility = Visibility.Hidden;
        foreach (Window window in Application.Current.Windows.Cast<Window>().Where(w => w != Application.Current.MainWindow).ToArray())
        {
            window.Opacity = 0;
            if (window.DataContext is SettingsViewModel preview) preview.RevertPreview();
            window.DataContext = null;
            try { window.Close(); } catch (InvalidOperationException) { /* An active Closing handler will unwind after its child prompt closes. */ }
        }
        SelectedEntry = null;
        Entries.Clear();
        Categories.Clear();
        StopCountdown();
        _autoLock.TimeToLock -= OnTimeToLock;
        _statusTimer.Stop();
        Locked?.Invoke(this, EventArgs.Empty);
        if (Application.Current.MainWindow?.Content is UIElement contentAfterLock) contentAfterLock.Visibility = Visibility.Visible;
    }
}