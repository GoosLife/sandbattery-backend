using Microsoft.AspNetCore.Mvc;
using sandbattery_backend.Filters;
using sandbattery_backend.Models;
using sandbattery_backend.Services;

namespace sandbattery_backend.Controllers;

[Route("api/v1/events")]
[ApiController]
[TypeFilter(typeof(ProductKeyAuthFilter))]
public class EventsController : ControllerBase
{
    private readonly IEventsService _eventsService;

    public EventsController(IEventsService eventsService) => _eventsService = eventsService;

    private string ProductKey => (string)HttpContext.Items["ProductKey"]!;

    /// <summary>GET /api/v1/events — paginated list of events in reverse chronological order.</summary>
    [HttpGet]
    public async Task<IActionResult> GetEvents(
        [FromQuery] string? from,
        [FromQuery] string? to,
        [FromQuery] string? type,
        [FromQuery] string? source,
        [FromQuery] int limit = 100,
        [FromQuery] int offset = 0)
    {
        DateTime? fromDate = null;
        DateTime? toDate = null;

        if (from is not null && !DateTime.TryParse(from, out var parsedFrom))
            return BadRequest(new { error = "Ugyldig 'from' dato – brug ISO 8601" });
        else if (from is not null)
            fromDate = DateTime.Parse(from).ToUniversalTime();

        if (to is not null && !DateTime.TryParse(to, out var parsedTo))
            return BadRequest(new { error = "Ugyldig 'to' dato – brug ISO 8601" });
        else if (to is not null)
            toDate = DateTime.Parse(to).ToUniversalTime();

        // type may be a comma-separated list
        string[]? types = type?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        limit  = Math.Clamp(limit, 1, 1000);
        offset = Math.Max(0, offset);

        var result = await _eventsService.GetEventsAsync(
            ProductKey, fromDate, toDate, types, source, limit, offset);

        return Ok(result);
    }

    /// <summary>GET /api/v1/events/alerts — returns all unacknowledged alerts.</summary>
    [HttpGet("alerts")]
    public async Task<IActionResult> GetAlerts()
    {
        var alerts = await _eventsService.GetActiveAlertsAsync(ProductKey);
        return Ok(alerts);
    }

    /// <summary>POST /api/v1/events/alerts/{id}/acknowledge — marks an alert as acknowledged.</summary>
    [HttpPost("alerts/{id:int}/acknowledge")]
    public async Task<IActionResult> AcknowledgeAlert(int id)
    {
        var alert = await _eventsService.AcknowledgeAlertAsync(id, ProductKey);

        if (alert is null)
            return NotFound(new { error = $"Alarm med ID {id} findes ikke" });

        return Ok(new
        {
            success         = true,
            alert_id        = id,
            acknowledged_at = DateTime.UtcNow.ToString("o")
        });
    }

    /// <summary>POST /api/v1/events/heartbeat — receives a heartbeat signal from the Arduino.</summary>
    [HttpPost("heartbeat")]
    public async Task<IActionResult> PostHeartbeat([FromBody] Heartbeat body)
    {
        if (string.IsNullOrEmpty(body.Timestamp))
            return BadRequest(new { error = "Feltet 'timestamp' er påkrævet" });

        await _eventsService.AddHeartbeatAsync(body);

        return Ok(new
        {
            success     = true,
            received_at = DateTime.UtcNow.ToString("o")
        });
    }
}
