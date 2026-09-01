namespace AkariToolbox.App.Services;

/// <summary>
/// Live-read/live-write abstraction for the two non-boolean registry dropdowns
/// GAMING-01 requires — SvcHost split threshold and Win32 Priority Separation.
/// Deliberately NOT an <see cref="ITweakHandler"/>: neither value is a boolean
/// state and neither needs <see cref="ITweakCatalog"/>'s prior-value-capture /
/// revert semantics (02-RESEARCH.md's Architectural Responsibility Map).
///
/// Preset lists are the D-09 checkpoint's approved "research-proposed" option
/// (02-05-PLAN.md's opening <c>checkpoint:decision</c> task, resolved by the
/// orchestrator via AskUserQuestion before this plan's tasks executed) — the
/// expanded 10-value SvcHost list and 13-value Win32PrioritySeparation list from
/// 02-RESEARCH.md, not the predecessor's narrower fixed lists.
/// </summary>
public interface IGamingDropdownService
{
    /// <summary>
    /// SvcHost split threshold presets, in display order. A <c>null</c>
    /// <c>ValueKb</c> represents "Default — delete the value" so Windows computes
    /// its own default, rather than writing either the predecessor's buggy
    /// decimal literal <c>380000</c> or a guessed hex-equivalent number
    /// (02-RESEARCH.md's Predecessor Discrepancy finding, Assumption A3).
    /// </summary>
    IReadOnlyList<(string Label, int? ValueKb)> SvcHostPresets { get; }

    /// <summary>
    /// Win32PrioritySeparation presets, in display order. No "Default = delete"
    /// case here — every preset writes a real DWORD hex value; there is no
    /// delete-equivalent in the approved list.
    /// </summary>
    IReadOnlyList<(string Label, int ValueHex)> Win32PriorityPresets { get; }

    /// <summary>
    /// Index into <see cref="SvcHostPresets"/> nearest the live
    /// <c>SvcHostSplitThresholdInKB</c> registry value: an exact match wins
    /// outright; otherwise the preset with the smallest absolute numeric
    /// distance is selected, and an exact tie between two presets breaks toward
    /// the lower-valued preset (this plan's explicit, deterministic tie-break
    /// contract — neither the source script nor RESEARCH.md defined one). When
    /// the registry value is absent, returns the index of the "Default" preset.
    /// </summary>
    int GetSvcHostPresetIndex();

    /// <summary>
    /// Validates <paramref name="index"/> against <c>[0, SvcHostPresets.Count)</c>
    /// before performing any registry write; an out-of-range index (negative, or
    /// equal to <c>SvcHostPresets.Count</c>) is rejected with no write performed.
    /// The "Default" preset deletes <c>SvcHostSplitThresholdInKB</c> entirely;
    /// every other preset writes its <c>ValueKb</c> as a DWORD.
    /// </summary>
    void SetSvcHostPreset(int index);

    /// <summary>
    /// Index into <see cref="Win32PriorityPresets"/> nearest the live
    /// <c>Win32PrioritySeparation</c> registry value, using the same
    /// nearest-match / tie-break-to-lower contract as
    /// <see cref="GetSvcHostPresetIndex"/>. When the registry value is absent it
    /// is treated as <c>0</c> (the value Windows uses when the key is unset) and
    /// matched through the same nearest-preset algorithm — there is no separate
    /// "Default" preset entry to special-case here.
    /// </summary>
    int GetWin32PriorityPresetIndex();

    /// <summary>
    /// Validates <paramref name="index"/> against
    /// <c>[0, Win32PriorityPresets.Count)</c> before performing any registry
    /// write; an out-of-range index is rejected with no write performed. Always
    /// writes the preset's <c>ValueHex</c> as a DWORD — no delete case.
    /// </summary>
    void SetWin32PriorityPreset(int index);
}
