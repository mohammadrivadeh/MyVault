using System.ComponentModel;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace MyVault.App.Services;

public sealed class LocalizationService : INotifyPropertyChanged
{
    public static LocalizationService Instance { get; } = new();
    private readonly Dictionary<string, string> _english = Load("en");
    private readonly Dictionary<string, string> _persian = Load("fa");
    public string Language { get; private set; } = "en";
    public event PropertyChangedEventHandler? PropertyChanged;
    private static Dictionary<string, string> Load(string language)
    {
        using var stream = typeof(LocalizationService).Assembly.GetManifestResourceStream("MyVault.App.Resources." + language + ".json");
        return stream is null ? new() : JsonSerializer.Deserialize<Dictionary<string, string>>(stream)!;
    }
    public string this[string key] => Language == "fa" ? _persian.GetValueOrDefault(key, _english.GetValueOrDefault(key, key)) : _english.GetValueOrDefault(key, key);
    public void SetLanguage(string language)
    {
        Language = language == "fa" ? "fa" : "en";
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Language)));
    }
    public static string Id(string english) => "k" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(english)))[..12].ToLowerInvariant();
    public static string Text(string english)
    {
        string key = Id(english), translated = Instance[key];
        return translated == key ? english : translated;
    }
    public static string Format(string english, params object[] args) => string.Format(CultureInfo.CurrentCulture, Text(english), args);
}
[MarkupExtensionReturnType(typeof(string))]
public sealed class LocExtension : MarkupExtension
{
    public string Key { get; set; } = "";
    public override object ProvideValue(IServiceProvider serviceProvider) => new Binding("[" + Key + "]") { Source = LocalizationService.Instance, Mode = BindingMode.OneWay }.ProvideValue(serviceProvider);
}
public sealed class ChoiceLabelConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        string text = values[0]?.ToString() ?? "";
        if(text.Split(", ").All(part=>part is "weak" or "reused" or "old")) return string.Join("، ",text.Split(", ").Select(LocalizationService.Text));
        return text switch { "en" => "English", "fa" => "فارسی", "OnePassword" => "1Password", _ => LocalizationService.Text(text) };
    }
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
