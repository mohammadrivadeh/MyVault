using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;

namespace MyVault.App.ViewModels;

public sealed record PaletteAction(string Title, string Description, Action Run);

public sealed class CommandPaletteViewModel : ObservableObject
{
    private string _query = "";
    private PaletteAction? _selected;
    public ICollectionView Actions { get; }
    public PaletteAction? Chosen { get; private set; }
    public event EventHandler? CloseRequested;
    public CommandPaletteViewModel(IEnumerable<PaletteAction> actions)
    {
        Actions = CollectionViewSource.GetDefaultView(actions.ToList());
        Actions.Filter = item => item is PaletteAction action && (action.Title.Contains(Query, StringComparison.CurrentCultureIgnoreCase) || action.Description.Contains(Query, StringComparison.CurrentCultureIgnoreCase));
        Selected = Actions.Cast<PaletteAction>().FirstOrDefault();
        RunCommand = new RelayCommand(() => { Chosen = Selected; CloseRequested?.Invoke(this, EventArgs.Empty); }, () => Selected is not null);
    }
    public string Query { get => _query; set { if (SetProperty(ref _query, value)) { Actions.Refresh(); Selected = Actions.Cast<PaletteAction>().FirstOrDefault(); } } }
    public PaletteAction? Selected { get => _selected; set { SetProperty(ref _selected, value); CommandManager.InvalidateRequerySuggested(); } }
    public ICommand RunCommand { get; }
}
