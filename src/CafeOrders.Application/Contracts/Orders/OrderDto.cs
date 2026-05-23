namespace CafeOrders.Application.Contracts.Orders;

public sealed record OrderDto(
    int Id,
    int TableId,
    Guid DeviceId,
    string Status,
    decimal TotalPrice,
    DateTime CreatedAt,
    DateTime? AcceptedAt,
    DateTime? RejectedAt,
    DateTime? CompletedAt,
    IReadOnlyCollection<OrderLineDto> Lines);
