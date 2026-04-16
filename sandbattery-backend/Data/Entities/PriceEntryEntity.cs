using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sandbattery_backend.Data.Entities;

[Table("price_entry")]
public class PriceEntryEntity
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("electricity_price_id")]
    public int ElectricityPriceId { get; set; }

    [Column("hour")]
    public DateTime Hour { get; set; }

    [Column("price_dkk_kwh")]
    public float PriceDkkKwh { get; set; }
}
