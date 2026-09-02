using AkariToolbox.App.Models;

namespace AkariToolbox.App.Services;

/// <inheritdoc cref="IDebloatCatalog"/>
/// <remarks>
/// The 28-row/5-category static catalog (DEBLOAT-01), authored in exact category sequence
/// (Privacy &amp; Telemetry, System &amp; Performance, Cleanup, Explorer &amp; UI, Tools) matching
/// the predecessor's <c>BuildGroup</c> call order — <see cref="DebloatViewModel"/>'s LINQ
/// <c>GroupBy</c> preserves this first-occurrence order with no separate sort step needed.
///
/// "28 vs 29" scope note (RESEARCH.md): the predecessor's page has 29 buttons; "Create
/// Restore Point" is excluded — it is a safety/utility action with no Undo, categorically
/// distinct from the 28 debloat/privacy/cleanup actions here.
///
/// "disablebitlocker" and "windowsai" are sourced from the "Ultimate" collection's
/// <c>3 Setup/1 BitLocker.ps1</c> and <c>6 Windows/9 Copilot.ps1</c> respectively
/// (D-12/D-13/D-14), not the predecessor's own BitLocker/WindowsAI script pair — both are
/// branch-extracted from self-elevating, menu-driven console scripts (embedded in later
/// Wave plans 03-03 and 03-02).
/// </remarks>
public sealed class DebloatCatalog : IDebloatCatalog
{
    public IReadOnlyList<DebloatAction> Actions { get; } =
    [
        // Privacy & Telemetry (8)
        new("telemetry", "Telemetry — Disable", "Disables Windows data collection and telemetry",
            "Privacy & Telemetry", "telemetry.ps1", "telemetry-undo.ps1", RequiresConfirmation: false),
        new("activityhistory", "Activity History — Disable", "Erases recent docs, clipboard, and run history",
            "Privacy & Telemetry", "activityhistory.ps1", "activityhistory-undo.ps1", RequiresConfirmation: false),
        new("locationtracking", "Location Tracking — Disable", "Disables Windows location services",
            "Privacy & Telemetry", "locationtracking.ps1", "locationtracking-undo.ps1", RequiresConfirmation: false),
        new("ps7telemetry", "PS7 Telemetry — Disable", "Opts out of PowerShell 7 telemetry collection",
            "Privacy & Telemetry", "ps7telemetry.ps1", "ps7telemetry-undo.ps1", RequiresConfirmation: false),
        new("windowsai", "Windows AI — Disable",
            "Removes the Copilot AppX package and disables it via registry policy (Copilot-only — per D-14 does not touch Recall, WSAIFabricSvc, or Notepad AI)",
            "Privacy & Telemetry", "windowsai.ps1", "windowsai-undo.ps1", RequiresConfirmation: false),
        new("consumerfeatures", "Consumer Features — Disable", "Disables suggested apps, tips, and Windows promotions",
            "Privacy & Telemetry", "consumerfeatures.ps1", "consumerfeatures-undo.ps1", RequiresConfirmation: false),
        new("disablebgapps", "Background Apps — Disable", "Stops all Microsoft Store apps from running in the background",
            "Privacy & Telemetry", "disablebgapps.ps1", "disablebgapps-undo.ps1", RequiresConfirmation: false),
        new("storesearch", "Store Search — Disable", "Hides Microsoft Store results from Start Menu search",
            "Privacy & Telemetry", "storesearch.ps1", "storesearch-undo.ps1", RequiresConfirmation: true),

        // System & Performance (8)
        new("visualeffects", "Visual Effects — Best Perf", "Disables animations and visual fluff for max speed",
            "System & Performance", "visualeffects.ps1", "visualeffects-undo.ps1", RequiresConfirmation: false),
        new("services", "Services — Set to Manual", "Sets non-essential services to manual startup",
            "System & Performance", "services.ps1", "services-undo.ps1", RequiresConfirmation: false),
        new("deliveryoptimization", "Delivery Optimization — Disable", "Stops Windows using your bandwidth to share updates",
            "System & Performance", "deliveryoptimization.ps1", "deliveryoptimization-undo.ps1", RequiresConfirmation: false),
        new("disablebitlocker", "BitLocker — Disable", "Disables BitLocker encryption on the system drive",
            "System & Performance", "disablebitlocker.ps1", "disablebitlocker-undo.ps1", RequiresConfirmation: true),
        new("hibernation", "Hibernation — Disable", "Disables hibernation and removes hiberfil.sys",
            "System & Performance", "hibernation.ps1", "hibernation-undo.ps1", RequiresConfirmation: true),
        new("storagesense", "Storage Sense — Disable", "Stops Windows from auto-deleting temp files",
            "System & Performance", "storagesense.ps1", "storagesense-undo.ps1", RequiresConfirmation: false),
        new("wpbt", "WPBT — Disable", "Disables Windows Platform Binary Table execution",
            "System & Performance", "wpbt.ps1", "wpbt-undo.ps1", RequiresConfirmation: false),
        new("utc", "Set Time to UTC", "Fixes time sync when dual booting with Linux",
            "System & Performance", "utc.ps1", "utc-undo.ps1", RequiresConfirmation: false),

        // Cleanup (6)
        new("diskcleanup", "Disk Cleanup — Run", "Runs cleanup on C: and removes old Windows Updates",
            "Cleanup", "diskcleanup.ps1", null, RequiresConfirmation: false),
        new("tempfiles", "Temporary Files — Remove", "Clears temp folders and prefetch files",
            "Cleanup", "tempfiles.ps1", null, RequiresConfirmation: false),
        new("bloatware", "Unwanted Apps — Remove",
            "Removes bloatware apps using an exclusion-list approach (broader than an allow-list), disables select optional Windows features/capabilities, and side-removes OneDrive, Remote Desktop Connection, Snipping Tool, and GameInput",
            "Cleanup", "bloatware-remove.ps1", "bloatware-installall.ps1", RequiresConfirmation: true),
        new("removeonedrive", "OneDrive — Remove", "Completely removes OneDrive from the system",
            "Cleanup", "removeonedrive.ps1", "removeonedrive-undo.ps1", RequiresConfirmation: true),
        new("edgesettings", "Microsoft Edge — Debloat", "Disables telemetry, popups, and annoyances in Edge",
            "Cleanup", "edgesettings-optimize.ps1", "edgesettings-default.ps1", RequiresConfirmation: false, UndoDownloadsUnverifiedBinary: true),
        new("edgewebview", "Microsoft Edge — Remove", "Fully uninstalls Microsoft Edge and the WebView2 runtime from the system",
            "Cleanup", "edgewebview-uninstall.ps1", "edgewebview-default.ps1", RequiresConfirmation: true, UndoDownloadsUnverifiedBinary: true),

        // Explorer & UI (5)
        new("endtask", "End Task — Enable", "Adds End Task when right-clicking taskbar apps",
            "Explorer & UI", "endtask.ps1", "endtask-undo.ps1", RequiresConfirmation: false),
        new("folderdiscovery", "Folder Discovery — Disable", "Stops Explorer auto-changing folder view layouts",
            "Explorer & UI", "folderdiscovery.ps1", "folderdiscovery-undo.ps1", RequiresConfirmation: false),
        new("removehomeandgallery", "Explorer Home — Remove", "Hides Home and Gallery from Explorer sidebar",
            "Explorer & UI", "removehomeandgallery.ps1", "removehomeandgallery-undo.ps1", RequiresConfirmation: false),
        new("rightclickmenu", "Right-Click — Classic", "Restores the old Windows 10 right-click menu",
            "Explorer & UI", "rightclickmenu.ps1", "rightclickmenu-undo.ps1", RequiresConfirmation: false),
        new("widgets", "Widgets — Remove", "Removes the Widgets button from the taskbar",
            "Explorer & UI", "widgets.ps1", "widgets-undo.ps1", RequiresConfirmation: false),

        // Tools (1)
        new("oosu", "O&O ShutUp10++ — Run", "Downloads and launches the O&O ShutUp10 privacy tool",
            "Tools", "oosu.ps1", null, RequiresConfirmation: false),
    ];
}
