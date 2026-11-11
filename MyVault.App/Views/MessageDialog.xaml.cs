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
                IconBubble.Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x2C, 0x4A));
                IconGlyph.Text = "\uE946";
                IconGlyph.Foreground = new SolidColorBrush(Color.FromRgb(0x6B, 0xA6, 0xFF));
                break;

            case DialogIcon.Question:
                IconBubble.Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x2C, 0x4A));
                IconGlyph.Text = "\uE897";
                IconGlyph.Foreground = new SolidColorBrush(Color.FromRgb(0x6B, 0xA6, 0xFF));
                break;

            case DialogIcon.Warning:
                IconBubble.Background = new SolidColorBrush(Color.FromRgb(0x3A, 0x2D, 0x12));
                IconGlyph.Text = "\uE7BA";
                IconGlyph.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0xD9, 0x7D));
                break;

            case DialogIcon.Error:
                IconBubble.Background = new SolidColorBrush(Color.FromRgb(0x3B, 0x1A, 0x1A));
                IconGlyph.Text = "\uEA39";
                IconGlyph.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x6B, 0x6B));
                break;

            case DialogIcon.Success:
                IconBubble.Background = new SolidColorBrush(Color.FromRgb(0x1A, 0x3A, 0x24));
                IconGlyph.Text = "\uE73E";
                IconGlyph.Foreground = new SolidColorBrush(Color.FromRgb(0x6B, 0xCB, 0x77));
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
                OkButton.Content = "Yes";
                OkButton.Visibility = Visibility.Visible;
                NoButton.Visibility = Visibility.Visible;
                break;

            case DialogButtons.YesNoCancel:
                OkButton.Content = "Yes";
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