using System.Windows;
using MyVault.App.ViewModels;
using MyVault.Core.Models;
using MyVault.Core.Services;

namespace MyVault.App.Views;

public partial class HealthWindow : GlassWindow
{
    public VaultEntry? SelectedEntry { get; private set; }

    public HealthWindow()
    {
        InitializeComponent();
    }

    private void Fix_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: HealthFinding finding })
        {
            SelectedEntry = finding.Entry;
            DialogResult = true;
            Close();
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}