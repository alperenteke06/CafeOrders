using CafeOrders.Application.Abstractions;
using CafeOrders.Application.Contracts.Tables;
using CafeOrders.Domain.Entities;
using CafeOrders.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CafeOrders.Infrastructure.Services;

public sealed class TableService(
    CafeOrdersDbContext dbContext,
    IRealtimeNotifier realtimeNotifier) : ITableService
{
    public async Task<IReadOnlyCollection<TableDto>> GetTablesAsync(CancellationToken cancellationToken = default)
    {
        var tables = await dbContext.Tables
            .AsNoTracking()
            .Include(x => x.Devices)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return tables.Select(x =>
        {
            var approvedDevice = x.Devices.FirstOrDefault(device => device.IsApproved);
            var hasPending = x.Devices.Any(device => !device.IsApproved);
            return new TableDto(
                x.Id,
                x.Name,
                x.Description,
                x.IsActive,
                x.CreatedAt,
                approvedDevice?.Id,
                approvedDevice?.HostName,
                approvedDevice?.IpAddress,
                hasPending);
        }).ToArray();
    }

    public async Task<TableDto> UpsertAsync(UpsertTableRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidOperationException("Masa adi bos birakilamaz.");
        }

        var table = request.Id.HasValue
            ? await dbContext.Tables.Include(x => x.Devices).FirstOrDefaultAsync(x => x.Id == request.Id.Value, cancellationToken)
            : null;

        var normalizedName = request.Name.Trim();
        var duplicateName = await dbContext.Tables.AnyAsync(x => x.Id != request.Id && x.Name == normalizedName, cancellationToken);
        if (duplicateName)
        {
            throw new InvalidOperationException("Ayni isimde baska bir masa bulunuyor.");
        }

        if (table is null)
        {
            table = new CafeTable();
            await dbContext.Tables.AddAsync(table, cancellationToken);
        }

        table.Name = normalizedName;
        table.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        table.IsActive = request.IsActive;

        await dbContext.SaveChangesAsync(cancellationToken);
        await realtimeNotifier.NotifyTablesUpdatedAsync(cancellationToken);

        var approvedDevice = table.Devices.FirstOrDefault(device => device.IsApproved);
        return new TableDto(
            table.Id,
            table.Name,
            table.Description,
            table.IsActive,
            table.CreatedAt,
            approvedDevice?.Id,
            approvedDevice?.HostName,
            approvedDevice?.IpAddress,
            table.Devices.Any(device => !device.IsApproved));
    }
}
