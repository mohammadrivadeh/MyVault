using System.Windows;
using MyVault.App.ViewModels;
namespace MyVault.App.Views;

public partial class BulkActionWindow : GlassWindow
{
    public BulkActionWindow(IReadOnlyList<CategoryOption> categories)
    { InitializeComponent(); CategoryBox.ItemsSource = categories; CategoryBox.SelectedIndex = 0; }
    public bool ChangeCategory => ChangeCategoryBox.IsChecked == true;
    public Guid? CategoryId => CategoryBox.SelectedItem is CategoryOption option ? option.Id : null;
    public string TagToAdd => TagBox.Text.Trim();
    private void Apply_Click(object sender, RoutedEventArgs e) { DialogResult = true; Close(); }
}
