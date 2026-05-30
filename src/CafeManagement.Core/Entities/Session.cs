using CafeManagement.Core.Enums;

namespace CafeManagement.Core.Entities;

public sealed class Session
{
    public int Id { get; set; }
    public int? BranchId { get; set; }
    public int TableId { get; set; }
    public Guid? DeviceId { get; set; }
    public int? MemberId { get; set; }
    public int? PriceProfileId { get; set; }
    public SessionType Type { get; set; }
    public SessionStatus Status { get; private set; } = SessionStatus.Pending;
    public DateTime StartedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? PlannedEndAt { get; set; }
    public DateTime? EndedAt { get; private set; }
    public decimal SessionAmount { get; private set; }
    public string? Notes { get; set; }

    public Branch? Branch { get; set; }
    public CafeTable? Table { get; set; }
    public Device? Device { get; set; }
    public Member? Member { get; set; }
    public PriceProfile? PriceProfile { get; set; }
    public ICollection<SessionEvent> Events { get; set; } = new List<SessionEvent>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public void Start(DateTime utcNow)
    {
        if (Status is not SessionStatus.Pending)
        {
            throw new InvalidOperationException("Sadece bekleyen oturum baslatilabilir.");
        }

        StartedAt = utcNow;
        Status = SessionStatus.Active;
        AddEvent(SessionEventType.Started, "Oturum baslatildi.", utcNow);
    }

    public void Complete(decimal sessionAmount, DateTime utcNow)
    {
        if (Status is SessionStatus.Completed or SessionStatus.Cancelled)
        {
            throw new InvalidOperationException("Kapanmis oturum tekrar kapatilamaz.");
        }

        if (sessionAmount < 0)
        {
            throw new InvalidOperationException("Oturum tutari negatif olamaz.");
        }

        SessionAmount = sessionAmount;
        EndedAt = utcNow;
        Status = SessionStatus.Completed;
        AddEvent(SessionEventType.Ended, "Oturum kapatildi.", utcNow);
    }

    public void Cancel(string? reason, DateTime utcNow)
    {
        if (Status is SessionStatus.Completed)
        {
            throw new InvalidOperationException("Tamamlanmis oturum iptal edilemez.");
        }

        EndedAt = utcNow;
        Status = SessionStatus.Cancelled;
        AddEvent(SessionEventType.Cancelled, reason ?? "Oturum iptal edildi.", utcNow);
    }

    public SessionEvent AddEvent(SessionEventType type, string? description, DateTime utcNow)
    {
        var sessionEvent = new SessionEvent
        {
            Session = this,
            SessionId = Id,
            Type = type,
            Description = description,
            CreatedAt = utcNow
        };

        Events.Add(sessionEvent);
        return sessionEvent;
    }
}
