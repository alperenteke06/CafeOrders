namespace CafeOrders.Application.Contracts.Devices;

public sealed record DeviceRegistrationRequest(string HostName, string MacAddress, string IpAddress);
