using CafeOrders.Domain.Enums;

namespace CafeOrders.Domain.Entities;

public sealed class MemberTransaction
{
    public int Id { get; set; }
    public int MemberId { get; set; }
    public MemberTransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Member? Member { get; set; }
}
