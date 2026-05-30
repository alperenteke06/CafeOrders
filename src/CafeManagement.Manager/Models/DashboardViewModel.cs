using CafeManagement.Application.Contracts.Catalog;
using CafeManagement.Application.Contracts.Dashboard;
using CafeManagement.Application.Contracts.Orders;
using CafeManagement.Application.Contracts.Settings;
using CafeManagement.Application.Contracts.Tables;

namespace CafeManagement.Manager.Models;

public sealed class DashboardViewModel
{
    public required string ActiveSection { get; init; }
    public required string SelectedRange { get; init; }
    public required string SearchQuery { get; init; }
    public required string CategoryFilter { get; init; }
    public required int CurrentPage { get; init; }
    public required DashboardSnapshotDto Snapshot { get; init; }
    public required CatalogResponseDto Catalog { get; init; }
    public required AppSettingsDto AppSettings { get; init; }
    public required IReadOnlyCollection<TableDto> Tables { get; init; }
    public required IReadOnlyCollection<OrderDto> RecentOrders { get; init; }
    public required IReadOnlyCollection<ProductCardViewModel> ProductCards { get; init; }
    public required IReadOnlyCollection<NotificationItemViewModel> Notifications { get; init; }
}

public sealed class ProductCardViewModel
{
    public required int Id { get; init; }
    public required int CategoryId { get; init; }
    public required string Name { get; init; }
    public required string CategoryName { get; init; }
    public string? Description { get; init; }
    public string? ImageUrl { get; init; }
    public required decimal Price { get; init; }
    public required string VisualClass { get; init; }
    public required bool InStock { get; init; }
}

public sealed class NotificationItemViewModel
{
    public required int OrderId { get; init; }
    public required string Title { get; init; }
    public required string Meta { get; init; }
    public required string Amount { get; init; }
    public required string Status { get; init; }
}
