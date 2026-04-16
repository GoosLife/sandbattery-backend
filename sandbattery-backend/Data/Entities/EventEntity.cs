using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sandbattery_backend.Data.Entities;

[Table("event")]
public class EventEntity
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("product_key")]
    public string ProductKey { get; set; } = string.Empty;

    [Column("type")]
    public string Type { get; set; } = string.Empty;

    [Column("source")]
    public string Source { get; set; } = string.Empty;

    [Column("timestamp")]
    public DateTime Timestamp { get; set; }

    [Column("description")]
    public string Description { get; set; } = string.Empty;
}
