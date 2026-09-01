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

        // IPostInstallService/PostInstallService are App-project types (Defender's only
        // dependency — Plan 01-06). Registered here rather than in the Framework
        // project's AddAkariSystemPrimitives (as the plan's action text originally
        // specified) because AkariToolbox.Framework has no ProjectReference to
        // AkariToolbox.App — adding one there while App already references Framework
        // would be a circular project reference and fail to build (deviation Rule 3:
        // auto-fixed blocking issue). This is the closest existing App-project
        // registration method, extended rather than replaced by a new one.
        services.AddHttpClient("PostInstall", c =>
        {
            c.DefaultRequestHeaders.Add("User-Agent", "AkariToolbox");
            c.Timeout = TimeSpan.FromMinutes(10);
        });
        services.AddSingleton<IPostInstallService, PostInstallService>();

        return services;
    }
}
