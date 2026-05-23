namespace CafeOrders.Application.Contracts.Orders;

public sealed record CreateOrderLineRequest(int ProductId, int Quantity);
