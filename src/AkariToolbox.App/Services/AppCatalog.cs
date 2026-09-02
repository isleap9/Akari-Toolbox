using AkariToolbox.App.Models;

namespace AkariToolbox.App.Services;

/// <inheritdoc cref="IAppCatalog"/>
/// <remarks>
/// The 29-row/5-category static catalog (DOWNLOADS-02, D-01), authored in exact category
/// sequence matching the predecessor's <c>DownloadsViewModel.SeedApps</c> tuple array —
/// 11 Browsers, 4 Comms, 6 Dev, 4 Gaming, 4 Utilities — ported verbatim (same Name/
/// Description/Category/WingetId strings, same order). <see cref="ViewModels.DownloadsViewModel"/>'s
/// LINQ <c>GroupBy</c> preserves this first-occurrence order with no separate sort step
/// needed, matching <see cref="DebloatCatalog"/>'s precedent.
///
/// "28 vs 29" note (correcting 04-RESEARCH.md's approximate count): the predecessor's
/// <c>SeedApps</c> array actually holds 29 rows, not 28. Plan 04-03 appends 13 more rows
/// on top of this base catalog (42 total): Frame View, Roblox, Battle.net, Electronic
/// Arts, League of Legends (NA), Rockstar Games, Ubisoft Connect, Valorant (NA), OBS
/// Studio, Onboard Memory Manager, PotPlayer, Nvidia App (msstore), and Escape From
/// Tarkov (direct-CDN exception, D-03) — Epic Games Launcher and GOG Galaxy are
/// deliberately NOT re-added since they already exist above (04-RESEARCH.md Pitfall 4).
/// </remarks>
public sealed class AppCatalog : IAppCatalog
{
    public IReadOnlyList<AppDefinition> Apps { get; } =
    [
        // Browsers (11)
        new("Google Chrome", "Fast browser with Google account sync", "Browsers", "Google.Chrome"),
        new("Ungoogled Chromium", "Chromium without any Google services or telemetry", "Browsers", "eloston.ungoogled-chromium"),
        new("Mozilla Firefox", "Open-source browser with strong privacy defaults", "Browsers", "Mozilla.Firefox"),
        new("LibreWolf", "Privacy-hardened Firefox fork, no telemetry", "Browsers", "LibreWolf.LibreWolf"),
        new("Zen Browser", "Beautifully designed, privacy-focused Firefox-based browser", "Browsers", "Zen-Team.Zen-Browser"),
        new("Waterfox", "64-bit Firefox fork focused on speed and privacy", "Browsers", "Waterfox.Waterfox"),
        new("Brave", "Privacy-focused browser with built-in ad-blocking", "Browsers", "Brave.Brave"),
        new("Arc Browser", "Innovative browser with built-in productivity tools", "Browsers", "TheBrowserCompany.Arc"),
        new("DuckDuckGo Privacy Browser", "Privacy-focused browser with built-in tracker blocking", "Browsers", "DuckDuckGo.DesktopBrowser"),
        new("Vivaldi", "Highly customisable browser that does not track you", "Browsers", "Vivaldi.Vivaldi"),
        new("Tor Browser", "Anonymity browser routed through the Tor network", "Browsers", "TorProject.TorBrowser"),

        // Comms (4)
        new("Discord", "Voice, video and text chat for communities", "Comms", "Discord.Discord"),
        new("TeamSpeak", "Low-latency voice communication for gamers", "Comms", "TeamSpeakSystems.TeamSpeakClient"),
        new("Telegram", "Fast, cloud-based secure messaging", "Comms", "Telegram.TelegramDesktop"),
        new("Signal", "End-to-end encrypted private messenger", "Comms", "OpenWhisperSystems.Signal"),

        // Dev (6)
        new("Notepad++", "Lightweight text and code editor with syntax highlighting", "Dev", "Notepad++.Notepad++"),
        new("Visual Studio Code", "Full-featured code editor with extensions and debugging", "Dev", "Microsoft.VisualStudioCode"),
        new("Visual Studio Community", "Full-featured IDE for .NET and C++ development", "Dev", "Microsoft.VisualStudio.2022.Community"),
        new("GitHub Desktop", "Official GitHub client for Windows", "Dev", "GitHub.GitHubDesktop"),
        new("Git", "Distributed version control system", "Dev", "Git.Git"),
        new("Python 3.13", "Dynamic programming language for rapid development", "Dev", "Python.Python.3.13"),

        // Gaming (4)
        new("Steam", "Popular game distribution platform with a large library", "Gaming", "Valve.Steam"),
        new("GOG Galaxy", "DRM-free game platform with cross-play features", "Gaming", "GOG.Galaxy"),
        new("Epic Games Launcher", "Store and launcher for Epic titles", "Gaming", "EpicGames.EpicGamesLauncher"),
        new("MSI Afterburner", "GPU overclocking and monitoring utility", "Gaming", "Guru3D.Afterburner"),

        // Utilities (4)
        new("7-Zip", "High-ratio file archiver and extractor", "Utilities", "7zip.7zip"),
        new("VLC Media Player", "Plays virtually any audio or video format", "Utilities", "VideoLAN.VLC"),
        new("ShareX", "Powerful screen capture and screen recording tool", "Utilities", "ShareX.ShareX"),
        new("PowerToys", "Windows system utilities for power users", "Utilities", "Microsoft.PowerToys"),

        // Plan 04-03 additions (13 new rows, 42 total) — Utilities: Frame View
        new("Frame View", "NVIDIA's lightweight FPS/performance overlay and benchmarking tool", "Utilities", "Nvidia.FrameView", HardeningResourceSuffix: "frameview-harden.ps1"),

        // Plan 04-03 additions — Gaming (8): no hardening unless noted; Battle.net/Electronic
        // Arts/League of Legends/Valorant have no hardening because their source-script
        // shortcut/custom-installpath logic is not OS-standard-path-safe (04-RESEARCH.md
        // Pitfall 1) once winget controls the install location.
        new("Roblox", "Cross-platform game creation and play platform", "Gaming", "Roblox.Roblox"),
        new("Battle.net", "Blizzard's game launcher and updater", "Gaming", "Blizzard.BattleNet"),
        new("Electronic Arts", "EA's desktop app for EA game titles", "Gaming", "ElectronicArts.EADesktop"),
        new("League of Legends (NA)", "Riot Games' MOBA launcher, NA server", "Gaming", "RiotGames.LeagueOfLegends.NA"),
        new("Rockstar Games", "Rockstar's game launcher", "Gaming", "RockstarGames.Launcher", HardeningResourceSuffix: "rockstar-harden.ps1"),
        new("Ubisoft Connect", "Ubisoft's game launcher and social platform", "Gaming", "Ubisoft.Connect", HardeningResourceSuffix: "ubisoft-harden.ps1"),
        new("Valorant (NA)", "Riot Games' tactical shooter, NA server", "Gaming", "RiotGames.Valorant.NA"),
        // D-03 direct-CDN exception — no winget package exists for this title
        // (04-RESEARCH.md live verification); bypasses winget entirely via
        // DirectInstallResourceSuffix, handled by AppInstallerService.InstallAsync.
        new("Escape From Tarkov", "Battlestate Games' tactical extraction shooter (installed via direct download — no winget package exists for this title)", "Gaming", "", DirectInstallResourceSuffix: "eft-install.ps1"),

        // Plan 04-03 additions — Utilities (4): Onboard Memory Manager has no hardening
        // because its source-script custom Program Files (x86) install path is
        // Pitfall-1-unsafe to port once winget controls the install location. Nvidia App
        // resolves through the msstore source (no plain winget listing exists) — see Task 3's
        // blocking human-verify checkpoint for this entry's silent-install verification.
        new("OBS Studio", "Free, open-source streaming and screen recording software", "Utilities", "OBSProject.OBSStudio"),
        new("Onboard Memory Manager", "Logitech peripheral onboard-memory configuration utility", "Utilities", "Logitech.OnboardMemoryManager"),
        new("PotPlayer", "Lightweight, feature-rich media player", "Utilities", "Daum.PotPlayer", HardeningResourceSuffix: "potplayer-harden.ps1"),
        new("Nvidia App", "NVIDIA's unified GPU driver/control app (Microsoft Store-sourced)", "Utilities", "XP8CLZL93F5Z4P", WingetSource: "msstore", HardeningResourceSuffix: "nvidiaapp-harden.ps1"),
    ];
}
