using CafeOrders.Domain.Enums;

namespace CafeOrders.Domain.Entities;

public sealed class CashRegisterTransaction
{
    public int Id { get; set; }
    public int? PaymentId { get; set; }
    public CashRegisterTransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Payment? Payment { get; set; }
}
