using CafeOrders.Application.Abstractions;
using CafeOrders.Application.Contracts.Tables;
using Microsoft.AspNetCore.Mvc;

namespace CafeOrders.API.Controllers;

[ApiController]
[Route("api/v1/tables")]
public sealed class TablesController(ITableService tableService, ILogger<TablesController> logger) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyCollection<TableDto>> Get(CancellationToken cancellationToken)
        => tableService.GetTablesAsync(cancellationToken);

    [HttpPost]
    public async Task<TableDto> Upsert([FromBody] UpsertTableRequest request, CancellationToken cancellationToken)
    {
        var table = await tableService.UpsertAsync(request, cancellationToken);
        logger.LogInformation("Table upserted. TableId={TableId}, Name={Name}, IsActive={IsActive}, DeviceId={DeviceId}", table.Id, table.Name, table.IsActive, table.DeviceId);
        return table;
    }
}
