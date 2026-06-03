namespace CafeOrders.Application.Contracts.Devices;

public sealed record HeartbeatRequest(Guid DeviceId, int? SessionRemainingSeconds = null);
