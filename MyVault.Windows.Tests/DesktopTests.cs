using System.IO;
using System.Reflection;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using MyVault.App.Services;
using MyVault.App.ViewModels;
using MyVault.App.Views;
using MyVault.Core.Models;
using MyVault.Core.Storage;
using Xunit;

namespace MyVault.Windows.Tests;

public sealed class DesktopTests
{
    private static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match) yield return match;
            foreach (var descendant in Descendants<T>(child)) yield return descendant;
        }
    }

    [Fact]
    public void PalettesPreviewClipboardAndModalLockWorkOnSta()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            var dir = Path.Combine(Path.GetTempPath(), "myvault-desktop-" + Guid.NewGuid());
            Directory.CreateDirectory(dir);
            try
            {
                var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/MyVault.App;component/Themes/MoonlightGlow.xaml", UriKind.RelativeOrAbsolute) });
                app.Resources.Add("BoolToVis",new MyVault.App.Converters.BoolToVisibilityConverter());
                app.Resources.Add("NullOrEmptyToVis",new MyVault.App.Converters.NullOrEmptyToVisibilityConverter());
                foreach (string theme in ThemeService.Themes)
                {
                    ThemeService.Apply(theme);
                    Assert.IsType<SolidColorBrush>(app.FindResource("App.Text"));
                    Assert.IsType<SolidColorBrush>(app.FindResource("App.Background"));
                    new EntryEditorWindow().Close();
                }
                MotionService.Mode = "Off";
                Assert.False(MotionService.Enabled);
                var main = new Window { Content = new System.Windows.Controls.Grid() };
                app.MainWindow = main;
                main.Show();
                using var clipboard = new ClipboardService(30);
                clipboard.Initialize(main);
                Assert.True(clipboard.SetSecret("synthetic-desktop-test"));
                Assert.Equal("synthetic-desktop-test", Clipboard.GetText());
                Clipboard.SetText("later-user-content");
                clipboard.ClearNow();
                Assert.Equal("later-user-content", Clipboard.GetText());
                Assert.True(clipboard.SetSecret("clear-this"));
                clipboard.ClearNow();
                Assert.False(Clipboard.ContainsText());
                using var repo = VaultRepository.CreateNew(Path.Combine(dir, "vault.dat"), "synthetic-password");
                repo.Update(d => {
                    d.Entries.Add(new VaultEntry { Title = "Test", Password = "secret", TotpSecret = "JBSWY3DPEHPK3PXP" });
                    d.Entries.Add(new VaultEntry { Title = "Restore me", DeletedAt = DateTimeOffset.UtcNow });
                });
                using var autoLock = new AutoLockService(5);
                var settings = new SettingsService();
                string original = settings.Current.Theme;
                var preview = new SettingsViewModel(settings, repo) { Theme = "Ivory" };
                preview.RevertPreview();
                Assert.Equal(original == "Ivory", !ThemeService.IsDarkActive());
                var vm = new VaultViewModel(repo, clipboard, settings, autoLock);
                vm.Locked += (_, _) => repo.Dispose();
                var setup = new EntryEditorViewModel(null, []) {Title="Setup test", TotpSecret="123456"};
                Assert.Contains("6-digit code",setup.Validate());
                setup.TotpSecret="JBSWY3DPEHPK3PXP";
                Assert.Null(setup.Validate());
                var view = new VaultView { DataContext = vm };
                main.Width = 900; main.Height = 700;
                main.Content = view;
                main.UpdateLayout();
                var list = (ListBox)view.FindName("EntryList");
                list.UpdateLayout();
                var codeButton = Assert.Single(Descendants<Button>(list), b => b.Command == vm.CopyTotpCommand && b.IsVisible);
                Assert.Equal("Test", Assert.IsType<VaultEntryViewModel>(codeButton.CommandParameter).Title);
                codeButton.Command!.Execute(codeButton.CommandParameter);
                string code = Clipboard.GetText();
                Assert.Equal(6, code.Length);
                Assert.All(code, c => Assert.True(char.IsDigit(c)));
                vm.SetFilterCommand.Execute("Trash");
                list.UpdateLayout();
                Assert.DoesNotContain(Descendants<Button>(list), b => b.Command == vm.CopyTotpCommand && b.IsVisible);
                var restoreButton = Assert.Single(Descendants<Button>(list), b => b.Command == vm.RestoreCommand && b.IsVisible);
                restoreButton.Command!.Execute(restoreButton.CommandParameter);
                Assert.Null(repo.Document.Entries.Single(e => e.Title == "Restore me").DeletedAt);
                vm.SetFilterCommand.Execute("All");
                var editor = new EntryEditorWindow { Owner = main, DataContext = new EntryEditorViewModel(repo.Document.Entries[0], []) };
                editor.Loaded += (_, _) => editor.Dispatcher.BeginInvoke(() => typeof(VaultViewModel).GetMethod("Lock", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(vm, null));
                editor.ShowDialog();
                Assert.False(repo.IsUnlocked);
                Assert.Empty(vm.Entries);
                Assert.Null(editor.DataContext);
                main.Close(); app.Shutdown();
            }
            catch (Exception ex) { failure = ex; }
            finally { Directory.Delete(dir, true); }
        })
        { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(45)), "Desktop test timed out.");
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
