using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sandbattery_backend.Data.Entities;

[Table("alert")]
public class AlertEntity
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("product_key")]
    public string ProductKey { get; set; } = string.Empty;

    [Column("severity")]
    public string Severity { get; set; } = string.Empty;

    [Column("type")]
    public string Type { get; set; } = string.Empty;

    [Column("message")]
    public string Message { get; set; } = string.Empty;

    [Column("timestamp")]
    public DateTime Timestamp { get; set; }

    [Column("acknowledged")]
    public bool Acknowledged { get; set; }
}
