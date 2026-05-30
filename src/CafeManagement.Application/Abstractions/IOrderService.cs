using CafeManagement.Application.Contracts.Orders;

namespace CafeManagement.Application.Abstractions;

public interface IOrderService
{
    Task<OrderDto?> GetByIdAsync(int orderId, CancellationToken cancellationToken = default);
    Task<OrderDto> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default);
    Task<OrderDto?> AcceptAsync(int orderId, CancellationToken cancellationToken = default);
    Task<OrderDto?> RejectAsync(int orderId, CancellationToken cancellationToken = default);
    Task<OrderDto?> CompleteAsync(int orderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<OrderDto>> GetActiveOrdersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<OrderDto>> GetRecentOrdersAsync(int take = 50, CancellationToken cancellationToken = default);
}
