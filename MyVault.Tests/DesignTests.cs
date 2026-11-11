using System.Xml.Linq;
using Xunit;

namespace MyVault.Tests;

public sealed class DesignTests
{
    [Fact]
    public void SixPalettesProvideReadableTextAndAccentPairs()
    {
        var files = Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Palettes"), "*Palette.xaml");
        Assert.Equal(6, files.Length);
        foreach (var file in files)
        {
            XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
            var colors = XDocument.Load(file).Root!.Elements().ToDictionary(e => (string)e.Attribute(x + "Key")!, e => (string)e.Attribute("Color")!);
            foreach (string surface in new[] { "App.Background", "App.Surface", "App.SurfaceAlt", "App.SurfaceHover" })
            {
                Assert.True(Contrast(colors["App.Text"], colors[surface]) >= 4.5, $"{file}: text/{surface}");
                Assert.True(Contrast(colors["App.TextMuted"], colors[surface]) >= 4.5, $"{file}: muted/{surface}");
            }
            Assert.True(Contrast(colors["App.TextOnAccent"], colors["App.Accent"]) >= 4.5, $"{file}: accent");
            Assert.True(Contrast(colors["App.Warning"], colors["App.WarningBg"]) >= 4.5, $"{file}: warning");
            Assert.True(Contrast(colors["App.Error"], colors["App.ErrorBg"]) >= 4.5, $"{file}: error");
        }
    }
    private static double Contrast(string a, string b)
    {
        double first = Luminance(a), second = Luminance(b);
        return (Math.Max(first, second) + .05) / (Math.Min(first, second) + .05);
    }
    private static double Luminance(string color)
    {
        var components = new[] { 1, 3, 5 }.Select(i => Convert.ToInt32(color.Substring(i, 2), 16) / 255.0).Select(v => v <= .04045 ? v / 12.92 : Math.Pow((v + .055) / 1.055, 2.4)).ToArray();
        return components[0] * .2126 + components[1] * .7152 + components[2] * .0722;
    }
}
