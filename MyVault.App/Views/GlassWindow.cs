using System.Windows;
using System.Windows.Interop;
using System.Windows.Shell;
using MyVault.App.Services;

namespace MyVault.App.Views;

public class GlassWindow : Window
{
    public GlassWindow()
    {
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = System.Windows.Media.Brushes.Transparent;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;

        WindowChrome.SetWindowChrome(this, new WindowChrome
        {
            CaptionHeight = 0,
            ResizeBorderThickness = new Thickness(0),
            GlassFrameThickness = new Thickness(0),
            CornerRadius = new CornerRadius(14)
        });

        SourceInitialized += (_, _) =>
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            WindowEffects.ApplyRoundedCorners(hwnd);
            // TEMP: acrylic call removed to isolate the build
            WindowBlurHelper.EnableAcrylic(this, 0x1A1A2Eu, (byte)0xD9);
        };
    }
}