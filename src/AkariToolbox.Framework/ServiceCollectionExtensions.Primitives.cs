using Microsoft.Extensions.DependencyInjection;
using AkariToolbox.Framework.Services;

namespace AkariToolbox.Framework;

/// <summary>
/// Extension methods for registering Akari Toolbox's system-primitive services
/// (registry, and later service-controller/script-runner/log-console) in a DI
/// container. This is the one registration call site for the primitive layer —
/// later plans extend this same method rather than adding a second one.
/// </summary>
public static class AkariPrimitivesServiceCollectionExtensions
{
    /// <summary>Registers the system-primitive services shared by every tweak handler.</summary>
    public static IServiceCollection AddAkariSystemPrimitives(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IRegistryService, RegistryService>();

        return services;
    }
}
