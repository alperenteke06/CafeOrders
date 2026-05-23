using CafeOrders.Domain.Enums;

namespace CafeOrders.Domain.Entities;

public sealed class Device
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int? TableId { get; set; }
    public string HostName { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
    public DeviceStatus Status { get; set; } = DeviceStatus.Pending;
    public DateTime? LastSeenAt { get; set; }
    public string? ConnectionId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public CafeTable? Table { get; set; }
    public ICollection<Order> Orders { get; set; } = new List<Order>();

    public string DeviceKey => MacAddress.Trim().ToLowerInvariant();
}
