namespace AkariToolbox.App.Models;

/// <summary>
/// A single row in the compiled-in winget app-installer catalog (DOWNLOADS-02, D-01).
/// Ported from the predecessor's <c>DownloadsViewModel.SeedApps</c> tuple array — this is
/// the "definition" half of the split; <see cref="AppItem"/> is the display/bindable half.
/// </summary>
/// <param name="Name">Display name.</param>
/// <param name="Description">Display description.</param>
/// <param name="Category">One of the 5 catalog categories (Browsers, Comms, Dev, Gaming, Utilities).</param>
/// <param name="WingetId">The winget package identifier passed to <c>winget install --id</c>.</param>
/// <param name="WingetSource">
/// A non-default winget source (e.g. <c>"msstore"</c>) to append as <c>--source</c>, or
/// <c>null</c> to let winget resolve the default source. Unused by any of the 29 apps in
/// this plan; starting Plan 04-03 for Nvidia App.
/// </param>
/// <param name="HardeningResourceSuffix">
/// D-04 post-install hardening embedded-script suffix, run via
/// <c>IScriptRunner.RunEmbeddedScriptAsync</c> immediately after a successful winget
/// install. <c>null</c> when winget alone suffices (all 29 apps in this plan).
/// </param>
/// <param name="DirectInstallResourceSuffix">
/// D-03 CDN-download exception path — when non-null, <see cref="Services.AppInstallerService"/>
/// bypasses winget entirely and runs this embedded script instead. Unused by any of the 29
/// apps in this plan; starting Plan 04-03 for Escape From Tarkov.
/// </param>
public sealed record AppDefinition(
    string Name,
    string Description,
    string Category,
    string WingetId,
    string? WingetSource = null,
    string? HardeningResourceSuffix = null,
    string? DirectInstallResourceSuffix = null);
