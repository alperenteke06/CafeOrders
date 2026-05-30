using CafeManagement.Core.Enums;

namespace CafeManagement.Core.Entities;

public sealed class CafeTable
{
    public int Id { get; set; }
    public int? BranchId { get; set; }
    public int? TableGroupId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TableStatus Status { get; set; } = TableStatus.Available;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Branch? Branch { get; set; }
    public TableGroup? TableGroup { get; set; }
    public ICollection<Device> Devices { get; set; } = new List<Device>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<Session> Sessions { get; set; } = new List<Session>();
}
