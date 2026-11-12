using System.Windows;
using System.Windows.Controls;
namespace MyVault.App.Services;

/// <summary>Matches the editor's existing managed-string model; never logs values.</summary>
public static class PasswordBinding
{
    private static readonly DependencyProperty UpdatingProperty = DependencyProperty.RegisterAttached("Updating", typeof(bool), typeof(PasswordBinding), new PropertyMetadata(false));
    public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached("Enabled", typeof(bool), typeof(PasswordBinding), new PropertyMetadata(false, (obj, args) =>
    {
        if (obj is PasswordBox box) { box.PasswordChanged -= OnPassword; if ((bool)args.NewValue) box.PasswordChanged += OnPassword; }
    }));
    public static bool GetEnabled(DependencyObject obj) => (bool)obj.GetValue(EnabledProperty);
    public static void SetEnabled(DependencyObject obj, bool value) => obj.SetValue(EnabledProperty, value);
    public static readonly DependencyProperty ValueProperty = DependencyProperty.RegisterAttached("Value", typeof(string), typeof(PasswordBinding), new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, Changed));
    public static string GetValue(DependencyObject obj) => (string)obj.GetValue(ValueProperty);
    public static void SetValue(DependencyObject obj, string value) => obj.SetValue(ValueProperty, value);
    private static void Changed(DependencyObject obj, DependencyPropertyChangedEventArgs e)
    {
        if (obj is not PasswordBox box || (bool)box.GetValue(UpdatingProperty)) return;
        box.PasswordChanged -= OnPassword;
        box.Password = (string?)e.NewValue ?? "";
        box.PasswordChanged += OnPassword;
    }
    private static void OnPassword(object sender, RoutedEventArgs e)
    {
        var box = (PasswordBox)sender; box.SetValue(UpdatingProperty, true);
        try { box.SetCurrentValue(ValueProperty, box.Password); } finally { box.SetValue(UpdatingProperty, false); }
    }
}
