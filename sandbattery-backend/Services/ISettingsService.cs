using sandbattery_backend.Models;

namespace sandbattery_backend.Services;

public interface ISettingsService
{
    Task<DeviceSettings> GetSettingsAsync(int deviceId);
    Task<(bool Success, List<string> UpdatedFields)> UpdateSettingsAsync(int deviceId, SettingsUpdateRequest request);
    Task<ElectricityPrice?> GetElectricityPriceAsync(string date, string area);
}
