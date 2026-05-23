using CafeOrders.Application.Contracts.Orders;
using CafeOrders.Application.Contracts.Settings;

namespace CafeOrders.Application.Contracts.Dashboard;

public sealed record DashboardSnapshotDto(
    DashboardStatsDto Stats,
    IReadOnlyCollection<DeviceDto> Devices,
    IReadOnlyCollection<OrderDto> Orders,
    InfoMessageDto? ActiveInfoMessage);
