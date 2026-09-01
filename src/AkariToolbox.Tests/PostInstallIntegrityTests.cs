using System.Security.Cryptography;
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
}
