namespace CafeOrders.Application.Contracts.Auth;

public sealed record AdminLoginRequest(string UserName, string Password);
