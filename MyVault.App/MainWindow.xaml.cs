using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Shell;
using System.Windows.Threading;
using MyVault.App.Services;
using MyVault.App.ViewModels;

namespace MyVault.App;

public partial class MainWindow : Window
{
    private const double VaultWidth = 1080;
    private const double VaultHeight = 760;

    private MainViewModel? _vm;

    public MainWindow()
    {
        InitializeComponent();

        _vm = new MainViewModel();
        _vm.PropertyChanged += OnVmPropertyChanged;
        DataContext = _vm;

        ContentHost.Content = _vm.CurrentScreen;
        ContentHost.Opacity = 1;

        TitleBarGrid.Visibility = Visibility.Collapsed;
        TitleBarGrid.Opacity = 0;

        SourceInitialized += OnSourceInitialized;
        StateChanged += OnStateChanged;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_vm is not null)
            ApplyWindowMode(_vm.CurrentScreen is VaultViewModel);
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.CurrentScreen) && _vm is not null)
            TransitionTo(_vm.CurrentScreen);
    }

    private void TransitionTo(object? newScreen)
    {
        if (newScreen is null) return;
        if (ReferenceEquals(ContentHost.Content, newScreen)) return;

        bool targetIsVault = newScreen is VaultViewModel;

        // Cancel any in-flight animations on the container / title bar
        ContentHost.BeginAnimation(UIElement.OpacityProperty, null);
        TitleBarGrid.BeginAnimation(UIElement.OpacityProperty, null);

        // Fade out current content (150ms, fast)
        var fadeOut = new DoubleAnimation(ContentHost.Opacity, 0, TimeSpan.FromMilliseconds(150))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };

        // Fade out title bar if currently visible
        if (TitleBarGrid.Visibility == Visibility.Visible && TitleBarGrid.Opacity > 0.01)
        {
            var titleFadeOut = new DoubleAnimation(TitleBarGrid.Opacity, 0,
                TimeSpan.FromMilliseconds(150))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            TitleBarGrid.BeginAnimation(UIElement.OpacityProperty, titleFadeOut);
        }

        fadeOut.Completed += (_, _) =>
        {
            // ---- Content is invisible → safe to resize + swap ----
            ApplyWindowMode(targetIsVault);

            // CRITICAL: cancel the fade-out animation FIRST.
            // Otherwise HoldEnd keeps ContentHost at opacity 0 and
            // nothing inside is ever visible.
            ContentHost.BeginAnimation(UIElement.OpacityProperty, null);
            ContentHost.Opacity = 1;

            ContentHost.Content = newScreen;

            // Title bar visibility per mode
            if (targetIsVault)
            {
                TitleBarGrid.Visibility = Visibility.Visible;
                TitleBarGrid.Opacity = 0;

                var titleFadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(280))
                {
                    BeginTime = TimeSpan.FromMilliseconds(120),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                TitleBarGrid.BeginAnimation(UIElement.OpacityProperty, titleFadeIn);
            }
            else
            {
                TitleBarGrid.Visibility = Visibility.Collapsed;
                TitleBarGrid.Opacity = 0;
            }
        };

        ContentHost.BeginAnimation(UIElement.OpacityProperty, fadeOut);
    }

    private void ApplyWindowMode(bool vaultUnlocked)
    {
        if (WindowState == WindowState.Maximized) WindowState = WindowState.Normal;

        WindowChrome.SetWindowChrome(this, new WindowChrome
        {
            CaptionHeight = vaultUnlocked ? 40 : 0,
            ResizeBorderThickness = vaultUnlocked ? new Thickness(6) : new Thickness(0),
            GlassFrameThickness = new Thickness(0),
            CornerRadius = new CornerRadius(8),
            UseAeroCaptionButtons = false
        });

        if (vaultUnlocked)
        {
            MinWidth = 840;
            MinHeight = 540;
            ResizeMode = ResizeMode.CanResize;
            SizeToContent = SizeToContent.Manual;
            Width = VaultWidth;
            Height = VaultHeight;
        }
        else
        {
            MinWidth = 0;
            MinHeight = 0;
            ResizeMode = ResizeMode.NoResize;
            SizeToContent = SizeToContent.WidthAndHeight;
        }

        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(CenterInScreen));
    }

    private void CenterInScreen()
    {
        var work = SystemParameters.WorkArea;
        Left = work.Left + (work.Width - ActualWidth) / 2;
        Top = work.Top + (work.Height - ActualHeight) / 2;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        bool isDark = ThemeService.IsDarkActive();
        WindowEffects.ApplyRoundedCorners(hwnd);
        WindowEffects.SetDarkMode(hwnd, isDark);
    }

    private void OnStateChanged(object? sender, EventArgs e)
    {
        if (MaxIcon is not null)
            MaxIcon.Text = WindowState == WindowState.Maximized ? "\uE923" : "\uE922";
    }

    private void MinBtn_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void MaxBtn_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void CloseBtn_Click(object sender, RoutedEventArgs e) => Close();

    protected override void OnClosing(CancelEventArgs e)
    {
        if (_vm is not null)
        {
            _vm.PropertyChanged -= OnVmPropertyChanged;
            _vm.Shutdown();
        }
        base.OnClosing(e);
    }
}