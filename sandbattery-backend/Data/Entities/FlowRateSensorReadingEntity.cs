using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sandbattery_backend.Data.Entities;

[Table("flow_rate_sensor_reading")]
public class FlowRateSensorReadingEntity
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("measurement_id")]
    public int MeasurementId { get; set; }

    [Column("sensor_index")]
    public int SensorIndex { get; set; }

    [Column("value")]
    public float Value { get; set; }
}
