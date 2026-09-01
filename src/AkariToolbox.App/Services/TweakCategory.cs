namespace AkariToolbox.App.Services;

/// <summary>
/// Discriminates which page an <see cref="ITweakHandler"/> renders on —
/// <c>AkariOS</c> for <c>AkariOSTweaksViewModel</c>, <c>Gaming</c> for
/// <c>GamingTweaksViewModel</c>. <see cref="ITweakCatalog.Handlers"/> itself stays
/// one flat, unfiltered list (RESEARCH.md Pattern 1); each page's ViewModel filters
/// on this discriminator rather than the catalog exposing category-scoped views.
/// </summary>
public enum TweakCategory
{
    AkariOS,
    Gaming,
}
