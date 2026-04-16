using System.Text.Json.Serialization;

namespace sandbattery_backend.Models;

public class SensorMeasurement
{
    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;

    [JsonPropertyName("product_key")]
    public string ProductKey { get; set; } = string.Empty;

    [JsonPropertyName("sand_temp")]
    public float SandTemp { get; set; }

    [JsonPropertyName("water_temp_in")]
    public float WaterTempIn { get; set; }

    [JsonPropertyName("water_temp_out")]
    public float WaterTempOut { get; set; }

    [JsonPropertyName("flow_rate")]
    public float FlowRate { get; set; }

    [JsonPropertyName("power_w")]
    public float PowerW { get; set; }

    [JsonPropertyName("energy_kwh")]
    public float EnergyKwh { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "OK";
}

public class DataHistory
{
    [JsonPropertyName("from")]
    public string From { get; set; } = string.Empty;

    [JsonPropertyName("to")]
    public string To { get; set; } = string.Empty;

    [JsonPropertyName("interval")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Interval { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("data")]
    public List<SensorMeasurement> Data { get; set; } = new();
}
