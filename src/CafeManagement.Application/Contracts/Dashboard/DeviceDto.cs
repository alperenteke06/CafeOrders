namespace CafeManagement.Application.Contracts.Dashboard;

public sealed record DeviceDto(Guid Id, string HostName, string MacAddress, string IpAddress, bool IsApproved, string Status, DateTime? LastSeenAt, int? TableId);
