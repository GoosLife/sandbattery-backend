using sandbattery_backend.Models;

namespace sandbattery_backend.Services;

public interface IControlService
{
    Task<SystemStatus> GetSystemStatusAsync(string productKey);
    Task<(bool Success, ControlCommandResponse? Result, bool TempExceeded)> ControlPumpAsync(string productKey, PumpAction action, CommandSource source);
    Task<(bool Success, ControlCommandResponse? Result, bool TempExceeded)> ControlHeaterAsync(string productKey, HeaterAction action, CommandSource source);
}
