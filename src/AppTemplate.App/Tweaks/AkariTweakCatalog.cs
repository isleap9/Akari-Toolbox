namespace AkariToolbox.Tweaks;

/// <summary>
/// The 32 Akari OS Tweaks, ported 1:1 from the old AkariOS Companion's TweakService 
/// but expressed as native registry / service / bcdedit operations (no PowerShell) and
/// paired with live state readers so each toggle reflects the real system state.
/// Every toggle write-throughs its state key to HKCU\Software\AkariTool.
/// </summary>
public static class AkariTweakCatalog
{
    public static IReadOnlyList<TweakToggle> All { get; } = Build();

    private static IReadOnlyList<TweakToggle> Build() =>
    [
        //  Connectivity / services 
        Toggle("wifi", "Disable WiFi", "Toggle WiFi On or Off", "DisableWiFi",
            read: () => RegRead.ServiceStart("WlanSvc") == 4,
            on: () => { Svc("WlanSvc", 4); Svc("vwififlt", 4); Svc("netprofm", 4); Svc("NlaSvc", 4); },
            off: () => { Svc("WlanSvc", 2); Svc("vwififlt", 1); Svc("netprofm", 3); Svc("NlaSvc", 2); }),

        Toggle("tsx", "Enable Intel TSX", "Enable Intel Transactional Synchronization Extensions", "EnableTSX",
            read: () => RegRead.Dword(@"HKLM\SYSTEM\ControlSet001\Control\Session Manager\kernel", "DisableTsx") != 1,
            on: () => D(@"HKLM\SYSTEM\ControlSet001\Control\Session Manager\kernel", "DisableTsx", 0),
            off: () => D(@"HKLM\SYSTEM\ControlSet001\Control\Session Manager\kernel", "DisableTsx", 1)),

        Toggle("actioncenter", "Disable Action Center", "Toggle Action Center On or Off", "DisableActionCenter",
            read: () => RegRead.Dword(@"HKCU\Software\Policies\Microsoft\Windows\Explorer", "DisableNotificationCenter") == 1,
            on: () => D(@"HKCU\Software\Policies\Microsoft\Windows\Explorer", "DisableNotificationCenter", 1),
            off: () => D(@"HKCU\Software\Policies\Microsoft\Windows\Explorer", "DisableNotificationCenter", 0)),

        Toggle("clipboard", "Enable Clipboard", "Toggle Clipboard service On or Off", "EnableClipboardSvc",
            read: () => RegRead.ServiceStart("cbdhsvc") == 2,
            on: () => Svc("cbdhsvc", 2),
            off: () => Svc("cbdhsvc", 4)),

        Toggle("bluetooth", "Disable Bluetooth", "Toggle Bluetooth On or Off", "DisableBluetooth",
            read: () => RegRead.ServiceStart("bthserv") == 4,
            on: () => BluetoothServices(4),
            off: () => BluetoothServices(3)),

        Toggle("vpn", "Disable VPN", "Toggle VPN On or Off", "DisableVPN",
            read: () => RegRead.ServiceStart("IKEEXT") == 4,
            on: () =>
            {
                foreach (string svc in new[] { "IKEEXT", "WinHttpAutoProxySvc", "RasMan", "SstpSvc", "iphlpsvc", "NdisVirtualBus", "Eaphost" })
                    Svc(svc, 4);
            },
            off: () =>
            {
                foreach (string svc in new[] { "IKEEXT", "WinHttpAutoProxySvc", "RasMan", "SstpSvc", "iphlpsvc", "NdisVirtualBus", "Eaphost" })
                    Svc(svc, 3);
                Svc("BFE", 2);
            }),

        Toggle("notifications", "Disable Notifications", "Toggle Notifications On or Off", "DisableNotifications",
            read: () => RegRead.ServiceStart("WpnService") == 4,
            on: () =>
            {
                Svc("WpnService", 4);
                D(@"HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\Settings", "NOC_GLOBAL_SETTING_ALLOW_NOTIFICATION_SOUND", 0);
                S(@"HKCU\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\userNotificationListener", "Value", "Deny");
                D(@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\PushNotifications", "ToastEnabled", 0);
                D(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\CurrentVersion\PushNotifications", "NoCloudApplicationNotification", 1);
            },
            off: () =>
            {
                Svc("WpnService", 2);
                D(@"HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\Settings", "NOC_GLOBAL_SETTING_ALLOW_NOTIFICATION_SOUND", 1);
                S(@"HKCU\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\userNotificationListener", "Value", "Allow");
                DelV(@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\PushNotifications", "ToastEnabled");
                DelV(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\CurrentVersion\PushNotifications", "NoCloudApplicationNotification");
            }),

        Toggle("spooler", "Disable Print Spooler", "Toggle Print Spooler On or Off", "DisablePrintSpooler",
            read: () => RegRead.ServiceStart("Spooler") == 4,
            on: () => Svc("Spooler", 4),
            off: () => Svc("Spooler", 2)),

        Toggle("cdrom", "CDROM", "Enable the CDROM service", "EnableCDROM",
            read: () => RegRead.ServiceStart("cdrom") == 3,
            on: () =>
            {
                Svc("cdrom", 3);
                // Re-register IMAPI2 exactly like the old app did.
                const string imapi2 = @"HKLM\SYSTEM\CurrentControlSet\Services\IMAPI2";
                TweakAction.RegExpandString($@"{imapi2}", "Description", @"%SystemRoot%\system32\imapi2.dll,-2");
                TweakAction.RegExpandString($@"{imapi2}", "DisplayName", @"%SystemRoot%\system32\imapi2.dll,-1");
                D(imapi2, "ErrorControl", 1);
                TweakAction.RegExpandString($@"{imapi2}", "ImagePath", @"%SystemRoot%\system32\svchost.exe -k imapi");
                S(imapi2, "ObjectName", "LocalSystem");
                D(imapi2, "Start", 3);
                D(imapi2, "Type", 32);
                TweakAction.RegExpandString($@"{imapi2}\Parameters", "ServiceDll", @"%SystemRoot%\system32\imapi2.dll");
                D($@"{imapi2}\Parameters", "ServiceDllUnloadOnStop", 1);
            },
            off: () => { Svc("cdrom", 4); Svc("IMAPI2", 4); Svc("IMAPI2FS", 4); }),

        //  Boot / kernel 
        Toggle("dep", "Disable DEP/NX", "Toggle Data Execution Prevention", "DisableNX",
            read: () => BcdEditDump().Contains("nx alwaysoff"),
            on: () => Exec("bcdedit", "/set NX AlwaysOff"),
            off: () => Exec("bcdedit", "/set NX OptIn")),

        Toggle("bootmenu", "BootMenuPolicy Standard", "Set Boot Menu Policy to Standard", "BootMenuPolicy",
            read: () => BcdEditDump().Contains("bootmenupolicy standard"),
            on: () => Exec("bcdedit", "/set bootmenupolicy Standard"),
            off: () => Exec("bcdedit", "/set bootmenupolicy legacy")),

        Toggle("hyperv", "Disable Hyper-V", "Toggle Hyper-V On or Off", "DisableHyperV",
            read: () => BcdEditDump().Contains("hypervisorlaunchtype off"),
            on: () =>
            {
                Exec("bcdedit", "/set hypervisorlaunchtype off");
                Exec("bcdedit", "/set vm no");
                Exec("bcdedit", "/set vsmlaunchtype Off");
                Exec("bcdedit", "/set loadoptions DISABLE-LSA-ISO,DISABLE-VBS");
                Exec("DISM", "/Online /Disable-Feature:Microsoft-Hyper-V-All /Quiet /NoRestart");
                D(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\DeviceGuard", "EnableVirtualizationBasedSecurity", 0);
                D(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\DeviceGuard", "RequirePlatformSecurityFeatures", 1);
                D(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\DeviceGuard", "HypervisorEnforcedCodeIntegrity", 0);
                D(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\DeviceGuard", "HVCIMATRequired", 0);
                D(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\DeviceGuard", "LsaCfgFlags", 0);
                D(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\DeviceGuard", "ConfigureSystemGuardLaunch", 0);
                D(@"HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard", "RequireMicrosoftSignedBootChain", 0);
                D(@"HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity", "Enabled", 0);
            },
            off: () =>
            {
                Exec("bcdedit", "/set hypervisorlaunchtype auto");
                Exec("bcdedit", "/deletevalue vm");
                Exec("bcdedit", "/deletevalue loadoptions");
                Exec("DISM", "/Online /Enable-Feature:Microsoft-Hyper-V-All /Quiet /NoRestart");
                D(@"HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard", "RequireMicrosoftSignedBootChain", 1);
                D(@"HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard", "EnableVirtualizationBasedSecurity", 1);
                D(@"HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard", "RequirePlatformSecurityFeatures", 1);
                D(@"HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity", "Enabled", 1);
            }),

        Toggle("vbs", "Enable VBS", "Toggle Virtualization Based Security", "EnableVBS",
            read: () => RegRead.Dword(@"HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard", "EnableVirtualizationBasedSecurity") == 1,
            on: () =>
            {
                D(@"HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard", "EnableVirtualizationBasedSecurity", 1);
                D(@"HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard", "RequirePlatformSecurityFeatures", 1);
                D(@"HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity", "Enabled", 1);
            },
            off: () =>
            {
                D(@"HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard", "EnableVirtualizationBasedSecurity", 0);
                D(@"HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard", "RequirePlatformSecurityFeatures", 0);
                D(@"HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity", "Enabled", 0);
            }),

        //  Storage / memory / performance 
        Toggle("ntfsenc", "Disable NTFS Encryption", "Toggle NTFS Encryption On or Off", "DisableNTFSEncryption",
            read: () => RegRead.Dword(@"HKLM\SYSTEM\CurrentControlSet\Policies", "NtfsDisableEncryption") == 1,
            on: () =>
            {
                Exec("fsutil", "behavior set disableencryption 1");
                D(@"HKLM\SYSTEM\CurrentControlSet\Policies", "NtfsDisableEncryption", 1);
            },
            off: () =>
            {
                Exec("fsutil", "behavior set disableencryption 0");
                DelV(@"HKLM\SYSTEM\CurrentControlSet\Policies", "NtfsDisableEncryption");
            }),

        Toggle("prefetch", "Disable Prefetch", "Toggle Prefetch On or Off", "DisablePrefetch",
            read: () => RegRead.ServiceStart("SysMain") == 4,
            on: () =>
            {
                Svc("SysMain", 4);
                Svc("FontCache", 4);
                D(@"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters", "EnablePrefetcher", 0);
            },
            off: () =>
            {
                Svc("SysMain", 2);
                Svc("FontCache", 2);
                D(@"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters", "EnablePrefetcher", 3);
            }),

        Toggle("nolazy", "NoLazyMode", "Disable MMCSS lazy mode", "NoLazyMode",
            read: () => RegRead.Dword(@"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NoLazyMode") == 1,
            on: () =>
            {
                D(@"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NoLazyMode", 1);
                D(@"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "AlwaysOn", 1);
            },
            off: () =>
            {
                D(@"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NoLazyMode", 0);
                D(@"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "AlwaysOn", 0);
            }),

        Toggle("nvme", "NVME Tweaks", "Apply NVME performance tweaks", "NVMETweaks",
            read: () => RegRead.HasValue(@"HKLM\SYSTEM\ControlSet001\Services\stornvme\Parameters\Device", "ContiguousMemoryFromAnyNode"),
            on: () =>
            {
                const string dev = @"HKLM\SYSTEM\ControlSet001\Services\stornvme\Parameters\Device";
                D(dev, "ContiguousMemoryFromAnyNode", 1);
                D(dev, "LogSize", 0);
                D(dev, "IdlePowerMode", 0);
                D(dev, "DiagnosticFlags", 0);
            },
            off: () =>
            {
                const string dev = @"HKLM\SYSTEM\ControlSet001\Services\stornvme\Parameters\Device";
                foreach (string name in new[] { "ContiguousMemoryFromAnyNode", "LogSize", "IdlePowerMode", "DiagnosticFlags" })
                    DelV(dev, name);
            }),

        Toggle("largecache", "LargeSystemCache", "Configure large system cache", "EnableLargeSystemCache",
            read: () => RegRead.Dword(@"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "LargeSystemCache") == 1,
            on: () => D(@"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "LargeSystemCache", 1),
            off: () => D(@"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "LargeSystemCache", 0)),

        Toggle("sysprofile", "System Profile Tweaks", "Apply various system profile tweaks", "SystemProfile",
            read: () => RegRead.String(@"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "SFIO Priority") == "High",
            on: () =>
            {
                const string games = @"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games";
                const string proAudio = @"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Pro Audio";
                const string audio = @"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Audio";
                D(games, "Affinity", 0);
                S(games, "Background Only", "False");
                D(games, "Clock Rate", 2710);
                D(games, "GPU Priority", 8);
                D(games, "Priority", 8);
                S(games, "SFIO Priority", "High");
                S(games, "Scheduling Category", "High");
                D(proAudio, "Priority", 8);
                S(proAudio, "Scheduling Category", "Medium");
                D(audio, "Priority", 8);
            },
            off: () =>
            {
                const string games = @"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games";
                const string proAudio = @"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Pro Audio";
                const string audio = @"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Audio";
                foreach (string name in new[] { "Affinity", "Background Only", "Clock Rate", "GPU Priority", "Priority", "SFIO Priority", "Scheduling Category" })
                    DelV(games, name);
                DelV(proAudio, "Priority");
                DelV(proAudio, "Scheduling Category");
                DelV(audio, "Priority");
            }),

        //  Security / UAC / Defender 
        Toggle("uac", "User Account Control", "Configure User Account Control settings", "EnableUAC",
            read: () => RegRead.Dword(@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "EnableLUA") == 1,
            on: () =>
            {
                Svc("AppInfo", 2);
                D(@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "EnableLUA", 1);
            },
            off: () =>
            {
                Svc("AppInfo", 4);
                D(@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "EnableLUA", 0);
            }),

        Toggle("uacadmin", "UAC For Admin Account", "Configure UAC for admin accounts", "EnableAdminUAC",
            read: () => RegRead.Dword(@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "ValidateAdminCodeSignatures") == 1,
            on: () =>
            {
                Svc("AppInfo", 2);
                D(@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "ValidateAdminCodeSignatures", 1);
            },
            off: () =>
            {
                Svc("AppInfo", 4);
                D(@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "ValidateAdminCodeSignatures", 0);
            }),

        Toggle("mitigation", "Enable Process Mitigation", "Enable process mitigation policies", "EnableProcessMitigations",
            read: () => RegRead.Dword(
                @"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "FeatureSettingsOverride") != 3,
            on: () =>
            {
                const string key = @"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management";
                D(key, "FeatureSettingsOverride", 0);
                D(key, "FeatureSettingsOverrideMask", 3);
            },
            off: () =>
            {
                const string key = @"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management";
                D(key, "FeatureSettingsOverride", 3);
                D(key, "FeatureSettingsOverrideMask", 3);
            }),

        Toggle("defender", "Disable Defender", "Toggle Windows Defender On or Off", "DisableDefender",
            read: () => RegRead.ServiceStart("WinDefend") == 4,
            on: DisableDefender,
            off: EnableDefender),

        //  Gaming / UX 
        Toggle("fso", "Disable FSO and Gamebar", "Toggle FSO and Gamebar On or Off", "DisableFSO",
            read: () => RegRead.Dword(@"HKCU\System\GameConfigStore", "GameDVR_FSEBehaviorMode") == 2,
            on: () =>
            {
                D(@"HKCU\Software\Microsoft\GameBar", "ShowStartupPanel", 0);
                D(@"HKCU\Software\Microsoft\GameBar", "GamePanelStartupTipIndex", 3);
                D(@"HKCU\Software\Microsoft\GameBar", "AllowAutoGameMode", 0);
                D(@"HKCU\Software\Microsoft\GameBar", "AutoGameModeEnabled", 0);
                D(@"HKCU\Software\Microsoft\GameBar", "UseNexusForGameBarEnabled", 0);
                D(@"HKCU\System\GameConfigStore", "GameDVR_Enabled", 0);
                D(@"HKCU\System\GameConfigStore", "GameDVR_FSEBehaviorMode", 2);
                D(@"HKCU\System\GameConfigStore", "GameDVR_FSEBehavior", 2);
                D(@"HKCU\System\GameConfigStore", "GameDVR_HonorUserFSEBehaviorMode", 1);
                D(@"HKCU\System\GameConfigStore", "GameDVR_DXGIHonorFSEWindowsCompatible", 1);
                D(@"HKCU\System\GameConfigStore", "GameDVR_EFSEFeatureFlags", 0);
                D(@"HKCU\System\GameConfigStore", "GameDVR_DSEBehavior", 2);
                D(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\GameDVR", "AllowGameDVR", 0);
                D(@"HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled", 0);
                Svc("BcastDVRUserService", 4);
                S(@"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Environment", "__COMPAT_LAYER", "~ DISABLEDXMAXIMIZEDWINDOWEDMODE");
            },
            off: () =>
            {
                D(@"HKCU\System\GameConfigStore", "GameDVR_Enabled", 0);
                D(@"HKCU\System\GameConfigStore", "GameDVR_FSEBehaviorMode", 0);
                D(@"HKCU\System\GameConfigStore", "GameDVR_FSEBehavior", 0);
                D(@"HKCU\System\GameConfigStore", "GameDVR_HonorUserFSEBehaviorMode", 1);
                D(@"HKCU\System\GameConfigStore", "GameDVR_DXGIHonorFSEWindowsCompatible", 0);
                D(@"HKCU\System\GameConfigStore", "GameDVR_EFSEFeatureFlags", 0);
                D(@"HKCU\System\GameConfigStore", "GameDVR_DSEBehavior", 0);
                Svc("BcastDVRUserService", 3);
                DelV(@"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Environment", "__COMPAT_LAYER");
            }),

        Toggle("mpo", "Disable Multi-Plane-Overlay", "Toggle MPO On or Off", "DisableMPO",
            read: () => RegRead.Dword(@"HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "DisableOverlays") == 1,
            on: () => D(@"HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "DisableOverlays", 1),
            off: () => DelV(@"HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "DisableOverlays")),

        Toggle("lockscreen", "Disable Lock Screen", "Toggle lock screen On or Off", "DisableLockScreen",
            read: () => RegRead.Dword(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\Personalization", "NoLockScreen") == 1,
            on: () => D(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\Personalization", "NoLockScreen", 1),
            off: () => D(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\Personalization", "NoLockScreen", 0)),

        Toggle("animations", "Disable Animations", "Toggle system animations", "DisableAnimations",
            read: () => RegRead.Dword(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\DWM", "DisallowAnimations") == 1
                      || RegRead.Dword(@"HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarAnimations") == 0,
            on: () =>
            {
                D(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\DWM", "DisallowAnimations", 1);
                D(@"HKCU\SOFTWARE\Microsoft\Windows\DWM", "EnableAeroPeek", 0);
                D(@"HKCU\SOFTWARE\Microsoft\Windows\DWM", "AlwaysHibernateThumbnails", 0);
                D(@"HKCU\Control Panel\Desktop\WindowMetrics", "MinAnimate", 0);
                D(@"HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarAnimations", 0);
                D(@"HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "IconsOnly", 1);
                D(@"HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ListviewAlphaSelect", 0);
                D(@"HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ListviewShadow", 0);
                D(@"HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", "VisualFXSetting", 3);
                TweakAction.RegBinary(@"HKCU\Control Panel\Desktop", "UserPreferencesMask",
                    [0x90, 0x12, 0x03, 0x80, 0x10, 0x00, 0x00, 0x00]).Apply();
                SystemParametersInfo();
            },
            off: () =>
            {
                DelV(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\DWM", "DisallowAnimations");
                D(@"HKCU\SOFTWARE\Microsoft\Windows\DWM", "EnableAeroPeek", 1);
                D(@"HKCU\SOFTWARE\Microsoft\Windows\DWM", "AlwaysHibernateThumbnails", 1);
                D(@"HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarAnimations", 1);
                D(@"HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "IconsOnly", 0);
                D(@"HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ListviewAlphaSelect", 1);
                D(@"HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ListviewShadow", 1);
                D(@"HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", "VisualFXSetting", 1);
                TweakAction.RegBinary(@"HKCU\Control Panel\Desktop", "UserPreferencesMask",
                    [0x9E, 0x3E, 0x07, 0x80, 0x12, 0x00, 0x00, 0x00]).Apply();
                SystemParametersInfo();
            }),

        Toggle("wallpaperq", "Disable Wallpaper Quality Reduction", "Prevent wallpaper quality reduction", "WallpaperQuality",
            read: () => RegRead.HasValue(@"HKCU\Control Panel\Desktop", "JPEGImportQuality"),
            on: () => D(@"HKCU\Control Panel\Desktop", "JPEGImportQuality", 100),
            off: () => DelV(@"HKCU\Control Panel\Desktop", "JPEGImportQuality")),

        Toggle("transparency", "Transparency Effects", "Toggle transparency effects", "TransparencyEffects",
            read: () => AkariToolState.RealHkcuDword(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize", "EnableTransparency") == 1,
            on: () => AkariToolState.SetRealHkcuDword(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize", "EnableTransparency", 1),
            off: () => AkariToolState.SetRealHkcuDword(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize", "EnableTransparency", 0)),

        Toggle("startmenu", "Disable Startmenu", "Toggle Start Menu search/Bing On or Off", "DisableStartmenu",
            read: () => AkariToolState.RealHkcuDword(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Search", "BingSearchEnabled") == 0,
            on: () =>
            {
                AkariToolState.SetRealHkcuDword(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Search", "BingSearchEnabled", 0);
                AkariToolState.SetRealHkcuDword(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Search", "SearchBoxTaskbarMode", 1);
                AkariToolState.SetRealHkcuDword(@"Software\Classes\Software\Microsoft\Windows\CurrentVersion\Search", "BingSearchEnabled", 0);
            },
            off: () =>
            {
                AkariToolState.SetRealHkcuDword(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Search", "BingSearchEnabled", 1);
                AkariToolState.SetRealHkcuDword(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Search", "SearchBoxTaskbarMode", 0);
                AkariToolState.SetRealHkcuDword(@"Software\Classes\Software\Microsoft\Windows\CurrentVersion\Search", "BingSearchEnabled", 1);
            }),

        Toggle("dcom", "Disable DCOM", "Toggle DCOM On or Off", "DisableDCOM",
            read: () => RegRead.String(@"HKLM\SOFTWARE\Microsoft\Ole", "EnableDCOM") == "N",
            on: () => S(@"HKLM\SOFTWARE\Microsoft\Ole", "EnableDCOM", "N"),
            off: () => S(@"HKLM\SOFTWARE\Microsoft\Ole", "EnableDCOM", "Y")),

        //  Ambiguous-state toggles (persisted state drives the position) 
        // VR touches 9 services with machine-specific stock values plus a DISM
        // feature, so its position comes from the persisted state store  same
        // behavior as the old app.
        Toggle("vr", "VR", "Enable VR Services", "EnableVR",
            read: () => AkariToolState.Has("EnableVR"),
            on: () =>
            {
                var services = new (string Svc, int Enable, int Disable)[]
                {
                    ("KSecPkg", 0, 4), ("LanmanWorkstation", 2, 4), ("mrxsmb", 3, 4),
                    ("mrxsmb20", 3, 4), ("rdbss", 1, 4), ("srv2", 2, 4),
                    ("QwaveDrv", 3, 4), ("Qwave", 3, 4), ("FontCache", 2, 4),
                };
                foreach (var (svc, en, _) in services)
                    TweakAction.RegDword($@"HKLM\SYSTEM\ControlSet001\Services\{svc}", "Start", en).Apply();
                Exec("DISM", "/Online /Enable-Feature /FeatureName:SmbDirect /NoRestart");
            },
            off: () =>
            {
                var services = new (string Svc, int Enable, int Disable)[]
                {
                    ("KSecPkg", 0, 4), ("LanmanWorkstation", 2, 4), ("mrxsmb", 3, 4),
                    ("mrxsmb20", 3, 4), ("rdbss", 1, 4), ("srv2", 2, 4),
                    ("QwaveDrv", 3, 4), ("Qwave", 3, 4), ("FontCache", 2, 4),
                };
                foreach (var (svc, _, dis) in services)
                    TweakAction.RegDword($@"HKLM\SYSTEM\ControlSet001\Services\{svc}", "Start", dis).Apply();
                Exec("DISM", "/Online /Disable-Feature /FeatureName:SmbDirect /NoRestart");
            }),
    ];

    // 
    // Defender (native rewrite of the old two-phase PostInstall flow)
    // 

    private static readonly string[] DefenderServices =
    {
        "MsSecCore", "MsSecFlt", "MsSecWfp", "SecurityHealthService",
        "Sense", "WdBoot", "WdFilter", "WdNisDrv", "WdNisSvc",
        "WinDefend", "wscsvc", "MDCoreSvc", "SgrmAgent", "SgrmBroker",
        "webthreatdefsvc", "webthreatdefusersvc",
    };

    private static readonly string[] DefenderTasks =
    {
        "Windows Defender Cache Maintenance",
        "Windows Defender Cleanup",
        "Windows Defender Scheduled Scan",
        "Windows Defender Verification",
    };

    /// <summary>True when Tamper Protection is on (=4)  Defender can't be changed then.</summary>
    private static bool TamperProtectionOn()
    {
        int? value = RegRead.Dword(@"HKLM\SOFTWARE\Microsoft\Windows Defender\Features", "TamperProtection");
        return value is not 4;
    }

    private static void DisableDefender()
    {
        if (TamperProtectionOn())
            throw new InvalidOperationException(
                "Tamper Protection is ON. Turn it off first: Windows Security  Virus & threat " +
                "protection  Manage settings  Tamper Protection  Off.");

        // Real-time monitoring off.
        NativeOps.PowerShellCommand("Set-MpPreference -DisableRealtimeMonitoring 1");

        // Kill every Defender service (ControlSet001, like the old Phase 2 cleanup).
        foreach (string svc in DefenderServices)
            D($@"HKLM\SYSTEM\ControlSet001\Services\{svc}", "Start", 4);

        // SecurityHealth autostart.
        DelV(@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Run", "SecurityHealth");

        // SmartScreen / CI / PUA registry block.
        S(@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer", "SmartScreenEnabled", "Off");
        D(@"HKLM\Software\Policies\Microsoft\System", "EnableSmartScreen", 0);
        D(@"HKLM\Software\Policies\Microsoft\Windows Defender\SmartScreen", "ConfigureAppInstallControlEnabled", 0);
        D(@"HKLM\Software\Policies\Microsoft\Windows Defender\SmartScreen", "EnableSmartScreen", 0);
        D(@"HKCU\Software\Microsoft\Windows\CurrentVersion\AppHost", "EnableWebContentEvaluation", 0);
        D(@"HKLM\SYSTEM\ControlSet001\Control\CI\Policy", "VerifiedAndReputablePolicyState", 0);
        D(@"HKLM\Software\Microsoft\Windows Defender", "PUAProtection", 0);
        D(@"HKLM\SYSTEM\ControlSet001\Control\CI\Config", "VulnerableDriverBlocklistEnable", 0);
        D(@"HKLM\SYSTEM\ControlSet001\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity", "Enabled", 0);

        // Defender scheduled tasks off.
        foreach (string task in DefenderTasks)
            NativeOps.DisableScheduledTask(task);

        // SmartScreen binary: kill, take ownership, move aside (best effort 
        // Windows may still protect it until reboot).
        DisableSmartscreenBinary();
    }

    private static void EnableDefender()
    {
        NativeOps.PowerShellCommand("Set-MpPreference -DisableRealtimeMonitoring 0");

        foreach (string svc in DefenderServices)
            D($@"HKLM\SYSTEM\ControlSet001\Services\{svc}", "Start", 2);

        foreach (string task in DefenderTasks)
            NativeOps.EnableScheduledTask(task);

        EnableSmartscreenBinary();
    }

    private static string System32Path(string fileName) =>
        System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), fileName);

    private static void DisableSmartscreenBinary()
    {
        string screen = System32Path("smartscreen.exe");
        string backup = screen + ".old";
        if (!System.IO.File.Exists(screen) || System.IO.File.Exists(backup))
            return;

        NativeOps.StopProcess("smartscreen");
        NativeOps.RunTool("takeown", $"/F \"{screen}\" /A");
        NativeOps.RunTool("icacls", $"\"{screen}\" /grant Administrators:F");
        NativeOps.RunTool("cmd.exe", $"/c copy \"{screen}\" \"{backup}\" /y && del /f /q \"{screen}\"");
    }

    private static void EnableSmartscreenBinary()
    {
        string screen = System32Path("smartscreen.exe");
        string backup = screen + ".old";
        if (!System.IO.File.Exists(backup) || System.IO.File.Exists(screen))
            return;

        NativeOps.RunTool("cmd.exe", $"/c move /y \"{backup}\" \"{screen}\"");
    }

    // 
    // Helpers
    // 

    private static TweakToggle Toggle(
        string key, string title, string description, string stateKey,
        Func<bool> read, Action on, Action off) =>
        new(key, title, description, stateKey, read,
            enabled => { if (enabled) on(); else off(); });

    private static void BluetoothServices(int start)
    {
        string[] services =
        {
            "BthA4dp", "BthEnum", "BthHFEnum", "BthLEEnum", "BTHMODEM",
            "Microsoft_Bluetooth_AvrcpTransport", "BluetoothUserService",
            "BthAvctpSvc", "RFCOMM", "bthserv", "BTAGService",
            "BTHUSB", "BTHPORT", "BthMini", "HidBth",
        };
        foreach (string svc in services)
            Svc(svc, start);
    }

    private static void Svc(string service, int start) =>
        D($@"HKLM\SYSTEM\CurrentControlSet\Services\{service}", "Start", start);

    private static void D(string path, string name, long value) =>
        TweakAction.RegDword(path, name, value).Apply();

    private static void S(string path, string name, string value) =>
        TweakAction.RegString(path, name, value).Apply();

    private static void DelV(string path, string name) =>
        TweakAction.RegDeleteValue(path, name).Apply();

    private static void Exec(string fileName, string arguments) =>
        TweakAction.Run(fileName, arguments).Apply();

    /// <summary>Normalized bcdedit {current} dump (lowercase, single spaces).</summary>
    private static string BcdEditDump()
    {
        string dump = NativeOps.RunToolCapture("bcdedit", "/enum {current}");
        return string.Join(' ', dump.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, IntPtr pvParam, uint fWinIni);

    private static void SystemParametersInfo() =>
        SystemParametersInfo(0x0014u /* SPI_SETANIMATION */, 0u, IntPtr.Zero, 0x0003u /* SPIF_UPDATEINIFILE | SPIF_SENDCHANGE */);
}
