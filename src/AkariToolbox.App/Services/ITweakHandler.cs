namespace AkariToolbox.App.Services;

/// <summary>
/// Self-describing contract every registry/service-backed OS tweak implements.
/// One handler per tweak — <see cref="GetState"/> and <see cref="SetState"/> both
/// target the same real, live system value (no app-tracked flag, no private
/// state hive — D-03/D-04). The Defender tweak (Plan 01-06) is dispatched by
/// <see cref="ITweakCatalog"/> as a special case keyed on <c>"defender"</c> and
/// does not need async members added here.
/// </summary>
public interface ITweakHandler
{
    /// <summary>Stable key matching the corresponding <c>TweakItem.Key</c>.</summary>
    string Key { get; }

    string Title { get; }

    string Description { get; }

    /// <summary>
    /// Position in the predecessor's known 32-tweak sequence. Used to sort
    /// <see cref="ITweakCatalog.Handlers"/> so the UI list order matches the
    /// predecessor regardless of reflection-scan discovery order.
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Discriminates which page (<c>AkariOSTweaksViewModel</c> vs
    /// <c>GamingTweaksViewModel</c>) this handler renders on. <see cref="ITweakCatalog.Handlers"/>
    /// itself stays one flat, unfiltered list — each page's ViewModel filters on this
    /// discriminator (RESEARCH.md Pattern 1).
    /// </summary>
    TweakCategory Category { get; }

    /// <summary>Reads the real, live current state — never a cached/app-tracked flag.</summary>
    bool GetState();

    /// <summary>Applies the tweak. Idempotent: safe to call repeatedly with the same value.</summary>
    void SetState(bool enabled);
}
