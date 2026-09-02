using AkariToolbox.App.Models;

namespace AkariToolbox.App.Services;

/// <summary>
/// Compiled-in winget app-installer catalog (DOWNLOADS-02, D-01) — a static list, not an
/// <see cref="ITweakHandler"/> registry, mirroring <see cref="IDebloatCatalog"/>'s shape.
/// </summary>
public interface IAppCatalog
{
    /// <summary>Every catalog row, in declared category order.</summary>
    IReadOnlyList<AppDefinition> Apps { get; }
}
