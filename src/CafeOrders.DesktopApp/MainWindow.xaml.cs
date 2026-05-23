using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using CafeOrders.DesktopApp.ViewModels;

namespace CafeOrders.DesktopApp;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Closing += OnClosingAsync;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
            SyncAnimatedStates(viewModel, useTransitions: false);
            await viewModel.InitializeAsync();
            SyncAnimatedStates(viewModel, useTransitions: true);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not MainViewModel viewModel)
        {
            return;
        }

        if (e.PropertyName is nameof(MainViewModel.IsCartOpen)
            or nameof(MainViewModel.IsMenuViewVisible)
            or nameof(MainViewModel.IsOrderStatusVisible))
        {
            Dispatcher.Invoke(() => SyncAnimatedStates(viewModel, useTransitions: true));
        }
    }

    private void SyncAnimatedStates(MainViewModel viewModel, bool useTransitions)
    {
        AnimateCartState(viewModel.IsCartOpen, useTransitions);
        AnimateContentState(MenuViewRoot, MenuViewTransform, viewModel.IsMenuViewVisible, useTransitions, offset: -28, animateAxis: "X");
        AnimateContentState(OrderViewRoot, OrderViewTransform, viewModel.IsOrderStatusVisible, useTransitions, offset: 18, animateAxis: "Y");
    }

    private void AnimateCartState(bool isOpen, bool useTransitions)
    {
        if (!useTransitions)
        {
            CartOverlay.Visibility = isOpen ? Visibility.Visible : Visibility.Collapsed;
            CartDrawer.Visibility = isOpen ? Visibility.Visible : Visibility.Collapsed;
            CartOverlay.Opacity = isOpen ? 1 : 0;
            CartDrawerTransform.X = isOpen ? 0 : 560;
            return;
        }

        if (isOpen)
        {
            CartOverlay.Visibility = Visibility.Visible;
            CartDrawer.Visibility = Visibility.Visible;
        }

        var overlayAnimation = new DoubleAnimation
        {
            To = isOpen ? 1 : 0,
            Duration = TimeSpan.FromMilliseconds(220),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };

        if (!isOpen)
        {
            overlayAnimation.Completed += (_, _) =>
            {
                if (DataContext is MainViewModel vm && !vm.IsCartOpen)
                {
                    CartOverlay.Visibility = Visibility.Collapsed;
                    CartDrawer.Visibility = Visibility.Collapsed;
                    SyncAnimatedStates(vm, useTransitions: false);
                }
            };
        }

        CartOverlay.BeginAnimation(OpacityProperty, overlayAnimation);

        var drawerAnimation = new DoubleAnimation
        {
            To = isOpen ? 0 : 560,
            Duration = TimeSpan.FromMilliseconds(260),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        CartDrawerTransform.BeginAnimation(TranslateTransform.XProperty, drawerAnimation);
    }

    private static void AnimateContentState(UIElement element, TranslateTransform transform, bool isVisible, bool useTransitions, double offset, string animateAxis)
    {
        var useVerticalAxis = string.Equals(animateAxis, "Y", StringComparison.OrdinalIgnoreCase);
        if (element is FrameworkElement frameworkElement)
        {
            frameworkElement.Tag = isVisible;
        }

        if (!useTransitions)
        {
            element.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
            element.Opacity = isVisible ? 1 : 0;
            if (useVerticalAxis)
            {
                transform.Y = isVisible ? 0 : offset;
            }
            else
            {
                transform.X = isVisible ? 0 : offset;
            }
            return;
        }

        if (isVisible)
        {
            element.Visibility = Visibility.Visible;
        }

        var opacityAnimation = new DoubleAnimation
        {
            To = isVisible ? 1 : 0,
            Duration = TimeSpan.FromMilliseconds(220),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };

        if (!isVisible)
        {
            opacityAnimation.Completed += (_, _) =>
            {
                if (element is FrameworkElement target && target.Tag is bool shouldBeVisible && !shouldBeVisible)
                {
                    element.Visibility = Visibility.Collapsed;
                }
            };
        }

        element.BeginAnimation(OpacityProperty, opacityAnimation);
        transform.BeginAnimation(useVerticalAxis ? TranslateTransform.YProperty : TranslateTransform.XProperty, new DoubleAnimation
        {
            To = isVisible ? 0 : offset,
            Duration = TimeSpan.FromMilliseconds(260),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        });
    }

    private void CartOverlay_OnMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel viewModel && viewModel.IsCartOpen)
        {
            viewModel.CloseCartCommand.Execute(null);
        }
    }

    private void MinimizeButton_OnClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void OnClosingAsync(object? sender, CancelEventArgs e)
    {
        Closing -= OnClosingAsync;

        if (DataContext is MainViewModel viewModel)
        {
            try
            {
                await viewModel.ShutdownAsync();
            }
            catch
            {
            }
        }
    }
}
