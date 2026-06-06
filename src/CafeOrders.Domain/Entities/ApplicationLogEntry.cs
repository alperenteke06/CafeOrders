namespace CafeOrders.Domain.Entities;

public sealed class ApplicationLogEntry
{
    public long Id { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string Source { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
    public string? Category { get; set; }
    public string? MachineName { get; set; }
    public string? DeviceKey { get; set; }
    public int? TableId { get; set; }
    public int? OrderId { get; set; }
}
