using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using AkariToolbox.App.Services;
using AkariToolbox.Framework.Services;
using Xunit;

namespace AkariToolbox.Tests;

/// <summary>
/// Covers this plan's must-have truth for <see cref="IPostInstallService.VerifyFileSha256Async"/>
/// (T-01-SC mitigation): a match returns <c>true</c>, a mismatch returns <c>false</c> with a
/// logged warning, and a missing file returns <c>false</c> without throwing. Independent of
/// the real downloaded PostInstall assets — exercises the primitive generically.
/// </summary>
public class PostInstallIntegrityTests
{
    /// <summary>A minimal <see cref="IHttpClientFactory"/> — never invoked by these tests.</summary>
    private sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }

    [Fact]
    public async Task VerifyFileSha256Async_matching_hash_returns_true()
    {
        var log = new LogConsoleService(dispatcher: null);
        var service = new PostInstallService(new FakeHttpClientFactory(), log);

        var tempFile = Path.GetTempFileName();
        try
        {
            var bytes = "akari-toolbox-integrity-check"u8.ToArray();
            await File.WriteAllBytesAsync(tempFile, bytes);
            var correctHex = Convert.ToHexStringLower(SHA256.HashData(bytes));

            var result = await service.VerifyFileSha256Async(tempFile, correctHex);

            Assert.True(result);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task VerifyFileSha256Async_mismatched_hash_returns_false()
    {
        var log = new LogConsoleService(dispatcher: null);
        var service = new PostInstallService(new FakeHttpClientFactory(), log);

        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllBytesAsync(tempFile, "akari-toolbox-integrity-check"u8.ToArray());

            var result = await service.VerifyFileSha256Async(tempFile, new string('0', 64));

            Assert.False(result);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task VerifyFileSha256Async_missing_file_returns_false_without_throwing()
    {
        var log = new LogConsoleService(dispatcher: null);
        var service = new PostInstallService(new FakeHttpClientFactory(), log);

        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"akari-missing-{Guid.NewGuid():N}.bin");

        var result = await service.VerifyFileSha256Async(nonExistentPath, new string('a', 64));

        Assert.False(result);
    }

    /// <summary>
    /// Loads the embedded <c>Resources/PostInstallManifest.json</c> (D-07/D-08) the same way
    /// <see cref="PostInstallService"/> itself does — by manifest-resource-name suffix match,
    /// via <see cref="PostInstallService"/>'s own assembly.
    /// </summary>
    private static Dictionary<string, string> LoadEmbeddedManifest()
    {
        var asm = typeof(PostInstallService).Assembly;
        var resourceName = asm.GetManifestResourceNames()
            .Single(n => n.EndsWith("PostInstallManifest.json", StringComparison.OrdinalIgnoreCase));

        using var stream = asm.GetManifestResourceStream(resourceName)!;
        return JsonSerializer.Deserialize<Dictionary<string, string>>(stream)!;
    }

    [Fact]
    public void PostInstallManifest_key_set_exactly_matches_AllFiles()
    {
        var manifest = LoadEmbeddedManifest();

        var manifestKeys = manifest.Keys.OrderBy(k => k, StringComparer.Ordinal);
        var allFilesKeys = PostInstallService.RelativeFilePaths.OrderBy(k => k, StringComparer.Ordinal);

        Assert.Equal(allFilesKeys, manifestKeys);
    }

    [Fact]
    public void PostInstallManifest_has_exactly_147_entries()
    {
        var manifest = LoadEmbeddedManifest();

        Assert.Equal(147, manifest.Count);
    }

    [Fact]
    public void PostInstallManifest_every_value_is_lowercase_hex_sha256()
    {
        var manifest = LoadEmbeddedManifest();
        var hexPattern = new Regex("^[0-9a-f]{64}$");

        Assert.All(manifest.Values, value => Assert.Matches(hexPattern, value));
    }
}
