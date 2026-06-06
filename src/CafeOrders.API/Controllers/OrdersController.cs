using CafeOrders.Application.Abstractions;
using CafeOrders.Application.Contracts.Orders;
using Microsoft.AspNetCore.Mvc;

namespace CafeOrders.API.Controllers;

[ApiController]
[Route("api/v1/orders")]
public sealed class OrdersController(IOrderService orderService, ILogger<OrdersController> logger) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyCollection<OrderDto>> Get([FromQuery] bool soundPendingOnly, CancellationToken cancellationToken)
        => orderService.GetActiveOrdersAsync(soundPendingOnly, cancellationToken);

    [HttpGet("{orderId:int}")]
    public async Task<IActionResult> GetById(int orderId, CancellationToken cancellationToken)
    {
        var result = await orderService.GetByIdAsync(orderId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var order = await orderService.CreateAsync(request, cancellationToken);
            logger.LogInformation(
                "Order created. OrderId={OrderId}, DeviceId={DeviceId}, TableId={TableId}, LineCount={LineCount}, TotalPrice={TotalPrice}",
                order.Id,
                request.DeviceId,
                request.TableId,
                request.Lines.Count,
                order.TotalPrice);
            return Ok(order);
        }
        catch (InvalidOperationException exception)
        {
            logger.LogWarning(
                exception,
                "Order create rejected. DeviceId={DeviceId}, TableId={TableId}, LineCount={LineCount}",
                request.DeviceId,
                request.TableId,
                request.Lines.Count);
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPost("{orderId:int}/accept")]
    public async Task<IActionResult> Accept(int orderId, CancellationToken cancellationToken)
    {
        var result = await orderService.AcceptAsync(orderId, cancellationToken);
        if (result is not null)
        {
            logger.LogInformation("Order accepted. OrderId={OrderId}, Status={Status}, TotalPrice={TotalPrice}", result.Id, result.Status, result.TotalPrice);
        }
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{orderId:int}/reject")]
    public async Task<IActionResult> Reject(int orderId, CancellationToken cancellationToken)
    {
        var result = await orderService.RejectAsync(orderId, cancellationToken);
        if (result is not null)
        {
            logger.LogInformation("Order rejected. OrderId={OrderId}, Status={Status}, TotalPrice={TotalPrice}", result.Id, result.Status, result.TotalPrice);
        }
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{orderId:int}/complete")]
    public async Task<IActionResult> Complete(int orderId, CancellationToken cancellationToken)
    {
        var result = await orderService.CompleteAsync(orderId, cancellationToken);
        if (result is not null)
        {
            logger.LogInformation("Order completed. OrderId={OrderId}, Status={Status}, TotalPrice={TotalPrice}", result.Id, result.Status, result.TotalPrice);
        }
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{orderId:int}/sound-played")]
    public async Task<IActionResult> MarkSoundPlayed(int orderId, CancellationToken cancellationToken)
    {
        var result = await orderService.MarkSoundPlayedAsync(orderId, cancellationToken);
        if (result is not null)
        {
            logger.LogInformation("Order sound marked as played. OrderId={OrderId}, SoundPlayedAt={SoundPlayedAt}", result.Id, result.SoundPlayedAt);
        }
        return result is null ? NotFound() : Ok(result);
    }
}
