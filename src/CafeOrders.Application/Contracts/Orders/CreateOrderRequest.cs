namespace CafeOrders.Application.Contracts.Orders;

public sealed record CreateOrderRequest(Guid DeviceId, int TableId, IReadOnlyCollection<CreateOrderLineRequest> Lines);
