namespace AkariToolbox.App.Services;

/// <summary>
/// Minimal, injectable port of the predecessor's <c>static PostInstallService</c>. Not
/// currently consumed by any Phase 1 handler — the <c>MinSudoPath</c>/<c>PowerRunPath</c>
/// era members and <c>EnsureDefenderFilesAsync</c>/<c>NoDefenderPath</c>/
/// <c>NoDefenderPresent</c> were removed (01-REVIEW.md CR-01/CR-03 fix, plus a follow-up
/// explicit project-owner direction): the Defender handler's elevation mechanism was
/// replaced with a native
/// SYSTEM-impersonation port (<see cref="ElevationService"/>) that no longer launches
/// MinSudo.exe/PowerRun.exe, and its cab+ps1 payload is now embedded directly in the
/// assembly instead of being fetched through this service (see
/// <see cref="TweakHandlers.DefenderTweakHandler"/>'s doc comment) — so nothing in this
/// app reads any PostInstall path for Defender any more. This service stays registered
/// ahead of the Phase 4 Downloads page (DOWNLOADS-01), which will consume it for the full
/// ~130-entry asset mirror. Converted from the predecessor's static class to an
/// injectable singleton to fit this app's DI-first convention — a structural adaptation,
/// not a behavior change.
/// </summary>
public interface IPostInstallService
{
    /// <summary>Local mirror root — <c>C:\PostInstall</c>, unchanged from the predecessor.</summary>
    string LocalRoot { get; }

    /// <summary>True when every entry in the full PostInstall asset manifest is present locally.</summary>
    bool IsFullyInstalled { get; }

    /// <summary>Downloads any missing files in the full manifest from the pinned GitHub PostInstall repo.</summary>
    Task<bool> EnsurePostInstallAsync();

    /// <summary>
    /// New, not-ported-from-predecessor integrity primitive (T-01-SC mitigation, closes
    /// BLOCKER from phase-plan review): computes the live SHA256 digest of
    /// <paramref name="filePath"/> and compares it against <paramref name="expectedHexSha256"/>.
    /// Never throws — a missing file is a gate failure (returns <c>false</c>), not an
    /// exceptional path. Deliberately generic (any file/hash pair) so it is directly
    /// unit-testable without needing the real downloaded PostInstall assets.
    /// </summary>
    Task<bool> VerifyFileSha256Async(string filePath, string expectedHexSha256);
}
