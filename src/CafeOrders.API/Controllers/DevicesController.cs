using CafeOrders.Application.Abstractions;
using CafeOrders.Application.Contracts.Devices;
using Microsoft.AspNetCore.Mvc;

namespace CafeOrders.API.Controllers;

[ApiController]
[Route("api/v1/devices")]
public sealed class DevicesController(IDeviceService deviceService) : ControllerBase
{
    [HttpPost("register")]
    public Task<DeviceRegistrationResponse> Register([FromBody] DeviceRegistrationRequest request, CancellationToken cancellationToken)
        => deviceService.RegisterAsync(request, cancellationToken);

    [HttpPost("approve")]
    public async Task<IActionResult> Approve([FromBody] ApproveDeviceRequest request, CancellationToken cancellationToken)
    {
        var result = await deviceService.ApproveAsync(request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("assign-table")]
    public async Task<IActionResult> AssignTable([FromBody] AssignDeviceTableRequest request, CancellationToken cancellationToken)
    {
        var result = await deviceService.AssignTableAsync(request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{deviceId:guid}")]
    public async Task<IActionResult> Reject(Guid deviceId, CancellationToken cancellationToken)
        => await deviceService.RejectAsync(deviceId, cancellationToken) ? Ok() : NotFound();

    [HttpPost("heartbeat")]
    public async Task<IActionResult> Heartbeat([FromBody] HeartbeatRequest request, CancellationToken cancellationToken)
        => await deviceService.HeartbeatAsync(request, cancellationToken) ? Ok() : NotFound();
}
