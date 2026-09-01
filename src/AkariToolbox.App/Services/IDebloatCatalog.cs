using AkariToolbox.App.Models;

namespace AkariToolbox.App.Services;

/// <summary>The compiled-in, static 28-action/5-category Debloat catalog (DEBLOAT-01).</summary>
public interface IDebloatCatalog
{
    IReadOnlyList<DebloatAction> Actions { get; }
}
