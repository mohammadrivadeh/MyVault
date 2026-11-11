using System.Windows;

namespace MyVault.App.Views;

public partial class AboutWindow : GlassWindow
{
    public AboutWindow()
    {
        InitializeComponent();

        var version = typeof(AboutWindow).Assembly.GetName().Version;
        if (version is not null)
            VersionText.Text = $"Version {version.Major}.{version.Minor}.{version.Build}";
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}