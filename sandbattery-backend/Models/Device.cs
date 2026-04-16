using System.Text.Json.Serialization;

namespace sandbattery_backend.Models;

public class Device
{
    [JsonPropertyName("product_key")]
    public string ProductKey { get; set; } = string.Empty;

    [JsonPropertyName("device_name")]
    public string DeviceName { get; set; } = string.Empty;
}
