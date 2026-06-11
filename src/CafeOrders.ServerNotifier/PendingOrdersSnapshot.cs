using CafeOrders.Application.Contracts.Orders;

namespace CafeOrders.ServerNotifier;

public sealed record PendingOrdersSnapshot(
    int Count,
    string MessageTitle,
    string MessageContent,
    IReadOnlyCollection<OrderDto> Orders)
{
    public static PendingOrdersSnapshot Empty { get; } = new(
        Count: 0,
        MessageTitle: string.Empty,
        MessageContent: string.Empty,
        Orders: Array.Empty<OrderDto>());

    public static PendingOrdersSnapshot FromOrders(IEnumerable<OrderDto> orders)
    {
        var pendingOrders = orders
            .Where(order => string.Equals(order.Status, "Pending", StringComparison.OrdinalIgnoreCase))
            .OrderBy(order => order.CreatedAt)
            .ToArray();

        if (pendingOrders.Length == 0)
        {
            return Empty;
        }

        var tables = pendingOrders
            .Select(order => $"Masa {order.TableId:00}")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(6)
            .ToArray();
        var suffix = pendingOrders.Select(order => order.TableId).Distinct().Count() > tables.Length ? " ve diger masalar" : string.Empty;

        return new PendingOrdersSnapshot(
            pendingOrders.Length,
            pendingOrders.Length == 1 ? "1 Adet Siparis Bekliyor" : $"{pendingOrders.Length} Adet Siparis Bekliyor",
            $"{string.Join(", ", tables)}{suffix} yeni siparisler mevcut.",
            pendingOrders);
    }
}
