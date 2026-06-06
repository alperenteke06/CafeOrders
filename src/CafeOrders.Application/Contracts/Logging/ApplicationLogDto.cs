namespace CafeOrders.Application.Contracts.Logging;

public sealed record ApplicationLogDto(
    long Id,
    DateTime CreatedAtUtc,
    string Source,
    string Level,
    string Message,
    string? Exception,
    string? Category,
    string? MachineName,
    string? DeviceKey,
    int? TableId,
    int? OrderId);
