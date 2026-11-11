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
        DialogResult = false;
        Close();
    }
}