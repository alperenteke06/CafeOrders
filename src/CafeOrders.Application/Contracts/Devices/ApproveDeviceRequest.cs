namespace CafeOrders.Application.Contracts.Devices;

public sealed record ApproveDeviceRequest(Guid DeviceId, int? TableId);
