using System.Windows;
using System.Windows.Input;
using MyVault.App.ViewModels;

namespace MyVault.App.Views;

public partial class CommandPaletteWindow : GlassWindow
{
    public CommandPaletteWindow(CommandPaletteViewModel vm)
    {
        InitializeComponent(); DataContext = vm;
        vm.CloseRequested += OnRequested;
        Closed += (_, _) => vm.CloseRequested -= OnRequested;
        Loaded += (_, _) => QueryBox.Focus();
        QueryBox.PreviewKeyDown += (_, e) => { if (e.Key == Key.Down) { Results.Focus(); e.Handled = true; } };
    }
    private void OnRequested(object? sender, EventArgs e) { DialogResult = true; Close(); }
    private void Run_Click(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is CommandPaletteViewModel vm && vm.RunCommand.CanExecute(null)) vm.RunCommand.Execute(null);
    }
}
