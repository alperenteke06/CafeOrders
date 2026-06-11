using System.Windows;
using System.Windows.Media.Animation;

namespace CafeOrders.ServerNotifier;

public partial class MainWindow : Window
{
    private readonly NotifierOptions _options;
    private readonly NotifierLogger _logger;
    private readonly NotifierViewModel _viewModel;
    private readonly ServerNotifierService _service;
    private bool _isNotificationVisible;
    private bool _isClosing;

    public MainWindow()
    {
        InitializeComponent();
        _options = NotifierOptions.Load();
        _logger = new NotifierLogger(_options.LogPath);
        _logger.ConfigureRemote(_options.ApiBaseUrl);
        _viewModel = new NotifierViewModel(_options, _logger);
        _service = new ServerNotifierService(_options, _logger);
        _service.SnapshotChanged += OnSnapshotChanged;
        DataContext = _viewModel;
    }

    public async Task StartAsync()
    {
        _logger.Info("CafeOrders ServerNotifier starting.");
        try
        {
            await _service.StartAsync();
        }
        catch (Exception exception)
        {
            _logger.Error("CafeOrders ServerNotifier startup failed.", exception);
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        PositionAboveTaskbar();
        BeginStoryboard((Storyboard)FindResource("PulseIconStoryboard"));
    }

    private async void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_isClosing)
        {
            return;
        }

        _isClosing = true;
        await _service.DisposeAsync();
        _logger.Info("CafeOrders ServerNotifier stopped.");
    }

    private void OnSnapshotChanged(PendingOrdersSnapshot snapshot)
    {
        Dispatcher.Invoke(() =>
        {
            if (snapshot.Count <= 0)
            {
                HideNotification();
                return;
            }

            _viewModel.Apply(snapshot);
            ShowNotification();
        });
    }

    private void ShowNotification()
    {
        PositionAboveTaskbar();
        Topmost = true;
        if (!_isNotificationVisible)
        {
            _isNotificationVisible = true;
            Show();
            BeginStoryboard((Storyboard)FindResource("ShowNotificationStoryboard"));
        }
    }

    private void HideNotification()
    {
        if (!_isNotificationVisible)
        {
            Hide();
            return;
        }

        _isNotificationVisible = false;
        var storyboard = (Storyboard)FindResource("HideNotificationStoryboard");
        storyboard.Completed += (_, _) =>
        {
            if (!_isNotificationVisible)
            {
                Hide();
            }
        };
        BeginStoryboard(storyboard);
    }

    private void PositionAboveTaskbar()
    {
        var area = SystemParameters.WorkArea;
        Left = Math.Max(area.Left, area.Right - Width - 18);
        Top = Math.Max(area.Top, area.Bottom - Height - 18);
    }
}
