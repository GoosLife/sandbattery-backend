using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using sandbattery_backend.Services;

namespace sandbattery_backend.Filters;

public class ProductKeyAuthFilter : IAsyncActionFilter
{
    private readonly IAuthService _authService;

    public ProductKeyAuthFilter(IAuthService authService) => _authService = authService;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var productKey = context.HttpContext.Request.Headers["X-Product-Key"].ToString();

        if (string.IsNullOrWhiteSpace(productKey))
        {
            context.Result = new ObjectResult(new { error = "X-Product-Key header mangler" })
            {
                StatusCode = StatusCodes.Status401Unauthorized
            };
            return;
        }

        var device = await _authService.ValidateProductKeyAsync(productKey);
        if (device is null)
        {
            context.Result = new ObjectResult(new { error = "Ugyldig produktnøgle" })
            {
                StatusCode = StatusCodes.Status401Unauthorized
            };
            return;
        }

        context.HttpContext.Items["DeviceId"] = device.Id;
        context.HttpContext.Items["Device"] = device;

        await next();
    }
}
