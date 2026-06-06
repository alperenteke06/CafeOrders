using CafeOrders.Application.Abstractions;
using CafeOrders.Application.Contracts.Devices;
using Microsoft.AspNetCore.Mvc;

namespace CafeOrders.API.Controllers;

[ApiController]
[Route("api/v1/devices")]
public sealed class DevicesController(IDeviceService deviceService, ILogger<DevicesController> logger) : ControllerBase
{
    [HttpPost("register")]
    public async Task<DeviceRegistrationResponse> Register([FromBody] DeviceRegistrationRequest request, CancellationToken cancellationToken)
    {
        var response = await deviceService.RegisterAsync(request, cancellationToken);
        logger.LogInformation(
            "Device registration received. DeviceId={DeviceId}, HostName={HostName}, IpAddress={IpAddress}, IsApproved={IsApproved}, TableId={TableId}",
            response.DeviceId,
            request.HostName,
            request.IpAddress,
            response.IsApproved,
            response.TableId);
        return response;
    }

    [HttpPost("approve")]
    public async Task<IActionResult> Approve([FromBody] ApproveDeviceRequest request, CancellationToken cancellationToken)
    {
        var result = await deviceService.ApproveAsync(request, cancellationToken);
        if (result is not null)
        {
            logger.LogInformation("Device approved. DeviceId={DeviceId}, TableId={TableId}", request.DeviceId, request.TableId);
        }
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("assign-table")]
    public async Task<IActionResult> AssignTable([FromBody] AssignDeviceTableRequest request, CancellationToken cancellationToken)
    {
        var result = await deviceService.AssignTableAsync(request, cancellationToken);
        if (result is not null)
        {
            logger.LogInformation("Device table assignment changed. DeviceId={DeviceId}, TableId={TableId}", request.DeviceId, request.TableId);
        }
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{deviceId:guid}")]
    public async Task<IActionResult> Reject(Guid deviceId, CancellationToken cancellationToken)
    {
        var rejected = await deviceService.RejectAsync(deviceId, cancellationToken);
        if (rejected)
        {
            logger.LogInformation("Device rejected/deleted. DeviceId={DeviceId}", deviceId);
        }
        return rejected ? Ok() : NotFound();
    }

    [HttpPost("heartbeat")]
    public async Task<IActionResult> Heartbeat([FromBody] HeartbeatRequest request, CancellationToken cancellationToken)
        => await deviceService.HeartbeatAsync(request, cancellationToken) ? Ok() : NotFound();
}
