using System.Text.Json.Serialization;

namespace sandbattery_backend.Models;

public class TemperatureReading
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public float Value { get; set; }
}

public class FlowRateReading
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("value")]
    public float Value { get; set; }
}

public class SensorMeasurement
{
    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;

    [JsonPropertyName("product_key")]
    public string ProductKey { get; set; } = string.Empty;

    [JsonPropertyName("temperatures")]
    public List<TemperatureReading> Temperatures { get; set; } = [];

    [JsonPropertyName("flow_rates")]
    public List<FlowRateReading> FlowRates { get; set; } = [];

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
    public List<SensorMeasurement> Data { get; set; } = [];
}
