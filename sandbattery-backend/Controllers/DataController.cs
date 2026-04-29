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

    private int DeviceId => (int)HttpContext.Items["DeviceId"]!;

    [HttpGet("latest")]
    public async Task<IActionResult> GetLatest()
    {
        var measurement = await _dataService.GetLatestMeasurementAsync(DeviceId);

        if (measurement is null)
            return NotFound(new { error = "Ingen målinger fundet for denne enhed" });

        return Ok(measurement);
    }

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
            DeviceId, fromDate.ToUniversalTime(), toDate.ToUniversalTime(), parsedInterval, limit);

        return Ok(history);
    }

    [HttpPost]
    public async Task<IActionResult> PostData([FromBody] SensorMeasurement body)
    {
        if (string.IsNullOrEmpty(body.Timestamp))
            return BadRequest(new { error = "Feltet 'timestamp' er påkrævet" });

        var saved = await _dataService.AddMeasurementAsync(DeviceId, body);
        return StatusCode(StatusCodes.Status201Created, saved);
    }

    [HttpGet("energy/latest")]
    public async Task<IActionResult> GetLatestEnergy()
    {
        var reading = await _dataService.GetLatestEnergyAsync(DeviceId);

        if (reading is null)
            return NotFound(new { error = "Ingen energimålinger fundet for denne enhed" });

        return Ok(reading);
    }

    [HttpGet("energy/history")]
    public async Task<IActionResult> GetEnergyHistory(
        [FromQuery] string? from,
        [FromQuery] string? to,
        [FromQuery] int limit = 1000)
    {
        if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to))
            return BadRequest(new { error = "Parametrene 'from' og 'to' er påkrævede" });

        if (!DateTime.TryParse(from, out var fromDate) || !DateTime.TryParse(to, out var toDate))
            return BadRequest(new { error = "Ugyldigt datoformat – brug ISO 8601" });

        limit = Math.Clamp(limit, 1, 5000);

        var history = await _dataService.GetEnergyHistoryAsync(
            DeviceId, fromDate.ToUniversalTime(), toDate.ToUniversalTime(), limit);

        return Ok(history);
    }

    [HttpPost("energy")]
    public async Task<IActionResult> PostEnergy([FromBody] EnergyReading body)
    {
        if (string.IsNullOrEmpty(body.Timestamp))
            return BadRequest(new { error = "Feltet 'timestamp' er påkrævet" });

        var saved = await _dataService.AddEnergyReadingAsync(DeviceId, body);
        return StatusCode(StatusCodes.Status201Created, saved);
    }
}
