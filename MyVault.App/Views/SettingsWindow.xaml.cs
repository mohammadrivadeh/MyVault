using System.Windows;
using MyVault.App.ViewModels;

namespace MyVault.App.Views;

public partial class SettingsWindow : GlassWindow
{
    public SettingsWindow()
    {
        InitializeComponent();
    }

    protected override void OnClosed(EventArgs e)
    {
        if (DialogResult != true && DataContext is SettingsViewModel vm) vm.RevertPreview();
        base.OnClosed(e);
    }
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
        {
            if (!vm.Apply()) { MyVault.App.Services.Dialogs.Error(vm.ValidationError ?? "Settings could not be saved. Check folder permissions.", "Save failed", this); return; }
            DialogResult = true;
        }
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}