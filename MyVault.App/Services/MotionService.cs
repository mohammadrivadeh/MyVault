using System.Windows;
using System.Windows.Media.Animation;

namespace MyVault.App.Services;

public static class MotionService
{
    public static string Mode { get; set; } = "System";
    public static bool Enabled => Mode != "Off" && SystemParameters.ClientAreaAnimation;
    public static bool Full => Enabled && Mode == "Full";
    public static void Reveal(UIElement element)
    {
        if (!Enabled) return;
        element.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(160)) { FillBehavior = FillBehavior.Stop });
    }
}
