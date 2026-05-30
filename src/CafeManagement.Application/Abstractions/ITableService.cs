using CafeManagement.Application.Contracts.Tables;

namespace CafeManagement.Application.Abstractions;

public interface ITableService
{
    Task<IReadOnlyCollection<TableDto>> GetTablesAsync(CancellationToken cancellationToken = default);
    Task<TableDto> UpsertAsync(UpsertTableRequest request, CancellationToken cancellationToken = default);
}
