using BrainRotDoctor.App.Runtime.Update;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Xunit;

namespace BrainRotDoctor.App.Tests;

public sealed class UpdateManifestTests
{
    [Fact]
    public void Parse_accepts_a_well_formed_manifest()
    {
        byte[] json = Bytes("""
        { "version": "1.2.0", "asset": "BrainRotDoctor.exe", "sha256": "abc123abc123abc123abc123abc123abc123abc123abc123abc123abc123abcd", "size": 100 }
        """);

        UpdateManifest? manifest = UpdateManifest.Parse(json);

        Assert.NotNull(manifest);
        Assert.Equal(new Version(1, 2, 0), manifest!.SemanticVersion);
        Assert.Equal("BrainRotDoctor.exe", manifest.Asset);
    }

    [Fact]
    public void Parse_rejects_malformed_json() =>
        Assert.Null(UpdateManifest.Parse(Bytes("{ not json")));

    [Fact]
    public void Parse_rejects_a_non_hex_sha256() =>
        Assert.Null(UpdateManifest.Parse(Bytes(
            """{ "version": "1.0.0", "asset": "a.exe", "sha256": "xyz", "size": 1 }""")));

    [Fact]
    public void Parse_rejects_a_missing_asset() =>
        Assert.Null(UpdateManifest.Parse(Bytes(
            """{ "version": "1.0.0", "sha256": "abc123abc123abc123abc123abc123abc123abc123abc123abc123abc123abcd" }""")));

    [Fact]
    public void Parse_rejects_an_unparseable_version() =>
        Assert.Null(UpdateManifest.Parse(Bytes(
            """{ "version": "latest", "asset": "a.exe", "sha256": "abc123abc123abc123abc123abc123abc123abc123abc123abc123abc123abcd" }""")));

    private static byte[] Bytes(string s) => Encoding.UTF8.GetBytes(s);
}

public sealed class UpdateSignatureTests
{
    [Fact]
    public void Verify_accepts_a_signature_from_the_matching_key()
    {
        using ECDsa key = ECDsa.Create(ECCurve.CreateFromFriendlyName("nistP256"));
        byte[] content = Encoding.UTF8.GetBytes("the manifest bytes");
        byte[] signature = Sign(key, content);

        var verifier = new UpdateSignature(key.ExportSubjectPublicKeyInfo());

        Assert.True(verifier.Verify(content, signature));
    }

    [Fact]
    public void Verify_rejects_tampered_content()
    {
        using ECDsa key = ECDsa.Create(ECCurve.CreateFromFriendlyName("nistP256"));
        byte[] signature = Sign(key, Encoding.UTF8.GetBytes("original"));

        var verifier = new UpdateSignature(key.ExportSubjectPublicKeyInfo());

        Assert.False(verifier.Verify(Encoding.UTF8.GetBytes("tampered"), signature));
    }

    [Fact]
    public void Verify_rejects_a_signature_from_a_different_key()
    {
        using ECDsa signer = ECDsa.Create(ECCurve.CreateFromFriendlyName("nistP256"));
        using ECDsa other = ECDsa.Create(ECCurve.CreateFromFriendlyName("nistP256"));
        byte[] content = Encoding.UTF8.GetBytes("content");

        var verifier = new UpdateSignature(other.ExportSubjectPublicKeyInfo());

        Assert.False(verifier.Verify(content, Sign(signer, content)));
    }

    [Fact]
    public void The_embedded_public_key_is_a_valid_p256_key()
    {
        using ECDsa ecdsa = ECDsa.Create();
        // Throws if the embedded base64 SPKI is malformed.
        ecdsa.ImportSubjectPublicKeyInfo(
            Convert.FromBase64String(UpdateSignature.EmbeddedPublicKeyBase64), out _);
        Assert.Equal(256, ecdsa.KeySize);
    }

    [Fact]
    public void VerifyFileHash_matches_the_files_sha256()
    {
        string path = Path.Combine(Path.GetTempPath(), $"brd-hash-{Guid.NewGuid():N}.bin");
        File.WriteAllBytes(path, new byte[] { 1, 2, 3, 4, 5 });
        try
        {
            string hash = UpdateSignature.ComputeFileSha256(path);
            Assert.True(UpdateSignature.VerifyFileHash(path, hash));
            Assert.True(UpdateSignature.VerifyFileHash(path, hash.ToUpperInvariant()));
            Assert.False(UpdateSignature.VerifyFileHash(path, new string('0', 64)));
        }
        finally
        {
            File.Delete(path);
        }
    }

    internal static byte[] Sign(ECDsa key, byte[] content) =>
        key.SignData(content, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
}

public sealed class UpdateServiceTests : IDisposable
{
    private readonly string _root;
    private readonly UpdatePaths _paths;
    private readonly ECDsa _key;
    private readonly UpdateSignature _signature;

    public UpdateServiceTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "brd-update-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        _paths = new UpdatePaths(Path.Combine(_root, "appdata"), Path.Combine(_root, "install", "BrainRotDoctor.exe"));
        _key = ECDsa.Create(ECCurve.CreateFromFriendlyName("nistP256"));
        _signature = new UpdateSignature(_key.ExportSubjectPublicKeyInfo());
    }

    [Fact]
    public async Task A_newer_signed_release_is_staged_and_the_applier_launched()
    {
        byte[] exe = RandomBytes(2048);
        var source = BuildSource("1.5.0", exe);
        var launcher = new FakeLauncher();
        var service = new UpdateService(source, _signature, _paths, launcher, new Version(1, 0, 0));

        bool staged = await service.CheckOnceAsync(CancellationToken.None);

        Assert.True(staged);
        Assert.Equal(1, launcher.Calls);
        Assert.True(File.Exists(_paths.StagedExePath));
        Assert.Equal(exe, File.ReadAllBytes(_paths.StagedExePath));
        Assert.True(File.Exists(Path.Combine(_paths.StagedDir, ApplyInfo.FileName)));
    }

    [Fact]
    public void Checks_run_twice_a_day_unless_the_frequent_checks_file_exists()
    {
        var service = new UpdateService(BuildSource("1.0.0", RandomBytes(64)), _signature, _paths, new FakeLauncher(), new Version(1, 0, 0));
        Assert.Equal(TimeSpan.FromHours(12), service.NextCheckDelay());

        Directory.CreateDirectory(_paths.UpdateDir);
        File.WriteAllText(_paths.FrequentChecksFlagPath, "");
        Assert.Equal(TimeSpan.FromMinutes(1), service.NextCheckDelay());

        File.Delete(_paths.FrequentChecksFlagPath);
        Assert.Equal(TimeSpan.FromHours(12), service.NextCheckDelay());
    }

    [Fact]
    public async Task The_same_version_is_not_applied()
    {
        var source = BuildSource("1.0.0", RandomBytes(64));
        var launcher = new FakeLauncher();
        var service = new UpdateService(source, _signature, _paths, launcher, new Version(1, 0, 0));

        Assert.False(await service.CheckOnceAsync(CancellationToken.None));
        Assert.Equal(0, launcher.Calls);
    }

    [Fact]
    public async Task An_older_version_is_refused_no_downgrade()
    {
        var source = BuildSource("0.9.0", RandomBytes(64));
        var launcher = new FakeLauncher();
        var service = new UpdateService(source, _signature, _paths, launcher, new Version(1, 0, 0));

        Assert.False(await service.CheckOnceAsync(CancellationToken.None));
        Assert.Equal(0, launcher.Calls);
    }

    [Fact]
    public async Task A_manifest_signed_by_the_wrong_key_is_refused()
    {
        using ECDsa attacker = ECDsa.Create(ECCurve.CreateFromFriendlyName("nistP256"));
        var source = BuildSource("2.0.0", RandomBytes(64), signingKey: attacker);
        var launcher = new FakeLauncher();
        var service = new UpdateService(source, _signature, _paths, launcher, new Version(1, 0, 0));

        Assert.False(await service.CheckOnceAsync(CancellationToken.None));
        Assert.Equal(0, launcher.Calls);
    }

    [Fact]
    public async Task A_binary_whose_hash_does_not_match_is_refused()
    {
        // Sign a manifest claiming a hash, but serve different exe bytes.
        var manifest = new { version = "2.0.0", asset = "BrainRotDoctor.exe", sha256 = new string('a', 64), size = 10 };
        byte[] manifestBytes = JsonSerializer.SerializeToUtf8Bytes(manifest);
        byte[] sig = UpdateSignatureTests.Sign(_key, manifestBytes);
        var source = new FakeReleaseSource();
        source.Release = new ReleaseInfo("v2.0.0", new[]
        {
            new ReleaseAsset(UpdateService.ManifestAssetName, "url://manifest"),
            new ReleaseAsset(UpdateService.SignatureAssetName, "url://sig"),
            new ReleaseAsset("BrainRotDoctor.exe", "url://exe"),
        });
        source.Bytes["url://manifest"] = manifestBytes;
        source.Bytes["url://sig"] = sig;
        source.Files["url://exe"] = RandomBytes(128); // hash will not match

        var launcher = new FakeLauncher();
        var service = new UpdateService(source, _signature, _paths, launcher, new Version(1, 0, 0));

        Assert.False(await service.CheckOnceAsync(CancellationToken.None));
        Assert.Equal(0, launcher.Calls);
        Assert.False(File.Exists(_paths.StagedExePath));
    }

    private FakeReleaseSource BuildSource(string version, byte[] exe, ECDsa? signingKey = null)
    {
        string sha = Convert.ToHexStringLower(SHA256.HashData(exe));
        var manifest = new { version, asset = "BrainRotDoctor.exe", sha256 = sha, size = (long)exe.Length };
        byte[] manifestBytes = JsonSerializer.SerializeToUtf8Bytes(manifest);
        byte[] sig = UpdateSignatureTests.Sign(signingKey ?? _key, manifestBytes);

        var source = new FakeReleaseSource
        {
            Release = new ReleaseInfo($"v{version}", new[]
            {
                new ReleaseAsset(UpdateService.ManifestAssetName, "url://manifest"),
                new ReleaseAsset(UpdateService.SignatureAssetName, "url://sig"),
                new ReleaseAsset("BrainRotDoctor.exe", "url://exe"),
            }),
        };
        source.Bytes["url://manifest"] = manifestBytes;
        source.Bytes["url://sig"] = sig;
        source.Files["url://exe"] = exe;
        return source;
    }

    private static byte[] RandomBytes(int n)
    {
        var b = new byte[n];
        Random.Shared.NextBytes(b);
        return b;
    }

    public void Dispose()
    {
        _key.Dispose();
        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    private sealed class FakeLauncher : IApplyLauncher
    {
        public int Calls { get; private set; }

        public void Launch(UpdatePaths paths) => Calls++;
    }

    private sealed class FakeReleaseSource : IReleaseSource
    {
        public ReleaseInfo? Release { get; set; }

        public Dictionary<string, byte[]> Bytes { get; } = new();

        public Dictionary<string, byte[]> Files { get; } = new();

        public Task<ReleaseInfo?> GetLatestReleaseAsync(CancellationToken cancellationToken) =>
            Task.FromResult(Release);

        public Task<byte[]> DownloadBytesAsync(string url, CancellationToken cancellationToken) =>
            Task.FromResult(Bytes[url]);

        public Task DownloadFileAsync(string url, string destinationPath, CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            File.WriteAllBytes(destinationPath, Files[url]);
            return Task.CompletedTask;
        }
    }
}
