using CafeOrders.Domain.Enums;

namespace CafeOrders.Domain.Entities;

public sealed class Order
{
    public int Id { get; set; }
    public int TableId { get; set; }
    public Guid DeviceId { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public decimal TotalPrice { get; private set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public CafeTable? Table { get; set; }
    public Device? Device { get; set; }
    public ICollection<OrderLine> OrderLines { get; set; } = new List<OrderLine>();

    public void RecalculateTotal()
    {
        TotalPrice = OrderLines.Sum(x => x.LineTotal);
    }
}
