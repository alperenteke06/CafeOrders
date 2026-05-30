using CafeManagement.Application.Abstractions;
using CafeManagement.Application.Contracts.Auth;
using CafeManagement.Core.Entities;
using CafeManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CafeManagement.Infrastructure.Services;

public sealed class AdminAuthService(
    CafeManagementDbContext dbContext,
    IPasswordHasher<AdminUser> passwordHasher) : IAdminAuthService
{
    public async Task<AdminUserDto?> ValidateCredentialsAsync(AdminLoginRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedUserName = request.UserName.Trim().ToLowerInvariant();
        var user = await dbContext.AdminUsers.FirstOrDefaultAsync(x => x.UserName == normalizedUserName && x.IsActive, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            return null;
        }

        return new AdminUserDto(user.Id, user.UserName, user.DisplayName, user.IsActive);
    }

    public async Task RecordLoginAsync(int adminUserId, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.AdminUsers.FirstOrDefaultAsync(x => x.Id == adminUserId, cancellationToken);
        if (user is null)
        {
            return;
        }

        user.LastLoginAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
