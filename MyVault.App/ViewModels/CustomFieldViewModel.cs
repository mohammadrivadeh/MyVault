using MyVault.Core.Models;
namespace MyVault.App.ViewModels;

public sealed class CustomFieldViewModel : ObservableObject
{
    private string _name = "", _value = "";
    private bool _secret;
    public CustomFieldViewModel(CustomField? field = null)
    { if (field is not null) { _name = field.Name; _value = field.Value; _secret = field.IsSecret; } }
    public string Name { get => _name; set => SetProperty(ref _name, value); }
    public string Value { get => _value; set => SetProperty(ref _value, value); }
    public bool IsSecret { get => _secret; set => SetProperty(ref _secret, value); }
    public CustomField ToModel() => new() { Name = Name.Trim(), Value = Value, IsSecret = IsSecret };
}
