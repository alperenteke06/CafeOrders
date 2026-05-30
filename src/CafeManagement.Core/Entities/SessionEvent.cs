using CafeManagement.Core.Enums;

namespace CafeManagement.Core.Entities;

public sealed class SessionEvent
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public SessionEventType Type { get; set; }
    public string? Description { get; set; }
    public string? PayloadJson { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Session? Session { get; set; }
}
