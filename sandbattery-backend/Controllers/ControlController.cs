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

    private int DeviceId => (int)HttpContext.Items["DeviceId"]!;

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var status = await _controlService.GetSystemStatusAsync(DeviceId);
        return Ok(status);
    }

    [HttpPost("pump")]
    public async Task<IActionResult> ControlPump([FromBody] PumpCommandRequest body)
    {
        var (success, result, _) = await _controlService.ControlPumpAsync(DeviceId, body.Action, body.Source);

        if (!success)
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Kunne ikke videresende kommando til IoT-enhed" });

        return Ok(result);
    }

    [HttpPost("heater")]
    public async Task<IActionResult> ControlHeater([FromBody] HeaterCommandRequest body)
    {
        var (success, result, tempExceeded) =
            await _controlService.ControlHeaterAsync(DeviceId, body.Index, body.Action, body.Source);

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
