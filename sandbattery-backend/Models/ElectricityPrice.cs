using System.Text.Json.Serialization;

namespace sandbattery_backend.Models;

public class PriceEntry
{
    [JsonPropertyName("hour")]
    public string Hour { get; set; } = string.Empty;

    [JsonPropertyName("price_dkk_kwh")]
    public float PriceDkkKwh { get; set; }
}

public class ElectricityPrice
{
    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    [JsonPropertyName("area")]
    public string Area { get; set; } = string.Empty;

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "DKK";

    [JsonPropertyName("last_updated")]
    public string LastUpdated { get; set; } = string.Empty;

    [JsonPropertyName("prices")]
    public List<PriceEntry> Prices { get; set; } = new();
}
