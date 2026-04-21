using Microsoft.AspNetCore.Mvc;
using sandbattery_backend.Filters;
using sandbattery_backend.Models;
using sandbattery_backend.Services;

namespace sandbattery_backend.Controllers;

[Route("api/v1/control")]
[ApiController]
[TypeFilter(typeof(ProductKeyAuthFilter))]
public class ControlController : ControllerBase
{
    private readonly IControlService _controlService;

    public ControlController(IControlService controlService) => _controlService = controlService;

    private string ProductKey => (string)HttpContext.Items["ProductKey"]!;

    /// <summary>GET /api/v1/control/status — returns current heater and pump state.</summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var status = await _controlService.GetSystemStatusAsync(ProductKey);
        return Ok(status);
    }

    /// <summary>POST /api/v1/control/pump — starts or stops the pump.</summary>
    [HttpPost("pump")]
    public async Task<IActionResult> ControlPump([FromBody] PumpCommandRequest body)
    {
        var (success, result, _) = await _controlService.ControlPumpAsync(ProductKey, body.Action, body.Source);

        if (!success)
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Kunne ikke videresende kommando til IoT-enhed" });

        return Ok(result);
    }

    /// <summary>POST /api/v1/control/heater — turns the heater on or off.</summary>
    [HttpPost("heater")]
    public async Task<IActionResult> ControlHeater([FromBody] HeaterCommandRequest body)
    {
        var (success, result, tempExceeded) =
            await _controlService.ControlHeaterAsync(ProductKey, body.Action, body.Source);

        if (tempExceeded)
            return UnprocessableEntity(new
            {
                error   = "TEMP_LIMIT_EXCEEDED",
                message = "Sandtemperaturen overskrider maksimumsgrænsen – varmelegeme kan ikke aktiveres"
            });

        if (!success)
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Kunne ikke videresende kommando til IoT-enhed" });

        return Ok(result);
    }
}
