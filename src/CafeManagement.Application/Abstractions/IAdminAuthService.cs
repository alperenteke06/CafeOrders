using CafeManagement.Application.Contracts.Auth;

namespace CafeManagement.Application.Abstractions;

public interface IAdminAuthService
{
    Task<AdminUserDto?> ValidateCredentialsAsync(AdminLoginRequest request, CancellationToken cancellationToken = default);
    Task RecordLoginAsync(int adminUserId, CancellationToken cancellationToken = default);
}
