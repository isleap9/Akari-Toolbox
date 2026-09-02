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
/// on top of this base catalog.
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
    ];
}
