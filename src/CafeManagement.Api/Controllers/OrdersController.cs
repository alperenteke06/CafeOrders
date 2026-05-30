using CafeManagement.Application.Abstractions;
using CafeManagement.Application.Contracts.Orders;
using Microsoft.AspNetCore.Mvc;

namespace CafeManagement.Api.Controllers;

[ApiController]
[Route("api/v1/orders")]
public sealed class OrdersController(IOrderService orderService) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyCollection<OrderDto>> Get(CancellationToken cancellationToken)
        => orderService.GetActiveOrdersAsync(cancellationToken);

    [HttpGet("{orderId:int}")]
    public async Task<IActionResult> GetById(int orderId, CancellationToken cancellationToken)
    {
        var result = await orderService.GetByIdAsync(orderId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public Task<OrderDto> Create([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
        => orderService.CreateAsync(request, cancellationToken);

    [HttpPost("{orderId:int}/accept")]
    public async Task<IActionResult> Accept(int orderId, CancellationToken cancellationToken)
    {
        var result = await orderService.AcceptAsync(orderId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{orderId:int}/reject")]
    public async Task<IActionResult> Reject(int orderId, CancellationToken cancellationToken)
    {
        var result = await orderService.RejectAsync(orderId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{orderId:int}/complete")]
    public async Task<IActionResult> Complete(int orderId, CancellationToken cancellationToken)
    {
        var result = await orderService.CompleteAsync(orderId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
