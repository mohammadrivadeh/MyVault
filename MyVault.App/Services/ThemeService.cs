using System.Windows;

namespace MyVault.App.Services;

public static class ThemeService
{
    public const string MoonlightGlow = "MoonlightGlow";
    public static IReadOnlyList<string> Themes { get; } = new[] { MoonlightGlow, "Graphite", "Nord", "Forest", "Ivory", "HighContrast" };
    private static ResourceDictionary? _palette;
    private static string _current = MoonlightGlow;
    public static string Normalize(string? theme) => Themes.Contains(theme ?? "") ? theme! : MoonlightGlow;
    public static bool IsDarkActive() => _current != "Ivory";
    public static void Apply(string? theme)
    {
        if (Application.Current is not { } app) return;
        _current = Normalize(theme);
        if (_palette is not null) app.Resources.MergedDictionaries.Remove(_palette);
        _palette = new ResourceDictionary { Source = new Uri("/MyVault.App;component/Themes/" + _current + "Palette.xaml", UriKind.RelativeOrAbsolute) };
        app.Resources.MergedDictionaries.Add(_palette);
    }
}
