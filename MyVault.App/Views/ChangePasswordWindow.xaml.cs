using System.Windows;
using System.Windows.Controls;
using MyVault.App.ViewModels;

namespace MyVault.App.Views;

public partial class ChangePasswordWindow : GlassWindow
{
    public ChangePasswordWindow()
    {
        InitializeComponent();
    }

    private bool _closed;
    protected override void OnClosed(EventArgs e) { _closed = true; base.OnClosed(e); }
    private ChangePasswordViewModel? Vm => DataContext as ChangePasswordViewModel;

    private void CurrentBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (Vm is not null && sender is PasswordBox box)
            Vm.CurrentPassword = box.Password;
    }

    private void NewBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (Vm is not null && sender is PasswordBox box)
            Vm.NewPassword = box.Password;
    }

    private void ConfirmBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (Vm is not null && sender is PasswordBox box)
            Vm.ConfirmPassword = box.Password;
    }

    private async void Change_Click(object sender, RoutedEventArgs e)
    {
        if (Vm is null) return;

        if (Vm.IsBusy) return;
        ChangeButton.IsEnabled = false;
        try
        {
            bool ok = await Vm.TryChangeAsync();
            if (ok && !_closed)
            {
                DialogResult = true;
                Close();
            }
        }
        finally
        {
            if (!_closed) ChangeButton.IsEnabled = true;
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        if (Vm?.IsBusy == true) return;
        DialogResult = false;
        Close();
    }
}