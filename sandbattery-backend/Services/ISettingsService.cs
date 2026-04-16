using sandbattery_backend.Models;

namespace sandbattery_backend.Services;

public interface ISettingsService
{
    Task<DeviceSettings> GetSettingsAsync(string productKey);
    Task<(bool Success, List<string> UpdatedFields)> UpdateSettingsAsync(string productKey, SettingsUpdateRequest request);
    Task<ElectricityPrice?> GetElectricityPriceAsync(string date, string area);
}
