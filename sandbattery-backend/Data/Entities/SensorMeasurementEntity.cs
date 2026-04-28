using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sandbattery_backend.Data.Entities;

[Table("sensor_measurement")]
public class SensorMeasurementEntity
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("device_id")]
    public int DeviceId { get; set; }

    [Column("timestamp")]
    public DateTime Timestamp { get; set; }

    [Column("power_w")]
    public float PowerW { get; set; }

    [Column("energy_kwh")]
    public float EnergyKwh { get; set; }

    [Column("status")]
    public string Status { get; set; } = "OK";

    public ICollection<TemperatureSensorReadingEntity> TemperatureReadings { get; set; } = [];
    public ICollection<FlowRateSensorReadingEntity> FlowRateReadings { get; set; } = [];
}
