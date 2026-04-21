using Microsoft.AspNetCore.Mvc;
using sandbattery_backend.Filters;
using sandbattery_backend.Models;
using sandbattery_backend.Services;
using static sandbattery_backend.Models.MeasurementIntervalExtensions;

namespace sandbattery_backend.Controllers;

[Route("api/v1/data")]
[ApiController]
[TypeFilter(typeof(ProductKeyAuthFilter))]
public class DataController : ControllerBase
{
    private readonly IDataService _dataService;

    public DataController(IDataService dataService) => _dataService = dataService;

    private string ProductKey => (string)HttpContext.Items["ProductKey"]!;

    /// <summary>GET /api/v1/data/latest — returns the most recent sensor measurement.</summary>
    [HttpGet("latest")]
    public async Task<IActionResult> GetLatest()
    {
        var measurement = await _dataService.GetLatestMeasurementAsync(ProductKey);

        if (measurement is null)
            return NotFound(new { error = "Ingen målinger fundet for denne enhed" });

        return Ok(measurement);
    }

    /// <summary>GET /api/v1/data/history — returns measurements within a time range.</summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(
        [FromQuery] string? from,
        [FromQuery] string? to,
        [FromQuery] string? interval,
        [FromQuery] int limit = 1000)
    {
        if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to))
            return BadRequest(new { error = "Parametrene 'from' og 'to' er påkrævede" });

        if (!DateTime.TryParse(from, out var fromDate) || !DateTime.TryParse(to, out var toDate))
            return BadRequest(new { error = "Ugyldigt datoformat – brug ISO 8601" });

        MeasurementInterval? parsedInterval = null;
        if (interval is not null)
        {
            parsedInterval = TryParse(interval);
            if (parsedInterval is null)
                return BadRequest(new { error = "Ugyldigt interval. Gyldige værdier: 1m, 5m, 15m, 30m, 1h, 6h, 1d" });
        }

        limit = Math.Clamp(limit, 1, 5000);

        var history = await _dataService.GetMeasurementHistoryAsync(
            ProductKey, fromDate.ToUniversalTime(), toDate.ToUniversalTime(), parsedInterval, limit);

        return Ok(history);
    }

    /// <summary>POST /api/v1/data — receives a sensor measurement from the Arduino.</summary>
    [HttpPost]
    public async Task<IActionResult> PostData([FromBody] SensorMeasurement body)
    {
        if (string.IsNullOrEmpty(body.Timestamp))
            return BadRequest(new { error = "Feltet 'timestamp' er påkrævet" });

        // Reject obviously invalid sensor readings (e.g. disconnected DS18B20 at -127 °C)
        if (body.SandTemp <= -126 || body.WaterTempIn <= -126 || body.WaterTempOut <= -126)
            return UnprocessableEntity(new { error = "Sensorværdier uden for acceptabelt interval" });

        var saved = await _dataService.AddMeasurementAsync(ProductKey, body);
        return StatusCode(StatusCodes.Status201Created, saved);
    }
}
