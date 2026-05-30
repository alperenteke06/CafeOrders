using CafeOrders.Domain.Enums;

namespace CafeOrders.Domain.Entities;

public sealed class AuditLog
{
    public long Id { get; set; }
    public string? ActorUserName { get; set; }
    public AuditActionType ActionType { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? Description { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
