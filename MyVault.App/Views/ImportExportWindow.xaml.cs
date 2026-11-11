using System.Windows;
using MyVault.App.ViewModels;

namespace MyVault.App.Views;

public partial class ImportExportWindow : GlassWindow
{
    private ImportExportViewModel? _vm;

    public ImportExportWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    public IReadOnlyList<MyVault.Core.Models.VaultEntry>? ImportedEntries => _vm?.ImportedEntries;
    public string? FinalStatusMessage => _vm?.FinalStatusMessage;

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_vm is not null) _vm.CloseRequested -= OnCloseRequested;

        _vm = DataContext as ImportExportViewModel;

        if (_vm is not null) _vm.CloseRequested += OnCloseRequested;
    }

    private void OnCloseRequested(object? sender, EventArgs e)
    {
        DialogResult = true;
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        if (_vm is not null) _vm.CloseRequested -= OnCloseRequested;
        base.OnClosed(e);
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}