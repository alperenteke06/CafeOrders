namespace CafeManagement.Application.Contracts.Settings;

public sealed record InfoMessageDto(int Id, string Message, string Type, string IconKey, bool IsActive, DateTime? StartDate, DateTime? EndDate);
