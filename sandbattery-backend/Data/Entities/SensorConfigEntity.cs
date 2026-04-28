using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sandbattery_backend.Data.Entities;

[Table("sensor_config")]
public class SensorConfigEntity
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("device_id")]
    public int DeviceId { get; set; }

    /// <summary>"temperature" or "flow_rate"</summary>
    [Column("type")]
    public string Type { get; set; } = string.Empty;

    [Column("sensor_index")]
    public int SensorIndex { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;
}
