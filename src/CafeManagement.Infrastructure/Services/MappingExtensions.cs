using CafeManagement.Application.Contracts.Catalog;
using CafeManagement.Application.Contracts.Dashboard;
using CafeManagement.Application.Contracts.Orders;
using CafeManagement.Application.Contracts.Settings;
using CafeManagement.Core.Entities;

namespace CafeManagement.Infrastructure.Services;

internal static class MappingExtensions
{
    public static CategoryDto ToDto(this Category category) => new(category.Id, category.Name, category.SortOrder, category.IsActive);

    public static ProductDto ToDto(this Product product)
        => new(product.Id, product.CategoryId, product.Name, product.Description, product.Price, product.ImageUrl, product.IsActive);

    public static DeviceDto ToDto(this Device device)
        => new(device.Id, device.HostName, device.MacAddress, device.IpAddress, device.IsApproved, device.Status.ToString(), device.LastSeenAt, device.TableId);

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
