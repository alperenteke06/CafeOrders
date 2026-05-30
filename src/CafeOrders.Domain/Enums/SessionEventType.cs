namespace CafeOrders.Domain.Enums;

public enum SessionEventType
{
    Created = 0,
    Started = 1,
    Paused = 2,
    Resumed = 3,
    Extended = 4,
    OrderAdded = 5,
    PaymentAdded = 6,
    Ended = 7,
    Expired = 8,
    Cancelled = 9,
    NoteAdded = 10
}
