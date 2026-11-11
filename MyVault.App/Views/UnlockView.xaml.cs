using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using MyVault.App.ViewModels;

namespace MyVault.App.Views;

public partial class UnlockView : UserControl
{
    private static bool _hasAnimatedThisSession;
    private int _animRetryCount;

    private readonly DispatcherTimer _idleTimer;
    private bool _isIdleMode;

    // ---- Intro timeline (unchanged) ----
    private const int HeroHoldMs = 500;
    private const int HeroSlideMs = 1300;
    private const int PauseMs = 200;
    private const int FormStartMs = HeroHoldMs + HeroSlideMs + PauseMs;
    private const int FormStepMs = 80;
    private const int FormDurationMs = 400;
    private const double FormSlidePx = 24;

    private const int IdleSeconds = 15;

    public UnlockView()
    {
        InitializeComponent();

        Form.Opacity = 0;
        Hero.Opacity = 0;

        _idleTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(IdleSeconds) };
        _idleTimer.Tick += (_, _) => GoToIdleMode();

        Loaded += OnLoaded;
        Unloaded += (_, _) => _idleTimer.Stop();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_hasAnimatedThisSession)
        {
            Form.Opacity = 1;
            ResetVisualState();

            Hero.Opacity = 0;
            Card.Opacity = 0;

            var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(260))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Card.BeginAnimation(UIElement.OpacityProperty, fade);

            var heroFade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(260))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Hero.BeginAnimation(UIElement.OpacityProperty, heroFade);

            FocusPassword();
            ResetIdleTimer();
            return;
        }

        _hasAnimatedThisSession = true;
        Form.Visibility = Visibility.Visible;
        Card.UpdateLayout();
        Form.UpdateLayout();

        Dispatcher.BeginInvoke(new Action(PlayIntroAnimation), DispatcherPriority.ApplicationIdle);
    }

    // ================================================================
    //  IDLE MODE
    // ================================================================

    private void ResetIdleTimer()
    {
        _idleTimer.Stop();
        _idleTimer.Start();
    }

    private void GoToIdleMode()
    {
        if (_isIdleMode) return;
        if (!_hasAnimatedThisSession) return;

        _isIdleMode = true;
        _idleTimer.Stop();

        try
        {
            // Release keyboard focus so hidden fields don't capture keystrokes
            Keyboard.ClearFocus();

            double offset = ComputeHeroOffset();
            double clickY = offset + Hero.ActualHeight + 16;

            // Fade out the form
            var formFade = new DoubleAnimation(Form.Opacity, 0, TimeSpan.FromMilliseconds(400))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            Form.BeginAnimation(UIElement.OpacityProperty, formFade);

            // Hero glides from top → card center
            var heroSlide = new DoubleAnimation(HeroTransform.Y, offset, TimeSpan.FromMilliseconds(800))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };
            HeroTransform.BeginAnimation(TranslateTransform.YProperty, heroSlide);

            // "Click to continue" fades in
            var clickFade = new DoubleAnimation(ClickToContinueText.Opacity, 1, TimeSpan.FromMilliseconds(400))
            {
                BeginTime = TimeSpan.FromMilliseconds(400),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            ClickToContinueText.BeginAnimation(UIElement.OpacityProperty, clickFade);

            // "Click to continue" slides to just below the centered logo
            var clickSlide = new DoubleAnimation(ClickToContinueTransform.Y, clickY, TimeSpan.FromMilliseconds(800))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };
            ClickToContinueTransform.BeginAnimation(TranslateTransform.YProperty, clickSlide);
            // Start the glow wave (begins 800ms later, when idle settles)
            StartGlowWave();
        }
        catch
        {
            // Fail-safe: force idle visual state
            Form.Opacity = 0;
            ClickToContinueText.Opacity = 1;
        }
    }

    private void WakeUp()
    {
        if (!_isIdleMode) return;

        _isIdleMode = false;

        try
        {
            double currentHeroY = HeroTransform.Y;
            double currentClickY = ClickToContinueTransform.Y;

            // Fade out "Click to continue"
            var clickFade = new DoubleAnimation(ClickToContinueText.Opacity, 0, TimeSpan.FromMilliseconds(300))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            ClickToContinueText.BeginAnimation(UIElement.OpacityProperty, clickFade);

            // "Click to continue" slides back up to Y=0
            var clickSlide = new DoubleAnimation(currentClickY, 0, TimeSpan.FromMilliseconds(700))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };
            ClickToContinueTransform.BeginAnimation(TranslateTransform.YProperty, clickSlide);

            // Hero glides back to top
            var heroSlide = new DoubleAnimation(currentHeroY, 0, TimeSpan.FromMilliseconds(700))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };
            HeroTransform.BeginAnimation(TranslateTransform.YProperty, heroSlide);

            // Form fades back in (starts 300ms after hero begins moving up)
            var formFade = new DoubleAnimation(Form.Opacity, 1, TimeSpan.FromMilliseconds(400))
            {
                BeginTime = TimeSpan.FromMilliseconds(300),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Form.BeginAnimation(UIElement.OpacityProperty, formFade);

            // Focus password after animation completes
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(750) };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                FocusPassword();
                ResetIdleTimer();
            };
            timer.Start();
            // Stop the glow wave immediately
            StopGlowWave();
        }
        catch
        {
            // Fail-safe: restore normal state
            Form.Opacity = 1;
            HeroTransform.Y = 0;
            ClickToContinueText.Opacity = 0;
            ClickToContinueTransform.Y = 0;
            FocusPassword();
            ResetIdleTimer();
        }
    }

    private void StartGlowWave()
    {
        try
        {
            var startDelay = TimeSpan.FromMilliseconds(1000);   // wait for idle animation to finish
            var waveDuration = TimeSpan.FromSeconds(2.5);        // full wave cycle
            var halfDuration = TimeSpan.FromSeconds(1.5);        // offset between rings

            // ---- Ring 1: starts 800ms after idle begins ----
            var ring1ScaleX = new DoubleAnimation(1.0, 1.5, waveDuration)
            {
                BeginTime = startDelay,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            var ring1ScaleY = new DoubleAnimation(1.0, 1.5, waveDuration)
            {
                BeginTime = startDelay,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            var ring1Fade = new DoubleAnimation(0.35, 0, waveDuration)
            {
                BeginTime = startDelay,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            GlowRing1Scale.BeginAnimation(ScaleTransform.ScaleXProperty, ring1ScaleX);
            GlowRing1Scale.BeginAnimation(ScaleTransform.ScaleYProperty, ring1ScaleY);
            GlowRing1.BeginAnimation(UIElement.OpacityProperty, ring1Fade);

            // ---- Ring 2: offset by half cycle (1000ms after Ring 1) ----
            var ring2Start = startDelay + halfDuration;
            var ring2ScaleX = new DoubleAnimation(1.0, 1.5, waveDuration)
            {
                BeginTime = ring2Start,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            var ring2ScaleY = new DoubleAnimation(1.0, 1.5, waveDuration)
            {
                BeginTime = ring2Start,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            var ring2Fade = new DoubleAnimation(0.35, 0, waveDuration)
            {
                BeginTime = ring2Start,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            GlowRing2Scale.BeginAnimation(ScaleTransform.ScaleXProperty, ring2ScaleX);
            GlowRing2Scale.BeginAnimation(ScaleTransform.ScaleYProperty, ring2ScaleY);
            GlowRing2.BeginAnimation(UIElement.OpacityProperty, ring2Fade);
        }
        catch { /* fail-safe: no glow */ }
    }

    private void StopGlowWave()
    {
        try
        {
            GlowRing1Scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            GlowRing1Scale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            GlowRing1.BeginAnimation(UIElement.OpacityProperty, null);
            GlowRing1Scale.ScaleX = 1;
            GlowRing1Scale.ScaleY = 1;
            GlowRing1.Opacity = 0;

            GlowRing2Scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            GlowRing2Scale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            GlowRing2.BeginAnimation(UIElement.OpacityProperty, null);
            GlowRing2Scale.ScaleX = 1;
            GlowRing2Scale.ScaleY = 1;
            GlowRing2.Opacity = 0;
        }
        catch { /* ignore */ }
    }

    // ================================================================
    //  INPUT INTERCEPTION — reset timer, wake from idle
    // ================================================================

    protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
    {
        if (_isIdleMode)
        {
            WakeUp();
            e.Handled = true;
            return;
        }

        ResetIdleTimer();
        base.OnPreviewMouseDown(e);
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (_isIdleMode)
        {
            WakeUp();
            e.Handled = true;
            return;
        }

        ResetIdleTimer();
        base.OnPreviewKeyDown(e);
    }

    // ================================================================
    //  INTRO ANIMATION (unchanged)
    // ================================================================

    private void PlayIntroAnimation()
    {
        try
        {
            if (_animRetryCount++ > 30)
            {
                _animRetryCount = 0;
                FinalizeWithoutAnimation();
                return;
            }

            Card.UpdateLayout();
            Form.UpdateLayout();

            if (Card.ActualHeight < 100 || Form.ActualHeight < 20)
            {
                Dispatcher.BeginInvoke(new Action(PlayIntroAnimation), DispatcherPriority.ApplicationIdle);
                return;
            }

            double offset = ComputeHeroOffset();

            if (offset < 5)
            {
                Dispatcher.BeginInvoke(new Action(PlayIntroAnimation), DispatcherPriority.ApplicationIdle);
                return;
            }

            _animRetryCount = 0;

            HeroTransform.Y = offset;
            Hero.Opacity = 1;

            var visibleChildren = System.Linq.Enumerable.ToList(
                System.Linq.Enumerable.Where(Form.Children.OfType<UIElement>(),
                    c => c.Visibility == Visibility.Visible));

            foreach (var child in visibleChildren)
            {
                child.Opacity = 0;
                child.RenderTransform = new TranslateTransform(0, FormSlidePx);
            }

            Form.Opacity = 1;
            Form.IsHitTestVisible = false;

            var heroSlide = new DoubleAnimation(offset, 0, TimeSpan.FromMilliseconds(HeroSlideMs))
            {
                BeginTime = TimeSpan.FromMilliseconds(HeroHoldMs),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };
            HeroTransform.BeginAnimation(TranslateTransform.YProperty, heroSlide);

            int lastEndMs = FormStartMs + visibleChildren.Count * FormStepMs + FormDurationMs;

            for (int i = 0; i < visibleChildren.Count; i++)
            {
                var child = visibleChildren[i];
                var beginMs = FormStartMs + i * FormStepMs;

                var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(FormDurationMs))
                {
                    BeginTime = TimeSpan.FromMilliseconds(beginMs),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                child.BeginAnimation(UIElement.OpacityProperty, fade);

                if (child.RenderTransform is TranslateTransform tt)
                {
                    var slide = new DoubleAnimation(FormSlidePx, 0, TimeSpan.FromMilliseconds(FormDurationMs))
                    {
                        BeginTime = TimeSpan.FromMilliseconds(beginMs),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    };
                    tt.BeginAnimation(TranslateTransform.YProperty, slide);
                }
            }

            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(lastEndMs + 100)
            };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                ResetVisualState();
                Form.IsHitTestVisible = true;
                FocusPassword();
                ResetIdleTimer();
            };
            timer.Start();
        }
        catch
        {
            FinalizeWithoutAnimation();
        }
    }

    private void FinalizeWithoutAnimation()
    {
        ResetVisualState();
        Form.Opacity = 1;
        Form.IsHitTestVisible = true;
        FocusPassword();
        ResetIdleTimer();
    }

    private void ResetVisualState()
    {
        Hero.Opacity = 1;
        HeroTransform.Y = 0;

        foreach (UIElement child in Form.Children)
        {
            child.Opacity = 1;
            if (child.RenderTransform is TranslateTransform tt)
                tt.Y = 0;
        }

        ClickToContinueText.Opacity = 0;
        ClickToContinueTransform.Y = 0;
        StopGlowWave();
    }

    private double ComputeHeroOffset()
    {
        var heroPos = Hero.TransformToAncestor(Card).Transform(new Point(0, 0));
        double heroCenter = heroPos.Y + Hero.ActualHeight / 2;
        double cardCenter = Card.ActualHeight / 2;
        return Math.Max(0, Math.Min(cardCenter - heroCenter, 240));
    }

    private void FocusPassword()
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (PasswordBox is not null && PasswordBox.IsVisible)
            {
                PasswordBox.Focus();
                Keyboard.Focus(PasswordBox);
            }
        }), DispatcherPriority.Input);
    }

    // ================================================================
    //  Password field handlers (unchanged)
    // ================================================================

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is UnlockViewModel vm && sender is PasswordBox box)
            vm.Password = box.Password;
    }

    private void ConfirmBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is UnlockViewModel vm && sender is PasswordBox box)
            vm.ConfirmPassword = box.Password;
    }

    private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        if (DataContext is not UnlockViewModel vm) return;
        if (!vm.UnlockCommand.CanExecute(null)) return;

        vm.UnlockCommand.Execute(null);
        e.Handled = true;
    }

    private void ConfirmBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        if (DataContext is not UnlockViewModel vm) return;
        if (!vm.UnlockCommand.CanExecute(null)) return;

        vm.UnlockCommand.Execute(null);
        e.Handled = true;
    }
}