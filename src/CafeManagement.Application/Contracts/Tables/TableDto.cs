namespace CafeManagement.Application.Contracts.Tables;

public sealed record TableDto(
    int Id,
    string Name,
    string? Description,
    bool IsActive,
    DateTime CreatedAt,
    Guid? DeviceId,
    string? DeviceHostName,
    string? DeviceIpAddress,
    bool HasPendingAssignment);
