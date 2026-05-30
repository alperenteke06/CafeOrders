using CafeOrders.Domain.Enums;

namespace CafeOrders.Domain.Entities;

public sealed class Member
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public decimal Balance { get; private set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastSeenAt { get; set; }

    public ICollection<MemberTransaction> Transactions { get; set; } = new List<MemberTransaction>();
    public ICollection<Session> Sessions { get; set; } = new List<Session>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public MemberTransaction AddCredit(decimal amount, string? description = null)
    {
        if (amount <= 0)
        {
            throw new InvalidOperationException("Yuklenecek bakiye sifirdan buyuk olmalidir.");
        }

        Balance += amount;
        var transaction = CreateTransaction(MemberTransactionType.Credit, amount, description);
        Transactions.Add(transaction);
        return transaction;
    }

    public MemberTransaction Debit(decimal amount, string? description = null)
    {
        if (amount <= 0)
        {
            throw new InvalidOperationException("Dusulecek bakiye sifirdan buyuk olmalidir.");
        }

        if (Balance < amount)
        {
            throw new InvalidOperationException("Uye bakiyesi yetersiz.");
        }

        Balance -= amount;
        var transaction = CreateTransaction(MemberTransactionType.Debit, amount, description);
        Transactions.Add(transaction);
        return transaction;
    }

    private MemberTransaction CreateTransaction(MemberTransactionType type, decimal amount, string? description)
        => new()
        {
            Member = this,
            MemberId = Id,
            Type = type,
            Amount = amount,
            BalanceAfter = Balance,
            Description = description
        };
}
