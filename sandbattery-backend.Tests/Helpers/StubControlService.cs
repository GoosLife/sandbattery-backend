using sandbattery_backend.Models;
using sandbattery_backend.Services;

namespace sandbattery_backend.Tests.Helpers;

internal class StubControlService : IControlService
{
    public Task<SystemStatus> GetSystemStatusAsync(int deviceId) =>
        Task.FromResult(new SystemStatus());

    public Task<(bool Success, ControlCommandResponse? Result, bool TempExceeded)> ControlPumpAsync(
        int deviceId, PumpAction action, CommandSource source) =>
        Task.FromResult<(bool, ControlCommandResponse?, bool)>((true, null, false));

    public Task<(bool Success, ControlCommandResponse? Result, bool TempExceeded)> ControlHeaterAsync(
        int deviceId, int heaterIndex, HeaterAction action, CommandSource source) =>
        Task.FromResult<(bool, ControlCommandResponse?, bool)>((true, null, false));
}
