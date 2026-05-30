namespace CafeManagement.Application.Contracts.Orders;

public sealed record OrderLineDto(int ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal);
