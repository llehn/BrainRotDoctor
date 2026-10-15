using System.IO;
using System.Net.Http;
using System.Text.Json;

namespace BrainRotDoctor.App.Runtime.Update;

/// <summary>
/// The silent auto-updater. On a timer it asks the release source for the latest
/// build, verifies the signed manifest and the downloaded exe's hash, and — only
/// for a strictly newer version — stages the binary and launches the applier.
/// Every failure is swallowed: a check that can't complete simply leaves the
/// running app untouched and retries next cycle. There is no user-facing UI by
/// design; applied versions are recorded to a local history log.
/// </summary>
internal sealed class UpdateService : IDisposable
{
    public const string ManifestAssetName = "update.json";
    public const string SignatureAssetName = "update.json.sig";

    private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(2);
    public static readonly TimeSpan CheckInterval = TimeSpan.FromHours(12);
    public static readonly TimeSpan FrequentCheckInterval = TimeSpan.FromMinutes(1);

    private readonly IReleaseSource _source;
    private readonly UpdateSignature _signature;
    private readonly UpdatePaths _paths;
    private readonly IApplyLauncher _launcher;
    private readonly Version _currentVersion;
    private readonly CancellationTokenSource _stop = new();
    private Task? _loop;

    public UpdateService(
        IReleaseSource source,
        UpdateSignature signature,
        UpdatePaths paths,
        IApplyLauncher launcher,
        Version currentVersion)
    {
        _source = source;
        _signature = signature;
        _paths = paths;
        _launcher = launcher;
        _currentVersion = currentVersion;
    }

    /// <summary>Builds the production updater wired to GitHub Releases.</summary>
    public static UpdateService CreateDefault(string installedExePath)
    {
        var http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        return new UpdateService(
            new GitHubReleaseSource("llehn", "BrainRotDoctor", http),
            new UpdateSignature(),
            UpdatePaths.ForCurrentProcess(installedExePath),
            new DetachedApplyLauncher(),
            AppVersion.Current);
    }

    public void Start()
    {
        _loop ??= Task.Run(() => RunLoopAsync(_stop.Token));
    }

    private async Task RunLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(InitialDelay, cancellationToken).ConfigureAwait(false);
            while (!cancellationToken.IsCancellationRequested)
            {
                await CheckOnceAsync(cancellationToken).ConfigureAwait(false);
                await Task.Delay(NextCheckDelay(), cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    /// <summary>
    /// Twice a day normally; every minute while the developer opt-in file
    /// (<see cref="UpdatePaths.FrequentChecksFlagPath"/>) exists. Re-read each
    /// cycle, so removing the file returns to the normal pace after one more check.
    /// </summary>
    public TimeSpan NextCheckDelay() =>
        File.Exists(_paths.FrequentChecksFlagPath) ? FrequentCheckInterval : CheckInterval;

    /// <summary>
    /// One check cycle. Returns true if a verified newer build was staged and the
    /// applier launched. Never throws — transient/network/verification problems
    /// are treated as "no update available".
    /// </summary>
    public async Task<bool> CheckOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            ReleaseInfo? release = await _source.GetLatestReleaseAsync(cancellationToken).ConfigureAwait(false);
            if (release is null)
            {
                return false;
            }

            ReleaseAsset? manifestAsset = release.FindAsset(ManifestAssetName);
            ReleaseAsset? signatureAsset = release.FindAsset(SignatureAssetName);
            if (manifestAsset is null || signatureAsset is null)
            {
                return false;
            }

            byte[] manifestBytes = await _source.DownloadBytesAsync(manifestAsset.DownloadUrl, cancellationToken).ConfigureAwait(false);
            byte[] signatureBytes = await _source.DownloadBytesAsync(signatureAsset.DownloadUrl, cancellationToken).ConfigureAwait(false);

            // Reject anything not signed by the release key before trusting a byte of it.
            if (!_signature.Verify(manifestBytes, signatureBytes))
            {
                return false;
            }

            UpdateManifest? manifest = UpdateManifest.Parse(manifestBytes);
            if (manifest?.SemanticVersion is null)
            {
                return false;
            }

            // Forward only: never reinstall the same version or roll back.
            if (manifest.SemanticVersion <= _currentVersion)
            {
                return false;
            }

            ReleaseAsset? exeAsset = release.FindAsset(manifest.Asset);
            if (exeAsset is null)
            {
                return false;
            }

            ResetStagingDir();
            await _source.DownloadFileAsync(exeAsset.DownloadUrl, _paths.StagedExePath, cancellationToken).ConfigureAwait(false);

            if (!UpdateSignature.VerifyFileHash(_paths.StagedExePath, manifest.Sha256))
            {
                ResetStagingDir();
                return false;
            }

            WriteApplyInfo(manifest);
            _launcher.Launch(_paths);
            return true;
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or TaskCanceledException
                                       or UnauthorizedAccessException or JsonException)
        {
            return false;
        }
    }

    private void ResetStagingDir()
    {
        try
        {
            if (Directory.Exists(_paths.StagedDir))
            {
                Directory.Delete(_paths.StagedDir, recursive: true);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }

        Directory.CreateDirectory(_paths.StagedDir);
    }

    private void WriteApplyInfo(UpdateManifest manifest)
    {
        var info = new ApplyInfo { Version = manifest.Version, Sha256 = manifest.Sha256 };
        File.WriteAllText(info.FilePath(_paths), JsonSerializer.Serialize(info));
    }

    public void Dispose()
    {
        _stop.Cancel();
        _stop.Dispose();
    }
}
