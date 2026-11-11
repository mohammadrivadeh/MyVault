using System.Windows;
using MyVault.App.Services;

namespace MyVault.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // One-time: move existing %APPDATA%\MyVault\* to <exe folder>\MyVault\*
        VaultPaths.MigrateFromAppDataIfNeeded();

        try
        {
            var settings = new SettingsService();
            ThemeService.Apply(settings.Current.Theme);
        }
        catch
        {
            // Fall back to whatever App.xaml loaded.
        }

        var window = new MainWindow();
        MainWindow = window;
        window.Show();
    }
}