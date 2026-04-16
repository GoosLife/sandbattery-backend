using System.Text.Json.Serialization;

namespace sandbattery_backend.Models;

public class DeviceSettings
{
    [JsonPropertyName("max_sand_temp")]
    public float MaxSandTemp { get; set; } = 70.0f;

    [JsonPropertyName("min_pump_temp")]
    public float MinPumpTemp { get; set; } = 50.0f;

    [JsonPropertyName("pump_interval_seconds")]
    public int PumpIntervalSeconds { get; set; } = 300;

    [JsonPropertyName("price_limit_dkk")]
    public float PriceLimitDkk { get; set; } = 1.50f;

    [JsonPropertyName("auto_heating_enabled")]
    public bool AutoHeatingEnabled { get; set; } = true;

    [JsonPropertyName("auto_pump_enabled")]
    public bool AutoPumpEnabled { get; set; } = true;
}

public class SettingsUpdateRequest
{
    [JsonPropertyName("max_sand_temp")]
    public float? MaxSandTemp { get; set; }

    [JsonPropertyName("min_pump_temp")]
    public float? MinPumpTemp { get; set; }

    [JsonPropertyName("pump_interval_seconds")]
    public int? PumpIntervalSeconds { get; set; }

    [JsonPropertyName("price_limit_dkk")]
    public float? PriceLimitDkk { get; set; }

    [JsonPropertyName("auto_heating_enabled")]
    public bool? AutoHeatingEnabled { get; set; }

    [JsonPropertyName("auto_pump_enabled")]
    public bool? AutoPumpEnabled { get; set; }
}
