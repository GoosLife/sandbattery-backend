using Microsoft.EntityFrameworkCore;
using sandbattery_backend.Data;
using sandbattery_backend.Models;

namespace sandbattery_backend.Services;

public class AuthService : IAuthService
{
    private readonly SandbatteryDbContext _db;

    public AuthService(SandbatteryDbContext db) => _db = db;

    public async Task<Device?> ValidateProductKeyAsync(string productKey)
    {
        var entity = await _db.Devices
            .FirstOrDefaultAsync(d => d.ProductKey == productKey);

        return entity is null ? null : new Device
        {
            ProductKey = entity.ProductKey,
            DeviceName = entity.DeviceName
        };
    }
}
