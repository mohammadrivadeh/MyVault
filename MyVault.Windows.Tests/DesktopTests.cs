using System.IO;
using System.Reflection;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Media.Imaging;
using System.Windows.Data;
using MyVault.Core.Services;
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

    private static void Pump(Task task)
    {
        if (!task.IsCompleted)
        {
            var frame = new DispatcherFrame(); var dispatcher = Dispatcher.CurrentDispatcher;
            task.ContinueWith(_ => dispatcher.BeginInvoke(() => frame.Continue = false), TaskScheduler.Default);
            Dispatcher.PushFrame(frame);
        }
        task.GetAwaiter().GetResult();
    }
    private static void Screenshot(Window window, string name)
    {
        window.UpdateLayout();
        var image = new RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        image.Render(window);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(image));
        string directory = Path.Combine(Environment.CurrentDirectory, "TestResults"); Directory.CreateDirectory(directory);
        using var file = File.Create(Path.Combine(directory, name + ".png")); encoder.Save(file);
    }
    private static void AssertIconCenters(ListBox list)
    {
        foreach (var button in Descendants<Button>(list).Where(b => b.IsVisible && b.Content is TextBlock && b.ActualWidth == 32))
        {
            var glyph = (TextBlock)button.Content;
            Point point = glyph.TranslatePoint(new Point(0, 0), button);
            Assert.InRange(Math.Abs(point.X + glyph.ActualWidth / 2 - button.ActualWidth / 2), 0, 1);
            Assert.InRange(Math.Abs(point.Y + glyph.ActualHeight / 2 - button.ActualHeight / 2), 0, 1);
            Assert.Equal(glyph.Text.Trim(), glyph.Text);
        }
    }
    private static void ExerciseApprovedPhases(Application app, Window main, VaultRepository repo, VaultViewModel vm, SettingsService settings, string dir, ListBox list)
    {
        // Execute a real palette choice through the modal's bound command.
        main.Dispatcher.BeginInvoke(() =>
        {
            var palette = app.Windows.OfType<CommandPaletteWindow>().Single();
            var model = (CommandPaletteViewModel)palette.DataContext;
            model.Query = "Trash"; Assert.Single(model.Actions.Cast<PaletteAction>());
            model.RunCommand.Execute(null);
        });
        vm.CommandPaletteCommand.Execute(null); Assert.True(vm.IsTrashFilter);
        vm.SetFilterCommand.Execute("All");
        vm.SelectVisibleCommand.Execute(null); Assert.Equal(2, vm.SelectionCount);
        main.Dispatcher.BeginInvoke(() =>
        {
            var bulk = app.Windows.OfType<BulkActionWindow>().Single();
            ((TextBox)bulk.FindName("TagBox")).Text = "work";
            Assert.Single(Descendants<Button>(bulk), b => Equals(b.Content, "Apply")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        });
        vm.BulkEditCommand.Execute(null);
        vm.TagFilter = "work"; Assert.Equal(2, vm.EntriesView.Cast<VaultEntryViewModel>().Count());
        Assert.All(repo.Document.Entries, e => Assert.Contains("work", e.Tags));
        vm.SelectVisibleCommand.Execute(null);
        main.Dispatcher.BeginInvoke(() =>
        {
            var confirmation = app.Windows.OfType<MessageDialog>().Single();
            ((Button)confirmation.FindName("OkButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        });
        vm.BulkDeleteCommand.Execute(null);
        Assert.All(repo.Document.Entries, e => Assert.NotNull(e.DeletedAt));
        foreach (var entry in vm.Entries.ToArray()) vm.RestoreCommand.Execute(entry);
        vm.TagFilter = ""; vm.SetFilterCommand.Execute("All"); main.UpdateLayout();
        AssertIconCenters(list); Screenshot(main, "rows-en");

        var preference = new SettingsViewModel(settings, repo) { Theme = "Forest", Accent = "Violet", Language = "fa", TextSize = 18, Density = "Compact" };
        Assert.Equal(FlowDirection.RightToLeft, main.FlowDirection);
        Assert.Equal("ذخیره", LocalizationService.Text("Save"));
        Assert.Equal(18d, app.Resources["App.Font.Body"]);
        main.UpdateLayout(); AssertIconCenters(list); Screenshot(main, "rows-fa");
        preference.RevertPreview(); Assert.Equal(FlowDirection.LeftToRight, main.FlowDirection);
        Assert.Equal("Save", LocalizationService.Text("Save"));
        Assert.Equal(13d, app.Resources["App.Font.Body"]);

        var draft = new EntryEditorViewModel(repo.Document.Entries[0], []);
        string originalTitle = draft.Title; draft.Title = "Unsaved draft"; Assert.True(draft.HasUnsavedChanges);
        bool cancellationWorked = false;
        var dirtyEditor = new EntryEditorWindow { Owner = main, DataContext = draft };
        dirtyEditor.Loaded += (_, _) => dirtyEditor.Dispatcher.BeginInvoke(() =>
        {
            dirtyEditor.Dispatcher.BeginInvoke(() =>
            {
                var confirmation = app.Windows.OfType<MessageDialog>().Single();
                ((Button)confirmation.FindName("NoButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            });
            dirtyEditor.Close(); cancellationWorked = dirtyEditor.IsVisible;
            draft.Title = originalTitle; dirtyEditor.Close();
        });
        dirtyEditor.ShowDialog(); Assert.True(cancellationWorked); Assert.False(draft.HasUnsavedChanges);

        draft.Kind = EntryKind.ApiKey; draft.CustomFields.Add(new CustomFieldViewModel { Name = "Region", Value = "synthetic-private-field", IsSecret = true });
        draft.Password = "replacement"; repo.Update(d => draft.ApplyTo(d.Entries[0]));
        Assert.Equal("secret", Assert.Single(repo.Document.Entries[0].PasswordHistory).Password);
        Assert.Contains(repo.Document.Entries[0].CustomFields, f => f.Name == "Region" && f.IsSecret);
        // Empty secret fields must bind on the very first keystroke.
        var field = new CustomFieldViewModel(); var passwordBox = new PasswordBox(); PasswordBinding.SetEnabled(passwordBox, true);
        BindingOperations.SetBinding(passwordBox, PasswordBinding.ValueProperty, new Binding(nameof(CustomFieldViewModel.Value)) { Source = field, Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
        passwordBox.Password = "first-value"; Assert.Equal("first-value", field.Value);

        settings.Current.BackupDirectory = Path.Combine(dir, "scheduled"); settings.Current.BackupMinutes = 15; settings.Current.BackupKeep = 3;
        using (var scheduled = new ScheduledBackupService(repo, settings))
        {
            Pump(scheduled.RunDueAsync(DateTimeOffset.UtcNow));
            var backup = Assert.Single(Directory.GetFiles(settings.Current.BackupDirectory, "*.vault"));
            Assert.Equal(2, VaultFile.ReadHeader(VaultFile.Read(backup)).FormatVersion);
            Pump(scheduled.RunDueAsync(DateTimeOffset.UtcNow)); Assert.Single(Directory.GetFiles(settings.Current.BackupDirectory, "*.vault"));
        }
        var pixels = new ZXing.BarcodeWriterPixelData { Format = ZXing.BarcodeFormat.QR_CODE, Options = new ZXing.Common.EncodingOptions { Width = 240, Height = 240, Margin = 4 } }
            .Write("otpauth://totp/Test?secret=JBSWY3DPEHPK3PXP&issuer=Test");
        var source = BitmapSource.Create(pixels.Width, pixels.Height, 96, 96, PixelFormats.Bgra32, null, pixels.Pixels, pixels.Width * 4);
        var png = new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(source));
        string qr = Path.Combine(dir, "synthetic-qr.png"); using (var file = File.Create(qr)) png.Save(file);
        Assert.Equal("JBSWY3DPEHPK3PXP", QrSetupReader.Read(qr).Secret);
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
                app.Resources.Add("ChoiceLabel", new MyVault.App.Services.ChoiceLabelConverter());
                app.Resources.Add("BoolToVis", new MyVault.App.Converters.BoolToVisibilityConverter());
                app.Resources.Add("NullOrEmptyToVis", new MyVault.App.Converters.NullOrEmptyToVisibilityConverter());
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
                repo.Update(d =>
                {
                    d.Entries.Add(new VaultEntry { Title = "Test", Password = "secret", TotpSecret = "JBSWY3DPEHPK3PXP" });
                    d.Entries.Add(new VaultEntry { Title = "Restore me", DeletedAt = DateTimeOffset.UtcNow });
                });
                using var autoLock = new AutoLockService(5);
                var settings = new SettingsService(Path.Combine(dir, "settings.json"));
                settings.Current.Motion = "Off";
                string original = settings.Current.Theme;
                var preview = new SettingsViewModel(settings, repo) { Theme = "Ivory" };
                preview.RevertPreview();
                Assert.Equal(original == "Ivory", !ThemeService.IsDarkActive());
                var vm = new VaultViewModel(repo, clipboard, settings, autoLock);
                vm.Locked += (_, _) => repo.Dispose();
                var setup = new EntryEditorViewModel(null, []) { Title = "Setup test", TotpSecret = "123456" };
                Assert.Contains("6-digit code", setup.Validate());
                setup.TotpSecret = "JBSWY3DPEHPK3PXP";
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
                ExerciseApprovedPhases(app, main, repo, vm, settings, dir, list);
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
        Assert.True(thread.Join(TimeSpan.FromSeconds(90)), "Desktop test timed out.");
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
