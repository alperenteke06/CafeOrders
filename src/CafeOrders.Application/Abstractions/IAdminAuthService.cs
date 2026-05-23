using CafeOrders.Application.Contracts.Auth;

namespace CafeOrders.Application.Abstractions;

public interface IAdminAuthService
{
    Task<AdminUserDto?> ValidateCredentialsAsync(AdminLoginRequest request, CancellationToken cancellationToken = default);
    Task RecordLoginAsync(int adminUserId, CancellationToken cancellationToken = default);
}
