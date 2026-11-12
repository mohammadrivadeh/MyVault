using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using MyVault.App.ViewModels;

namespace MyVault.App.Views;

public partial class EntryEditorWindow : GlassWindow
{
    private bool _isRevealed;
    private bool _syncing;

    public EntryEditorWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private EntryEditorViewModel? Vm => DataContext as EntryEditorViewModel;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (Vm is null) return;

        Vm.PropertyChanged += Vm_PropertyChanged;

        // Initial fill of PasswordBox from the VM.
        _syncing = true;
        PwdBox.Password = Vm.Password;
        TotpBox.Password = Vm.TotpSecret;
        _syncing = false;

        TitleBox.Focus();
        TitleBox.SelectAll();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (DialogResult != true && Vm is { HasUnsavedChanges: true })
        {
            bool discard = MyVault.App.Services.Dialogs.Confirm("Discard unsaved changes?", "Unsaved changes", this);
            // Session locking clears DataContext and must never be vetoed by a discard prompt.
            if (Vm is not null && !discard) e.Cancel = true;
        }
        base.OnClosing(e);
    }
    protected override void OnClosed(EventArgs e)
    {
        if (Vm is not null) Vm.PropertyChanged -= Vm_PropertyChanged;
        base.OnClosed(e);
    }

    private void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(EntryEditorViewModel.Password)) return;
        if (Vm is null || _syncing) return;

        // Password was changed programmatically (e.g. Generate) — sync to the visible control.
        _syncing = true;
        if (_isRevealed) RevealBox.Text = Vm.Password;
        else PwdBox.Password = Vm.Password;
        _syncing = false;
    }

    private void PwdBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_syncing || Vm is null) return;
        _syncing = true;
        Vm.Password = PwdBox.Password;
        if (_isRevealed) RevealBox.Text = PwdBox.Password;
        _syncing = false;
    }

    private void RevealBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_syncing || Vm is null) return;
        _syncing = true;
        Vm.Password = RevealBox.Text;
        if (!_isRevealed) PwdBox.Password = RevealBox.Text;
        _syncing = false;
    }

    private void ToggleReveal_Click(object sender, RoutedEventArgs e)
    {
        _isRevealed = !_isRevealed;
        _syncing = true;
        if (_isRevealed)
        {
            RevealBox.Text = PwdBox.Password;
            RevealBox.Visibility = Visibility.Visible;
            PwdBox.Visibility = Visibility.Collapsed;
            RevealBox.Focus();
            RevealBox.CaretIndex = RevealBox.Text.Length;
        }
        else
        {
            PwdBox.Password = RevealBox.Text;
            PwdBox.Visibility = Visibility.Visible;
            RevealBox.Visibility = Visibility.Collapsed;
            PwdBox.Focus();
        }
        _syncing = false;
    }

    private void TotpBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (!_syncing && Vm is not null) Vm.TotpSecret = TotpBox.Password;
    }
    private async void ImportQr_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog { Title = MyVault.App.Services.LocalizationService.Text("Import authenticator QR image"), Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp" };
        if (dialog.ShowDialog() != true) return;
        if (sender is not System.Windows.Controls.Button button) return;
        button.IsEnabled = false;
        try
        {
            var setup = await System.Threading.Tasks.Task.Run(() => MyVault.App.Services.QrSetupReader.Read(dialog.FileName));
            if (Vm is not { } vm) return;
            if (vm.TotpSecret.Length > 0 && !MyVault.App.Services.Dialogs.Confirm("Replace this entry's authenticator key?", "Replace authenticator key", this)) return;
            if (Vm is null) return;
            vm.TotpSecret = setup.Secret; TotpBox.Password = setup.Secret;
            if (string.IsNullOrWhiteSpace(vm.Title)) vm.Title = setup.Account;
        }
        catch (Exception ex) when (ex is ArgumentException or System.IO.IOException or NotSupportedException or System.IO.FileFormatException)
        { if (Vm is not null) MyVault.App.Services.Dialogs.Error("Could not import the QR image. " + ex.Message, "QR import failed", this); }
        finally { button.IsEnabled = true; }
    }
    private void ImportLink_Click(object sender, RoutedEventArgs e)
    {
        if (Vm is not { } vm) return;
        try
        {
            var setup = MyVault.Core.Services.AuthenticatorSetup.Parse(SetupLinkBox.Password);
            if (vm.TotpSecret.Length > 0 && !MyVault.App.Services.Dialogs.Confirm("Replace this entry's authenticator key?", "Replace authenticator key", this)) return;
            if (Vm is null) return;
            vm.TotpSecret = setup.Secret; TotpBox.Password = setup.Secret; SetupLinkBox.Clear();
        }
        catch (ArgumentException ex) { MyVault.App.Services.Dialogs.Error(ex.Message, "Setup link not supported", this); }
    }
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (Vm is null) return;

        // Pull the freshest password value from the currently-visible control.
        Vm.Password = _isRevealed ? RevealBox.Text : PwdBox.Password;

        string? error = Vm.Validate();
        if (error is not null)
        {
            Vm.ErrorText = error;
            return;
        }

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}