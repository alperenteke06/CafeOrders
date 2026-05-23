using CafeOrders.Application.Abstractions;
using CafeOrders.Application.Contracts.Tables;
using Microsoft.AspNetCore.Mvc;

namespace CafeOrders.API.Controllers;

[ApiController]
[Route("api/v1/tables")]
public sealed class TablesController(ITableService tableService) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyCollection<TableDto>> Get(CancellationToken cancellationToken)
        => tableService.GetTablesAsync(cancellationToken);

    [HttpPost]
    public Task<TableDto> Upsert([FromBody] UpsertTableRequest request, CancellationToken cancellationToken)
        => tableService.UpsertAsync(request, cancellationToken);
}
