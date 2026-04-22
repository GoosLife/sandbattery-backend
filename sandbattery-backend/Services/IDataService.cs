using sandbattery_backend.Models;

namespace sandbattery_backend.Services;

public interface IDataService
{
    Task<SensorMeasurement?> GetLatestMeasurementAsync(string productKey);
    Task<DataHistory> GetMeasurementHistoryAsync(string productKey, DateTime from, DateTime to, MeasurementInterval? interval, int limit);
    Task<SensorMeasurement> AddMeasurementAsync(string productKey, SensorMeasurement measurement);
}
