using sandbattery_backend.Models;

namespace sandbattery_backend.Services;

public interface IAuthService
{
    Task<Device?> ValidateProductKeyAsync(string productKey);
}
