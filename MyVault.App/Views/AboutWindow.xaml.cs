using System.Windows;

namespace MyVault.App.Views;

public partial class AboutWindow : GlassWindow
{
    public AboutWindow()
    {
        InitializeComponent();

        var version = typeof(AboutWindow).Assembly.GetName().Version;
        if (version is not null)
            VersionText.Text = MyVault.App.Services.LocalizationService.Format("Version {0}.{1}.{2}",version.Major,version.Minor,version.Build);
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}