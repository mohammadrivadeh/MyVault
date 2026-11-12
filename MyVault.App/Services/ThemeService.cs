using System.Windows;

namespace MyVault.App.Services;

public static class ThemeService
{
    public const string MoonlightGlow = "MoonlightGlow";
    public static IReadOnlyList<string> Themes { get; } = new[] { "System", MoonlightGlow, "Graphite", "Nord", "Forest", "Ivory", "HighContrast" };
    private static ResourceDictionary? _palette;
    public static string ActiveTheme => _current;
    private static bool _subscribed;
    private static string _requested = MoonlightGlow;
    private static string _current = MoonlightGlow;
    public static string Normalize(string? theme) => Themes.Contains(theme ?? "") ? theme! : MoonlightGlow;
    public static bool IsDarkActive() => _current != "Ivory";
    public static void Apply(string? theme)
    {
        if (Application.Current is not { } app) return;
        _requested = Normalize(theme);
        _current = _requested;
        if (_requested == "System")
        {
            _current = MoonlightGlow;
            if (SystemParameters.HighContrast) _current = "HighContrast";
            else
            {
                try { using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"); if (key?.GetValue("AppsUseLightTheme") is int light && light == 1) _current = "Ivory"; }
                catch (System.Security.SecurityException) { }
            }
        }
        if (!_subscribed)
        {
            Microsoft.Win32.SystemEvents.UserPreferenceChanged += (_, _) =>
            {
                if (_requested == "System") Application.Current?.Dispatcher.BeginInvoke(() => AppearanceService.Apply(AppearanceService.Current));
            };
            _subscribed = true;
        }
        if (_palette is not null) app.Resources.MergedDictionaries.Remove(_palette);
        _palette = new ResourceDictionary { Source = new Uri("/MyVault.App;component/Themes/" + _current + "Palette.xaml", UriKind.RelativeOrAbsolute) };
        app.Resources.MergedDictionaries.Add(_palette);
    }
}
