using MyVault.Core.Models;

namespace MyVault.App.ViewModels;

public sealed class CategoryViewModel : ObservableObject
{
    public Category Model { get; }

    public CategoryViewModel(Category model)
    {
        Model = model;
    }

    public Guid Id => Model.Id;
    public string Name => Model.Name;

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public void RefreshName() => OnPropertyChanged(nameof(Name));
}