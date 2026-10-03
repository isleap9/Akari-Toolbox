namespace AkariToolbox.Tweaks;

/// <summary>
/// The 169 granular Control Panel rows (Akari-Tool Render-CpTweaks data,
/// auto-generated from FR33THY registryoptimize/defaults). GENERATED FILE:
/// produced from Invoke-CpTweaks.ps1, do not edit by hand.
/// </summary>
public sealed record CpTweakRow(string Id, string Title, string Tab, string Section, bool Revertible, string Optimize, string Default);

public static partial class ControlPanelRows
{
    public static readonly CpTweakRow[] All =
    [
        new(
            "disable_narrator",
            "Disable Narrator",
            "Appearance",
            "Ease of Access",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Narrator\NoRoam]
""DuckAudio""=dword:00000000
""WinEnterLaunchEnabled""=dword:00000000
""ScriptingEnabled""=dword:00000000
""OnlineServicesEnabled""=dword:00000000
[HKEY_CURRENT_USER\Software\Microsoft\Narrator]
""NarratorCursorHighlight""=dword:00000000
""CoupleNarratorCursorKeyboard""=dword:00000000",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Narrator\NoRoam]
""DuckAudio""=-
""WinEnterLaunchEnabled""=-
""ScriptingEnabled""=-
""OnlineServicesEnabled""=-

[HKEY_CURRENT_USER\Software\Microsoft\Narrator]
""NarratorCursorHighlight""=-
""CoupleNarratorCursorKeyboard""=-"),
        new(
            "disable_ease_of_access_settings",
            "Disable Ease Of Access Settings",
            "Appearance",
            "Ease of Access",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Ease of Access]
""selfvoice""=dword:00000000
""selfscan""=dword:00000000
[HKEY_CURRENT_USER\Control Panel\Accessibility]
""Sound on Activation""=dword:00000000
""Warning Sounds""=dword:00000000
[HKEY_CURRENT_USER\Control Panel\Accessibility\HighContrast]
""Flags""=""4194""
[HKEY_CURRENT_USER\Control Panel\Accessibility\Keyboard Response]
""Flags""=""2""
""AutoRepeatRate""=""0""
""AutoRepeatDelay""=""0""
[HKEY_CURRENT_USER\Control Panel\Accessibility\MouseKeys]
""Flags""=""130""
""MaximumSpeed""=""39""
""TimeToMaximumSpeed""=""3000""
[HKEY_CURRENT_USER\Control Panel\Accessibility\StickyKeys]
""Flags""=""2""
[HKEY_CURRENT_USER\Control Panel\Accessibility\ToggleKeys]
""Flags""=""34""
[HKEY_CURRENT_USER\Control Panel\Accessibility\SoundSentry]
""Flags""=""0""
""FSTextEffect""=""0""
""TextEffect""=""0""
""WindowsEffect""=""0""
[HKEY_CURRENT_USER\Control Panel\Accessibility\SlateLaunch]
""ATapp""=""""
""LaunchAT""=dword:00000000",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Ease of Access]
""selfvoice""=-
""selfscan""=-

[HKEY_CURRENT_USER\Control Panel\Accessibility]
""Sound on Activation""=-
""Warning Sounds""=-

[HKEY_CURRENT_USER\Control Panel\Accessibility\HighContrast]
""Flags""=""126""

[HKEY_CURRENT_USER\Control Panel\Accessibility\Keyboard Response]
""Flags""=""126""
""AutoRepeatRate""=""500""
""AutoRepeatDelay""=""1000""

[HKEY_CURRENT_USER\Control Panel\Accessibility\MouseKeys]
""Flags""=""62""
""MaximumSpeed""=""80""
""TimeToMaximumSpeed""=""3000""

[HKEY_CURRENT_USER\Control Panel\Accessibility\StickyKeys]
""Flags""=""510""

[HKEY_CURRENT_USER\Control Panel\Accessibility\ToggleKeys]
""Flags""=""62""

[HKEY_CURRENT_USER\Control Panel\Accessibility\SoundSentry]
""Flags""=""2""
""FSTextEffect""=""0""
""TextEffect""=""0""
""WindowsEffect""=""1""

[HKEY_CURRENT_USER\Control Panel\Accessibility\SlateLaunch]
""ATapp""=""narrator""
""LaunchAT""=dword:00000001"),
        new(
            "disable_notify_me_when_the_clock_changes",
            "Disable Notify Me When The Clock Changes",
            "Appearance",
            "Clock & Region",
            true,
            @"[HKEY_CURRENT_USER\Control Panel\TimeDate]
""DstNotification""=dword:00000000",
            @"[HKEY_CURRENT_USER\Control Panel\TimeDate]
""DstNotification""=-"),
        new(
            "open_file_explorer_to_this_pc",
            "Open File Explorer To This Pc",
            "Appearance",
            "File Explorer",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced]
""LaunchTo""=dword:00000001",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced]
""LaunchTo""=-"),
        new(
            "hide_frequent_folders_in_quick_access",
            "Hide Frequent Folders In Quick Access",
            "Appearance",
            "File Explorer",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer]
""ShowFrequent""=dword:00000000",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer]
""ShowFrequent""=-"),
        new(
            "show_file_name_extensions",
            "Show File Name Extensions",
            "Appearance",
            "File Explorer",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced]
""HideFileExt""=dword:00000000",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced]
""HideFileExt""=dword:00000001"),
        new(
            "disable_search_history",
            "Disable Search History",
            "Appearance",
            "File Explorer",
            true,
            @"[HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\SearchSettings]
""IsDeviceSearchHistoryEnabled""=dword:00000000",
            @"[HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\SearchSettings]
""IsDeviceSearchHistoryEnabled""=-"),
        new(
            "disable_show_files_from_office_com",
            "Disable Show Files From Office.Com",
            "Appearance",
            "File Explorer",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer]
""ShowCloudFilesInQuickAccess""=dword:00000000",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer]
""ShowCloudFilesInQuickAccess""=-"),
        new(
            "disable_display_file_size_information_in_fol",
            "Disable Display File Size Information In Folder Tips",
            "Appearance",
            "File Explorer",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced]
""FolderContentsInfoTip""=dword:00000000",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced]
""FolderContentsInfoTip""=-"),
        new(
            "enable_display_full_path_in_the_title_bar",
            "Enable Display Full Path In The Title Bar",
            "Appearance",
            "File Explorer",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\CabinetState]
""FullPath""=dword:00000001",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\CabinetState]
""FullPath""=dword:00000000"),
        new(
            "disable_show_pop_up_description_for_folder_a",
            "Disable Show Pop-Up Description For Folder And Desktop Items",
            "Appearance",
            "File Explorer",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced]
""ShowInfoTip""=dword:00000000",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced]
""ShowInfoTip""=dword:00000001"),
        new(
            "disable_show_preview_handlers_in_preview_pan",
            "Disable Show Preview Handlers In Preview Pane",
            "Appearance",
            "File Explorer",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced]
""ShowPreviewHandlers""=dword:00000000",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced]
""ShowPreviewHandlers""=-"),
        new(
            "disable_show_status_bar",
            "Disable Show Status Bar",
            "Appearance",
            "File Explorer",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced]
""ShowStatusBar""=dword:00000000",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced]
""ShowStatusBar""=dword:00000001"),
        new(
            "disable_show_sync_provider_notifications",
            "Disable Show Sync Provider Notifications",
            "Appearance",
            "File Explorer",
            true,
            @"[HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced]
""ShowSyncProviderNotifications""=dword:00000000",
            @"[HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced]
""ShowSyncProviderNotifications""=-"),
        new(
            "disable_use_sharing_wizard",
            "Disable Use Sharing Wizard",
            "Appearance",
            "File Explorer",
            true,
            @"[HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced]
""SharingWizardOn""=dword:00000000",
            @"[HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced]
""SharingWizardOn""=-"),
        new(
            "disable_show_network",
            "Disable Show Network",
            "Appearance",
            "File Explorer",
            true,
            @"[HKEY_CURRENT_USER\Software\Classes\CLSID\{F02C1A0D-BE21-4350-88B0-7367FC96EF3C}]
""System.IsPinnedToNameSpaceTree""=dword:00000000",
            @"[HKEY_CURRENT_USER\Software\Classes\CLSID\{F02C1A0D-BE21-4350-88B0-7367FC96EF3C}]
""System.IsPinnedToNameSpaceTree""=-"),
        new(
            "disable_lock",
            "Disable Lock",
            "Tweaks",
            "Hardware & Sound",
            true,
            @"[HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\Explorer\FlyoutMenuSettings]
""ShowLockOption""=dword:00000000",
            @"[HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\Explorer\FlyoutMenuSettings]
""ShowLockOption""=-"),
        new(
            "disable_sleep",
            "Disable Sleep",
            "Tweaks",
            "Hardware & Sound",
            true,
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\FlyoutMenuSettings]
""ShowSleepOption""=dword:00000000",
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\FlyoutMenuSettings]
""ShowSleepOption""=-"),
        new(
            "sound_communications_do_nothing",
            "Sound Communications Do Nothing",
            "Tweaks",
            "Hardware & Sound",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Multimedia\Audio]
""UserDuckingPreference""=dword:00000003",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Multimedia\Audio]
""UserDuckingPreference""=-"),
        new(
            "disable_startup_sound",
            "Disable Startup Sound",
            "Tweaks",
            "Hardware & Sound",
            true,
            @"[HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\Authentication\LogonUI\BootAnimation]
""DisableStartupSound""=dword:00000001
[HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\EditionOverrides]
""UserSetting_DisableStartupSound""=dword:00000001",
            @"[HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\Authentication\LogonUI\BootAnimation]
""DisableStartupSound""=dword:00000000

[HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\EditionOverrides]
""UserSetting_DisableStartupSound""=dword:00000000"),
        new(
            "sound_scheme_none",
            "Sound Scheme None",
            "Tweaks",
            "Hardware & Sound",
            true,
            @"[HKEY_CURRENT_USER\AppEvents\Schemes]
@="".None""
[HKEY_CURRENT_USER\AppEvents\Schemes\Apps\.Default\.Default\.Current]
@=""""
[HKEY_CURRENT_USER\AppEvents\Schemes\Apps\.Default\CriticalBatteryAlarm\.Current]
@=""""
[HKEY_CURRENT_USER\AppEvents\Schemes\Apps\.Default\DeviceConnect\.Current]
@=""""
[HKEY_CURRENT_USER\AppEvents\Schemes\Apps\.Default\DeviceDisconnect\.Current]
@=""""
[HKEY_CURRENT_USER\AppEvents\Schemes\Apps\.Default\DeviceFail\.Current]
@=""""
[HKEY_CURRENT_USER\AppEvents\Schemes\Apps\.Default\FaxBeep\.Current]
@=""""
[HKEY_CURRENT_USER\AppEvents\Schemes\Apps\.Default\LowBatteryAlarm\.Current]
@=""""
[HKEY_CURRENT_USER\AppEvents\Schemes\Apps\.Default\MailBeep\.Current]
@=""""
[HKEY_CURRENT_USER\AppEvents\Schemes\Apps\.Default\MessageNudge\.Current]
@=""""
[HKEY_CURRENT_USER\AppEvents\Schemes\Apps\.Default\Notification.Default\.Current]
@=""""
[HKEY_CURRENT_USER\AppEvents\Schemes\Apps\.Default\Notification.IM\.Current]
@=""""
[HKEY_CURRENT_USER\AppEvents\Schemes\Apps\.Default\Notification.Mail\.Current]
@=""""
[HKEY_CURRENT_USER\AppEvents\Schemes\Apps\.Default\Notification.Proximity\.Current]
@=""""
[HKEY_CURRENT_USER\AppEvents\Schemes\Apps\.Default\Notification.Reminder\.Current]
@=""""
[HKEY_CURRENT_USER\AppEvents\Schemes\Apps\.Default\Notification.SMS\.Current]
@=""""
[HKEY_CURRENT_USER\AppEvents\Schemes\Apps\.Default\ProximityConnection\.Current]
@=""""
[HKEY_CURRENT_USER\AppEvents\Schemes\Apps\.Default\SystemAsterisk\.Current]
@=""""
[HKEY_CURRENT_USER\AppEvents\Schemes\Apps\.Default\SystemExclamation\.Current]
@=""""
[HKEY_CURRENT_USER\AppEvents\Schemes\Apps\.Default\SystemHand\.Current]
@=""""
[HKEY_CURRENT_USER\AppEvents\Schemes\Apps\.Default\SystemNotification\.Current]
@=""""
[HKEY_CURRENT_USER\AppEvents\Schemes\Apps\.Default\WindowsUAC\.Current]
@=""""
[HKEY_CURRENT_USER\AppEvents\Schemes\Apps\sapisvr\DisNumbersSound\.current]
@=""""
[HKEY_CURRENT_USER\AppEvents\Schemes\Apps\sapisvr\HubOffSound\.current]
@=""""
[HKEY_CURRENT_USER\AppEvents\Schemes\Apps\sapisvr\HubOnSound\.current]
@=""""
[HKEY_CURRENT_USER\AppEvents\Schemes\Apps\sapisvr\HubSleepSound\.current]
@=""""
[HKEY_CURRENT_USER\AppEvents\Schemes\Apps\sapisvr\MisrecoSound\.current]
@=""""
[HKEY_CURRENT_USER\AppEvents\Schemes\Apps\sapisvr\PanelSound\.current]
@=""""",
            @"[HKEY_CURRENT_USER\AppEvents\Schemes]
@="".Default""

[HKEY_CURRENT_USER\AppEvents\Schemes\Apps\.Default\.Default\.Current]
@=""C:\\Windows\\media\\Windows Background.wav""

[HKEY_CURRENT_USER\AppEvents\Schemes\Apps\.Default\CriticalBatteryAlarm\.Current]
@=""C:\\Windows\\media\\Windows Foreground.wav""

[HKEY_CURRENT_USER\AppEvents\Schemes\Apps\.Default\DeviceConnect\.Current]
@=""C:\\Windows\\media\\Windows Hardware Insert.wav""

[HKEY_CURRENT_USER\AppEvents\Schemes\Apps\.Default\DeviceDisconnect\.Current]
@=""C:\\Windows\\media\\Windows Hardware Remove.wav""

[HKEY_CURRENT_USER\AppEvents\Schemes\Apps\.Default\DeviceFail\.Current]
@=""C:\\Windows\\media\\Windows Hardware Fail.wav""

[HKEY_CURRENT_USER\AppEvents\Schemes\Apps\.Default\FaxBeep\.Current]
@=""C:\\Windows\\media\\Windows Notify Email.wav""

[HKEY_CURRENT_USER\AppEvents\Schemes\Apps\.Default\LowBatteryAlarm\.Current]
@=""C:\\Windows\\media\\Windows Background.wav""

[HKEY_CURRENT_USER\AppEvents\Schemes\Apps\.Default\MailBeep\.Current]
@=""C:\\Windows\\media\\Windows Notify Email.wav""

[HKEY_CURRENT_USER\AppEvents\Schemes\Apps\.Default\MessageNudge\.Current]
@=""C:\\Windows\\media\\Windows Message Nudge.wav""

[HKEY_CURRENT_USER\AppEvents\Schemes\Apps\.Default\Notification.Default\.Current]
@=""C:\\Windows\\media\\Windows Notify System Generic.wav""

[HKEY_CURRENT_USER\AppEvents\Schemes\Apps\.Default\Notification.IM\.Current]
@=""C:\\Windows\\media\\Windows Notify Messaging.wav""

[HKEY_CURRENT_USER\AppEvents\Schemes\Apps\.Default\Notification.Mail\.Current]
@=""C:\\Windows\\media\\Windows Notify Email.wav""

[HKEY_CURRENT_USER\AppEvents\Schemes\Apps\.Default\Notification.Proximity\.Current]
@=""C:\\Windows\\media\\Windows Proximity Notification.wav""

[HKEY_CURRENT_USER\AppEvents\Schemes\Apps\.Default\Notification.Reminder\.Current]
@=""C:\\Windows\\media\\Windows Notify Calendar.wav""

[HKEY_CURRENT_USER\AppEvents\Schemes\Apps\.Default\Notification.SMS\.Current]
@=""C:\\Windows\\media\\Windows Notify Messaging.wav""

[HKEY_CURRENT_USER\AppEvents\Schemes\Apps\.Default\ProximityConnection\.Current]
@=""C:\\Windows\\media\\Windows Proximity Connection.wav""

[HKEY_CURRENT_USER\AppEvents\Schemes\Apps\.Default\SystemAsterisk\.Current]
@=""C:\\Windows\\media\\Windows Background.wav""

[HKEY_CURRENT_USER\AppEvents\Schemes\Apps\.Default\SystemExclamation\.Current]
@=""C:\\Windows\\media\\Windows Background.wav""

[HKEY_CURRENT_USER\AppEvents\Schemes\Apps\.Default\SystemHand\.Current]
@=""C:\\Windows\\media\\Windows Foreground.wav""

[HKEY_CURRENT_USER\AppEvents\Schemes\Apps\.Default\SystemNotification\.Current]
@=""C:\\Windows\\media\\Windows Background.wav""

[HKEY_CURRENT_USER\AppEvents\Schemes\Apps\.Default\WindowsUAC\.Current]
@=""C:\\Windows\\media\\Windows User Account Control.wav""

[HKEY_CURRENT_USER\AppEvents\Schemes\Apps\sapisvr\DisNumbersSound\.current]
@=""C:\\Windows\\media\\Speech Disambiguation.wav""

[HKEY_CURRENT_USER\AppEvents\Schemes\Apps\sapisvr\HubOffSound\.current]
@=""C:\\Windows\\media\\Speech Off.wav""

[HKEY_CURRENT_USER\AppEvents\Schemes\Apps\sapisvr\HubOnSound\.current]
@=""C:\\Windows\\media\\Speech On.wav""

[HKEY_CURRENT_USER\AppEvents\Schemes\Apps\sapisvr\HubSleepSound\.current]
@=""C:\\Windows\\media\\Speech Sleep.wav""

[HKEY_CURRENT_USER\AppEvents\Schemes\Apps\sapisvr\MisrecoSound\.current]
@=""C:\\Windows\\media\\Speech Misrecognition.wav""

[HKEY_CURRENT_USER\AppEvents\Schemes\Apps\sapisvr\PanelSound\.current]
@=""C:\\Windows\\media\\Speech Disambiguation.wav"""),
        new(
            "disable_autoplay",
            "Disable Autoplay",
            "Tweaks",
            "Hardware & Sound",
            true,
            @"[HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers]
""DisableAutoplay""=dword:00000001",
            @"[HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers]
""DisableAutoplay""=dword:00000000"),
        new(
            "mouse_pointers_scheme_none",
            "Mouse Pointers Scheme None",
            "Tweaks",
            "Hardware & Sound",
            true,
            @"[HKEY_CURRENT_USER\Control Panel\Cursors]
""AppStarting""=hex(2):00,00
""Arrow""=hex(2):00,00
""ContactVisualization""=dword:00000000
""Crosshair""=hex(2):00,00
""GestureVisualization""=dword:00000000
""Hand""=hex(2):00,00
""Help""=hex(2):00,00
""IBeam""=hex(2):00,00
""No""=hex(2):00,00
""NWPen""=hex(2):00,00
""Scheme Source""=dword:00000000
""SizeAll""=hex(2):00,00
""SizeNESW""=hex(2):00,00
""SizeNS""=hex(2):00,00
""SizeNWSE""=hex(2):00,00
""SizeWE""=hex(2):00,00
""UpArrow""=hex(2):00,00
""Wait""=hex(2):00,00
@=""""",
            @"[HKEY_CURRENT_USER\Control Panel\Cursors]
""AppStarting""=""C:\\Windows\\cursors\\aero_working.ani""
""Arrow""=""C:\\Windows\\cursors\\aero_arrow.cur""
""ContactVisualization""=dword:00000001
""Crosshair""=""""
""GestureVisualization""=dword:0000001f
""Hand""=""C:\\Windows\\cursors\\aero_link.cur""
""Help""=""C:\\Windows\\cursors\\aero_helpsel.cur""
""IBeam""=""""
""No""=""C:\\Windows\\cursors\\aero_unavail.cur""
""NWPen""=""C:\\Windows\\cursors\\aero_pen.cur""
""Scheme Source""=dword:00000002
""SizeAll""=""C:\\Windows\\cursors\\aero_move.cur""
""SizeNESW""=""C:\\Windows\\cursors\\aero_nesw.cur""
""SizeNS""=""C:\\Windows\\cursors\\aero_ns.cur""
""SizeNWSE""=""C:\\Windows\\cursors\\aero_nwse.cur""
""SizeWE""=""C:\\Windows\\cursors\\aero_ew.cur""
""UpArrow""=""C:\\Windows\\cursors\\aero_up.cur""
""Wait""=""C:\\Windows\\cursors\\aero_busy.ani""
@=""Windows Default"""),
        new(
            "disable_device_installation_settings",
            "Disable Device Installation Settings",
            "Tweaks",
            "Hardware & Sound",
            true,
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Device Metadata]
""PreventDeviceMetadataFromNetwork""=dword:00000001",
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Device Metadata]
""PreventDeviceMetadataFromNetwork""=dword:00000000"),
        new(
            "disable_allow_other_network_users_to_control",
            "Disable Allow Other Network Users To Control Or Disable The Shared Internet Connection",
            "Tweaks",
            "Network",
            true,
            @"[HKEY_LOCAL_MACHINE\System\ControlSet001\Control\Network\SharedAccessConnection]
""EnableControl""=dword:00000000",
            @"[HKEY_LOCAL_MACHINE\System\ControlSet001\Control\Network\SharedAccessConnection]
""EnableControl""=dword:00000001"),
        new(
            "disable_defragment_and_optimize_your_drives",
            "Disable Defragment And Optimize Your Drives",
            "Tweaks",
            "Visual Effects",
            true,
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Dfrg\TaskSettings]
""fAllVolumes""=dword:00000001
""fDeadlineEnabled""=dword:00000000
""fExclude""=dword:00000000
""fTaskEnabled""=dword:00000000
""fUpgradeRestored""=dword:00000001
""TaskFrequency""=dword:00000004
""Volumes""="" """,
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Dfrg\TaskSettings]
""fAllVolumes""=-
""fDeadlineEnabled""=-
""fExclude""=-
""fTaskEnabled""=-
""fUpgradeRestored""=-
""TaskFrequency""=-
""Volumes""=-"),
        new(
            "set_appearance_options_to_custom",
            "Set Appearance Options To Custom",
            "Tweaks",
            "Visual Effects",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects]
""VisualFXSetting""=dword:3",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects]
""VisualFXSetting""=-"),
        new(
            "enable_animate_controls_and_elements_inside_",
            "Enable Animate Controls And Elements Inside Windows (Disabled Breaks Instagram Scrolling) (+7 more)",
            "Tweaks",
            "Visual Effects",
            true,
            @"[HKEY_CURRENT_USER\Control Panel\Desktop]
""UserPreferencesMask""=hex(2):90,12,03,80,12,00,00,00",
            @"[HKEY_CURRENT_USER\Control Panel\Desktop]
""UserPreferencesMask""=hex(2):9e,1e,07,80,12,00,00,00"),
        new(
            "disable_animate_windows_when_minimizing_and_",
            "Disable Animate Windows When Minimizing And Maximizing",
            "Tweaks",
            "Visual Effects",
            true,
            @"[HKEY_CURRENT_USER\Control Panel\Desktop\WindowMetrics]
""MinAnimate""=""0""",
            @"[HKEY_CURRENT_USER\Control Panel\Desktop\WindowMetrics]
""MinAnimate""=""1"""),
        new(
            "disable_animations_in_the_taskbar",
            "Disable Animations In The Taskbar",
            "Tweaks",
            "Visual Effects",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced]
""TaskbarAnimations""=dword:0",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced]
""TaskbarAnimations""=dword:1"),
        new(
            "disable_enable_peek",
            "Disable Enable Peek",
            "Tweaks",
            "Visual Effects",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\DWM]
""EnableAeroPeek""=dword:0",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\DWM]
""EnableAeroPeek""=dword:1"),
        new(
            "disable_save_taskbar_thumbnail_previews",
            "Disable Save Taskbar Thumbnail Previews",
            "Tweaks",
            "Visual Effects",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\DWM]
""AlwaysHibernateThumbnails""=dword:0",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\DWM]
""AlwaysHibernateThumbnails""=dword:0"),
        new(
            "enable_show_thumbnails_instead_of_icons",
            "Enable Show Thumbnails Instead Of Icons",
            "Tweaks",
            "Visual Effects",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced]
""IconsOnly""=dword:0",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced]
""IconsOnly""=dword:0"),
        new(
            "disable_show_translucent_selection_rectangle",
            "Disable Show Translucent Selection Rectangle",
            "Tweaks",
            "Visual Effects",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced]
""ListviewAlphaSelect""=dword:0",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced]
""ListviewAlphaSelect""=dword:1"),
        new(
            "disable_show_window_contents_while_dragging",
            "Disable Show Window Contents While Dragging",
            "Tweaks",
            "Visual Effects",
            true,
            @"[HKEY_CURRENT_USER\Control Panel\Desktop]
""DragFullWindows""=""0""",
            @"[HKEY_CURRENT_USER\Control Panel\Desktop]
""DragFullWindows""=""1"""),
        new(
            "enable_smooth_edges_of_screen_fonts",
            "Enable Smooth Edges Of Screen Fonts",
            "Tweaks",
            "Visual Effects",
            true,
            @"[HKEY_CURRENT_USER\Control Panel\Desktop]
""FontSmoothing""=""2""",
            @"[HKEY_CURRENT_USER\Control Panel\Desktop]
""FontSmoothing""=""2"""),
        new(
            "disable_use_drop_shadows_for_icon_labels_on_",
            "Disable Use Drop Shadows For Icon Labels On The Desktop",
            "Tweaks",
            "Visual Effects",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced]
""ListviewShadow""=dword:0",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced]
""ListviewShadow""=dword:1"),
        new(
            "adjust_for_best_performance_of_programs",
            "Adjust For Best Performance Of Programs",
            "Tweaks",
            "Visual Effects",
            true,
            @"[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\PriorityControl]
""Win32PrioritySeparation""=dword:00000026",
            @"[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\PriorityControl]
""Win32PrioritySeparation""=dword:00000002"),
        new(
            "disable_remote_assistance",
            "Disable Remote Assistance",
            "Tweaks",
            "Visual Effects",
            true,
            @"[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Remote Assistance]
""fAllowToGetHelp""=dword:00000000",
            @"[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Remote Assistance]
""fAllowToGetHelp""=dword:00000001"),
        new(
            "disable_automatic_maintenance",
            "Disable Automatic Maintenance",
            "Tweaks",
            "Maintenance",
            true,
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\Maintenance]
""MaintenanceDisabled""=dword:00000001",
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\Maintenance]
""MaintenanceDisabled""=-"),
        new(
            "disable_report_problems",
            "Disable Report Problems",
            "Tweaks",
            "Maintenance",
            true,
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting]
""Disabled""=dword:00000001",
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting]
""Disabled""=-"),
        new(
            "disable_delivery_optimization",
            "Disable Delivery Optimization",
            "Debloat",
            "Windows Update",
            true,
            @"[HKEY_USERS\S-1-5-20\Software\Microsoft\Windows\CurrentVersion\DeliveryOptimization\Settings]
""DownloadMode""=dword:00000000",
            @"[HKEY_USERS\S-1-5-20\Software\Microsoft\Windows\CurrentVersion\DeliveryOptimization\Settings]
""DownloadMode""=-"),
        new(
            "disable_find_my_device",
            "Disable Find My Device",
            "Debloat",
            "Privacy",
            true,
            @"[HKEY_LOCAL_MACHINE\Software\Microsoft\MdmCommon\SettingValues]
""LocationSyncEnabled""=dword:00000000",
            @"[HKEY_LOCAL_MACHINE\Software\Microsoft\MdmCommon\SettingValues]
""LocationSyncEnabled""=dword:00000001"),
        new(
            "disable_show_me_notification_in_the_settings",
            "Disable Show Me Notification In The Settings App",
            "Debloat",
            "Privacy",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\SystemSettings\AccountNotifications]
""EnableAccountNotifications""=dword:00000000",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\SystemSettings\AccountNotifications]
""EnableAccountNotifications""=-"),
        new(
            "disable_tailored_experiences",
            "Disable Tailored Experiences",
            "Debloat",
            "Privacy",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CPSS\Store\TailoredExperiencesWithDiagnosticDataEnabled]
""Value""=dword:00000000
[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Privacy]
""TailoredExperiencesWithDiagnosticDataEnabled""=dword:00000000",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CPSS\Store\TailoredExperiencesWithDiagnosticDataEnabled]
""Value""=dword:00000001

[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Privacy]
""TailoredExperiencesWithDiagnosticDataEnabled""=dword:00000001"),
        new(
            "disable_location",
            "Disable Location",
            "Debloat",
            "Privacy",
            true,
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location]
""Value""=""Deny""",
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location]
""Value""=""Allow"""),
        new(
            "disable_allow_location_override",
            "Disable Allow Location Override",
            "Debloat",
            "Privacy",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CPSS\Store\UserLocationOverridePrivacySetting]
""Value""=dword:00000000",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CPSS\Store\UserLocationOverridePrivacySetting]
""Value""=dword:00000001"),
        new(
            "disable_notify_when_apps_request_location",
            "Disable Notify When Apps Request Location",
            "Debloat",
            "Privacy",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location]
""ShowGlobalPrompts""=dword:00000000",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location]
""ShowGlobalPrompts""=-"),
        new(
            "enable_camera",
            "Enable Camera",
            "Debloat",
            "Privacy",
            true,
            @"[HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\webcam]
""Value""=""Allow""",
            @"[HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\webcam]
""Value""=""Allow"""),
        new(
            "enable_microphone",
            "Enable Microphone",
            "Debloat",
            "Privacy",
            true,
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\microphone]
""Value""=""Allow""",
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\microphone]
""Value""=""Allow"""),
        new(
            "disable_voice_activation",
            "Disable Voice Activation",
            "Debloat",
            "Privacy",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Speech_OneCore\Settings\VoiceActivation\UserPreferenceForAllApps]
""AgentActivationEnabled""=dword:00000000
[HKEY_CURRENT_USER\SOFTWARE\Microsoft\Speech_OneCore\Settings\VoiceActivation\UserPreferenceForAllApps]
""AgentActivationLastUsed""=dword:00000000",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Speech_OneCore\Settings\VoiceActivation\UserPreferenceForAllApps]
""AgentActivationEnabled""=-
""AgentActivationLastUsed""=-"),
        new(
            "disable_notifications",
            "Disable Notifications",
            "Debloat",
            "Notifications",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Notifications\Settings]
""NOC_GLOBAL_SETTING_ALLOW_NOTIFICATION_SOUND""=dword:00000000
""NOC_GLOBAL_SETTING_ALLOW_CRITICAL_TOASTS_ABOVE_LOCK""=dword:00000000
""NOC_GLOBAL_SETTING_ALLOW_TOASTS_ABOVE_LOCK""=dword:00000000
[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Notifications\Settings\Microsoft.SkyDrive.Desktop]
""Enabled""=dword:00000000
[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Notifications\Settings\Windows.SystemToast.AutoPlay]
""Enabled""=dword:00000000
[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Notifications\Settings\Windows.SystemToast.SecurityAndMaintenance]
""Enabled""=dword:00000000
[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Notifications\Settings\windows.immersivecontrolpanel_cw5n1h2txyewy!microsoft.windows.immersivecontrolpanel]
""Enabled""=dword:00000000
[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Notifications\Settings\Windows.SystemToast.CapabilityAccess]
""Enabled""=dword:00000000
[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Notifications\Settings\Windows.SystemToast.StartupApp]
""Enabled""=dword:00000000
[HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\UserProfileEngagement]
""ScoobeSystemSettingEnabled""=dword:00000000",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Notifications\Settings]
""NOC_GLOBAL_SETTING_ALLOW_NOTIFICATION_SOUND""=-
""NOC_GLOBAL_SETTING_ALLOW_CRITICAL_TOASTS_ABOVE_LOCK""=-
""NOC_GLOBAL_SETTING_ALLOW_TOASTS_ABOVE_LOCK""=-

[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Notifications\Settings\Microsoft.SkyDrive.Desktop]
""Enabled""=-

[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Notifications\Settings\Windows.SystemToast.AutoPlay]
""Enabled""=-

[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Notifications\Settings\Windows.SystemToast.SecurityAndMaintenance]
""Enabled""=-

[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Notifications\Settings\windows.immersivecontrolpanel_cw5n1h2txyewy!microsoft.windows.immersivecontrolpanel]
""Enabled""=-

[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Notifications\Settings\Windows.SystemToast.CapabilityAccess]
""Enabled""=-

[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Notifications\Settings\Windows.SystemToast.StartupApp]
""Enabled""=dword:00000000

[HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\UserProfileEngagement]
""ScoobeSystemSettingEnabled""=-"),
        new(
            "disable_account_info",
            "Disable Account Info",
            "Debloat",
            "Privacy",
            true,
            @"[HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\userAccountInformation]
""Value""=""Deny""",
            @"[HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\userAccountInformation]
""Value""=""Allow"""),
        new(
            "disable_contacts",
            "Disable Contacts",
            "Debloat",
            "Privacy",
            true,
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\contacts]
""Value""=""Deny""",
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\contacts]
""Value""=""Allow"""),
        new(
            "disable_calendar",
            "Disable Calendar",
            "Debloat",
            "Privacy",
            true,
            @"[HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\appointments]
""Value""=""Deny""",
            @"[HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\appointments]
""Value""=""Allow"""),
        new(
            "disable_phone_calls",
            "Disable Phone Calls",
            "Debloat",
            "Privacy",
            true,
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\phoneCall]
""Value""=""Deny""",
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\phoneCall]
""Value""=""Allow"""),
        new(
            "disable_call_history",
            "Disable Call History",
            "Debloat",
            "Privacy",
            true,
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\phoneCallHistory]
""Value""=""Deny""",
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\phoneCallHistory]
""Value""=""Allow"""),
        new(
            "disable_email",
            "Disable Email",
            "Debloat",
            "Privacy",
            true,
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\email]
""Value""=""Deny""",
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\email]
""Value""=""Allow"""),
        new(
            "disable_tasks",
            "Disable Tasks",
            "Debloat",
            "Privacy",
            true,
            @"[HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\userDataTasks]
""Value""=""Deny""",
            @"[HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\userDataTasks]
""Value""=""Allow"""),
        new(
            "disable_messaging",
            "Disable Messaging",
            "Debloat",
            "Privacy",
            true,
            @"[HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\chat]
""Value""=""Deny""",
            @"[HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\chat]
""Value""=""Allow"""),
        new(
            "disable_radios",
            "Disable Radios",
            "Debloat",
            "Privacy",
            true,
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\radios]
""Value""=""Deny""",
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\radios]
""Value""=""Allow"""),
        new(
            "disable_other_devices",
            "Disable Other Devices",
            "Debloat",
            "Privacy",
            true,
            @"[HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\bluetoothSync]
""Value""=""Deny""",
            @"[HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\bluetoothSync]
""Value""=-"),
        new(
            "disable_app_diagnostics",
            "Disable App Diagnostics",
            "Debloat",
            "Privacy",
            true,
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\appDiagnostics]
""Value""=""Deny""",
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\appDiagnostics]
""Value""=""Allow"""),
        new(
            "disable_documents",
            "Disable Documents",
            "Debloat",
            "Privacy",
            true,
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\documentsLibrary]
""Value""=""Deny""",
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\documentsLibrary]
""Value""=""Allow"""),
        new(
            "disable_downloads_folder",
            "Disable Downloads Folder",
            "Debloat",
            "Privacy",
            true,
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\downloadsFolder]
""Value""=""Deny""",
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\downloadsFolder]
""Value""=-"),
        new(
            "disable_music_library",
            "Disable Music Library",
            "Debloat",
            "Privacy",
            true,
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\musicLibrary]
""Value""=""Deny""",
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\musicLibrary]
""Value""=""Allow"""),
        new(
            "disable_pictures",
            "Disable Pictures",
            "Debloat",
            "Privacy",
            true,
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\picturesLibrary]
""Value""=""Deny""",
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\picturesLibrary]
""Value""=""Deny"""),
        new(
            "disable_videos",
            "Disable Videos",
            "Debloat",
            "Privacy",
            true,
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\videosLibrary]
""Value""=""Deny""",
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\videosLibrary]
""Value""=""Allow"""),
        new(
            "disable_file_system",
            "Disable File System",
            "Debloat",
            "Privacy",
            true,
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\broadFileSystemAccess]
""Value""=""Deny""",
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\broadFileSystemAccess]
""Value""=""Allow"""),
        new(
            "disable_text_and_image_generation",
            "Disable Text And Image Generation",
            "Debloat",
            "Privacy",
            true,
            @"[HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\systemAIModels]
""Value""=""Deny""",
            @"[HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\systemAIModels]
""Value""=""Allow"""),
        new(
            "disable_passkey_access",
            "Disable Passkey Access",
            "Debloat",
            "Privacy",
            true,
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\passkeys]
""Value""=""Deny""",
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\passkeys]
""Value""=""Allow"""),
        new(
            "disable_passkey_autofill_access",
            "Disable Passkey Autofill Access",
            "Debloat",
            "Privacy",
            true,
            @"[HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\passkeysEnumeration]
""Value""=""Deny""",
            @"[HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\passkeysEnumeration]
""Value""=""Allow"""),
        new(
            "disable_let_websites_show_me_locally_relevan",
            "Disable Let Websites Show Me Locally Relevant Content By Accessing My Language List",
            "Debloat",
            "Privacy",
            true,
            @"[HKEY_CURRENT_USER\Control Panel\International\User Profile]
""HttpAcceptLanguageOptOut""=dword:00000001",
            @"[HKEY_CURRENT_USER\Control Panel\International\User Profile]
""HttpAcceptLanguageOptOut""=-"),
        new(
            "disable_let_windows_improve_start_and_search",
            "Disable Let Windows Improve Start And Search Results By Tracking App Launches",
            "Debloat",
            "Privacy",
            true,
            @"[HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\EdgeUI]
""DisableMFUTracking""=dword:00000001
[HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\EdgeUI]
""DisableMFUTracking""=dword:00000001",
            @"[HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\EdgeUI]
""DisableMFUTracking""=-

[HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\EdgeUI]
""DisableMFUTracking""=-"),
        new(
            "disable_personal_inking_and_typing_dictionar",
            "Disable Personal Inking And Typing Dictionary",
            "Debloat",
            "Privacy",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\InputPersonalization]
""RestrictImplicitInkCollection""=dword:00000001
""RestrictImplicitTextCollection""=dword:00000001
[HKEY_CURRENT_USER\Software\Microsoft\InputPersonalization\TrainedDataStore]
""HarvestContacts""=dword:00000000
[HKEY_CURRENT_USER\Software\Microsoft\Personalization\Settings]
""AcceptedPrivacyPolicy""=dword:00000000",
            @"[HKEY_CURRENT_USER\Software\Microsoft\InputPersonalization]
""RestrictImplicitInkCollection""=dword:00000000
""RestrictImplicitTextCollection""=dword:00000000

[HKEY_CURRENT_USER\Software\Microsoft\InputPersonalization\TrainedDataStore]
""HarvestContacts""=dword:00000001

[HKEY_CURRENT_USER\Software\Microsoft\Personalization\Settings]
""AcceptedPrivacyPolicy""=dword:00000001"),
        new(
            "disable_sending_required_data",
            "Disable Sending Required Data",
            "Debloat",
            "Privacy",
            true,
            @"[HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\DataCollection]
""AllowTelemetry""=dword:00000000",
            @"[HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\DataCollection]
""AllowTelemetry""=-"),
        new(
            "feedback_frequency_never",
            "Feedback Frequency Never",
            "Debloat",
            "Privacy",
            true,
            @"[HKEY_CURRENT_USER\SOFTWARE\Microsoft\Siuf\Rules]
""NumberOfSIUFInPeriod""=dword:00000000
""PeriodInNanoSeconds""=-",
            @"[HKEY_CURRENT_USER\SOFTWARE\Microsoft\Siuf\Rules]
""NumberOfSIUFInPeriod""=-
""PeriodInNanoSeconds""=-"),
        new(
            "disable_store_my_activity_history_on_this_de",
            "Disable Store My Activity History On This Device",
            "Debloat",
            "Privacy",
            true,
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System]
""PublishUserActivities""=dword:00000000",
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System]
""PublishUserActivities""=-"),
        new(
            "disable_search_highlights",
            "Disable Search Highlights",
            "Debloat",
            "Search",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\SearchSettings]
""IsDynamicSearchBoxEnabled""=dword:00000000",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\SearchSettings]
""IsDynamicSearchBoxEnabled""=-"),
        new(
            "disable_safe_search",
            "Disable Safe Search",
            "Debloat",
            "Search",
            true,
            @"[HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\SearchSettings]
""SafeSearchMode""=dword:00000000",
            @"[HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\SearchSettings]
""SafeSearchMode""=-"),
        new(
            "disable_cloud_content_search_for_work_or_sch",
            "Disable Cloud Content Search For Work Or School Account",
            "Debloat",
            "Search",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\SearchSettings]
""IsAADCloudSearchEnabled""=dword:00000000",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\SearchSettings]
""IsAADCloudSearchEnabled""=-"),
        new(
            "disable_cloud_content_search_for_microsoft_a",
            "Disable Cloud Content Search For Microsoft Account",
            "Debloat",
            "Search",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\SearchSettings]
""IsMSACloudSearchEnabled""=dword:00000000",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\SearchSettings]
""IsMSACloudSearchEnabled""=-"),
        new(
            "disable_magnifier_settings",
            "Disable Magnifier Settings",
            "Appearance",
            "Ease of Access",
            true,
            @"[HKEY_CURRENT_USER\SOFTWARE\Microsoft\ScreenMagnifier]
""FollowCaret""=dword:00000000
""FollowNarrator""=dword:00000000
""FollowMouse""=dword:00000000
""FollowFocus""=dword:00000000",
            @"[HKEY_CURRENT_USER\SOFTWARE\Microsoft\ScreenMagnifier]
""FollowCaret""=-
""FollowNarrator""=-
""FollowMouse""=-
""FollowFocus""=-"),
        new(
            "disable_narrator_settings",
            "Disable Narrator Settings",
            "Appearance",
            "Ease of Access",
            true,
            @"[HKEY_CURRENT_USER\SOFTWARE\Microsoft\Narrator]
""IntonationPause""=dword:00000000
""ReadHints""=dword:00000000
""ErrorNotificationType""=dword:00000000
""EchoChars""=dword:00000000
""EchoWords""=dword:00000000
[HKEY_CURRENT_USER\SOFTWARE\Microsoft\Narrator\NarratorHome]
""MinimizeType""=dword:00000000
""AutoStart""=dword:00000000
[HKEY_CURRENT_USER\SOFTWARE\Microsoft\Narrator\NoRoam]
""EchoToggleKeys""=dword:00000000",
            @"[HKEY_CURRENT_USER\SOFTWARE\Microsoft\Narrator]
""IntonationPause""=-
""ReadHints""=-
""ErrorNotificationType""=-
""EchoChars""=-
""EchoWords""=-

[HKEY_CURRENT_USER\SOFTWARE\Microsoft\Narrator\NarratorHome]
""MinimizeType""=-
""AutoStart""=-

[HKEY_CURRENT_USER\SOFTWARE\Microsoft\Narrator\NoRoam]
""EchoToggleKeys""=-"),
        new(
            "disable_use_the_print_screen_key_to_open_scr",
            "Disable Use The Print Screen Key To Open Screen Capture",
            "Appearance",
            "Ease of Access",
            true,
            @"[HKEY_CURRENT_USER\Control Panel\Keyboard]
""PrintScreenKeyForSnippingEnabled""=dword:00000000",
            @"[HKEY_CURRENT_USER\Control Panel\Keyboard]
""PrintScreenKeyForSnippingEnabled""=-"),
        new(
            "disable_game_bar",
            "Disable Game Bar",
            "Tweaks",
            "Gaming",
            true,
            @"[HKEY_CURRENT_USER\System\GameConfigStore]
""GameDVR_Enabled""=dword:00000000
[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\GameDVR]
""AppCaptureEnabled""=dword:00000000",
            @"[HKEY_CURRENT_USER\System\GameConfigStore]
""GameDVR_Enabled""=dword:00000000

[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\GameDVR]
""AppCaptureEnabled""=-"),
        new(
            "disable_enable_open_xbox_game_bar_using_game",
            "Disable Enable Open Xbox Game Bar Using Game Controller",
            "Tweaks",
            "Gaming",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\GameBar]
""UseNexusForGameBarEnabled""=dword:00000000",
            @"[HKEY_CURRENT_USER\Software\Microsoft\GameBar]
""UseNexusForGameBarEnabled""=-"),
        new(
            "disable_use_view_menu_as_guide_button_in_app",
            "Disable Use View + Menu As Guide Button In Apps",
            "Tweaks",
            "Gaming",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\GameBar]
""GamepadNexusChordEnabled""=dword:00000000",
            @"[HKEY_CURRENT_USER\Software\Microsoft\GameBar]
""GamepadNexusChordEnabled""=-"),
        new(
            "other_settings",
            "Other Settings",
            "Tweaks",
            "Gaming",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\GameDVR]
""AudioEncodingBitrate""=dword:0001f400
""AudioCaptureEnabled""=dword:00000000
""CustomVideoEncodingBitrate""=dword:003d0900
""CustomVideoEncodingHeight""=dword:000002d0
""CustomVideoEncodingWidth""=dword:00000500
""HistoricalBufferLength""=dword:0000001e
""HistoricalBufferLengthUnit""=dword:00000001
""HistoricalCaptureEnabled""=dword:00000000
""HistoricalCaptureOnBatteryAllowed""=dword:00000001
""HistoricalCaptureOnWirelessDisplayAllowed""=dword:00000001
""MaximumRecordLength""=hex(b):00,D0,88,C3,10,00,00,00
""VideoEncodingBitrateMode""=dword:00000002
""VideoEncodingResolutionMode""=dword:00000002
""VideoEncodingFrameRateMode""=dword:00000000
""EchoCancellationEnabled""=dword:00000001
""CursorCaptureEnabled""=dword:00000000
""VKToggleGameBar""=dword:00000000
""VKMToggleGameBar""=dword:00000000
""VKSaveHistoricalVideo""=dword:00000000
""VKMSaveHistoricalVideo""=dword:00000000
""VKToggleRecording""=dword:00000000
""VKMToggleRecording""=dword:00000000
""VKTakeScreenshot""=dword:00000000
""VKMTakeScreenshot""=dword:00000000
""VKToggleRecordingIndicator""=dword:00000000
""VKMToggleRecordingIndicator""=dword:00000000
""VKToggleMicrophoneCapture""=dword:00000000
""VKMToggleMicrophoneCapture""=dword:00000000
""VKToggleCameraCapture""=dword:00000000
""VKMToggleCameraCapture""=dword:00000000
""VKToggleBroadcast""=dword:00000000
""VKMToggleBroadcast""=dword:00000000
""MicrophoneCaptureEnabled""=dword:00000000
""SystemAudioGain""=hex(b):10,27,00,00,00,00,00,00
""MicrophoneGain""=hex(b):10,27,00,00,00,00,00,00",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\GameDVR]
""AudioEncodingBitrate""=-
""AudioCaptureEnabled""=-
""CustomVideoEncodingBitrate""=-
""CustomVideoEncodingHeight""=-
""CustomVideoEncodingWidth""=-
""HistoricalBufferLength""=-
""HistoricalBufferLengthUnit""=-
""HistoricalCaptureEnabled""=-
""HistoricalCaptureOnBatteryAllowed""=-
""HistoricalCaptureOnWirelessDisplayAllowed""=-
""MaximumRecordLength""=-
""VideoEncodingBitrateMode""=-
""VideoEncodingResolutionMode""=-
""VideoEncodingFrameRateMode""=-
""EchoCancellationEnabled""=-
""CursorCaptureEnabled""=-
""VKToggleGameBar""=-
""VKMToggleGameBar""=-
""VKSaveHistoricalVideo""=-
""VKMSaveHistoricalVideo""=-
""VKToggleRecording""=-
""VKMToggleRecording""=-
""VKTakeScreenshot""=-
""VKMTakeScreenshot""=-
""VKToggleRecordingIndicator""=-
""VKMToggleRecordingIndicator""=-
""VKToggleMicrophoneCapture""=-
""VKMToggleMicrophoneCapture""=-
""VKToggleCameraCapture""=-
""VKMToggleCameraCapture""=-
""VKToggleBroadcast""=-
""VKMToggleBroadcast""=-
""MicrophoneCaptureEnabled""=-
""SystemAudioGain""=-
""MicrophoneGain""=-"),
        new(
            "disable_show_the_voice_typing_mic_button",
            "Disable Show The Voice Typing Mic Button",
            "Appearance",
            "Typing & Input",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\input\Settings]
""IsVoiceTypingKeyEnabled""=dword:00000000",
            @"[HKEY_CURRENT_USER\Software\Microsoft\input\Settings]
""IsVoiceTypingKeyEnabled""=-"),
        new(
            "disable_capitalize_the_first_letter_of_each_",
            "Disable Capitalize The First Letter Of Each Sentence (+2 more)",
            "Appearance",
            "Typing & Input",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\TabletTip\1.7]
""EnableAutoShiftEngage""=dword:00000000
""EnableKeyAudioFeedback""=dword:00000000
""EnableDoubleTapSpace""=dword:00000000",
            @"[HKEY_CURRENT_USER\Software\Microsoft\TabletTip\1.7]
""EnableAutoShiftEngage""=-
""EnableKeyAudioFeedback""=-
""EnableDoubleTapSpace""=-"),
        new(
            "disable_typing_insights",
            "Disable Typing Insights",
            "Appearance",
            "Typing & Input",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\input\Settings]
""InsightsEnabled""=dword:00000000",
            @"[HKEY_CURRENT_USER\Software\Microsoft\input\Settings]
""InsightsEnabled""=-"),
        new(
            "show_the_touch_keyboard_never",
            "Show The Touch Keyboard Never",
            "Appearance",
            "Typing & Input",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\TabletTip\1.7]
""TouchKeyboardTapInvoke""=dword:00000000",
            @"[HKEY_CURRENT_USER\Software\Microsoft\TabletTip\1.7]
""TouchKeyboardTapInvoke""=-"),
        new(
            "disable_language_bar",
            "Disable Language Bar",
            "Appearance",
            "Typing & Input",
            true,
            @"[HKEY_CURRENT_USER\SOFTWARE\Microsoft\CTF\LangBar]
""ExtraIconsOnMinimized""=dword:00000000
""Label""=dword:00000000
""ShowStatus""=dword:00000003
""Transparency""=dword:000000ff",
            @"[HKEY_CURRENT_USER\SOFTWARE\Microsoft\CTF\LangBar]
""ExtraIconsOnMinimized""=-
""Label""=-
""ShowStatus""=-
""Transparency""=-"),
        new(
            "disable_language_hotkey",
            "Disable Language Hotkey",
            "Appearance",
            "Typing & Input",
            true,
            @"[HKEY_CURRENT_USER\Keyboard Layout\Toggle]
""Language Hotkey""=""3""
""Hotkey""=""3""
""Layout Hotkey""=""3""",
            @"[HKEY_CURRENT_USER\Keyboard Layout\Toggle]
""Language Hotkey""=-
""Hotkey""=-
""Layout Hotkey""=-"),
        new(
            "disable_calendar_events",
            "Disable Calendar Events",
            "Appearance",
            "Typing & Input",
            true,
            @"[HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Search]
""GleamEnabled""=dword:00000000
""WeatherEnabled""=dword:00000000
""HolidayEnabled""=dword:00000000",
            @"[HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Search]
""GleamEnabled""=-
""WeatherEnabled""=-
""HolidayEnabled""=-"),
        new(
            "disable_dynamic_lock",
            "Disable Dynamic Lock",
            "System",
            "Accounts & Sign-in",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows NT\CurrentVersion\Winlogon]
""EnableGoodbye""=dword:00000000",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows NT\CurrentVersion\Winlogon]
""EnableGoodbye""=-"),
        new(
            "disable_use_my_sign_in_info_after_restart",
            "Disable Use My Sign In Info After Restart",
            "System",
            "Accounts & Sign-in",
            true,
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System]
""DisableAutomaticRestartSignOn""=dword:00000001",
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System]
""DisableAutomaticRestartSignOn""=-"),
        new(
            "disable_for_improved_security_only_allow_win",
            "Disable For Improved Security, Only Allow Windows Hello Sign-In",
            "System",
            "Accounts & Sign-in",
            true,
            @"[HKEY_LOCAL_MACHINE\Software\Microsoft\Windows NT\CurrentVersion\PasswordLess\Device]
""DevicePasswordLessBuildVersion""=dword:00000000
""DevicePasswordLessUpdateType""=dword:00000001",
            @"[HKEY_LOCAL_MACHINE\Software\Microsoft\Windows NT\CurrentVersion\PasswordLess\Device]
""DevicePasswordLessBuildVersion""=dword:00000002
""DevicePasswordLessUpdateType""=-"),
        new(
            "disable_windows_backup",
            "Disable Windows Backup",
            "System",
            "Accounts & Sign-in",
            true,
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync]
""DisableAccessibilitySettingSync""=dword:00000002
""DisableAccessibilitySettingSyncUserOverride""=dword:00000001
""DisableAppSyncSettingSync""=dword:00000002
""DisableAppSyncSettingSyncUserOverride""=dword:00000001
""DisableApplicationSettingSync""=dword:00000002
""DisableApplicationSettingSyncUserOverride""=dword:00000001
""DisableCredentialsSettingSync""=dword:00000002
""DisableCredentialsSettingSyncUserOverride""=dword:00000001
""DisableDesktopThemeSettingSync""=dword:00000002
""DisableDesktopThemeSettingSyncUserOverride""=dword:00000001
""DisableLanguageSettingSync""=dword:00000002
""DisableLanguageSettingSyncUserOverride""=dword:00000001
""DisablePersonalizationSettingSync""=dword:00000002
""DisablePersonalizationSettingSyncUserOverride""=dword:00000001
""DisableSettingSync""=dword:00000002
""DisableSettingSyncUserOverride""=dword:00000001
""DisableStartLayoutSettingSync""=dword:00000002
""DisableStartLayoutSettingSyncUserOverride""=dword:00000001
""DisableSyncOnPaidNetwork""=dword:00000001
""DisableWebBrowserSettingSync""=dword:00000002
""DisableWebBrowserSettingSyncUserOverride""=dword:00000001
""DisableWindowsSettingSync""=dword:00000002
""DisableWindowsSettingSyncUserOverride""=dword:00000001
""EnableWindowsBackup""=dword:00000000",
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync]
""DisableAccessibilitySettingSync""=-
""DisableAccessibilitySettingSyncUserOverride""=-
""DisableAppSyncSettingSync""=-
""DisableAppSyncSettingSyncUserOverride""=-
""DisableApplicationSettingSync""=-
""DisableApplicationSettingSyncUserOverride""=-
""DisableCredentialsSettingSync""=-
""DisableCredentialsSettingSyncUserOverride""=-
""DisableDesktopThemeSettingSync""=-
""DisableDesktopThemeSettingSyncUserOverride""=-
""DisableLanguageSettingSync""=-
""DisableLanguageSettingSyncUserOverride""=-
""DisablePersonalizationSettingSync""=-
""DisablePersonalizationSettingSyncUserOverride""=-
""DisableSettingSync""=-
""DisableSettingSyncUserOverride""=-
""DisableStartLayoutSettingSync""=-
""DisableStartLayoutSettingSyncUserOverride""=-
""DisableSyncOnPaidNetwork""=-
""DisableWebBrowserSettingSync""=-
""DisableWebBrowserSettingSyncUserOverride""=-
""DisableWindowsSettingSync""=-
""DisableWindowsSettingSyncUserOverride""=-
""EnableWindowsBackup""=-"),
        new(
            "disable_automatically_update_maps",
            "Disable Automatically Update Maps",
            "Debloat",
            "Apps",
            true,
            @"[HKEY_LOCAL_MACHINE\SYSTEM\Maps]
""AutoUpdateEnabled""=dword:00000000",
            @"[HKEY_LOCAL_MACHINE\SYSTEM\Maps]
""AutoUpdateEnabled""=-"),
        new(
            "disable_archive_apps",
            "Disable Archive Apps",
            "Debloat",
            "Apps",
            true,
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Appx]
""AllowAutomaticAppArchiving""=dword:00000000",
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Appx]
""AllowAutomaticAppArchiving""=-"),
        new(
            "hide_recycle_bin_from_desktop",
            "Hide Recycle Bin From Desktop",
            "Appearance",
            "Personalization",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\ClassicStartMenu]
""{645FF040-5081-101B-9F08-00AA002F954E}""=dword:00000001
[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel]
""{645FF040-5081-101B-9F08-00AA002F954E}""=dword:00000001",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\ClassicStartMenu]
""{645FF040-5081-101B-9F08-00AA002F954E}""=-

[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel]
""{645FF040-5081-101B-9F08-00AA002F954E}""=-"),
        new(
            "always_hide_most_used_list_in_start_menu",
            "Always Hide Most Used List In Start Menu",
            "Appearance",
            "Personalization",
            true,
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Explorer]
""ShowOrHideMostUsedApps""=dword:00000002
[HKEY_CURRENT_USER\SOFTWARE\Policies\Microsoft\Windows\Explorer]
""ShowOrHideMostUsedApps""=-
[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer]
""NoStartMenuMFUprogramsList""=-
""NoInstrumentation""=-
[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer]
""NoStartMenuMFUprogramsList""=-
""NoInstrumentation""=-",
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Explorer]
""ShowOrHideMostUsedApps""=-

[HKEY_CURRENT_USER\SOFTWARE\Policies\Microsoft\Windows\Explorer]
""ShowOrHideMostUsedApps""=-

[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer]
""NoStartMenuMFUprogramsList""=-
""NoInstrumentation""=-

[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer]
""NoStartMenuMFUprogramsList""=-
""NoInstrumentation""=-"),
        new(
            "start_menu_hide_recommended",
            "Start Menu Hide Recommended",
            "Appearance",
            "Personalization",
            true,
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\PolicyManager\current\device\Start]
""HideRecommendedSection""=dword:00000001
[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\PolicyManager\current\device\Education]
""IsEducationEnvironment""=dword:00000001
[HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Explorer]
""HideRecommendedSection""=dword:00000001",
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\PolicyManager\current\device\Start]
""HideRecommendedSection""=-

[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\PolicyManager\current\device\Education]
""IsEducationEnvironment""=-

[HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Explorer]
""HideRecommendedSection""=-"),
        new(
            "more_pins_personalization_start",
            "More Pins Personalization Start",
            "Appearance",
            "Personalization",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced]
""Start_Layout""=dword:00000001",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced]
""Start_Layout""=-"),
        new(
            "disable_show_recently_added_apps",
            "Disable Show Recently Added Apps",
            "Appearance",
            "Personalization",
            true,
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Explorer]
""HideRecentlyAddedApps""=dword:00000001
[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer]
""HideRecentlyAddedApps""=dword:00000001",
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Explorer]
""HideRecentlyAddedApps""=-

[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer]
""HideRecentlyAddedApps""=-"),
        new(
            "disable_show_account_related_notifications",
            "Disable Show Account-Related Notifications",
            "Appearance",
            "Personalization",
            true,
            @"[HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced]
""Start_AccountNotifications""=dword:00000000",
            @"[HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced]
""Start_AccountNotifications""=-"),
        new(
            "disable_show_websites_from_your_browsing_his",
            "Disable Show Websites From Your Browsing History",
            "Appearance",
            "Personalization",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced]
""Start_RecoPersonalizedSites""=dword:00000000",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced]
""Start_RecoPersonalizedSites""=-"),
        new(
            "disable_show_recently_opened_items_in_start_",
            "Disable Show Recently Opened Items In Start, Jump Lists And File Explorer",
            "Appearance",
            "Personalization",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced]
""Start_TrackDocs""=dword:00000000",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced]
""Start_TrackDocs""=-"),
        new(
            "touch_keyboard_never",
            "Touch Keyboard Never",
            "Appearance",
            "Personalization",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\TabletTip\1.7]
""TipbandDesiredVisibility""=dword:00000000",
            @"[HKEY_CURRENT_USER\Software\Microsoft\TabletTip\1.7]
""TipbandDesiredVisibility""=-"),
        new(
            "show_smaller_taskbar_icons_never",
            "Show Smaller Taskbar Icons Never",
            "Appearance",
            "Personalization",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced]
""IconSizePreference""=dword:00000001",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced]
""IconSizePreference""=-"),
        new(
            "disable_desktop_preview",
            "Disable Desktop Preview",
            "Appearance",
            "Personalization",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced]
""TaskbarSd""=dword:00000000",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced]
""TaskbarSd""=-"),
        new(
            "remove_resume_from_taskbar",
            "Remove Resume From Taskbar",
            "Appearance",
            "Personalization",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced]
""IsEnabled""=dword:00000000",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced]
""IsEnabled""=-"),
        new(
            "remove_meet_now",
            "Remove Meet Now",
            "Appearance",
            "Personalization",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer]
""HideSCAMeetNow""=dword:00000001",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer]
""HideSCAMeetNow""=-"),
        new(
            "remove_news_and_interests",
            "Remove News And Interests",
            "Appearance",
            "Personalization",
            true,
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Feeds]
""EnableFeeds""=dword:00000000",
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Feeds]
""EnableFeeds""=-"),
        new(
            "show_all_taskbar_icons",
            "Show All Taskbar Icons",
            "Appearance",
            "Personalization",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer]
""EnableAutoTray""=dword:00000000",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer]
""EnableAutoTray""=-"),
        new(
            "remove_security_taskbar_icon",
            "Remove Security Taskbar Icon",
            "Appearance",
            "Personalization",
            true,
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run]
""SecurityHealth""=hex(3):07,00,00,00,05,DB,8A,69,8A,49,D9,01",
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run]
""SecurityHealth""=hex:04,00,00,00,00,00,00,00,00,00,00,00"),
        new(
            "disable_use_dynamic_lighting_on_my_devices",
            "Disable Use Dynamic Lighting On My Devices",
            "Appearance",
            "Personalization",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Lighting]
""AmbientLightingEnabled""=dword:00000000",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Lighting]
""AmbientLightingEnabled""=dword:00000001"),
        new(
            "disable_compatible_apps_in_the_foreground_al",
            "Disable Compatible Apps In The Foreground Always Control Lighting",
            "Appearance",
            "Personalization",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Lighting]
""ControlledByForegroundApp""=dword:00000000",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Lighting]
""ControlledByForegroundApp""=-"),
        new(
            "disable_match_my_windows_accent_color",
            "Disable Match My Windows Accent Color",
            "Appearance",
            "Personalization",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Lighting]
""UseSystemAccentColor""=dword:00000000",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Lighting]
""UseSystemAccentColor""=dword:00000001"),
        new(
            "disable_show_key_background",
            "Disable Show Key Background",
            "Appearance",
            "Personalization",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\TabletTip\1.7]
""IsKeyBackgroundEnabled""=dword:00000000",
            @"[HKEY_CURRENT_USER\Software\Microsoft\TabletTip\1.7]
""IsKeyBackgroundEnabled""=-"),
        new(
            "disable_show_recommendations_for_tips_shortc",
            "Disable Show Recommendations For Tips Shortcuts New Apps And More",
            "Appearance",
            "Personalization",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced]
""Start_IrisRecommendations""=dword:00000000
[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Start]
""ShowRecentList""=dword:00000000",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced]
""Start_IrisRecommendations""=-

[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Start]
""ShowRecentList""=-"),
        new(
            "disable_share_any_window_from_my_taskbar",
            "Disable Share Any Window From My Taskbar",
            "Appearance",
            "Personalization",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced]
""TaskbarSn""=dword:00000000",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced]
""TaskbarSn""=dword:00000000"),
        new(
            "disable_device_usage",
            "Disable Device Usage",
            "Appearance",
            "Personalization",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CloudExperienceHost\Intent\developer]
""Intent""=dword:00000000
""Priority""=dword:00000000
[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CloudExperienceHost\Intent\gaming]
""Intent""=dword:00000000
""Priority""=dword:00000000
[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CloudExperienceHost\Intent\family]
""Intent""=dword:00000000
""Priority""=dword:00000000
[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CloudExperienceHost\Intent\creative]
""Intent""=dword:00000000
""Priority""=dword:00000000
[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CloudExperienceHost\Intent\schoolwork]
""Intent""=dword:00000000
""Priority""=dword:00000000
[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CloudExperienceHost\Intent\entertainment]
""Intent""=dword:00000000
""Priority""=dword:00000000
[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CloudExperienceHost\Intent\business]
""Intent""=dword:00000000
""Priority""=dword:00000000",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CloudExperienceHost\Intent\developer]
""Intent""=dword:00000000
""Priority""=dword:00000000

[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CloudExperienceHost\Intent\gaming]
""Intent""=dword:00000000
""Priority""=dword:00000000

[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CloudExperienceHost\Intent\family]
""Intent""=dword:00000000
""Priority""=dword:00000000

[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CloudExperienceHost\Intent\creative]
""Intent""=dword:00000000
""Priority""=dword:00000000

[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CloudExperienceHost\Intent\schoolwork]
""Intent""=dword:00000000
""Priority""=dword:00000000

[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CloudExperienceHost\Intent\entertainment]
""Intent""=dword:00000000
""Priority""=dword:00000000

[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CloudExperienceHost\Intent\business]
""Intent""=dword:00000000
""Priority""=dword:00000000"),
        new(
            "disable_usb_issues_notify",
            "Disable Usb Issues Notify",
            "Tweaks",
            "Devices",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Shell\USB]
""NotifyOnUsbErrors""=dword:00000000",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Shell\USB]
""NotifyOnUsbErrors""=-"),
        new(
            "disable_let_windows_manage_my_default_printe",
            "Disable Let Windows Manage My Default Printer",
            "Tweaks",
            "Devices",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows NT\CurrentVersion\Windows]
""LegacyDefaultPrinterMode""=dword:00000001",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows NT\CurrentVersion\Windows]
""LegacyDefaultPrinterMode""=dword:ffffffff"),
        new(
            "disable_write_with_your_fingertip",
            "Disable Write With Your Fingertip",
            "Tweaks",
            "Devices",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\TabletTip\EmbeddedInkControl]
""EnableInkingWithTouch""=dword:00000000",
            @"[HKEY_CURRENT_USER\Software\Microsoft\TabletTip\EmbeddedInkControl]
""EnableInkingWithTouch""=-"),
        new(
            "disable_notifications_suggested",
            "Disable Notifications Suggested",
            "Debloat",
            "Notifications",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Notifications\Settings\Windows.SystemToast.Suggested]
""Enabled""=dword:00000000",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Notifications\Settings\Windows.SystemToast.Suggested]
""Enabled""=-"),
        new(
            "disable_suggested_actions",
            "Disable Suggested Actions",
            "Debloat",
            "Notifications",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\SmartActionPlatform\SmartClipboard]
""Disabled""=dword:00000001",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\SmartActionPlatform\SmartClipboard]
""Disabled""=-"),
        new(
            "disable_focus_assist",
            "Disable Focus Assist",
            "Debloat",
            "Notifications",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CloudStore\Store\Cache\DefaultAccount\$$windows.data.notifications.quiethourssettings\Current]
""Data""=hex(3):02,00,00,00,B4,67,2B,68,F0,0B,D8,01,00,00,00,00,43,42,01,00,C2,0A,01,D2,14,28,4D,00,69,00,63,00,72,00,6F,00,73,00,6F,00,66,00,74,00,2E,00,51,00,75,00,69,00,65,00,74,00,48,00,6F,00,75,00,72,00,73,00,50,00,72,00,6F,00,66,00,69,00,6C,00,65,00,2E,00,55,00,6E,00,72,00,65,00,73,00,74,00,72,00,69,00,63,00,74,00,65,00,64,00,CA,28,D0,14,02,00,00
[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CloudStore\Store\Cache\DefaultAccount\$quietmomentfullscreen$windows.data.notifications.quietmoment\Current]
""Data""=hex(3):02,00,00,00,97,1D,2D,68,F0,0B,D8,01,00,00,00,00,43,42,01,00,C2,0A,01,D2,1E,26,4D,00,69,00,63,00,72,00,6F,00,73,00,6F,00,66,00,74,00,2E,00,51,00,75,00,69,00,65,00,74,00,48,00,6F,00,75,00,72,00,73,00,50,00,72,00,6F,00,66,00,69,00,6C,00,65,00,2E,00,41,00,6C,00,61,00,72,00,6D,00,73,00,4F,00,6E,00,6C,00,79,00,C2,28,01,CA,50,00,00
[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CloudStore\Store\Cache\DefaultAccount\$quietmomentgame$windows.data.notifications.quietmoment\Current]
""Data""=hex(3):02,00,00,00,6C,39,2D,68,F0,0B,D8,01,00,00,00,00,43,42,01,00,C2,0A,01,D2,1E,28,4D,00,69,00,63,00,72,00,6F,00,73,00,6F,00,66,00,74,00,2E,00,51,00,75,00,69,00,65,00,74,00,48,00,6F,00,75,00,72,00,73,00,50,00,72,00,6F,00,66,00,69,00,6C,00,65,00,2E,00,50,00,72,00,69,00,6F,00,72,00,69,00,74,00,79,00,4F,00,6E,00,6C,00,79,00,C2,28,01,CA,50,00,00
[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CloudStore\Store\Cache\DefaultAccount\$quietmomentpostoobe$windows.data.notifications.quietmoment\Current]
""Data""=hex(3):02,00,00,00,06,54,2D,68,F0,0B,D8,01,00,00,00,00,43,42,01,00,C2,0A,01,D2,1E,28,4D,00,69,00,63,00,72,00,6F,00,73,00,6F,00,66,00,74,00,2E,00,51,00,75,00,69,00,65,00,74,00,48,00,6F,00,75,00,72,00,73,00,50,00,72,00,6F,00,66,00,69,00,6C,00,65,00,2E,00,50,00,72,00,69,00,6F,00,72,00,69,00,74,00,79,00,4F,00,6E,00,6C,00,79,00,C2,28,01,CA,50,00,00
[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CloudStore\Store\Cache\DefaultAccount\$quietmomentpresentation$windows.data.notifications.quietmoment\Current]
""Data""=hex(3):02,00,00,00,83,6E,2D,68,F0,0B,D8,01,00,00,00,00,43,42,01,00,C2,0A,01,D2,1E,26,4D,00,69,00,63,00,72,00,6F,00,73,00,6F,00,66,00,74,00,2E,00,51,00,75,00,69,00,65,00,74,00,48,00,6F,00,75,00,72,00,73,00,50,00,72,00,6F,00,66,00,69,00,6C,00,65,00,2E,00,41,00,6C,00,61,00,72,00,6D,00,73,00,4F,00,6E,00,6C,00,79,00,C2,28,01,CA,50,00,00
[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CloudStore\Store\Cache\DefaultAccount\$quietmomentscheduled$windows.data.notifications.quietmoment\Current]
""Data""=hex(3):02,00,00,00,2E,8A,2D,68,F0,0B,D8,01,00,00,00,00,43,42,01,00,C2,0A,01,D2,1E,28,4D,00,69,00,63,00,72,00,6F,00,73,00,6F,00,66,00,74,00,2E,00,51,00,75,00,69,00,65,00,74,00,48,00,6F,00,75,00,72,00,73,00,50,00,72,00,6F,00,66,00,69,00,6C,00,65,00,2E,00,50,00,72,00,69,00,6F,00,72,00,69,00,74,00,79,00,4F,00,6E,00,6C,00,79,00,C2,28,01,D1,32,80,E0,AA,8A,99,30,D1,3C,80,E0,F6,C5,D5,0E,CA,50,00,00",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CloudStore\Store\Cache\DefaultAccount\$$windows.data.notifications.quiethourssettings\Current]
""Data""=hex:02,00,00,00,74,a9,70,73,03,82,da,01,00,00,00,00,43,42,01,00,c2,0a,01,d2,14,28,4d,00,69,00,63,00,72,00,6f,00,73,00,6f,00,66,00,74,00,2e,00,51,00,75,00,69,00,65,00,74,00,48,00,6f,00,75,00,72,00,73,00,50,00,72,00,6f,00,66,00,69,00,6c,00,65,00,2e,00,55,00,6e,00,72,00,65,00,73,00,74,00,72,00,69,00,63,00,74,00,65,00,64,00,ca,28,00,00

[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CloudStore\Store\Cache\DefaultAccount\$quietmomentfullscreen$windows.data.notifications.quietmoment\Current]
""Data""=hex:02,00,00,00,82,a3,71,73,03,82,da,01,00,00,00,00,43,42,01,00,c2,0a,01,c2,14,01,d2,1e,26,4d,00,69,00,63,00,72,00,6f,00,73,00,6f,00,66,00,74,00,2e,00,51,00,75,00,69,00,65,00,74,00,48,00,6f,00,75,00,72,00,73,00,50,00,72,00,6f,00,66,00,69,00,6c,00,65,00,2e,00,41,00,6c,00,61,00,72,00,6d,00,73,00,4f,00,6e,00,6c,00,79,00,ca,50,00,00

[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CloudStore\Store\Cache\DefaultAccount\$quietmomentgame$windows.data.notifications.quietmoment\Current]
""Data""=hex:02,00,00,00,a5,c1,71,73,03,82,da,01,00,00,00,00,43,42,01,00,c2,0a,01,c2,14,01,d2,1e,28,4d,00,69,00,63,00,72,00,6f,00,73,00,6f,00,66,00,74,00,2e,00,51,00,75,00,69,00,65,00,74,00,48,00,6f,00,75,00,72,00,73,00,50,00,72,00,6f,00,66,00,69,00,6c,00,65,00,2e,00,50,00,72,00,69,00,6f,00,72,00,69,00,74,00,79,00,4f,00,6e,00,6c,00,79,00,ca,50,00,00

[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CloudStore\Store\Cache\DefaultAccount\$quietmomentpostoobe$windows.data.notifications.quietmoment\Current]
""Data""=hex:02,00,00,00,85,de,71,73,03,82,da,01,00,00,00,00,43,42,01,00,c2,0a,01,c2,14,01,d2,1e,28,4d,00,69,00,63,00,72,00,6f,00,73,00,6f,00,66,00,74,00,2e,00,51,00,75,00,69,00,65,00,74,00,48,00,6f,00,75,00,72,00,73,00,50,00,72,00,6f,00,66,00,69,00,6c,00,65,00,2e,00,50,00,72,00,69,00,6f,00,72,00,69,00,74,00,79,00,4f,00,6e,00,6c,00,79,00,ca,50,00,00

[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CloudStore\Store\Cache\DefaultAccount\$quietmomentpresentation$windows.data.notifications.quietmoment\Current]
""Data""=hex:02,00,00,00,a4,fa,71,73,03,82,da,01,00,00,00,00,43,42,01,00,c2,0a,01,c2,14,01,d2,1e,26,4d,00,69,00,63,00,72,00,6f,00,73,00,6f,00,66,00,74,00,2e,00,51,00,75,00,69,00,65,00,74,00,48,00,6f,00,75,00,72,00,73,00,50,00,72,00,6f,00,66,00,69,00,6c,00,65,00,2e,00,41,00,6c,00,61,00,72,00,6d,00,73,00,4f,00,6e,00,6c,00,79,00,ca,50,00,00

[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CloudStore\Store\Cache\DefaultAccount\$quietmomentscheduled$windows.data.notifications.quietmoment\Current]
""Data""=hex:02,00,00,00,fe,17,72,73,03,82,da,01,00,00,00,00,43,42,01,00,c2,0a,01,d2,1e,28,4d,00,69,00,63,00,72,00,6f,00,73,00,6f,00,66,00,74,00,2e,00,51,00,75,00,69,00,65,00,74,00,48,00,6f,00,75,00,72,00,73,00,50,00,72,00,6f,00,66,00,69,00,6c,00,65,00,2e,00,50,00,72,00,69,00,6f,00,72,00,69,00,74,00,79,00,4f,00,6e,00,6c,00,79,00,d1,32,80,e0,aa,8a,99,30,d1,3c,80,e0,f6,c5,d5,0e,ca,50,00,00"),
        new(
            "disable_turn_on_do_not_disturb_automatically",
            "Disable Turn On Do Not Disturb Automatically",
            "Debloat",
            "Notifications",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CloudStore\Store\DefaultAccount\Current\default$windows.data.donotdisturb.quietmoment$quietmomentlist\windows.data.donotdisturb.quietmoment$quietmomentpresentation]
""Data""=hex(3):43,42,01,00,0A,02,01,00,2A,06,E2,F3,AA,CC,06,2A,2B,0E,5A,43,42,01,00,C2,0A,01,D2,1E,26,4D,00,69,00,63,00,72,00,6F,00,73,00,6F,00,66,00,74,00,2E,00,51,00,75,00,69,00,65,00,74,00,48,00,6F,00,75,00,72,00,73,00,50,00,72,00,6F,00,66,00,69,00,6C,00,65,00,2E,00,41,00,6C,00,61,00,72,00,6D,00,73,00,4F,00,6E,00,6C,00,79,00,CA,50,00,00,00,00,00
[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CloudStore\Store\DefaultAccount\Current\default$windows.data.donotdisturb.quietmoment$quietmomentlist\windows.data.donotdisturb.quietmoment$quietmomentgame]
""Data""=hex(3):43,42,01,00,0A,02,01,00,2A,06,E1,F3,AA,CC,06,2A,2B,0E,5E,43,42,01,00,C2,0A,01,D2,1E,28,4D,00,69,00,63,00,72,00,6F,00,73,00,6F,00,66,00,74,00,2E,00,51,00,75,00,69,00,65,00,74,00,48,00,6F,00,75,00,72,00,73,00,50,00,72,00,6F,00,66,00,69,00,6C,00,65,00,2E,00,50,00,72,00,69,00,6F,00,72,00,69,00,74,00,79,00,4F,00,6E,00,6C,00,79,00,CA,50,00,00,00,00,00
[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CloudStore\Store\DefaultAccount\Current\default$windows.data.donotdisturb.quietmoment$quietmomentlist\windows.data.donotdisturb.quietmoment$quietmomentfullscreen]
""Data""=hex(3):43,42,01,00,0A,02,01,00,2A,06,E0,F3,AA,CC,06,2A,2B,0E,5A,43,42,01,00,C2,0A,01,D2,1E,26,4D,00,69,00,63,00,72,00,6F,00,73,00,6F,00,66,00,74,00,2E,00,51,00,75,00,69,00,65,00,74,00,48,00,6F,00,75,00,72,00,73,00,50,00,72,00,6F,00,66,00,69,00,6C,00,65,00,2E,00,41,00,6C,00,61,00,72,00,6D,00,73,00,4F,00,6E,00,6C,00,79,00,CA,50,00,00,00,00,00
[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CloudStore\Store\DefaultAccount\Current\default$windows.data.donotdisturb.quietmoment$quietmomentlist\windows.data.donotdisturb.quietmoment$quietmomentpostoobe]
""Data""=hex(3):43,42,01,00,0A,02,01,00,2A,06,DF,F3,AA,CC,06,2A,2B,0E,5E,43,42,01,00,C2,0A,01,D2,1E,28,4D,00,69,00,63,00,72,00,6F,00,73,00,6F,00,66,00,74,00,2E,00,51,00,75,00,69,00,65,00,74,00,48,00,6F,00,75,00,72,00,73,00,50,00,72,00,6F,00,66,00,69,00,6C,00,65,00,2E,00,50,00,72,00,69,00,6F,00,72,00,69,00,74,00,79,00,4F,00,6E,00,6C,00,79,00,CA,50,00,00,00,00,00",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CloudStore\Store\DefaultAccount\Current\default$windows.data.donotdisturb.quietmoment$quietmomentlist\windows.data.donotdisturb.quietmoment$quietmomentpresentation]
""Data""=hex:43,42,01,00,0a,02,01,00,2a,2a,00,00,00

[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CloudStore\Store\DefaultAccount\Current\default$windows.data.donotdisturb.quietmoment$quietmomentlist\windows.data.donotdisturb.quietmoment$quietmomentgame]
""Data""=hex:43,42,01,00,0a,02,01,00,2a,2a,00,00,00

[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CloudStore\Store\DefaultAccount\Current\default$windows.data.donotdisturb.quietmoment$quietmomentlist\windows.data.donotdisturb.quietmoment$quietmomentfullscreen]
""Data""=hex:43,42,01,00,0a,02,01,00,2a,2a,00,00,00

[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CloudStore\Store\DefaultAccount\Current\default$windows.data.donotdisturb.quietmoment$quietmomentlist\windows.data.donotdisturb.quietmoment$quietmomentpostoobe]
""Data""=hex:43,42,01,00,0a,02,01,00,2a,2a,00,00,00"),
        new(
            "disable_set_priority_notifications",
            "Disable Set Priority Notifications",
            "Debloat",
            "Notifications",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CloudStore\Store\DefaultAccount\Current\default$windows.data.donotdisturb.quiethoursprofile$quiethoursprofilelist\windows.data.donotdisturb.quiethoursprofile$microsoft.quiethoursprofile.priorityonly]
""Data""=hex:43,42,01,00,0a,02,01,00,2a,06,be,89,ab,cc,06,2a,2b,0e,d0,03,43,42,01,00,c2,0a,01,cd,14,06,02,05,00,00,01,01,02,00,03,01,04,00,cc,32,12,05,28,4d,00,69,00,63,00,72,00,6f,00,73,00,6f,00,66,00,74,00,2e,00,53,00,63,00,72,00,65,00,65,00,6e,00,53,00,6b,00,65,00,74,00,63,00,68,00,5f,00,38,00,77,00,65,00,6b,00,79,00,62,00,33,00,64,00,38,00,62,00,62,00,77,00,65,00,21,00,41,00,70,00,70,00,29,4d,00,69,00,63,00,72,00,6f,00,73,00,6f,00,66,00,74,00,2e,00,57,00,69,00,6e,00,64,00,6f,00,77,00,73,00,41,00,6c,00,61,00,72,00,6d,00,73,00,5f,00,38,00,77,00,65,00,6b,00,79,00,62,00,33,00,64,00,38,00,62,00,62,00,77,00,65,00,21,00,41,00,70,00,70,00,31,4d,00,69,00,63,00,72,00,6f,00,73,00,6f,00,66,00,74,00,2e,00,58,00,62,00,6f,00,78,00,41,00,70,00,70,00,5f,00,38,00,77,00,65,00,6b,00,79,00,62,00,33,00,64,00,38,00,62,00,62,00,77,00,65,00,21,00,4d,00,69,00,63,00,72,00,6f,00,73,00,6f,00,66,00,74,00,2e,00,58,00,62,00,6f,00,78,00,41,00,70,00,70,00,2d,4d,00,69,00,63,00,72,00,6f,00,73,00,6f,00,66,00,74,00,2e,00,58,00,62,00,6f,00,78,00,47,00,61,00,6d,00,69,00,6e,00,67,00,4f,00,76,00,65,00,72,00,6c,00,61,00,79,00,5f,00,38,00,77,00,65,00,6b,00,79,00,62,00,33,00,64,00,38,00,62,00,62,00,77,00,65,00,21,00,41,00,70,00,70,00,29,57,00,69,00,6e,00,64,00,6f,00,77,00,73,00,2e,00,53,00,79,00,73,00,74,00,65,00,6d,00,2e,00,4e,00,65,00,61,00,72,00,53,00,68,00,61,00,72,00,65,00,45,00,78,00,70,00,65,00,72,00,69,00,65,00,6e,00,63,00,65,00,52,00,65,00,63,00,65,00,69,00,76,00,65,00,00,00,00,00",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CloudStore\Store\DefaultAccount\Current\default$windows.data.donotdisturb.quiethoursprofile$quiethoursprofilelist\windows.data.donotdisturb.quiethoursprofile$microsoft.quiethoursprofile.priorityonly]
""Data""=hex:43,42,01,00,0a,02,01,00,2a,2a,00,00,00"),
        new(
            "disable_focus_settings",
            "Disable Focus Settings",
            "Debloat",
            "Notifications",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CloudStore\Store\DefaultAccount\Current\default$windows.data.shell.focussessionactivetheme\windows.data.shell.focussessionactivetheme${1b019365-25a5-4ff1-b50a-c155229afc8f}]
""Data""=hex(3):43,42,01,00,0A,00,2A,06,F4,E2,AA,CC,06,2A,2B,0E,08,43,42,01,00,C2,0A,01,00,00,00,00",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CloudStore\Store\DefaultAccount\Current\default$windows.data.shell.focussessionactivetheme\windows.data.shell.focussessionactivetheme${1b019365-25a5-4ff1-b50a-c155229afc8f}]
""Data""=-"),
        new(
            "battery_options_optimize_for_video_quality",
            "Battery Options Optimize For Video Quality",
            "Tweaks",
            "Performance",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\VideoSettings]
""VideoQualityOnBattery""=dword:00000001",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\VideoSettings]
""VideoQualityOnBattery""=-"),
        new(
            "disable_storage_sense",
            "Disable Storage Sense",
            "Tweaks",
            "Performance",
            true,
            @"""04""=dword:00000000",
            @"[]
""04""=-"),
        new(
            "disable_keep_windows_running_smoothly",
            "Disable Keep Windows Running Smoothly",
            "Tweaks",
            "Performance",
            false,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\StorageSense]
[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\StorageSense\Parameters]
[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\StorageSense\Parameters\CachedSizes]
[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\StorageSense\Parameters\StoragePolicy]",
            @""),
        new(
            "don_t_auto_delete_temp_files",
            "Don''t Auto Delete Temp Files",
            "Tweaks",
            "Performance",
            true,
            @"""2048""=dword:00000000",
            @"[]
""2048""=-"),
        new(
            "don_t_auto_empty_recycle_bin",
            "Don''t Auto Empty Recycle Bin",
            "Tweaks",
            "Performance",
            true,
            @"""08""=dword:00000000",
            @"[]
""08""=-"),
        new(
            "don_t_auto_delete_downloads",
            "Don''t Auto Delete Downloads",
            "Tweaks",
            "Performance",
            true,
            @"""256""=dword:00000000",
            @"[]
""256""=-"),
        new(
            "never_auto_run_storage_sense",
            "Never Auto Run Storage Sense",
            "Tweaks",
            "Performance",
            true,
            @"""32""=dword:00000000",
            @"[]
""32""=-"),
        new(
            "settings_set",
            "Settings Set",
            "Tweaks",
            "Performance",
            true,
            @"""StoragePoliciesChanged""=dword:00000001
[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\StorageSense\Parameters\StoragePolicy\SpaceHistory]",
            @"[]
""StoragePoliciesChanged""=-"),
        new(
            "disable_drag_tray",
            "Disable Drag Tray",
            "Tweaks",
            "Performance",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CDP]
""DragTrayEnabled""=dword:00000000",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CDP]
""DragTrayEnabled""=-"),
        new(
            "disable_snap_window_settings",
            "Disable Snap Window Settings",
            "Tweaks",
            "Performance",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced]
""SnapAssist""=dword:00000000
""DITest""=dword:00000000
""EnableSnapBar""=dword:00000000
""EnableTaskGroups""=dword:00000000
""EnableSnapAssistFlyout""=dword:00000000
""SnapFill""=dword:00000000
""JointResize""=dword:00000000",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced]
""SnapAssist""=-
""DITest""=-
""EnableSnapBar""=-
""EnableTaskGroups""=-
""EnableSnapAssistFlyout""=-
""SnapFill""=-
""JointResize""=-"),
        new(
            "enable_endtask_menu_taskbar",
            "Enable Endtask Menu Taskbar",
            "Tweaks",
            "Performance",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced\TaskbarDeveloperSettings]
""TaskbarEndTask""=dword:00000001",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced\TaskbarDeveloperSettings]
""TaskbarEndTask""=dword:00000000"),
        new(
            "enable_long_paths",
            "Enable Long Paths",
            "Tweaks",
            "Performance",
            true,
            @"[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FileSystem]
""LongPathsEnabled""=dword:00000001",
            @"[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FileSystem]
""LongPathsEnabled""=-"),
        new(
            "alt_tab_open_windows_only",
            "Alt Tab Open Windows Only",
            "Tweaks",
            "Performance",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced]
""MultiTaskingAltTabFilter""=dword:00000003",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced]
""MultiTaskingAltTabFilter""=-"),
        new(
            "disable_share_across_devices",
            "Disable Share Across Devices",
            "Tweaks",
            "Performance",
            true,
            @"[HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\CDP]
""RomeSdkChannelUserAuthzPolicy""=dword:00000000
""CdpSessionUserAuthzPolicy""=dword:00000000",
            @"[HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\CDP]
""RomeSdkChannelUserAuthzPolicy""=dword:00000001
""CdpSessionUserAuthzPolicy""=-"),
        new(
            "disable_recommended_troubleshooter_preferenc",
            "Disable Recommended Troubleshooter Preferences",
            "Tweaks",
            "Performance",
            true,
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\WindowsMitigation]
""UserPreference""=dword:00000001",
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\WindowsMitigation]
""UserPreference""=-"),
        new(
            "disable_update_apps_automatically",
            "Disable Update Apps Automatically",
            "Debloat",
            "Store",
            true,
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsStore\WindowsUpdate]
""AutoDownload""=dword:00000002
[HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Control\FeatureManagement\Overrides\14\2792562829]
""EnabledState""=dword:00000002
[HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Control\FeatureManagement\Overrides\14\3036241548]
""EnabledState""=dword:00000002
[HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Control\FeatureManagement\Overrides\14\734731404]
""EnabledState""=dword:00000002
[HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Control\FeatureManagement\Overrides\14\762256525]
""EnabledState""=dword:00000002",
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsStore\WindowsUpdate]
""AutoDownload""=-

[HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Control\FeatureManagement\Overrides\14\2792562829]
""EnabledState""=-

[HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Control\FeatureManagement\Overrides\14\3036241548]
""EnabledState""=-

[HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Control\FeatureManagement\Overrides\14\734731404]
""EnabledState""=-

[HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Control\FeatureManagement\Overrides\14\762256525]
""EnabledState""=-"),
        new(
            "set_start_menu_apps_view_to_list",
            "Set Start Menu Apps View To List",
            "Appearance",
            "Start Menu",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Start]
""AllAppsViewMode""=dword:00000002",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Start]
""AllAppsViewMode""=dword:00000000"),
        new(
            "disable_windows_input_experience_preload",
            "Disable Windows Input Experience Preload",
            "Debloat",
            "Apps & Background",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\input]
""IsInputAppPreloadEnabled""=dword:00000000
[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Dsh]
""IsPrelaunchEnabled""=dword:00000000",
            @"[HKEY_CURRENT_USER\Software\Microsoft\input]
""IsInputAppPreloadEnabled""=-

[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Dsh]
""IsPrelaunchEnabled""=-"),
        new(
            "disable_ms_gamebar_notifications_with_xbox_c",
            "Disable Ms-Gamebar Notifications With Xbox Controller Plugged In",
            "Debloat",
            "Apps & Background",
            true,
            @"[HKEY_CLASSES_ROOT\ms-gamebar]
""(Default)""=""URL:ms-gamebar""
""URL Protocol""=""""
""NoOpenWith""=""""
[HKEY_CLASSES_ROOT\ms-gamebar\shell\open\command]
""(Default)""=""%SystemRoot%\\System32\\systray.exe""
[HKEY_CLASSES_ROOT\ms-gamebarservices]
""(Default)""=""URL:ms-gamebarservices""
""URL Protocol""=""""
""NoOpenWith""=""""
[HKEY_CLASSES_ROOT\ms-gamebarservices\shell\open\command]
""(Default)""=""%SystemRoot%\\System32\\systray.exe""
[HKEY_CLASSES_ROOT\ms-gamingoverlay]
""(Default)""=""URL:ms-gamingoverlay""
""URL Protocol""=""""
""NoOpenWith""=""""
[HKEY_CLASSES_ROOT\ms-gamingoverlay\shell\open\command]
""(Default)""=""%SystemRoot%\\System32\\systray.exe""
[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\WindowsRuntime\ActivatableClassId\Windows.Gaming.GameBar.PresenceServer.Internal.PresenceWriter]
""ActivationType""=dword:00000000
[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager]
""ContentDeliveryAllowed""=dword:00000000
""FeatureManagementEnabled""=dword:00000000
""OemPreInstalledAppsEnabled""=dword:00000000
""PreInstalledAppsEnabled""=dword:00000000
""PreInstalledAppsEverEnabled""=dword:00000000
""RotatingLockScreenEnabled""=dword:00000000
""RotatingLockScreenOverlayEnabled""=dword:00000000
""SilentInstalledAppsEnabled""=dword:00000000
""SlideshowEnabled""=dword:00000000
""SoftLandingEnabled""=dword:00000000
""SubscribedContent-310093Enabled""=dword:00000000
""SubscribedContent-314563Enabled""=dword:00000000
""SubscribedContent-338388Enabled""=dword:00000000
""SubscribedContent-338389Enabled""=dword:00000000
""SubscribedContent-338393Enabled""=dword:00000000
""SubscribedContent-353694Enabled""=dword:00000000
""SubscribedContent-353696Enabled""=dword:00000000
""SubscribedContent-353698Enabled""=dword:00000000
""SubscribedContentEnabled""=dword:00000000
""SystemPaneSuggestionsEnabled""=dword:00000000",
            @"[HKEY_CLASSES_ROOT\ms-gamebar]
""(Default)""=-
""URL Protocol""=""""
""NoOpenWith""=-

[HKEY_CLASSES_ROOT\ms-gamebar\shell\open\command]
""(Default)""=-

[HKEY_CLASSES_ROOT\ms-gamebarservices]
""(Default)""=-
""URL Protocol""=-
""NoOpenWith""=-

[HKEY_CLASSES_ROOT\ms-gamebarservices\shell\open\command]
""(Default)""=-

[HKEY_CLASSES_ROOT\ms-gamingoverlay]
""(Default)""=-
""URL Protocol""=""""
""NoOpenWith""=-

[HKEY_CLASSES_ROOT\ms-gamingoverlay\shell\open\command]
""(Default)""=-

[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\WindowsRuntime\ActivatableClassId\Windows.Gaming.GameBar.PresenceServer.Internal.PresenceWriter]
""ActivationType""=dword:00000001

[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager]
""ContentDeliveryAllowed""=dword:00000001
""FeatureManagementEnabled""=dword:00000001
""OemPreInstalledAppsEnabled""=dword:00000001
""PreInstalledAppsEnabled""=dword:00000001
""PreInstalledAppsEverEnabled""=dword:00000001
""RotatingLockScreenEnabled""=dword:00000001
""RotatingLockScreenOverlayEnabled""=dword:00000001
""SilentInstalledAppsEnabled""=dword:00000001
""SlideshowEnabled""=dword:00000001
""SoftLandingEnabled""=dword:00000001
""SubscribedContent-310093Enabled""=-
""SubscribedContent-314563Enabled""=-
""SubscribedContent-338388Enabled""=-
""SubscribedContent-338389Enabled""=-
""SubscribedContent-338393Enabled""=-
""SubscribedContent-353694Enabled""=-
""SubscribedContent-353696Enabled""=-
""SubscribedContent-353698Enabled""=-
""SubscribedContentEnabled""=dword:00000001
""SystemPaneSuggestionsEnabled""=dword:00000001"),
        new(
            "remove_3d_objects",
            "Remove 3D Objects",
            "Appearance",
            "File Explorer",
            false,
            @"[-HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{0DB7E03F-FC29-4DC6-9020-FF41B59E513A}]
[-HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{0DB7E03F-FC29-4DC6-9020-FF41B59E513A}]",
            @""),
        new(
            "remove_quick_access",
            "Remove Quick Access",
            "Appearance",
            "File Explorer",
            true,
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer]
""HubMode""=dword:00000001",
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer]
""HubMode""=-"),
        new(
            "remove_home_broken_on_new_update",
            "Remove Home (Broken On New Update) (+1 more)",
            "Appearance",
            "File Explorer",
            true,
            @"[HKEY_CURRENT_USER\Software\Classes\CLSID\{e88865ea-0e1c-4e20-9aa6-edcd0212c87c}]
""System.IsPinnedToNameSpaceTree""=dword:00000000",
            @"[HKEY_CURRENT_USER\Software\Classes\CLSID\{e88865ea-0e1c-4e20-9aa6-edcd0212c87c}]
""System.IsPinnedToNameSpaceTree""=-"),
        new(
            "disable_menu_show_delay",
            "Disable Menu Show Delay",
            "Appearance",
            "File Explorer",
            true,
            @"[HKEY_CURRENT_USER\Control Panel\Desktop]
""MenuShowDelay""=""0""",
            @"[HKEY_CURRENT_USER\Control Panel\Desktop]
""MenuShowDelay""=""400"""),
        new(
            "disable_driver_searching_updates",
            "Disable Driver Searching & Updates",
            "System",
            "System Extras",
            true,
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\DriverSearching]
""SearchOrderConfig""=dword:00000000",
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\DriverSearching]
""SearchOrderConfig""=dword:00000001"),
        new(
            "disable_phone_companion_in_start_menu",
            "Disable Phone Companion In Start Menu",
            "System",
            "System Extras",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Start]
""RightCompanionToggledOpen""=dword:00000000
[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Start\Companions\Microsoft.YourPhone_8wekyb3d8bbwe]
""IsEnabled""=dword:00000000
""IsAvailable""=dword:00000000",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Start]
""RightCompanionToggledOpen""=-

[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Start\Companions\Microsoft.YourPhone_8wekyb3d8bbwe]
""IsEnabled""=-
""IsAvailable""=-"),
        new(
            "more_info_on_bsod",
            "More Info On Bsod",
            "System",
            "System Extras",
            true,
            @"[HKEY_LOCAL_MACHINE\System\CurrentControlSet\Control\CrashControl]
""DisplayParameters""=dword:00000001",
            @"[HKEY_LOCAL_MACHINE\System\CurrentControlSet\Control\CrashControl]
""DisplayParameters""=dword:00000000"),
        new(
            "disable_windows_platform_binary_table",
            "Disable Windows Platform Binary Table",
            "System",
            "System Extras",
            true,
            @"[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager]
""DisableWpbtExecution""=dword:00000001",
            @"[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager]
""DisableWpbtExecution""=-"),
        new(
            "no_web_services_in_explorer",
            "No Web Services In Explorer",
            "Appearance",
            "File Explorer",
            true,
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer]
""NoWebServices""=dword:00000001",
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer]
""NoWebServices""=-"),
        new(
            "disable_cross_device_resume",
            "Disable Cross Device Resume",
            "System",
            "System Extras",
            true,
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CrossDeviceResume\Configuration]
""IsResumeAllowed""=dword:00000000
""IsOneDriveResumeAllowed""=dword:00000000
[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\PolicyManager\default\Connectivity\DisableCrossDeviceResume]
""value""=dword:00000001
[HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Control\FeatureManagement\Overrides\8\1387020943]
""EnabledState""=dword:00000001
[HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Control\FeatureManagement\Overrides\8\1694661260]
""EnabledState""=dword:00000001",
            @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CrossDeviceResume\Configuration]
""IsResumeAllowed""=-
""IsOneDriveResumeAllowed""=-

[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\PolicyManager\default\Connectivity\DisableCrossDeviceResume]
""value""=-

[HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Control\FeatureManagement\Overrides\8\1387020943]
""EnabledState""=-

[HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Control\FeatureManagement\Overrides\8\1694661260]
""EnabledState""=-"),
        new(
            "hide_home_in_settings",
            "Hide Home In Settings",
            "System",
            "System Extras",
            true,
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer]
""SettingsPageVisibility""=""hide:home;""",
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer]
""SettingsPageVisibility""=-"),
        new(
            "disable_open_terminal_by_default",
            "Disable Open Terminal By Default",
            "Appearance",
            "File Explorer",
            true,
            @"[HKEY_CURRENT_USER\Console\%%Startup]
""DelegationConsole""=""{B23D10C0-E52E-411E-9D5B-C09FDF709C7D}""
""DelegationTerminal""=""{B23D10C0-E52E-411E-9D5B-C09FDF709C7D}""",
            @"[HKEY_CURRENT_USER\Console\%%Startup]
""DelegationConsole""=-
""DelegationTerminal""=-"),
        new(
            "black_powershell_console",
            "Black Powershell Console",
            "System",
            "System Extras",
            true,
            @"[HKEY_CURRENT_USER\Console\%SystemRoot%_System32_WindowsPowerShell_v1.0_powershell.exe]
""ScreenColors""=dword:0000000F",
            @"[HKEY_CURRENT_USER\Console\%SystemRoot%_System32_WindowsPowerShell_v1.0_powershell.exe]
""ScreenColors""=dword:00000056"),
        new(
            "fix_enter_your_pin_hello_face_sign_in_bug_al",
            "Fix Enter Your Pin Hello Face Sign In Bug Allow Password Instead",
            "System",
            "System Extras",
            true,
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\PasswordLess\Device]
""DevicePasswordLessBuildVersion""=dword:00000000",
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\PasswordLess\Device]
""DevicePasswordLessBuildVersion""=dword:00000002"),
        new(
            "disable_finish_setting_up_your_device",
            "Disable Finish Setting Up Your Device",
            "System",
            "System Extras",
            true,
            @"[HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\UserProfileEngagement]
""ScoobeSystemSettingEnabled""=dword:00000000",
            @"[HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\UserProfileEngagement]
""ScoobeSystemSettingEnabled""=-"),
        new(
            "disable_background_blur_during_sign_in",
            "Disable Background Blur During Sign-In",
            "System",
            "System Extras",
            true,
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System]
""DisableAcrylicBackgroundOnLogon""=dword:00000001",
            @"[HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System]
""DisableAcrylicBackgroundOnLogon""=-"),
    ];
}
