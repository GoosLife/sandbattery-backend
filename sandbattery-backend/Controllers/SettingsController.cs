using Microsoft.AspNetCore.Mvc;
using sandbattery_backend.Filters;
using sandbattery_backend.Models;
using sandbattery_backend.Services;

namespace sandbattery_backend.Controllers;

[Route("api/v1/settings")]
[ApiController]
[TypeFilter(typeof(ProductKeyAuthFilter))]
public class SettingsController : ControllerBase
{
    private readonly ISettingsService _settingsService;

    public SettingsController(ISettingsService settingsService) => _settingsService = settingsService;

    private int DeviceId => (int)HttpContext.Items["DeviceId"]!;

    [HttpGet]
    public async Task<IActionResult> GetSettings()
    {
        var settings = await _settingsService.GetSettingsAsync(DeviceId);
        return Ok(settings);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateSettings([FromBody] SettingsUpdateRequest request)
    {
        if (request.MaxSandTemp.HasValue && (request.MaxSandTemp <= 0 || request.MaxSandTemp > 70))
            return BadRequest(new { error = "max_sand_temp skal være > 0 og <= 70" });

        if (request.PumpIntervalSeconds.HasValue && request.PumpIntervalSeconds <= 0)
            return BadRequest(new { error = "pump_interval_seconds skal være et positivt heltal" });

        if (request.PriceLimitDkk.HasValue && request.PriceLimitDkk <= 0)
            return BadRequest(new { error = "price_limit_dkk skal være et positivt decimaltal" });

        if (request.MinPumpTemp.HasValue || request.MaxSandTemp.HasValue)
        {
            var current = await _settingsService.GetSettingsAsync(DeviceId);
            var finalMax = request.MaxSandTemp ?? current.MaxSandTemp;
            var finalMin = request.MinPumpTemp ?? current.MinPumpTemp;

            if (finalMin >= finalMax)
                return UnprocessableEntity(new { error = "min_pump_temp skal være lavere end max_sand_temp" });
        }

        var (_, updatedFields) = await _settingsService.UpdateSettingsAsync(DeviceId, request);
        return Ok(new { success = true, updated_fields = updatedFields });
    }

    [HttpGet("electricity-price")]
    public async Task<IActionResult> GetElectricityPrice(
        [FromQuery] string? date,
        [FromQuery] string? area)
    {
        date ??= DateTime.UtcNow.ToString("yyyy-MM-dd");
        area ??= "DK2";

        if (!DateTime.TryParseExact(date, "yyyy-MM-dd", null,
            System.Globalization.DateTimeStyles.None, out _))
            return BadRequest(new { error = "Ugyldig dato – brug formatet YYYY-MM-DD" });

        var validAreas = new[] { "DK1", "DK2" };
        if (!validAreas.Contains(area.ToUpper()))
            return BadRequest(new { error = "Ugyldig priszone. Gyldige værdier: DK1, DK2" });

        var price = await _settingsService.GetElectricityPriceAsync(date, area.ToUpper());

        if (price is null)
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = "Eksternt elpris-API utilgængeligt og ingen cache tilgængelig" });

        return Ok(price);
    }
}
