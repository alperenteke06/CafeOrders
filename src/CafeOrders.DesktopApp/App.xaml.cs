using System.Configuration;
using System.Data;
using System.Windows;
using System.Windows.Media.Imaging;

namespace CafeOrders.DesktopApp;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var window = new MainWindow
        {
            Icon = new BitmapImage(new Uri("pack://application:,,,/Assets/app-icon.ico", UriKind.Absolute))
        };

        MainWindow = window;
        window.Show();
    }
}

