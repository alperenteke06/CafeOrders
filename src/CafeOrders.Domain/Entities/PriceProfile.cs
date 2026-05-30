namespace CafeOrders.Domain.Entities;

public sealed class PriceProfile
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PriceRule> Rules { get; set; } = new List<PriceRule>();
    public ICollection<Session> Sessions { get; set; } = new List<Session>();
}
