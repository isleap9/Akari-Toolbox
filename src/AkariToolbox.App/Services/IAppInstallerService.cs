using AkariToolbox.App.Models;

namespace AkariToolbox.App.Services;

/// <summary>
/// Installs a single catalog app — shells out to winget by default, or bypasses winget
/// entirely via an embedded direct-install script when <see cref="AppDefinition.DirectInstallResourceSuffix"/>
/// is set (D-03). Fixes the predecessor's swallowed-exit-code pitfall (T-04-03): a non-zero
/// exit code is treated as a failed install, not silently swallowed.
/// </summary>
public interface IAppInstallerService
{
    /// <summary>Installs <paramref name="app"/>, returning <c>true</c> only when the underlying process exit code was zero.</summary>
    Task<bool> InstallAsync(AppDefinition app);
}
