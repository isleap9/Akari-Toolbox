using Microsoft.Extensions.DependencyInjection;
using AkariToolbox.App.Services.TweakHandlers;

namespace AkariToolbox.App.Services;

/// <summary>
/// The one registration call site for the tweak-handler layer. Reflection-scans
/// this assembly for every non-abstract <see cref="ITweakHandler"/> implementation
/// and registers each as a multi-bound <see cref="ITweakHandler"/> instance, plus
/// the <see cref="ITweakCatalog"/> singleton that orchestrates them. Every later
/// tweak-handler batch plan adds classes only — this method is never touched again.
/// </summary>
public static class TweakHandlerServiceCollectionExtensions
{
    public static IServiceCollection AddTweakHandlers(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var handlerTypes = typeof(WifiTweakHandler).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && typeof(ITweakHandler).IsAssignableFrom(type));

        foreach (var type in handlerTypes)
        {
            services.AddSingleton(typeof(ITweakHandler), type);
        }

        services.AddSingleton<ITweakCatalog, TweakCatalog>();

        // IPostInstallService/PostInstallService are App-project types (Plan 01-06).
        // Defender no longer depends on this service (its cab+ps1 payload is embedded
        // directly — see DefenderTweakHandler's doc comment); it stays registered here,
        // currently unused, ahead of the Phase 4 Downloads page (DOWNLOADS-01) which will
        // consume it for the general ~130-entry PostInstall asset mirror. Registered here
        // rather than in the Framework project's AddAkariSystemPrimitives (as the plan's
        // action text originally specified) because AkariToolbox.Framework has no
        // ProjectReference to AkariToolbox.App — adding one there while App already
        // references Framework would be a circular project reference and fail to build
        // (deviation Rule 3: auto-fixed blocking issue). This is the closest existing
        // App-project registration method, extended rather than replaced by a new one.
        services.AddHttpClient("PostInstall", c =>
        {
            c.DefaultRequestHeaders.Add("User-Agent", "AkariToolbox");
            c.Timeout = TimeSpan.FromMinutes(10);
        });
        services.AddSingleton<IPostInstallService, PostInstallService>();

        // GAMING-01's two registry dropdowns (SvcHost split threshold, Win32 Priority
        // Separation) — not an ITweakHandler (no boolean state, no revert semantics),
        // so it is registered here directly rather than picked up by the reflection
        // scan above (Plan 02-05).
        services.AddSingleton<IGamingDropdownService, GamingDropdownService>();

        // Debloat's compiled-in 28-action catalog (DEBLOAT-01) — not an ITweakHandler
        // either (no GetState/SetState, D-01), and not reflection-scanned since it is one
        // static catalog, not per-action handler classes, so it is registered here directly.
        services.AddSingleton<IDebloatCatalog, DebloatCatalog>();

        // Downloads' compiled-in 29-app winget catalog (DOWNLOADS-02, D-01) — same
        // rationale as IDebloatCatalog above: a static catalog, not per-app handler
        // classes, so it is registered here directly rather than reflection-scanned.
        services.AddSingleton<IAppCatalog, AppCatalog>();
        services.AddSingleton<IAppInstallerService, AppInstallerService>();

        return services;
    }
}
