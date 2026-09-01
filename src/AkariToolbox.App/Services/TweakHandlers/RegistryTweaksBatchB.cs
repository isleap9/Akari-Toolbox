using System.Runtime.InteropServices;
using Microsoft.Win32;
using AkariToolbox.Framework.Services;

namespace AkariToolbox.App.Services.TweakHandlers;

/// <summary>
/// Second 11 of the 22 remaining pure-registry <see cref="ITweakHandler"/>s ported from
/// the predecessor's <c>TweakService.cs</c>. Same live-read/live-write pattern as
/// batch A — no legacy per-tweak state-flag tracking, no private state hive (D-03/D-04).
/// </summary>
public sealed class VbsTweakHandler(IRegistryService registry) : ITweakHandler
{
    private const string DeviceGuard = @"SYSTEM\CurrentControlSet\Control\DeviceGuard";
    private const string HvciScenario = @"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity";

    public string Key => "vbs";
    public string Title => "Enable VBS";
    public string Description => "Toggle Virtualization Based Security";
    public int Order => 20;

    public bool GetState() =>
        registry.GetValue(RegistryHive.LocalMachine, DeviceGuard, "EnableVirtualizationBasedSecurity") is int v && v == 1;

    public void SetState(bool enabled)
    {
        registry.SetValue(RegistryHive.LocalMachine, DeviceGuard, "EnableVirtualizationBasedSecurity", enabled ? 1 : 0, RegistryValueKind.DWord);
        registry.SetValue(RegistryHive.LocalMachine, DeviceGuard, "RequirePlatformSecurityFeatures", enabled ? 1 : 0, RegistryValueKind.DWord);
        registry.SetValue(RegistryHive.LocalMachine, HvciScenario, "Enabled", enabled ? 1 : 0, RegistryValueKind.DWord);
    }
}

public sealed class WallpaperQualityTweakHandler(IRegistryService registry) : ITweakHandler
{
    // Plain RegistryHive.CurrentUser (NOT OpenRealUserHive) per RESEARCH's explicit
    // distinction from startmenu/transparency.
    private const string Desktop = @"Control Panel\Desktop";

    public string Key => "wallpaperq";
    public string Title => "Disable Wallpaper Quality Reduction";
    public string Description => "Prevent wallpaper quality reduction";
    public int Order => 21;

    public bool GetState() =>
        registry.GetValue(RegistryHive.CurrentUser, Desktop, "JPEGImportQuality") is int v && v == 100;

    public void SetState(bool enabled)
    {
        if (enabled)
        {
            registry.SetValue(RegistryHive.CurrentUser, Desktop, "JPEGImportQuality", 100, RegistryValueKind.DWord);
        }
        else
        {
            registry.DeleteValue(RegistryHive.CurrentUser, Desktop, "JPEGImportQuality");
        }
    }
}

public sealed class MpoTweakHandler(IRegistryService registry) : ITweakHandler
{
    // IRegistryService always opens via RegistryView.Registry64 internally, satisfying
    // the "explicitly Registry64" requirement without a separate view parameter.
    private const string GraphicsDrivers = @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers";

    public string Key => "mpo";
    public string Title => "Disable Multi-Plane-Overlay";
    public string Description => "Toggle MPO On or Off";
    public int Order => 22;

    public bool GetState() =>
        registry.GetValue(RegistryHive.LocalMachine, GraphicsDrivers, "DisableOverlays") is int v && v == 1;

    public void SetState(bool enabled)
    {
        if (enabled)
        {
            registry.SetValue(RegistryHive.LocalMachine, GraphicsDrivers, "DisableOverlays", 1, RegistryValueKind.DWord);
        }
        else
        {
            registry.DeleteValue(RegistryHive.LocalMachine, GraphicsDrivers, "DisableOverlays");
        }
    }
}

public sealed class TransparencyTweakHandler(IRegistryService registry) : ITweakHandler
{
    private const string Personalize = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    public string Key => "transparency";
    public string Title => "Transparency Effects";
    public string Description => "Toggle transparency effects";
    public int Order => 23;

    // OpenRealUserHive throws (D-14) if explorer.exe is not found — let it propagate,
    // same as StartMenuTweakHandler.
    public bool GetState()
    {
        using var key = registry.OpenRealUserHive(Personalize);
        return key.GetValue("EnableTransparency") is int v && v == 1;
    }

    public void SetState(bool enabled)
    {
        using var key = registry.OpenRealUserHive(Personalize);
        key.SetValue("EnableTransparency", enabled ? 1 : 0, RegistryValueKind.DWord);
    }
}

public sealed class LockScreenTweakHandler(IRegistryService registry) : ITweakHandler
{
    private const string Personalization = @"SOFTWARE\Policies\Microsoft\Windows\Personalization";

    public string Key => "lockscreen";
    public string Title => "Disable Lock Screen";
    public string Description => "Toggle lock screen On or Off";
    public int Order => 24;

    public bool GetState() =>
        registry.GetValue(RegistryHive.LocalMachine, Personalization, "NoLockScreen") is int v && v == 1;

    public void SetState(bool enabled) =>
        registry.SetValue(RegistryHive.LocalMachine, Personalization, "NoLockScreen", enabled ? 1 : 0, RegistryValueKind.DWord);
}

public sealed class AnimationsTweakHandler(IRegistryService registry) : ITweakHandler
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, IntPtr pvParam, uint fWinIni);

    private const string DwmPolicy = @"SOFTWARE\Policies\Microsoft\Windows\DWM";
    private const string Dwm = @"SOFTWARE\Microsoft\Windows\DWM";
    private const string WindowMetrics = @"Control Panel\Desktop\WindowMetrics";
    private const string ExplorerAdvanced = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
    private const string ExplorerVisualEffects = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects";
    private const string Desktop = @"Control Panel\Desktop";

    private static readonly byte[] DisabledUserPreferencesMask = { 0x90, 0x12, 0x03, 0x80, 0x10, 0x00, 0x00, 0x00 };
    private static readonly byte[] EnabledUserPreferencesMask = { 0x9E, 0x3E, 0x07, 0x80, 0x12, 0x00, 0x00, 0x00 };

    public string Key => "animations";
    public string Title => "Disable Animations";
    public string Description => "Toggle system animations";
    public int Order => 25;

    public bool GetState() =>
        registry.GetValue(RegistryHive.LocalMachine, DwmPolicy, "DisallowAnimations") is int v && v == 1;

    public void SetState(bool enabled)
    {
        if (enabled)
        {
            registry.SetValue(RegistryHive.LocalMachine, DwmPolicy, "DisallowAnimations", 1, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.CurrentUser, Dwm, "EnableAeroPeek", 0, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.CurrentUser, Dwm, "AlwaysHibernateThumbnails", 0, RegistryValueKind.DWord);
            // Written ONLY on the disable path — the enable path does not touch this value.
            registry.SetValue(RegistryHive.CurrentUser, WindowMetrics, "MinAnimate", 0, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.CurrentUser, ExplorerAdvanced, "TaskbarAnimations", 0, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.CurrentUser, ExplorerAdvanced, "IconsOnly", 1, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.CurrentUser, ExplorerAdvanced, "ListviewAlphaSelect", 0, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.CurrentUser, ExplorerAdvanced, "ListviewShadow", 0, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.CurrentUser, ExplorerVisualEffects, "VisualFXSetting", 3, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.CurrentUser, Desktop, "UserPreferencesMask", DisabledUserPreferencesMask, RegistryValueKind.Binary);
        }
        else
        {
            registry.DeleteValue(RegistryHive.LocalMachine, DwmPolicy, "DisallowAnimations");
            registry.SetValue(RegistryHive.CurrentUser, Dwm, "EnableAeroPeek", 1, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.CurrentUser, Dwm, "AlwaysHibernateThumbnails", 1, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.CurrentUser, ExplorerAdvanced, "TaskbarAnimations", 1, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.CurrentUser, ExplorerAdvanced, "IconsOnly", 0, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.CurrentUser, ExplorerAdvanced, "ListviewAlphaSelect", 1, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.CurrentUser, ExplorerAdvanced, "ListviewShadow", 1, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.CurrentUser, ExplorerVisualEffects, "VisualFXSetting", 1, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.CurrentUser, Desktop, "UserPreferencesMask", EnabledUserPreferencesMask, RegistryValueKind.Binary);
        }

        // Best-effort UI-refresh broadcast — ignore the return value, matches
        // TweakService.cs:697,713.
        SystemParametersInfo(0x0014u, 0u, IntPtr.Zero, 0x0003u);
    }
}

public sealed class DcomTweakHandler(IRegistryService registry) : ITweakHandler
{
    private const string Ole = @"SOFTWARE\Microsoft\Ole";

    public string Key => "dcom";
    public string Title => "Disable DCOM";
    public string Description => "Toggle DCOM On or Off";
    public int Order => 26;

    public bool GetState() =>
        registry.GetValue(RegistryHive.LocalMachine, Ole, "EnableDCOM") is string v && v == "N";

    public void SetState(bool enabled) =>
        registry.SetValue(RegistryHive.LocalMachine, Ole, "EnableDCOM", enabled ? "N" : "Y", RegistryValueKind.String);
}

public sealed class NvmeTweaksTweakHandler(IRegistryService registry) : ITweakHandler
{
    private const string StorNvmeDevice = @"SYSTEM\ControlSet001\Services\stornvme\Parameters\Device";

    public string Key => "nvme";
    public string Title => "NVME Tweaks";
    public string Description => "Apply NVME performance tweaks";
    public int Order => 27;

    public bool GetState() =>
        registry.GetValue(RegistryHive.LocalMachine, StorNvmeDevice, "ContiguousMemoryFromAnyNode") is int v && v == 1;

    public void SetState(bool enabled)
    {
        if (enabled)
        {
            registry.SetValue(RegistryHive.LocalMachine, StorNvmeDevice, "ContiguousMemoryFromAnyNode", 1, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.LocalMachine, StorNvmeDevice, "LogSize", 0, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.LocalMachine, StorNvmeDevice, "IdlePowerMode", 0, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.LocalMachine, StorNvmeDevice, "DiagnosticFlags", 0, RegistryValueKind.DWord);
        }
        else
        {
            registry.DeleteValue(RegistryHive.LocalMachine, StorNvmeDevice, "ContiguousMemoryFromAnyNode");
            registry.DeleteValue(RegistryHive.LocalMachine, StorNvmeDevice, "LogSize");
            registry.DeleteValue(RegistryHive.LocalMachine, StorNvmeDevice, "IdlePowerMode");
            registry.DeleteValue(RegistryHive.LocalMachine, StorNvmeDevice, "DiagnosticFlags");
        }
    }
}

public sealed class LargeSystemCacheTweakHandler(IRegistryService registry) : ITweakHandler
{
    private const string MemoryManagement = @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management";

    public string Key => "largecache";
    public string Title => "LargeSystemCache";
    public string Description => "Configure large system cache";
    public int Order => 28;

    public bool GetState() =>
        registry.GetValue(RegistryHive.LocalMachine, MemoryManagement, "LargeSystemCache") is int v && v == 1;

    public void SetState(bool enabled) =>
        registry.SetValue(RegistryHive.LocalMachine, MemoryManagement, "LargeSystemCache", enabled ? 1 : 0, RegistryValueKind.DWord);
}

public sealed class SystemProfileTweakHandler(IRegistryService registry) : ITweakHandler
{
    private const string GamesTask = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games";
    private const string ProAudioTask = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Pro Audio";
    private const string AudioTask = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Audio";

    public string Key => "sysprofile";
    public string Title => "System Profile Tweaks";
    public string Description => "Apply various system profile tweaks";
    public int Order => 29;

    public bool GetState() =>
        registry.GetValue(RegistryHive.LocalMachine, GamesTask, "Priority") is int v && v == 8;

    public void SetState(bool enabled)
    {
        if (enabled)
        {
            registry.SetValue(RegistryHive.LocalMachine, GamesTask, "Affinity", 0, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.LocalMachine, GamesTask, "Background Only", "False", RegistryValueKind.String);
            registry.SetValue(RegistryHive.LocalMachine, GamesTask, "Clock Rate", 2710, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.LocalMachine, GamesTask, "GPU Priority", 8, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.LocalMachine, GamesTask, "Priority", 8, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.LocalMachine, GamesTask, "SFIO Priority", "High", RegistryValueKind.String);
            registry.SetValue(RegistryHive.LocalMachine, GamesTask, "Scheduling Category", "High", RegistryValueKind.String);
            registry.SetValue(RegistryHive.LocalMachine, ProAudioTask, "Priority", 8, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.LocalMachine, ProAudioTask, "Scheduling Category", "Medium", RegistryValueKind.String);
            registry.SetValue(RegistryHive.LocalMachine, AudioTask, "Priority", 8, RegistryValueKind.DWord);
        }
        else
        {
            registry.DeleteValue(RegistryHive.LocalMachine, GamesTask, "Affinity");
            registry.DeleteValue(RegistryHive.LocalMachine, GamesTask, "Background Only");
            registry.DeleteValue(RegistryHive.LocalMachine, GamesTask, "Clock Rate");
            registry.DeleteValue(RegistryHive.LocalMachine, GamesTask, "GPU Priority");
            registry.DeleteValue(RegistryHive.LocalMachine, GamesTask, "Priority");
            registry.DeleteValue(RegistryHive.LocalMachine, GamesTask, "SFIO Priority");
            registry.DeleteValue(RegistryHive.LocalMachine, GamesTask, "Scheduling Category");
            registry.DeleteValue(RegistryHive.LocalMachine, ProAudioTask, "Priority");
            registry.DeleteValue(RegistryHive.LocalMachine, ProAudioTask, "Scheduling Category");
            registry.DeleteValue(RegistryHive.LocalMachine, AudioTask, "Priority");
        }
    }
}

public sealed class ProcessMitigationsTweakHandler(IRegistryService registry) : ITweakHandler
{
    private const string MemoryManagement = @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management";

    public string Key => "mitigation";
    public string Title => "Enable Process Mitigation";
    public string Description => "Enable process mitigation policies";
    public int Order => 31;

    public bool GetState() =>
        registry.GetValue(RegistryHive.LocalMachine, MemoryManagement, "FeatureSettingsOverride") is int v && v == 0;

    public void SetState(bool enabled)
    {
        registry.SetValue(RegistryHive.LocalMachine, MemoryManagement, "FeatureSettingsOverride", enabled ? 0 : 3, RegistryValueKind.DWord);
        registry.SetValue(RegistryHive.LocalMachine, MemoryManagement, "FeatureSettingsOverrideMask", 3, RegistryValueKind.DWord);
    }
}
