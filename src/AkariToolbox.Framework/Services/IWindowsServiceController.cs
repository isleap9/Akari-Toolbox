namespace AkariToolbox.Framework.Services;

/// <summary>
/// Named wrapper over a Windows service's <c>Start</c> registry DWORD, used by the
/// service-backed tweak handlers instead of calling <see cref="IRegistryService"/>
/// directly. Deliberately a thin registry pass-through today — the 32 OS tweaks in
/// this phase read/write the <c>Start</c> DWORD directly, not live running-status —
/// so a future phase (e.g. Gaming Tweaks' service configuration dropdowns) can extend
/// this interface with live <c>ServiceControllerStatus</c>/start-stop-restart operations
/// using <c>System.ServiceProcess.ServiceController</c> without touching any call site
/// that already depends on <see cref="IWindowsServiceController"/> by name.
/// </summary>
public interface IWindowsServiceController
{
    /// <summary>Reads the raw <c>Start</c> DWORD for <paramref name="serviceName"/>, or <c>null</c> if absent.</summary>
    int? GetStartType(string serviceName);

    /// <summary>Writes the raw <c>Start</c> DWORD for <paramref name="serviceName"/>.</summary>
    void SetStartType(string serviceName, int startValue);
}
