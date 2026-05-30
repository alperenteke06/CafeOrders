using CafeManagement.Core.Enums;

namespace CafeManagement.Core.Entities;

public sealed class Payment
{
    public int Id { get; set; }
    public int? SessionId { get; set; }
    public int? OrderId { get; set; }
    public int? MemberId { get; set; }
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    public decimal Amount { get; set; }
    public string? ReferenceNumber { get; set; }
    public DateTime? PaidAt { get; private set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Session? Session { get; set; }
    public Order? Order { get; set; }
    public Member? Member { get; set; }
    public ICollection<CashRegisterTransaction> CashRegisterTransactions { get; set; } = new List<CashRegisterTransaction>();

    public void Complete(DateTime utcNow)
    {
        if (Amount <= 0)
        {
            throw new InvalidOperationException("Odeme tutari sifirdan buyuk olmalidir.");
        }

        Status = PaymentStatus.Completed;
        PaidAt = utcNow;
    }

    public void Fail()
    {
        Status = PaymentStatus.Failed;
    }
}
