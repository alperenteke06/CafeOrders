namespace CafeManagement.Application.Contracts.Devices;

public sealed record DeviceRegistrationResponse(Guid DeviceId, bool IsApproved, string DeviceKey, string? Token, string? Message, int? TableId);
