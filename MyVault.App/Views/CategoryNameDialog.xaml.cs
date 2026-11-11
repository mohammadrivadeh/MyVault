using System.Windows;
using System.Windows.Controls;

namespace MyVault.App.Views;

public partial class CategoryNameDialog : GlassWindow
{
    private readonly HashSet<string> _existingNames;

    public string CategoryName { get; private set; } = string.Empty;

    public CategoryNameDialog(string title, string initialName, IEnumerable<string> existingNames)
    {
        InitializeComponent();

        Title = title;
        TitleText.Text = title;
        NameBox.Text = initialName;
        _existingNames = new HashSet<string>(existingNames, StringComparer.OrdinalIgnoreCase);

        Loaded += (_, _) =>
        {
            NameBox.Focus();
            NameBox.SelectAll();
        };
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        var name = NameBox.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(name))
        {
            ErrorText.Text = "Name cannot be empty.";
            return;
        }
        if (name.Length > 50)
        {
            ErrorText.Text = "Name too long (max 50 characters).";
            return;
        }
        if (_existingNames.Contains(name))
        {
            ErrorText.Text = "A category with this name already exists.";
            return;
        }

        CategoryName = name;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}