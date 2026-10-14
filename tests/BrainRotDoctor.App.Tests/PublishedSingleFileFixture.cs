using System.Diagnostics;
using System.IO;
using Xunit;

namespace BrainRotDoctor.App.Tests;

/// <summary>
/// Publishes the app the same way a release does (single-file, natives
/// extracted on start) so launch tests hit the real host/extract path.
/// </summary>
public sealed class PublishedSingleFileFixture : IDisposable
{
    public string ExePath { get; }

    public PublishedSingleFileFixture()
    {
        string repoRoot = FindRepoRoot();
        string output = Path.Combine(Path.GetTempPath(), "brd-test-singlefile-publish");
        Directory.CreateDirectory(output);

        var psi = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = repoRoot,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        psi.ArgumentList.Add("publish");
        psi.ArgumentList.Add("src/BrainRotDoctor.App");
        psi.ArgumentList.Add("-c");
        psi.ArgumentList.Add("Release");
        psi.ArgumentList.Add("-r");
        psi.ArgumentList.Add("win-x64");
        psi.ArgumentList.Add("--self-contained");
        psi.ArgumentList.Add("true");
        psi.ArgumentList.Add("-p:PublishSingleFile=true");
        psi.ArgumentList.Add("-p:IncludeNativeLibrariesForSelfExtract=true");
        psi.ArgumentList.Add("-p:EnableCompressionInSingleFile=true");
        psi.ArgumentList.Add("-o");
        psi.ArgumentList.Add(output);

        using Process process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start dotnet publish.");
        string stdout = process.StandardOutput.ReadToEnd();
        string stderr = process.StandardError.ReadToEnd();
        if (!process.WaitForExit(180_000))
        {
            try { process.Kill(entireProcessTree: true); } catch (InvalidOperationException) { }

            throw new TimeoutException("Timed out publishing the single-file app for extract-lock tests.");
        }

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                "dotnet publish failed (" + process.ExitCode + "):\n" + stdout + "\n" + stderr);
        }

        ExePath = Path.Combine(output, "BrainRotDoctor.exe");
        if (!File.Exists(ExePath))
        {
            throw new InvalidOperationException("Publish succeeded but " + ExePath + " is missing.");
        }
    }

    public void Dispose()
    {
    }

    private static string FindRepoRoot()
    {
        string? dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "BrainRotDoctor.slnx")))
            {
                return dir;
            }

            dir = Path.GetDirectoryName(dir);
        }

        throw new InvalidOperationException("Could not find BrainRotDoctor.slnx from " + AppContext.BaseDirectory);
    }
}

[CollectionDefinition("SingleFileLaunch")]
public sealed class SingleFileLaunchCollection : ICollectionFixture<PublishedSingleFileFixture>
{
}
