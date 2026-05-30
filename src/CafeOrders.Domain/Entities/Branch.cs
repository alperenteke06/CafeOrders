namespace CafeOrders.Domain.Entities;

public sealed class Branch
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<CafeTable> Tables { get; set; } = new List<CafeTable>();
    public ICollection<TableGroup> TableGroups { get; set; } = new List<TableGroup>();
    public ICollection<Session> Sessions { get; set; } = new List<Session>();
}
