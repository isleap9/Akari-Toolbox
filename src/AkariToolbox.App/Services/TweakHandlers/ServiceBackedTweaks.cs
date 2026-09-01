using Microsoft.Win32;
using AkariToolbox.Framework.Services;

namespace AkariToolbox.App.Services.TweakHandlers;

/// <summary>
/// 4 service-backed <see cref="ITweakHandler"/>s ported from the predecessor's
/// <c>TweakService.cs</c> (<c>SetClipboard</c>, <c>SetBluetooth</c>, <c>SetCdrom</c>,
/// <c>SetPrintSpooler</c>). Three of the four read/write via
/// <see cref="IWindowsServiceController"/> rather than <see cref="IRegistryService"/>
/// directly, per RESEARCH's Architectural Responsibility Map. <see cref="CdromTweakHandler"/>
/// is a documented exception — its enable path re-creates the entire <c>IMAPI2</c> service
/// registry key from scratch, which needs <see cref="IRegistryService"/> subkey-creation
/// beyond a simple <c>Start</c> DWord toggle.
/// </summary>
public sealed class ClipboardTweakHandler(IWindowsServiceController serviceController) : ITweakHandler
{
    private const string Cbdhsvc = "cbdhsvc";

    public string Key => "clipboard";
    public string Title => "Enable Clipboard";
    public string Description => "Toggle Clipboard service On or Off";
    public int Order => 4;

    public bool GetState() => serviceController.GetStartType(Cbdhsvc) == 2;

    public void SetState(bool enable) => serviceController.SetStartType(Cbdhsvc, enable ? 2 : 4);
}

public sealed class BluetoothTweakHandler(IWindowsServiceController serviceController) : ITweakHandler
{
    private static readonly string[] Services =
    [
        "BthA4dp", "BthEnum", "BthHFEnum", "BthLEEnum", "BTHMODEM",
        "Microsoft_Bluetooth_AvrcpTransport", "BluetoothUserService",
        "BthAvctpSvc", "RFCOMM", "bthserv", "BTAGService",
        "BTHUSB", "BTHPORT", "BthMini", "HidBth",
    ];

    public string Key => "bluetooth";
    public string Title => "Disable Bluetooth";
    public string Description => "Toggle Bluetooth On or Off";
    public int Order => 5;

    // Representative single-service read (RESEARCH Assumption A3) — matches the
    // pattern already established for wifi/vpn/vr in this phase.
    public bool GetState() => serviceController.GetStartType("bthserv") == 4;

    public void SetState(bool disable)
    {
        var startVal = disable ? 4 : 3;
        foreach (var svc in Services)
        {
            serviceController.SetStartType(svc, startVal);
        }
    }
}

public sealed class CdromTweakHandler(IRegistryService registry) : ITweakHandler
{
    private const string CdromKey = @"SYSTEM\CurrentControlSet\Services\cdrom";
    private const string Imapi2Key = @"SYSTEM\CurrentControlSet\Services\IMAPI2";
    private const string Imapi2ParametersKey = @"SYSTEM\CurrentControlSet\Services\IMAPI2\Parameters";
    private const string Imapi2FsKey = @"SYSTEM\CurrentControlSet\Services\IMAPI2FS";

    public string Key => "cdrom";
    public string Title => "CDROM";
    public string Description => "Enable the CDROM service";
    public int Order => 12;

    public bool GetState() =>
        registry.GetValue(RegistryHive.LocalMachine, CdromKey, "Start") is int v && v == 3;

    public void SetState(bool enable)
    {
        if (enable)
        {
            registry.SetValue(RegistryHive.LocalMachine, CdromKey, "Start", 3, RegistryValueKind.DWord);

            // Re-create the IMAPI2 service key from scratch — this is the documented
            // exception to the IWindowsServiceController boundary (needs subkey creation
            // beyond a simple Start DWord toggle).
            registry.SetValue(RegistryHive.LocalMachine, Imapi2Key, "Description", "@%SystemRoot%\\system32\\imapi2.dll,-2", RegistryValueKind.ExpandString);
            registry.SetValue(RegistryHive.LocalMachine, Imapi2Key, "DisplayName", "@%SystemRoot%\\system32\\imapi2.dll,-1", RegistryValueKind.ExpandString);
            registry.SetValue(RegistryHive.LocalMachine, Imapi2Key, "ErrorControl", 1, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.LocalMachine, Imapi2Key, "ImagePath", "%SystemRoot%\\system32\\svchost.exe -k imapi", RegistryValueKind.ExpandString);
            registry.SetValue(RegistryHive.LocalMachine, Imapi2Key, "ObjectName", "LocalSystem", RegistryValueKind.String);
            registry.SetValue(RegistryHive.LocalMachine, Imapi2Key, "Start", 3, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.LocalMachine, Imapi2Key, "Type", 32, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.LocalMachine, Imapi2ParametersKey, "ServiceDll", "%SystemRoot%\\system32\\imapi2.dll", RegistryValueKind.ExpandString);
            registry.SetValue(RegistryHive.LocalMachine, Imapi2ParametersKey, "ServiceDllUnloadOnStop", 1, RegistryValueKind.DWord);
        }
        else
        {
            registry.SetValue(RegistryHive.LocalMachine, CdromKey, "Start", 4, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.LocalMachine, Imapi2Key, "Start", 4, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.LocalMachine, Imapi2FsKey, "Start", 4, RegistryValueKind.DWord);
        }
    }
}

public sealed class PrintSpoolerTweakHandler(IWindowsServiceController serviceController) : ITweakHandler
{
    private const string Spooler = "Spooler";

    public string Key => "spooler";
    public string Title => "Disable Print Spooler";
    public string Description => "Toggle Print Spooler On or Off";
    public int Order => 13;

    public bool GetState() => serviceController.GetStartType(Spooler) == 4;

    public void SetState(bool disable) => serviceController.SetStartType(Spooler, disable ? 4 : 2);
}
