using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sandbattery_backend.Data.Entities;

[Table("electricity_price")]
public class ElectricityPriceEntity
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("date")]
    public string Date { get; set; } = string.Empty;

    [Column("area")]
    public string Area { get; set; } = string.Empty;

    [Column("currency")]
    public string Currency { get; set; } = "DKK";

    [Column("last_updated")]
    public DateTime LastUpdated { get; set; }

    public ICollection<PriceEntryEntity> Entries { get; set; } = new List<PriceEntryEntity>();
}
