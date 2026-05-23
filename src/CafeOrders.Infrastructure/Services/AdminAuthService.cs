using CafeOrders.Application.Abstractions;
using CafeOrders.Application.Contracts.Auth;
using CafeOrders.Domain.Entities;
using CafeOrders.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CafeOrders.Infrastructure.Services;

public sealed class AdminAuthService(
    CafeOrdersDbContext dbContext,
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
