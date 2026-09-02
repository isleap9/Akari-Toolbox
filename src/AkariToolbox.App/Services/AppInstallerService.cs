using AkariToolbox.App.Models;
using AkariToolbox.Framework.Services;

namespace AkariToolbox.App.Services;

/// <inheritdoc cref="IAppInstallerService"/>
/// <remarks>
/// Ported from the predecessor's <c>AppInstallerService</c>, which shells out to winget but
/// discards the process exit code entirely (only logs on a thrown exception) — a failed
/// install (e.g. package already up to date returning non-zero, or a genuine failure) was
/// silently treated as success. This port fixes that (T-04-03, RESEARCH.md "Don't Hand-Roll"
/// table): <see cref="InstallAsync"/> returns <c>false</c> whenever the winget/script exit
/// code is non-zero, not only when an exception is thrown.
/// </remarks>
public sealed class AppInstallerService(IScriptRunner scriptRunner, ILogConsoleService log) : IAppInstallerService
{
    public async Task<bool> InstallAsync(AppDefinition app)
    {
        bool success;

        if (app.DirectInstallResourceSuffix is not null)
        {
            // D-03 exception path — bypasses winget entirely. Unused by any of the 29 apps
            // in this plan.
            log.Log($"[DOWNLOADS] Installing {app.Name} via embedded direct-install script (bypassing winget)...");
            var directExitCode = await scriptRunner.RunEmbeddedScriptAsync(app.DirectInstallResourceSuffix);
            success = directExitCode == 0;
        }
        else
        {
            var arguments = $"install --id {app.WingetId} --silent --accept-package-agreements --accept-source-agreements";
            if (app.WingetSource is not null)
            {
                arguments += $" --source {app.WingetSource}";
            }

            log.Log($"[DOWNLOADS] Running: winget {arguments}");
            var exitCode = await scriptRunner.RunProcessAsync("winget", arguments);
            success = exitCode == 0;
        }

        log.Log(success
            ? $"[DOWNLOADS] {app.Name} installed successfully."
            : $"[DOWNLOADS] {app.Name} FAILED to install (non-zero exit code).");

        if (success && app.HardeningResourceSuffix is not null)
        {
            // A hardening-script failure is logged but never flips a successful install to
            // failed — it is a no-op-shortcut cleanup, not part of the install contract.
            log.Log($"[DOWNLOADS] Running post-install hardening for {app.Name}...");
            var hardeningExitCode = await scriptRunner.RunEmbeddedScriptAsync(app.HardeningResourceSuffix);
            log.Log(hardeningExitCode == 0
                ? $"[DOWNLOADS] Post-install hardening for {app.Name} completed."
                : $"[DOWNLOADS] Post-install hardening for {app.Name} reported a non-zero exit code (install itself still succeeded).");
        }

        return success;
    }
}
