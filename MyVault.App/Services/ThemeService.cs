using System.Windows;

namespace MyVault.App.Services;

public static class ThemeService
{
    public const string MoonlightGlow = "MoonlightGlow";

    public static void Apply(string? theme)
    {
        var app = Application.Current;
        if (app is null) return;

        var merged = app.Resources.MergedDictionaries;

        // Remove any existing theme dictionaries by name.
        for (int i = merged.Count - 1; i >= 0; i--)
        {
            var src = merged[i].Source?.OriginalString;
            if (src is null) continue;
            if (src.EndsWith("Light.xaml", StringComparison.OrdinalIgnoreCase) ||
                src.EndsWith("Dark.xaml", StringComparison.OrdinalIgnoreCase) ||
                src.EndsWith("MoonlightGlow.xaml", StringComparison.OrdinalIgnoreCase))
            {
                merged.RemoveAt(i);
            }
        }

        merged.Add(new ResourceDictionary
        {
            Source = new Uri("Themes/MoonlightGlow.xaml", UriKind.Relative)
        });
    }

    public static bool IsDarkActive()
    {
        // Moonlight Glow is the only theme, and it's dark.
        return true;
    }

    /// <summary>Forces any incoming value to MoonlightGlow (for old settings.json migration).</summary>
    public static string Normalize(string? theme) => MoonlightGlow;
}