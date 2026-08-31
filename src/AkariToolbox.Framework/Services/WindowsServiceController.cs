using Microsoft.Win32;

namespace AkariToolbox.Framework.Services;

/// <inheritdoc cref="IWindowsServiceController"/>
public sealed class WindowsServiceController(IRegistryService registry) : IWindowsServiceController
{
    private const string ServicesKeyFormat = @"SYSTEM\CurrentControlSet\Services\{0}";

    public int? GetStartType(string serviceName) =>
        registry.GetValue(RegistryHive.LocalMachine, string.Format(ServicesKeyFormat, serviceName), "Start") as int?;

    public void SetStartType(string serviceName, int startValue) =>
        registry.SetValue(
            RegistryHive.LocalMachine,
            string.Format(ServicesKeyFormat, serviceName),
            "Start",
            startValue,
            RegistryValueKind.DWord);
}
