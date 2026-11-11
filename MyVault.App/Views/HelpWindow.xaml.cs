using System.Windows;

namespace MyVault.App.Views;

public partial class HelpWindow : GlassWindow
{
    public HelpWindow()
    {
        InitializeComponent();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}