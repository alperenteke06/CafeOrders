namespace CafeOrders.Application.Contracts.Logging;

public sealed record ApplicationLogCreateRequest(
    string Source,
    string Level,
    string Message,
    string? Exception = null,
    string? Category = null,
    string? MachineName = null,
    string? DeviceKey = null,
    int? TableId = null,
    int? OrderId = null,
    DateTime? CreatedAtUtc = null);
