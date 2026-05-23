using CafeOrders.Domain.Entities;

namespace CafeOrders.Application.Abstractions;

public interface IJwtTokenService
{
    string CreateDeviceToken(Device device);
}
