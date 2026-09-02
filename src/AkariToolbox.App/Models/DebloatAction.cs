namespace AkariToolbox.App.Models;

/// <summary>
/// A single row in the compiled-in 28-action Debloat catalog. This is deliberately NOT an
/// <c>ITweakHandler</c> — Debloat has no live-state read-back, so there is no
/// <c>GetState</c>/<c>SetState</c> here (D-01); a Debloat action either runs (or undoes) a
/// fixed embedded PowerShell script, it does not report an on/off toggle state.
/// </summary>
/// <param name="Key">Stable identifier used to look up this action from a <see cref="Models.DebloatActionItem"/>.</param>
/// <param name="Title">Display title.</param>
/// <param name="Description">Display description.</param>
/// <param name="Category">One of the 5 predecessor categories (Privacy &amp; Telemetry, System &amp; Performance, Cleanup, Explorer &amp; UI, Tools).</param>
/// <param name="RunResourceSuffix">Embedded resource suffix passed to <c>IScriptRunner.RunEmbeddedScriptAsync</c> for the forward (Run) direction.</param>
/// <param name="UndoResourceSuffix">Embedded resource suffix for the Undo direction, or <c>null</c> if this action has no Undo.</param>
/// <param name="RequiresConfirmation">
/// True for the 6 risk-classified actions: the original 5 D-11 actions (BitLocker,
/// Bloatware, Edge &amp; WebView removal, Hibernation, OneDrive removal) plus
/// <c>storesearch</c>, added post-launch per 03-REVIEW.md CR-03 — its Run script applies an
/// effectively irreversible-by-average-user <c>icacls /deny Everyone:F</c> ACL change and
/// previously shipped with both a broken Undo and no confirmation gate. Gates only the Run
/// (forward) direction.
/// </param>
/// <param name="UndoDownloadsUnverifiedBinary">
/// D-10 accepted-risk flag: true for the 2 actions whose Undo branch downloads an
/// unverified installer binary (no SHA256/signature verification), so the ViewModel can
/// surface the same accepted-risk log line the D-06 driver-tools actions already use.
/// </param>
public sealed record DebloatAction(
    string Key,
    string Title,
    string Description,
    string Category,
    string RunResourceSuffix,
    string? UndoResourceSuffix,
    bool RequiresConfirmation,
    bool UndoDownloadsUnverifiedBinary = false);
