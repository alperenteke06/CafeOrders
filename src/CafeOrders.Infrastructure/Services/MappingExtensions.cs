using CafeOrders.Application.Contracts.Catalog;
using CafeOrders.Application.Contracts.Dashboard;
using CafeOrders.Application.Contracts.Orders;
using CafeOrders.Application.Contracts.Settings;
using CafeOrders.Domain.Entities;

namespace CafeOrders.Infrastructure.Services;

internal static class MappingExtensions
{
    public static CategoryDto ToDto(this Category category) => new(category.Id, category.Name, category.SortOrder, category.IsActive);

    public static ProductDto ToDto(this Product product)
        => new(product.Id, product.CategoryId, product.Name, product.Description, product.Price, product.ImageUrl, product.IsActive);

    public static DeviceDto ToDto(this Device device, DateTime now)
        => new(
            device.Id,
            device.HostName,
            device.MacAddress,
            device.IpAddress,
            device.IsApproved,
            device.Status.ToString(),
            device.LastSeenAt,
            device.TableId,
            ResolveSessionRemainingSeconds(device, now),
            device.SessionExpiresAtUtc);

    private static int? ResolveSessionRemainingSeconds(Device device, DateTime now)
    {
        if (!device.IsApproved ||
            device.Status != CafeOrders.Domain.Enums.DeviceStatus.Online ||
            !device.SessionExpiresAtUtc.HasValue)
        {
            return null;
        }

        var remaining = device.SessionExpiresAtUtc.Value - now;
        return remaining <= TimeSpan.Zero ? 0 : (int)Math.Ceiling(remaining.TotalSeconds);
    }

    public static OrderDto ToDto(this Order order)
        => new(
            order.Id,
            order.TableId,
            order.DeviceId,
            order.Status.ToString(),
            order.TotalPrice,
            order.CreatedAt,
            order.AcceptedAt,
            order.RejectedAt,
            order.CompletedAt,
            order.OrderLines.Select(line => new OrderLineDto(
                line.ProductId,
                line.Product?.Name ?? $"Urun {line.ProductId}",
                line.Quantity,
                line.UnitPrice,
                line.LineTotal)).ToArray());

    public static InfoMessageDto ToDto(this InfoMessage message)
        => new(message.Id, message.Message, message.Type.ToString(), message.IconKey, message.IsActive, message.StartDate, message.EndDate);
}
