using sandbattery_backend.Models;

namespace sandbattery_backend.Services;

public interface IControlService
{
    Task<SystemStatus> GetSystemStatusAsync(int deviceId);
    Task<(bool Success, ControlCommandResponse? Result, bool TempExceeded)> ControlPumpAsync(int deviceId, PumpAction action, CommandSource source);
    Task<(bool Success, ControlCommandResponse? Result, bool TempExceeded)> ControlHeaterAsync(int deviceId, int heaterIndex, HeaterAction action, CommandSource source);
}
