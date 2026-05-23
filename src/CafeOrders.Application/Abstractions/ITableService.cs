using CafeOrders.Application.Contracts.Tables;

namespace CafeOrders.Application.Abstractions;

public interface ITableService
{
    Task<IReadOnlyCollection<TableDto>> GetTablesAsync(CancellationToken cancellationToken = default);
    Task<TableDto> UpsertAsync(UpsertTableRequest request, CancellationToken cancellationToken = default);
}
