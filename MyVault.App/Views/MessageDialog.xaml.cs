using MyVault.App.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MyVault.App.Views;

public partial class MessageDialog : GlassWindow
{
    public MessageDialog()
    {
        InitializeComponent();
    }

    public MessageDialog(string message, string title, DialogIcon icon, DialogButtons buttons)
    {
        InitializeComponent();

        TitleText.Text = title;
        MessageText.Text = message;

        ApplyIcon(icon);
        ApplyButtons(buttons);
    }

    private void ApplyIcon(DialogIcon icon)
    {
        switch (icon)
        {
            case DialogIcon.Info:
                IconBubble.SetResourceReference(BackgroundProperty, "App.AccentSubtle");
                IconGlyph.Text = "\uE946";
                IconGlyph.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "App.Accent");
                break;

            case DialogIcon.Question:
                IconBubble.SetResourceReference(BackgroundProperty, "App.AccentSubtle");
                IconGlyph.Text = "\uE897";
                IconGlyph.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "App.Accent");
                break;

            case DialogIcon.Warning:
                IconBubble.SetResourceReference(BackgroundProperty, "App.WarningBg");
                IconGlyph.Text = "\uE7BA";
                IconGlyph.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "App.Warning");
                break;

            case DialogIcon.Error:
                IconBubble.SetResourceReference(BackgroundProperty, "App.ErrorBg");
                IconGlyph.Text = "\uEA39";
                IconGlyph.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "App.Error");
                break;

            case DialogIcon.Success:
                IconBubble.SetResourceReference(BackgroundProperty, "App.WarningBg");
                IconGlyph.Text = "\uE73E";
                IconGlyph.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "App.Success");
                break;

            default:
                IconBubble.Visibility = Visibility.Collapsed;
                break;
        }
    }

    private void ApplyButtons(DialogButtons buttons)
    {
        switch (buttons)
        {
            case DialogButtons.OK:
                OkButton.Visibility = Visibility.Visible;
                break;

            case DialogButtons.OKCancel:
                OkButton.Visibility = Visibility.Visible;
                CancelButton.Visibility = Visibility.Visible;
                break;

            case DialogButtons.YesNo:
                OkButton.Content = MyVault.App.Services.LocalizationService.Text("Yes");
                OkButton.Visibility = Visibility.Visible;
                NoButton.Visibility = Visibility.Visible;
                break;

            case DialogButtons.YesNoCancel:
                OkButton.Content = MyVault.App.Services.LocalizationService.Text("Yes");
                OkButton.Visibility = Visibility.Visible;
                NoButton.Visibility = Visibility.Visible;
                CancelButton.Visibility = Visibility.Visible;
                break;
        }
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void No_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}