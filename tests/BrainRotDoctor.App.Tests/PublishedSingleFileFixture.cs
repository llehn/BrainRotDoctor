using System.Diagnostics;
using System.IO;
using Xunit;

namespace BrainRotDoctor.App.Tests;

/// <summary>
/// Publishes the app the same way a release does (single-file, natives
/// extracted on start) and a self-contained starter so launch tests can
/// drive the retry path.
/// </summary>
public sealed class PublishedSingleFileFixture : IDisposable
{
    public string ExePath { get; }
    public string StarterExePath { get; }

    public PublishedSingleFileFixture()
    {
        string repoRoot = FindRepoRoot();
        ExePath = Publish(
            repoRoot,
            "src/BrainRotDoctor.App",
            Path.Combine(Path.GetTempPath(), "brd-test-singlefile-publish"),
            "BrainRotDoctor.exe",
            extra: ["-p:PublishSingleFile=true", "-p:IncludeNativeLibrariesForSelfExtract=true", "-p:EnableCompressionInSingleFile=true"]);
        StarterExePath = Publish(
            repoRoot,
            "src/BrainRotDoctor.Starter",
            Path.Combine(Path.GetTempPath(), "brd-test-starter-publish"),
            "BrainRotDoctor.Starter.exe",
            extra: ["-p:PublishSingleFile=true", "-p:PublishAot=false"]);
    }

    public void Dispose()
    {
    }

    private static string Publish(string repoRoot, string project, string output, string exeName, string[] extra)
    {
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
        psi.ArgumentList.Add(project);
        psi.ArgumentList.Add("-c");
        psi.ArgumentList.Add("Release");
        psi.ArgumentList.Add("-r");
        psi.ArgumentList.Add("win-x64");
        psi.ArgumentList.Add("--self-contained");
        psi.ArgumentList.Add("true");
        psi.ArgumentList.Add("-o");
        psi.ArgumentList.Add(output);
        foreach (string arg in extra)
        {
            psi.ArgumentList.Add(arg);
        }

        using Process process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start dotnet publish.");
        string stdout = process.StandardOutput.ReadToEnd();
        string stderr = process.StandardError.ReadToEnd();
        if (!process.WaitForExit(180_000))
        {
            try { process.Kill(entireProcessTree: true); } catch (InvalidOperationException) { }

            throw new TimeoutException("Timed out publishing " + project + ".");
        }

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                "dotnet publish failed (" + process.ExitCode + ") for " + project + ":\n" + stdout + "\n" + stderr);
        }

        string exe = Path.Combine(output, exeName);
        if (!File.Exists(exe))
        {
            throw new InvalidOperationException("Publish succeeded but " + exe + " is missing.");
        }

        return exe;
    }

    internal static string FindRepoRoot()
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
