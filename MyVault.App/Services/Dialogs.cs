using System.Windows;

namespace MyVault.App.Services;

public enum DialogIcon
{
    None,
    Info,
    Warning,
    Error,
    Question,
    Success
}

public enum DialogButtons
{
    OK,
    OKCancel,
    YesNo,
    YesNoCancel
}

public static class Dialogs
{
    public static void Info(string message, string title = "Information", Window? owner = null)
        => Show(message, title, DialogIcon.Info, DialogButtons.OK, owner);

    public static void Warning(string message, string title = "Warning", Window? owner = null)
        => Show(message, title, DialogIcon.Warning, DialogButtons.OK, owner);

    public static void Error(string message, string title = "Error", Window? owner = null)
        => Show(message, title, DialogIcon.Error, DialogButtons.OK, owner);

    public static void Success(string message, string title = "Success", Window? owner = null)
        => Show(message, title, DialogIcon.Success, DialogButtons.OK, owner);

    /// <summary>Returns true if user clicked Yes, false for No/Cancel.</summary>
    public static bool Confirm(string message, string title = "Confirm", Window? owner = null)
        => Show(message, title, DialogIcon.Question, DialogButtons.YesNo, owner);

    private static bool Show(string message, string title, DialogIcon icon, DialogButtons buttons, Window? owner)
    {
        var dialog = new Views.MessageDialog(message, title, icon, buttons)
        {
            Owner = owner ?? Application.Current?.MainWindow
        };
        return dialog.ShowDialog() == true;
    }
}