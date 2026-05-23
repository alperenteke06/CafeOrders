namespace CafeOrders.Application.Contracts.Dashboard;

public sealed record DashboardStatsDto(int OnlineDevices, int PendingApprovals, int PendingOrders, decimal RevenueToday);
