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
    public enum FilterKind { All, Favorites, Category }

    private readonly VaultRepository _repository;
    private readonly ClipboardService _clipboard;
    private readonly SettingsService _settings;
    private readonly AutoLockService _autoLock;
    private readonly DispatcherTimer _statusTimer;
    private readonly DispatcherTimer _countdownTimer;

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
            Entries.Add(new VaultEntryViewModel(entry));

        Categories = new ObservableCollection<CategoryViewModel>();
        foreach (var cat in _repository.Document.Categories.OrderBy(c => c.Order))
            Categories.Add(new CategoryViewModel(cat));

        EntriesView = CollectionViewSource.GetDefaultView(Entries);
        EntriesView.Filter = FilterEntry;

        _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _statusTimer.Tick += (_, _) => { _statusTimer.Stop(); StatusMessage = null; };

        _countdownTimer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(16)   // ~60fps, runs regardless of focus
        };
        _countdownTimer.Tick += OnCountdownTick;

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
            }
        }
    }

    public Guid? SelectedCategoryId
    {
        get => _selectedCategoryId;
        private set => SetProperty(ref _selectedCategoryId, value);
    }

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
    public string EmptyStateText =>
        _repository.Document.Entries.Count == 0
            ? "No entries yet — press Ctrl+N or click + New to add one."
            : "No entries match your filter or search.";

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
        if (dialog.ShowDialog() != true) return;

        var category = new Category
        {
            Name = dialog.CategoryName.Trim(),
            Order = Categories.Count == 0 ? 0 : Categories.Max(c => c.Model.Order) + 1
        };
        _repository.Document.Categories.Add(category);
        _repository.Save();
        Categories.Add(new CategoryViewModel(category));
    }

    private void RenameCategory(CategoryViewModel? catVm)
    {
        if (catVm is null) return;

        var existing = Categories.Where(c => c.Id != catVm.Id).Select(c => c.Name).ToList();
        var dialog = new CategoryNameDialog("Rename Category", catVm.Name, existing)
        {
            Owner = Application.Current.MainWindow
        };
        if (dialog.ShowDialog() != true) return;

        catVm.Model.Name = dialog.CategoryName.Trim();
        _repository.Save();
        catVm.RefreshName();
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

        foreach (var entry in _repository.Document.Entries.Where(e => e.CategoryId == catVm.Id))
            entry.CategoryId = null;

        _repository.Document.Categories.Remove(catVm.Model);
        _repository.Save();
        Categories.Remove(catVm);

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

        if (_currentFilter == FilterKind.Favorites && !vm.IsFavorite) return false;

        if (_currentFilter == FilterKind.Category)
        {
            if (_selectedCategoryId is null) return false;
            if (vm.Model.CategoryId != _selectedCategoryId) return false;
        }

        return vm.Matches(_searchText);
    }

    // ---------- entry ops ----------

    private VaultEntryViewModel? Resolve(object? param) =>
        param as VaultEntryViewModel ?? _selectedEntry;

    private void ShowStatus(string message)
    {
        StatusMessage = message;
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
        _repository.Document.Entries.Add(entry);
        _repository.Save();

        var vm = new VaultEntryViewModel(entry);
        Entries.Add(vm);
        SelectedEntry = vm;
        RefreshEmptyState();
        ShowStatus("Entry added.");
    }

    private void EditEntry(VaultEntryViewModel? vm)
    {
        if (vm is null) return;

        var editor = new EntryEditorViewModel(vm.Model, SnapshotCategories());
        if (!ShowEditor(editor)) return;

        editor.ApplyTo(vm.Model);
        _repository.Save();
        vm.RefreshAll();
        EntriesView.Refresh();
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(EmptyStateText));
        ShowStatus("Entry updated.");
    }

    private void DeleteEntry(VaultEntryViewModel? vm)
    {
        if (vm is null) return;

        if (!Dialogs.Confirm(
                $"Delete \"{vm.Title}\"?\n\nThis cannot be undone.",
                "Delete entry"))
            return;

        _repository.Document.Entries.Remove(vm.Model);
        _repository.Save();
        Entries.Remove(vm);
        if (SelectedEntry == vm) SelectedEntry = null;
        RefreshEmptyState();
        ShowStatus("Entry deleted.");
    }

    private void CopyPassword(VaultEntryViewModel? vm)
    {
        if (vm is null || string.IsNullOrEmpty(vm.Password)) return;
        _clipboard.SetSecret(vm.Password);

        int secs = _settings.Current.ClearClipboardSeconds;
        if (secs > 0) StartCountdown("Password copied", secs);
        else ShowStatus("Password copied to clipboard.");
    }

    private void CopyUsername(VaultEntryViewModel? vm)
    {
        if (vm is null || string.IsNullOrEmpty(vm.Model.Username)) return;
        _clipboard.SetSecret(vm.Model.Username);

        int secs = _settings.Current.ClearClipboardSeconds;
        if (secs > 0) StartCountdown("Username copied", secs);
        else ShowStatus("Username copied to clipboard.");
    }

    private void ToggleFavorite(VaultEntryViewModel? vm)
    {
        if (vm is null) return;
        vm.Model.IsFavorite = !vm.Model.IsFavorite;
        vm.Model.UpdatedAt = DateTimeOffset.UtcNow;
        _repository.Save();
        vm.RefreshAll();
        EntriesView.Refresh();
    }

    // ---------- countdown ----------

    private void StartCountdown(string message, int seconds)
    {
        StopCountdown();

        CountdownMessage = message;
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
        return window.ShowDialog() == true;
    }

    private void OpenSettings()
    {
        var vm = new SettingsViewModel(_settings, _repository);
        var window = new SettingsWindow { DataContext = vm, Owner = Application.Current.MainWindow };
        if (window.ShowDialog() == true)
        {
            _autoLock.SetIdleTimeout(_settings.Current.AutoLockMinutes);
            _clipboard.ClearSeconds = _settings.Current.ClearClipboardSeconds;
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
        if (window.ShowDialog() == true && window.SelectedEntry is not null)
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

        if (window.ImportedEntries is { Count: > 0 } imported)
        {
            foreach (var entry in imported)
            {
                _repository.Document.Entries.Add(entry);
                Entries.Add(new VaultEntryViewModel(entry));
            }
            _repository.Save();
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
        StopCountdown();
        _autoLock.TimeToLock -= OnTimeToLock;
        _statusTimer.Stop();
        Locked?.Invoke(this, EventArgs.Empty);
    }
}