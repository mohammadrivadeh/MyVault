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

        ChangeButton.IsEnabled = false;
        try
        {
            bool ok = await Vm.TryChangeAsync();
            if (ok)
            {
                DialogResult = true;
                Close();
            }
        }
        finally
        {
            ChangeButton.IsEnabled = true;
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}