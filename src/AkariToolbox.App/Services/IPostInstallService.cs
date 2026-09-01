namespace AkariToolbox.App.Services;

/// <summary>
/// Minimal, injectable port of the predecessor's <c>static PostInstallService</c> —
/// covers only the subset of members <see cref="TweakHandlers.DefenderTweakHandler"/>
/// needs (<see cref="EnsureDefenderFilesAsync"/>/<see cref="EnsureMinSudoAsync"/>), not
/// the full ~130-entry Downloads-page asset mirror (that remains Phase 4/DOWNLOADS-01
/// scope). Converted from the predecessor's static class to an injectable singleton to
/// fit this app's DI-first convention — a structural adaptation, not a behavior change.
/// </summary>
public interface IPostInstallService
{
    /// <summary>Local mirror root — <c>C:\PostInstall</c>, unchanged from the predecessor.</summary>
    string LocalRoot { get; }

    string MinSudoPath { get; }

    string PowerRunPath { get; }

    string NoDefenderPath { get; }

    bool MinSudoPresent { get; }

    bool PowerRunPresent { get; }

    bool NoDefenderPresent { get; }

    /// <summary>True when every entry in the full PostInstall asset manifest is present locally.</summary>
    bool IsFullyInstalled { get; }

    /// <summary>Ensures <see cref="MinSudoPath"/> is present, downloading the full manifest if not.</summary>
    Task<bool> EnsureMinSudoAsync();

    /// <summary>Ensures the 5 Defender-specific files are present, downloading the full manifest if not.</summary>
    Task<bool> EnsureDefenderFilesAsync();

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
