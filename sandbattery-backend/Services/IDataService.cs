using sandbattery_backend.Models;

namespace sandbattery_backend.Services;

public interface IDataService
{
    Task<SensorMeasurement?> GetLatestMeasurementAsync(int deviceId);
    Task<DataHistory> GetMeasurementHistoryAsync(int deviceId, DateTime from, DateTime to, MeasurementInterval? interval, int limit);
    Task<SensorMeasurement> AddMeasurementAsync(int deviceId, SensorMeasurement measurement);
}
