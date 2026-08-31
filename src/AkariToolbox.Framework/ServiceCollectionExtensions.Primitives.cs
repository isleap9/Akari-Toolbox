using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using AkariToolbox.Framework.Services;

namespace AkariToolbox.Framework;

/// <summary>
/// Extension methods for registering Akari Toolbox's system-primitive services
/// (registry, log console, service controller, script runner) in a DI
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

        // Captured at first resolution, which happens on the UI thread when the
        // MainWindow singleton (and its LogConsole property) is first constructed.
        services.AddSingleton<ILogConsoleService>(_ => new LogConsoleService(DispatcherQueue.GetForCurrentThread()));

        services.AddSingleton<IWindowsServiceController, WindowsServiceController>();

        services.AddSingleton<IScriptRunner, ScriptRunner>();

        return services;
    }
}
