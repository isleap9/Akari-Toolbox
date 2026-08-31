using Microsoft.Win32;
using AkariToolbox.Framework.Services;

namespace AkariToolbox.App.Services.TweakHandlers;

/// <summary>
/// First real <see cref="ITweakHandler"/> — proves the vertical slice end-to-end.
/// Ported from the predecessor's <c>TweakService.SetWifi</c> (write values only;
/// the predecessor's private-state-hive idempotency guard is replaced by
/// <see cref="ITweakCatalog"/>'s live <see cref="GetState"/> comparison, per D-03/D-04).
/// </summary>
public sealed class WifiTweakHandler(IRegistryService registry) : ITweakHandler
{
    private const string WlanSvc = @"SYSTEM\CurrentControlSet\Services\WlanSvc";
    private const string VWifiFlt = @"SYSTEM\CurrentControlSet\Services\vwififlt";
    private const string NetProfm = @"SYSTEM\CurrentControlSet\Services\netprofm";
    private const string NlaSvc = @"SYSTEM\CurrentControlSet\Services\NlaSvc";

    // Real prior "enabled" Start values, captured the last time GetState() observed
    // WiFi as not-disabled. Used to restore the exact prior value on re-enable
    // instead of a hardcoded default (TWEAKS-03/D-04) — the predecessor's fixed
    // 2/1/3/2 enable-path constants are only used as a last-resort fallback if
    // GetState() was never called first (should not happen: ITweakCatalog always
    // reads live state before writing).
    private int _priorWlanSvcStart = 2;
    private int _priorVWifiFltStart = 1;
    private int _priorNetProfmStart = 3;
    private int _priorNlaSvcStart = 2;

    public string Key => "wifi";

    public string Title => "Disable WiFi";

    public string Description => "Toggle WiFi On or Off";

    public int Order => 0;

    public bool GetState()
    {
        var wlanStart = registry.GetValue(RegistryHive.LocalMachine, WlanSvc, "Start");
        if (wlanStart is int started && started == 4)
        {
            return true;
        }

        // Not disabled — capture the real current values so a later re-enable can
        // restore them exactly, rather than falling back to a hardcoded default.
        if (wlanStart is int wlan)
        {
            _priorWlanSvcStart = wlan;
        }

        if (registry.GetValue(RegistryHive.LocalMachine, VWifiFlt, "Start") is int vWifiFlt)
        {
            _priorVWifiFltStart = vWifiFlt;
        }

        if (registry.GetValue(RegistryHive.LocalMachine, NetProfm, "Start") is int netProfm)
        {
            _priorNetProfmStart = netProfm;
        }

        if (registry.GetValue(RegistryHive.LocalMachine, NlaSvc, "Start") is int nlaSvc)
        {
            _priorNlaSvcStart = nlaSvc;
        }

        return false;
    }

    public void SetState(bool disable)
    {
        registry.SetValue(RegistryHive.LocalMachine, WlanSvc, "Start", disable ? 4 : _priorWlanSvcStart, RegistryValueKind.DWord);
        registry.SetValue(RegistryHive.LocalMachine, VWifiFlt, "Start", disable ? 4 : _priorVWifiFltStart, RegistryValueKind.DWord);
        registry.SetValue(RegistryHive.LocalMachine, NetProfm, "Start", disable ? 4 : _priorNetProfmStart, RegistryValueKind.DWord);
        registry.SetValue(RegistryHive.LocalMachine, NlaSvc, "Start", disable ? 4 : _priorNlaSvcStart, RegistryValueKind.DWord);
    }
}
