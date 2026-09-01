using Microsoft.Win32;
using AkariToolbox.Framework.Services;

namespace AkariToolbox.App.Services.TweakHandlers;

/// <summary>
/// First 11 of the 22 remaining pure-registry <see cref="ITweakHandler"/>s ported from
/// the predecessor's <c>TweakService.cs</c>. Every handler here follows the same
/// live-read/live-write pattern proven by <see cref="WifiTweakHandler"/> — no
/// legacy per-tweak state-flag tracking, no private state hive (D-03/D-04).
/// </summary>
public sealed class TsxTweakHandler(IRegistryService registry) : ITweakHandler
{
    private const string Key_ = @"SYSTEM\ControlSet001\Control\Session Manager\kernel";

    public string Key => "tsx";
    public string Title => "Enable Intel TSX";
    public string Description => "Enable Intel Transactional Synchronization Extensions";
    public int Order => 1;

    public bool GetState() =>
        registry.GetValue(RegistryHive.LocalMachine, Key_, "DisableTsx") is int v && v == 0;

    public void SetState(bool enabled) =>
        registry.SetValue(RegistryHive.LocalMachine, Key_, "DisableTsx", enabled ? 0 : 1, RegistryValueKind.DWord);
}

public sealed class ActionCenterTweakHandler(IRegistryService registry) : ITweakHandler
{
    private const string Key_ = @"Software\Policies\Microsoft\Windows\Explorer";

    public string Key => "actioncenter";
    public string Title => "Disable Action Center";
    public string Description => "Toggle Action Center On or Off";
    public int Order => 2;

    public bool GetState() =>
        registry.GetValue(RegistryHive.CurrentUser, Key_, "DisableNotificationCenter") is int v && v == 1;

    public void SetState(bool enabled) =>
        registry.SetValue(RegistryHive.CurrentUser, Key_, "DisableNotificationCenter", enabled ? 1 : 0, RegistryValueKind.DWord);
}

public sealed class VpnTweakHandler(IRegistryService registry) : ITweakHandler
{
    private const string IkeExt = @"SYSTEM\CurrentControlSet\Services\IKEEXT";
    private const string Bfe = @"SYSTEM\CurrentControlSet\Services\BFE";
    private const string WinHttpAutoProxySvc = @"SYSTEM\CurrentControlSet\Services\WinHttpAutoProxySvc";
    private const string RasMan = @"SYSTEM\CurrentControlSet\Services\RasMan";
    private const string SstpSvc = @"SYSTEM\CurrentControlSet\Services\SstpSvc";
    private const string Iphlpsvc = @"SYSTEM\CurrentControlSet\Services\iphlpsvc";
    private const string NdisVirtualBus = @"SYSTEM\CurrentControlSet\Services\NdisVirtualBus";
    private const string Eaphost = @"SYSTEM\CurrentControlSet\Services\Eaphost";

    public string Key => "vpn";
    public string Title => "Disable VPN";
    public string Description => "Toggle VPN On or Off";
    public int Order => 7;

    public bool GetState() =>
        registry.GetValue(RegistryHive.LocalMachine, IkeExt, "Start") is int v && v == 4;

    public void SetState(bool enabled)
    {
        if (enabled)
        {
            // Disable path: Start=4 on all 7 services. BFE is NOT touched here — the
            // predecessor's asymmetry (BFE is only ever written on the enable path).
            registry.SetValue(RegistryHive.LocalMachine, IkeExt, "Start", 4, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.LocalMachine, WinHttpAutoProxySvc, "Start", 4, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.LocalMachine, RasMan, "Start", 4, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.LocalMachine, SstpSvc, "Start", 4, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.LocalMachine, Iphlpsvc, "Start", 4, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.LocalMachine, NdisVirtualBus, "Start", 4, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.LocalMachine, Eaphost, "Start", 4, RegistryValueKind.DWord);
        }
        else
        {
            registry.SetValue(RegistryHive.LocalMachine, IkeExt, "Start", 3, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.LocalMachine, Bfe, "Start", 2, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.LocalMachine, WinHttpAutoProxySvc, "Start", 3, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.LocalMachine, RasMan, "Start", 3, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.LocalMachine, SstpSvc, "Start", 3, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.LocalMachine, Iphlpsvc, "Start", 3, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.LocalMachine, NdisVirtualBus, "Start", 3, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.LocalMachine, Eaphost, "Start", 3, RegistryValueKind.DWord);
        }
    }
}

public sealed class NtfsEncryptionTweakHandler(IRegistryService registry, IScriptRunner scriptRunner) : ITweakHandler
{
    private const string PoliciesKey = @"SYSTEM\CurrentControlSet\Policies";

    public string Key => "ntfsenc";
    public string Title => "Disable NTFS Encryption";
    public string Description => "Toggle NTFS Encryption On or Off";
    public int Order => 8;

    public bool GetState() =>
        registry.GetValue(RegistryHive.LocalMachine, PoliciesKey, "NtfsDisableEncryption") is int v && v == 1;

    public void SetState(bool enabled)
    {
        if (enabled)
        {
            scriptRunner.RunProcessAsync("fsutil", "behavior set disableencryption 1").GetAwaiter().GetResult();
            registry.SetValue(RegistryHive.LocalMachine, PoliciesKey, "NtfsDisableEncryption", 1, RegistryValueKind.DWord);
        }
        else
        {
            scriptRunner.RunProcessAsync("fsutil", "behavior set disableencryption 0").GetAwaiter().GetResult();
            registry.DeleteValue(RegistryHive.LocalMachine, PoliciesKey, "NtfsDisableEncryption");
        }
    }
}

public sealed class FsoGamebarTweakHandler(IRegistryService registry) : ITweakHandler
{
    private const string GameBar = @"Software\Microsoft\GameBar";
    private const string GameConfigStore = @"System\GameConfigStore";
    private const string GameDvrPolicy = @"SOFTWARE\Policies\Microsoft\Windows\GameDVR";
    private const string GameDvrCurrentVersion = @"SOFTWARE\Microsoft\Windows\CurrentVersion\GameDVR";
    private const string BcastDvrUserService = @"SYSTEM\CurrentControlSet\Services\BcastDVRUserService";
    private const string SessionManagerEnvironment = @"SYSTEM\CurrentControlSet\Control\Session Manager\Environment";

    public string Key => "fso";
    public string Title => "Disable FSO and Gamebar";
    public string Description => "Toggle FSO and Gamebar On or Off";
    public int Order => 9;

    // Representative single-key read (RESEARCH Assumption A3) — matches WifiTweakHandler's
    // precedent for multi-key tweaks.
    public bool GetState() =>
        registry.GetValue(RegistryHive.LocalMachine, BcastDvrUserService, "Start") is int v && v == 4;

    public void SetState(bool enabled)
    {
        if (enabled)
        {
            registry.SetValue(RegistryHive.CurrentUser, GameBar, "ShowStartupPanel", 0, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.CurrentUser, GameBar, "GamePanelStartupTipIndex", 3, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.CurrentUser, GameBar, "AllowAutoGameMode", 0, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.CurrentUser, GameBar, "AutoGameModeEnabled", 0, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.CurrentUser, GameBar, "UseNexusForGameBarEnabled", 0, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.CurrentUser, GameConfigStore, "GameDVR_Enabled", 0, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.CurrentUser, GameConfigStore, "GameDVR_FSEBehaviorMode", 2, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.CurrentUser, GameConfigStore, "GameDVR_FSEBehavior", 2, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.CurrentUser, GameConfigStore, "GameDVR_HonorUserFSEBehaviorMode", 1, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.CurrentUser, GameConfigStore, "GameDVR_DXGIHonorFSEWindowsCompatible", 1, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.CurrentUser, GameConfigStore, "GameDVR_EFSEFeatureFlags", 0, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.CurrentUser, GameConfigStore, "GameDVR_DSEBehavior", 2, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.LocalMachine, GameDvrPolicy, "AllowGameDVR", 0, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.CurrentUser, GameDvrCurrentVersion, "AppCaptureEnabled", 0, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.LocalMachine, BcastDvrUserService, "Start", 4, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.LocalMachine, SessionManagerEnvironment, "__COMPAT_LAYER", "~ DISABLEDXMAXIMIZEDWINDOWEDMODE", RegistryValueKind.String);
        }
        else
        {
            registry.SetValue(RegistryHive.CurrentUser, GameConfigStore, "GameDVR_Enabled", 0, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.CurrentUser, GameConfigStore, "GameDVR_FSEBehaviorMode", 0, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.CurrentUser, GameConfigStore, "GameDVR_FSEBehavior", 0, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.CurrentUser, GameConfigStore, "GameDVR_HonorUserFSEBehaviorMode", 1, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.CurrentUser, GameConfigStore, "GameDVR_DXGIHonorFSEWindowsCompatible", 0, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.CurrentUser, GameConfigStore, "GameDVR_EFSEFeatureFlags", 0, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.CurrentUser, GameConfigStore, "GameDVR_DSEBehavior", 0, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.LocalMachine, BcastDvrUserService, "Start", 3, RegistryValueKind.DWord);
            registry.DeleteValue(RegistryHive.LocalMachine, SessionManagerEnvironment, "__COMPAT_LAYER");
        }
    }
}

public sealed class NotificationsTweakHandler(IRegistryService registry) : ITweakHandler
{
    private const string WpnService = @"SYSTEM\CurrentControlSet\Services\WpnService";
    private const string NotificationsSettings = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\Settings";
    private const string ConsentStore = @"Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\userNotificationListener";
    private const string PushNotifications = @"SOFTWARE\Microsoft\Windows\CurrentVersion\PushNotifications";
    private const string PushNotificationsPolicy = @"SOFTWARE\Policies\Microsoft\Windows\CurrentVersion\PushNotifications";

    public string Key => "notifications";
    public string Title => "Disable Notifications";
    public string Description => "Toggle Notifications On or Off";
    public int Order => 10;

    public bool GetState() =>
        registry.GetValue(RegistryHive.LocalMachine, WpnService, "Start") is int v && v == 4;

    public void SetState(bool enabled)
    {
        if (enabled)
        {
            registry.SetValue(RegistryHive.LocalMachine, WpnService, "Start", 4, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.CurrentUser, NotificationsSettings, "NOC_GLOBAL_SETTING_ALLOW_NOTIFICATION_SOUND", 0, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.CurrentUser, ConsentStore, "Value", "Deny", RegistryValueKind.String);
            registry.SetValue(RegistryHive.LocalMachine, PushNotifications, "ToastEnabled", 0, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.LocalMachine, PushNotificationsPolicy, "NoCloudApplicationNotification", 1, RegistryValueKind.DWord);
        }
        else
        {
            registry.SetValue(RegistryHive.LocalMachine, WpnService, "Start", 2, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.CurrentUser, NotificationsSettings, "NOC_GLOBAL_SETTING_ALLOW_NOTIFICATION_SOUND", 1, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.CurrentUser, ConsentStore, "Value", "Allow", RegistryValueKind.String);
            registry.DeleteValue(RegistryHive.LocalMachine, PushNotifications, "ToastEnabled");
            registry.DeleteValue(RegistryHive.LocalMachine, PushNotificationsPolicy, "NoCloudApplicationNotification");
        }
    }
}

public sealed class PrefetchTweakHandler(IRegistryService registry) : ITweakHandler
{
    private const string SysMain = @"SYSTEM\CurrentControlSet\Services\SysMain";
    private const string FontCache = @"SYSTEM\CurrentControlSet\Services\FontCache";
    private const string PrefetchParameters = @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters";

    public string Key => "prefetch";
    public string Title => "Disable Prefetch";
    public string Description => "Toggle Prefetch On or Off";
    public int Order => 11;

    public bool GetState() =>
        registry.GetValue(RegistryHive.LocalMachine, SysMain, "Start") is int v && v == 4;

    public void SetState(bool enabled)
    {
        registry.SetValue(RegistryHive.LocalMachine, SysMain, "Start", enabled ? 4 : 2, RegistryValueKind.DWord);
        registry.SetValue(RegistryHive.LocalMachine, FontCache, "Start", enabled ? 4 : 2, RegistryValueKind.DWord);
        registry.SetValue(RegistryHive.LocalMachine, PrefetchParameters, "EnablePrefetcher", enabled ? 0 : 3, RegistryValueKind.DWord);
    }
}

public sealed class NoLazyModeTweakHandler(IRegistryService registry) : ITweakHandler
{
    private const string SystemProfile = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile";

    public string Key => "nolazy";
    public string Title => "NoLazyMode";
    public string Description => "Disable MMCSS lazy mode";
    public int Order => 14;

    public bool GetState() =>
        registry.GetValue(RegistryHive.LocalMachine, SystemProfile, "NoLazyMode") is int v && v == 1;

    // enabled=true means NoLazyMode is turned ON, per the predecessor's own "enable"
    // parameter naming for SetNoLazyMode.
    public void SetState(bool enabled)
    {
        registry.SetValue(RegistryHive.LocalMachine, SystemProfile, "NoLazyMode", enabled ? 1 : 0, RegistryValueKind.DWord);
        registry.SetValue(RegistryHive.LocalMachine, SystemProfile, "AlwaysOn", enabled ? 1 : 0, RegistryValueKind.DWord);
    }
}

public sealed class AdminUacTweakHandler(IRegistryService registry) : ITweakHandler
{
    private const string AppInfo = @"SYSTEM\CurrentControlSet\Services\AppInfo";
    private const string PoliciesSystem = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System";

    public string Key => "uacadmin";
    public string Title => "UAC For Admin Account";
    public string Description => "Configure UAC for admin accounts";
    public int Order => 15;

    public bool GetState() =>
        registry.GetValue(RegistryHive.LocalMachine, PoliciesSystem, "ValidateAdminCodeSignatures") is int v && v == 1;

    public void SetState(bool enabled)
    {
        registry.SetValue(RegistryHive.LocalMachine, AppInfo, "Start", enabled ? 2 : 4, RegistryValueKind.DWord);
        registry.SetValue(RegistryHive.LocalMachine, PoliciesSystem, "ValidateAdminCodeSignatures", enabled ? 1 : 0, RegistryValueKind.DWord);
    }
}

public sealed class UacTweakHandler(IRegistryService registry) : ITweakHandler
{
    private const string AppInfo = @"SYSTEM\CurrentControlSet\Services\AppInfo";
    private const string PoliciesSystem = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System";

    public string Key => "uac";
    public string Title => "User Account Control";
    public string Description => "Configure User Account Control settings";
    public int Order => 17;

    public bool GetState() =>
        registry.GetValue(RegistryHive.LocalMachine, PoliciesSystem, "EnableLUA") is int v && v == 1;

    public void SetState(bool enabled)
    {
        registry.SetValue(RegistryHive.LocalMachine, AppInfo, "Start", enabled ? 2 : 4, RegistryValueKind.DWord);
        registry.SetValue(RegistryHive.LocalMachine, PoliciesSystem, "EnableLUA", enabled ? 1 : 0, RegistryValueKind.DWord);
    }
}

public sealed class StartMenuTweakHandler(IRegistryService registry) : ITweakHandler
{
    private const string SearchKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Search";
    private const string ClassesSearchKeyPath = @"Software\Classes\Software\Microsoft\Windows\CurrentVersion\Search";

    public string Key => "startmenu";
    public string Title => "Disable Startmenu";
    public string Description => "Toggle Start Menu search/Bing On or Off";
    public int Order => 18;

    // OpenRealUserHive throws (D-14) if explorer.exe is not found — let it propagate,
    // the caller (ITweakCatalog/AkariOSTweaksViewModel) surfaces it via ILogConsoleService.
    public bool GetState()
    {
        using var searchKey = registry.OpenRealUserHive(SearchKeyPath);
        return searchKey.GetValue("BingSearchEnabled") is int v && v == 0;
    }

    public void SetState(bool enabled)
    {
        using var searchKey = registry.OpenRealUserHive(SearchKeyPath);
        searchKey.SetValue("BingSearchEnabled", enabled ? 0 : 1, RegistryValueKind.DWord);
        searchKey.SetValue("SearchBoxTaskbarMode", enabled ? 1 : 0, RegistryValueKind.DWord);

        using var classKey = registry.OpenRealUserHive(ClassesSearchKeyPath);
        classKey.SetValue("BingSearchEnabled", enabled ? 0 : 1, RegistryValueKind.DWord);
    }
}
