using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sandbattery_backend.Data.Entities;

[Table("temperature_sensor_reading")]
public class TemperatureSensorReadingEntity
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("measurement_id")]
    public int MeasurementId { get; set; }

    [Column("sensor_index")]
    public int SensorIndex { get; set; }

    [Column("label")]
    public string Label { get; set; } = string.Empty;

    [Column("value")]
    public float Value { get; set; }
}
