namespace AkariToolbox.Tweaks;

/// <summary>
/// One live-state toggle from the old Akari OS Tweaks page: it reads the real system
/// state (registry / services / bcdedit) so the switch always reflects reality, applies
/// instantly on flip, and write-throughs its state key to HKCU\Software\AkariTool.
/// </summary>
public sealed class TweakToggle(
    string key,
    string title,
    string description,
    string stateKey,
    Func<bool> read,
    Action<bool> apply)
{
    /// <summary>Stable id used by the old app ("wifi", "defender", ).</summary>
    public string Key { get; } = key;

    public string Title { get; } = title;
    public string Description { get; } = description;

    /// <summary>The HKCU\Software\AkariTool value name this toggle persists to.</summary>
    public string StateKey { get; } = stateKey;

    /// <summary>Live system state  true means the toggle is ON.</summary>
    public Func<bool> Read { get; } = read;

    /// <summary>Applies the change for ON/OFF and updates the persisted state key.</summary>
    public Action<bool> Apply { get; } = apply;
}
