namespace CafeOrders.Application.Contracts.Settings;

public sealed record UpdateInfoMessageRequest(string Message, string Type, string IconKey, bool IsActive, DateTime? StartDate, DateTime? EndDate);
