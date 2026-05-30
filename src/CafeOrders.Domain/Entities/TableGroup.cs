namespace CafeOrders.Domain.Entities;

public sealed class TableGroup
{
    public int Id { get; set; }
    public int? BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Branch? Branch { get; set; }
    public ICollection<CafeTable> Tables { get; set; } = new List<CafeTable>();
}
