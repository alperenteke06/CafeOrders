using CafeManagement.Application.Contracts.Orders;
using CafeManagement.Application.Contracts.Settings;

namespace CafeManagement.Application.Contracts.Dashboard;

public sealed record DashboardSnapshotDto(
    DashboardStatsDto Stats,
    IReadOnlyCollection<DeviceDto> Devices,
    IReadOnlyCollection<OrderDto> Orders,
    InfoMessageDto? ActiveInfoMessage);
