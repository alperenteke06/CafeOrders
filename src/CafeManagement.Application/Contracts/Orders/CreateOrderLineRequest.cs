namespace CafeManagement.Application.Contracts.Orders;

public sealed record CreateOrderLineRequest(int ProductId, int Quantity);
