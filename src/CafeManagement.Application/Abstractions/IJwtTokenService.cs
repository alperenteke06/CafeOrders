using CafeManagement.Core.Entities;

namespace CafeManagement.Application.Abstractions;

public interface IJwtTokenService
{
    string CreateDeviceToken(Device device);
}
