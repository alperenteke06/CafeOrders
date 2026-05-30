namespace CafeOrders.Domain.Enums;

public enum AuditActionType
{
    Created = 0,
    Updated = 1,
    Deleted = 2,
    Approved = 3,
    Rejected = 4,
    Started = 5,
    Ended = 6,
    CommandSent = 7,
    PaymentReceived = 8,
    SecurityViolation = 9
}
