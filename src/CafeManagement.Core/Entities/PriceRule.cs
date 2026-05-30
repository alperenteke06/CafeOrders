using CafeManagement.Core.Enums;

namespace CafeManagement.Core.Entities;

public sealed class PriceRule
{
    public int Id { get; set; }
    public int PriceProfileId { get; set; }
    public string Name { get; set; } = string.Empty;
    public SessionType SessionType { get; set; } = SessionType.OpenEnded;
    public DayOfWeek? DayOfWeek { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public decimal PricePerMinute { get; set; }
    public int MinimumMinutes { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public PriceProfile? PriceProfile { get; set; }
}
