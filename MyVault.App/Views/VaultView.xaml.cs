using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using MyVault.App.ViewModels;
using MyVault.App.Services;

namespace MyVault.App.Views;

public partial class VaultView : UserControl
{
    public VaultView()
    {
        InitializeComponent();
        // Hide elements before first render (safe — null-checked)
        HideAll();

        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        if (!MotionService.Enabled) { ShowAll(); return; }

        // Start animation at Render priority (fires sooner than Loaded)
        Dispatcher.BeginInvoke(new Action(PlayIntroAnimation), DispatcherPriority.Render);

        // Fallback: if elements are still invisible at 300ms, force-show
        var fallbackTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(300)
        };
        fallbackTimer.Tick += (_, _) =>
        {
            fallbackTimer.Stop();

            // If the primary animation didn't take effect, show everything
            if (SidebarBorder != null && SidebarBorder.Opacity < 0.5)
                ShowAll();
        };
        fallbackTimer.Start();
    }

    private void PlayIntroAnimation()
    {
        try
        {
            AnimateElement(SidebarBorder, slideX: -20, slideY: 0, delayMs: 0, durationMs: 320);
            AnimateElement(SearchBarBorder, slideX: 0, slideY: -10, delayMs: 80, durationMs: 260);
            AnimateElement(EntryListBorder, slideX: 0, slideY: 15, delayMs: 150, durationMs: 320);
            AnimateElement(StatusBarBorder, slideX: 0, slideY: 15, delayMs: 220, durationMs: 260);
        }
        catch
        {
            // If anything throws, force-show immediately
            ShowAll();
        }
    }

    private static void AnimateElement(
        UIElement? element,
        double slideX, double slideY,
        int delayMs, int durationMs)
    {
        if (element == null) return;

        var transform = new TranslateTransform(slideX, slideY);
        element.RenderTransform = transform;

        var duration = TimeSpan.FromMilliseconds(durationMs);
        var delay = TimeSpan.FromMilliseconds(delayMs);
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

        element.BeginAnimation(UIElement.OpacityProperty,
            new DoubleAnimation(0, 1, duration)
            {
                BeginTime = delay,
                EasingFunction = ease,
                FillBehavior = FillBehavior.HoldEnd
            });

        transform.BeginAnimation(TranslateTransform.XProperty,
            new DoubleAnimation(slideX, 0, duration)
            {
                BeginTime = delay,
                EasingFunction = ease
            });

        transform.BeginAnimation(TranslateTransform.YProperty,
            new DoubleAnimation(slideY, 0, duration)
            {
                BeginTime = delay,
                EasingFunction = ease
            });
    }

    private void HideAll()
    {
        HideElement(SidebarBorder);
        HideElement(SearchBarBorder);
        HideElement(EntryListBorder);
        HideElement(StatusBarBorder);
    }

    private void ShowAll()
    {
        ShowElement(SidebarBorder);
        ShowElement(SearchBarBorder);
        ShowElement(EntryListBorder);
        ShowElement(StatusBarBorder);
    }

    private static void HideElement(UIElement? element)
    {
        if (element == null) return;
        element.Opacity = 0;
    }

    private static void ShowElement(UIElement? element)
    {
        if (element == null) return;

        element.BeginAnimation(UIElement.OpacityProperty, null);
        element.Opacity = 1;

        if (element.RenderTransform is TranslateTransform t)
        {
            t.BeginAnimation(TranslateTransform.XProperty, null);
            t.BeginAnimation(TranslateTransform.YProperty, null);
            t.X = 0;
            t.Y = 0;
        }
    }

    // ============================================================
    //  Event handlers (unchanged)
    // ============================================================

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.F && (Keyboard.Modifiers & ModifierKeys.Control) != 0)
        {
            Keyboard.Focus(SearchBox);
            SearchBox.SelectAll();
            e.Handled = true;
            return;
        }
        base.OnPreviewKeyDown(e);
    }

    private void EntryList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is VaultViewModel vm && vm.SelectedEntry is not null)
            vm.EditCommand.Execute(vm.SelectedEntry);
    }

    private void EntryList_KeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not VaultViewModel vm) return;

        if (e.Key == Key.Delete && vm.SelectedEntry is not null)
        {
            vm.DeleteCommand.Execute(vm.SelectedEntry);
            e.Handled = true;
        }
        else if (e.Key == Key.Enter && vm.SelectedEntry is not null)
        {
            vm.EditCommand.Execute(vm.SelectedEntry);
            e.Handled = true;
        }
    }

    private void RenameCategoryInline_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn
            && btn.DataContext is CategoryViewModel catVm
            && DataContext is VaultViewModel vm)
        {
            vm.RenameCategoryCommand.Execute(catVm);
        }
    }

    private void DeleteCategoryInline_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn
            && btn.DataContext is CategoryViewModel catVm
            && DataContext is VaultViewModel vm)
        {
            vm.DeleteCategoryCommand.Execute(catVm);
        }
    }
}