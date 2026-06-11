using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace CafeOrders.ServerNotifier;

public sealed class NotifierViewModel : INotifyPropertyChanged
{
    private readonly NotifierOptions _options;
    private readonly NotifierLogger _logger;
    private string _headerTitle = "Sistem Bildirimi";
    private string _messageTitle = string.Empty;
    private string _messageContent = string.Empty;

    public NotifierViewModel(NotifierOptions options, NotifierLogger logger)
    {
        _options = options;
        _logger = logger;
        OpenOrdersCommand = new RelayCommand(OpenOrders);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string HeaderTitle
    {
        get => _headerTitle;
        private set => SetField(ref _headerTitle, value);
    }

    public string MessageTitle
    {
        get => _messageTitle;
        private set => SetField(ref _messageTitle, value);
    }

    public string MessageContent
    {
        get => _messageContent;
        private set => SetField(ref _messageContent, value);
    }

    public ICommand OpenOrdersCommand { get; }

    public void Apply(PendingOrdersSnapshot snapshot)
    {
        HeaderTitle = "Sistem Bildirimi";
        MessageTitle = snapshot.MessageTitle;
        MessageContent = snapshot.MessageContent;
    }

    private void OpenOrders()
    {
        try
        {
            Process.Start(new ProcessStartInfo(_options.OrdersUrl)
            {
                UseShellExecute = true
            });
            _logger.Info($"Orders page opened. Url={_options.OrdersUrl}");
        }
        catch (Exception exception)
        {
            _logger.Error("Orders page could not be opened.", exception);
        }
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
