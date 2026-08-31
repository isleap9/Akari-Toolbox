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

        return services;
    }
}
