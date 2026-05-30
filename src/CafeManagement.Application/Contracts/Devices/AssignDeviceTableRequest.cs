namespace CafeManagement.Application.Contracts.Devices;

public sealed record AssignDeviceTableRequest(Guid DeviceId, int? TableId);
