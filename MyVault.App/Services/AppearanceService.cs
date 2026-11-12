using System.Windows;
using System.Windows.Media;
using System.Text.Json;

namespace MyVault.App.Services;

public static class AppearanceService
{
    public static AppSettings Current { get; private set; } = new();
    public static AppSettings Copy(AppSettings settings) => JsonSerializer.Deserialize<AppSettings>(JsonSerializer.Serialize(settings))!;
    public static IReadOnlyList<string> Accents { get; } = new[] { "Palette", "Blue", "Violet", "Green", "Amber" };
    public static void Apply(AppSettings settings)
    {
        Current = Copy(settings);
        ThemeService.Apply(settings.Theme);
        MotionService.Mode = settings.Motion;
        LocalizationService.Instance.SetLanguage(settings.Language);
        if (Application.Current is not { } app) return;
        app.Resources["App.DialogMaxHeight"]=Math.Max(400,SystemParameters.WorkArea.Height-60);
        double size = Math.Clamp(settings.TextSize, 12, 18);
        app.Resources["App.Font.Small"] = size - 1;
        app.Resources["App.Font.Body"] = size;
        app.Resources["App.Font.Title"] = size + 4;
        app.Resources["App.Font.Hero"] = size + 16;
        app.Resources["App.RowPadding"] = settings.Density == "Compact" ? new Thickness(10, 5, 10, 5) : new Thickness(14, 11, 14, 11);
        app.Resources["App.FontFamily"] = new FontFamily(settings.Language == "fa" ? "Segoe UI, Tahoma" : "Segoe UI Variable Display, Segoe UI");
        if (settings.Accent != "Palette" && ThemeService.ActiveTheme != "HighContrast" && Accents.Contains(settings.Accent))
        {
            bool dark = ThemeService.IsDarkActive();
            string color = settings.Accent switch
            {
                "Violet" => dark ? "#C4A7FF" : "#663399",
                "Green" => dark ? "#8CE0AF" : "#176636",
                "Amber" => dark ? "#FFD97D" : "#805000",
                _ => dark ? "#6BA6FF" : "#194FA3"
            };
            foreach (string key in new[] { "Accent", "AccentHover", "AccentPressed" }) app.Resources["App." + key] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
            app.Resources["App.TextOnAccent"] = new SolidColorBrush(dark ? Color.FromRgb(10, 16, 32) : Colors.White);
        }
        else foreach (string key in new[] { "Accent", "AccentHover", "AccentPressed", "TextOnAccent" }) app.Resources.Remove("App." + key);
        foreach (Window window in app.Windows) ApplyWindow(window);
    }
    public static void ApplyWindow(Window window)
    {
        window.FlowDirection = Current.Language == "fa" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        window.Language = System.Windows.Markup.XmlLanguage.GetLanguage(Current.Language == "fa" ? "fa-IR" : "en-US");
    }
}
