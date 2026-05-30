namespace CafeManagement.Application.Contracts.Tables;

public sealed record UpsertTableRequest(int? Id, string Name, string? Description, bool IsActive);
