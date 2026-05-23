using CafeOrders.Domain.Enums;

namespace CafeOrders.Domain.Entities;

public sealed class InfoMessage
{
    public int Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public InfoMessageType Type { get; set; } = InfoMessageType.Info;
    public string IconKey { get; set; } = "campaign";
    public bool IsActive { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsCurrentlyActive(DateTime utcNow)
    {
        if (!IsActive)
        {
            return false;
        }

        if (StartDate.HasValue && StartDate.Value > utcNow)
        {
            return false;
        }

        if (EndDate.HasValue && EndDate.Value < utcNow)
        {
            return false;
        }

        return true;
    }
}
