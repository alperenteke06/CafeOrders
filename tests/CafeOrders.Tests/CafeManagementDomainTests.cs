using CafeOrders.Domain.Entities;
using CafeOrders.Domain.Enums;

namespace CafeOrders.Tests;

public sealed class CafeManagementDomainTests
{
    [Fact]
    public void Session_StartAndComplete_TracksLifecycleEvents()
    {
        var now = DateTime.UtcNow;
        var session = new Session
        {
            TableId = 1,
            Type = SessionType.OpenEnded
        };

        session.Start(now);
        session.Complete(125m, now.AddMinutes(45));

        Assert.Equal(SessionStatus.Completed, session.Status);
        Assert.Equal(125m, session.SessionAmount);
        Assert.Equal(now.AddMinutes(45), session.EndedAt);
        Assert.Contains(session.Events, x => x.Type == SessionEventType.Started);
        Assert.Contains(session.Events, x => x.Type == SessionEventType.Ended);
    }

    [Fact]
    public void Member_Debit_ThrowsWhenBalanceIsInsufficient()
    {
        var member = new Member
        {
            Code = "M-001",
            FullName = "Test Member"
        };

        member.AddCredit(50m);

        var exception = Assert.Throws<InvalidOperationException>(() => member.Debit(75m));

        Assert.Equal("Uye bakiyesi yetersiz.", exception.Message);
        Assert.Equal(50m, member.Balance);
    }

    [Fact]
    public void Member_CreditAndDebit_CreatesBalanceTransactions()
    {
        var member = new Member
        {
            Code = "M-002",
            FullName = "Balance Member"
        };

        member.AddCredit(100m, "Bakiye yukleme");
        member.Debit(30m, "Session odemesi");

        Assert.Equal(70m, member.Balance);
        Assert.Equal(2, member.Transactions.Count);
        Assert.Contains(member.Transactions, x => x.Type == MemberTransactionType.Credit && x.BalanceAfter == 100m);
        Assert.Contains(member.Transactions, x => x.Type == MemberTransactionType.Debit && x.BalanceAfter == 70m);
    }

    [Fact]
    public void Payment_Complete_RequiresPositiveAmount()
    {
        var payment = new Payment
        {
            Method = PaymentMethod.Cash,
            Amount = 0m
        };

        Assert.Throws<InvalidOperationException>(() => payment.Complete(DateTime.UtcNow));
    }

    [Fact]
    public void Payment_Complete_MarksPaidAt()
    {
        var now = DateTime.UtcNow;
        var payment = new Payment
        {
            Method = PaymentMethod.Pos,
            Amount = 250m
        };

        payment.Complete(now);

        Assert.Equal(PaymentStatus.Completed, payment.Status);
        Assert.Equal(now, payment.PaidAt);
    }
}
