using Microsoft.AspNetCore.Mvc;
using sandbattery_backend.Services;

namespace sandbattery_backend.Controllers;

[Route("api/v1/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService) => _authService = authService;

    /// <summary>
    /// Validates the product key supplied in X-Product-Key.
    /// This is the only endpoint that does not require a pre-validated key.
    /// </summary>
    [HttpPost("validate-key")]
    public async Task<IActionResult> ValidateKey()
    {
        var productKey = Request.Headers["X-Product-Key"].ToString();

        if (string.IsNullOrWhiteSpace(productKey))
            return BadRequest(new { error = "X-Product-Key header mangler" });

        var device = await _authService.ValidateProductKeyAsync(productKey);

        if (device is null)
            return Ok(new { valid = false, error = "Ugyldig produktnøgle" });

        return Ok(new
        {
            valid        = true,
            device_name  = device.DeviceName,
            product_key  = device.ProductKey
        });
    }
}
