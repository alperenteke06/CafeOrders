namespace CafeOrders.Application.Contracts.Auth;

public sealed record AdminUserDto(int Id, string UserName, string DisplayName, bool IsActive);
