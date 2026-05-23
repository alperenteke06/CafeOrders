using CafeOrders.Application.Abstractions;
using CafeOrders.Application.Contracts.Orders;
using CafeOrders.Domain.Entities;
using CafeOrders.Domain.Enums;
using CafeOrders.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CafeOrders.Infrastructure.Services;

public sealed class OrderService(
    CafeOrdersDbContext dbContext,
    IRealtimeNotifier realtimeNotifier,
    ISettingsService settingsService) : IOrderService
{
    public async Task<OrderDto?> GetByIdAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var order = await LoadOrderAsync(orderId, cancellationToken);
        return order?.ToDto();
    }

    public async Task<OrderDto> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        var productIds = request.Lines.Select(x => x.ProductId).Distinct().ToArray();
        var products = await dbContext.Products.Where(x => productIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, cancellationToken);

        var order = new Order
        {
            DeviceId = request.DeviceId,
            TableId = request.TableId,
            Status = OrderStatus.Pending,
            OrderLines = request.Lines.Where(x => x.Quantity > 0 && products.ContainsKey(x.ProductId)).Select(line =>
            {
                var product = products[line.ProductId];
                return new OrderLine
                {
                    ProductId = product.Id,
                    Quantity = line.Quantity,
                    UnitPrice = product.Price,
                    LineTotal = product.Price * line.Quantity
                };
            }).ToList()
        };

        order.RecalculateTotal();
        await dbContext.Orders.AddAsync(order, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var loadedOrder = await LoadOrderAsync(order.Id, cancellationToken) ?? throw new InvalidOperationException("Order reload failed.");
        var dto = loadedOrder.ToDto();
        await realtimeNotifier.NotifyOrderCreatedAsync(dto, cancellationToken);
        return dto;
    }

    public async Task<OrderDto?> AcceptAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var order = await LoadOrderAsync(orderId, cancellationToken);
        if (order is null)
        {
            return null;
        }

        order.AcceptedAt = DateTime.UtcNow;
        order.CompletedAt = order.AcceptedAt;
        order.Status = OrderStatus.Completed;
        await dbContext.SaveChangesAsync(cancellationToken);

        var dto = order.ToDto();
        var message = (await settingsService.GetAppSettingsAsync(cancellationToken)).OrderAcceptedMessage;
        await realtimeNotifier.NotifyOrderAcceptedAsync(order.Device!, dto, message, cancellationToken);
        await realtimeNotifier.NotifyOrderCompletedAsync(order.Device!, dto, cancellationToken);
        return dto;
    }

    public async Task<OrderDto?> RejectAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var order = await LoadOrderAsync(orderId, cancellationToken);
        if (order is null)
        {
            return null;
        }

        order.Status = OrderStatus.Rejected;
        order.RejectedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        var dto = order.ToDto();
        var message = (await settingsService.GetAppSettingsAsync(cancellationToken)).OrderRejectedMessage;
        await realtimeNotifier.NotifyOrderRejectedAsync(order.Device!, dto, message, cancellationToken);
        return dto;
    }

    public async Task<OrderDto?> CompleteAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var order = await LoadOrderAsync(orderId, cancellationToken);
        if (order is null)
        {
            return null;
        }

        order.Status = OrderStatus.Completed;
        order.CompletedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        var dto = order.ToDto();
        await realtimeNotifier.NotifyOrderCompletedAsync(order.Device!, dto, cancellationToken);
        return dto;
    }

    public async Task<IReadOnlyCollection<OrderDto>> GetActiveOrdersAsync(CancellationToken cancellationToken = default)
    {
        var orders = await dbContext.Orders
            .Include(x => x.OrderLines)
            .ThenInclude(x => x.Product)
            .Where(x => x.Status != OrderStatus.Completed)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return orders.Select(x => x.ToDto()).ToArray();
    }

    public async Task<IReadOnlyCollection<OrderDto>> GetRecentOrdersAsync(int take = 50, CancellationToken cancellationToken = default)
    {
        var orders = await dbContext.Orders
            .Include(x => x.OrderLines)
            .ThenInclude(x => x.Product)
            .OrderByDescending(x => x.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

        return orders.Select(x => x.ToDto()).ToArray();
    }

    private Task<Order?> LoadOrderAsync(int orderId, CancellationToken cancellationToken)
    {
        return dbContext.Orders
            .Include(x => x.Device)
            .Include(x => x.OrderLines)
            .ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == orderId, cancellationToken);
    }
}
