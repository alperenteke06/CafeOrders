using System.Net;
using System.Net.NetworkInformation;

namespace CafeManagement.Kiosk.Services;

public sealed class DeviceIdentityService
{
    public string GetHostName() => Environment.MachineName;

    public string GetMacAddress()
    {
        var adapter = NetworkInterface.GetAllNetworkInterfaces()
            .FirstOrDefault(x => x.OperationalStatus == OperationalStatus.Up &&
                                 x.NetworkInterfaceType != NetworkInterfaceType.Loopback);

        return adapter?.GetPhysicalAddress().ToString() ?? "unknown-device";
    }

    public string GetIpAddress()
    {
        try
        {
            return Dns.GetHostEntry(Dns.GetHostName())
                .AddressList
                .FirstOrDefault(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                ?.ToString() ?? "127.0.0.1";
        }
        catch
        {
            return "127.0.0.1";
        }
    }
}
