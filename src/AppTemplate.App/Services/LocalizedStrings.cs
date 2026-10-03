using System.ComponentModel;
using System.Reflection;
using System.Resources;

namespace AkariToolbox.Services;

/// <summary>
/// Localized string accessor. Exposes resources by key and raises
/// <see cref="INotifyPropertyChanged"/> when the culture changes so that
/// x:Bind function bindings (e.g. <c>{x:Bind Strings.Get("Key")}</c>) re-evaluate.
/// </summary>
public sealed class LocalizedStrings : INotifyPropertyChanged
{
    private readonly ResourceManager _resources = new(
        ResolveBaseName(typeof(LocalizedStrings).Assembly),
        typeof(LocalizedStrings).Assembly);

    /// <summary>
    /// Resolves the neutral resource base name from the assembly manifest.
    /// The MSBuild <c>RootNamespace</c> (AkariToolbox) deliberately differs from this type's CLR
    /// namespace, so the embedded .resx name cannot be derived from the type - probe the manifest
    /// for the resource set instead so renames cannot silently break it again.
    /// </summary>
    private static string ResolveBaseName(Assembly assembly)
    {
        const string suffix = ".resources";
        const string marker = ".Resources.Resources";
        foreach (string name in assembly.GetManifestResourceNames())
        {
            if (name.EndsWith(marker + suffix, StringComparison.OrdinalIgnoreCase))
            {
                return name[..^suffix.Length];
            }
        }

        return "AkariToolbox.Resources.Resources";
    }

    /// <summary>Returns the localized string for <paramref name="key"/> (or the key itself when missing).</summary>
    public string Get(string key) => _resources.GetString(key) ?? key;

    /// <summary>Indexer form: <c>Strings["Key"]</c>.</summary>
    public string this[string key] => Get(key);

    /// <summary>Raised when resources should be re-read (culture change).</summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Tells all bound elements to re-resolve their localized text.</summary>
    public void Refresh()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
    }
}
