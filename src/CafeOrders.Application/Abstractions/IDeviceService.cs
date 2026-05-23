using CafeOrders.Application.Contracts.Devices;

namespace CafeOrders.Application.Abstractions;

public interface IDeviceService
{
    Task<DeviceRegistrationResponse> RegisterAsync(DeviceRegistrationRequest request, CancellationToken cancellationToken = default);
    Task<DeviceRegistrationResponse?> ApproveAsync(ApproveDeviceRequest request, CancellationToken cancellationToken = default);
    Task<DeviceRegistrationResponse?> AssignTableAsync(AssignDeviceTableRequest request, CancellationToken cancellationToken = default);
    Task<bool> RejectAsync(Guid deviceId, CancellationToken cancellationToken = default);
    Task<bool> HeartbeatAsync(HeartbeatRequest request, CancellationToken cancellationToken = default);
}
