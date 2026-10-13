using System.Text;

namespace BrainRotDoctor.Starter;

/// <summary>
/// The downloadable setup file is the starter PE plus the real app appended
/// with a 16-byte footer (payload size, then magic). Tests and unpackaged
/// runs can also point at an exe via <see cref="PayloadEnvVar"/> or a sibling
/// <c>BrainRotDoctor.exe</c>.
/// </summary>
internal static class SetupPayload
{
    public const string Magic = "BRDSET01";
    public const string PayloadEnvVar = "BRAINROTDOCTOR_PAYLOAD";
    public const string ExeName = "BrainRotDoctor.exe";

    public static string Materialize(string selfPath)
    {
        string? fromEnv = Environment.GetEnvironmentVariable(PayloadEnvVar);
        if (!string.IsNullOrWhiteSpace(fromEnv) && File.Exists(fromEnv))
        {
            return Path.GetFullPath(fromEnv);
        }

        if (TryGetOverlay(selfPath, out long offset, out long size))
        {
            string dest = Path.Combine(
                Path.GetTempPath(), "BrainRotDoctor", "setup-payload", ExeName);
            ExtractOverlay(selfPath, dest, offset, size);
            return dest;
        }

        string sibling = Path.Combine(Path.GetDirectoryName(selfPath) ?? "", ExeName);
        if (File.Exists(sibling) && !PathsEqual(sibling, selfPath))
        {
            return Path.GetFullPath(sibling);
        }

        throw new InvalidOperationException("This setup file is incomplete.");
    }

    public static void Append(string starterPath, string appPath, string outPath)
    {
        byte[] starter = File.ReadAllBytes(starterPath);
        byte[] app = File.ReadAllBytes(appPath);
        string? dir = Path.GetDirectoryName(outPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        using var output = new FileStream(outPath, FileMode.Create, FileAccess.Write, FileShare.None);
        output.Write(starter);
        output.Write(app);
        output.Write(BitConverter.GetBytes((long)app.Length));
        output.Write(Encoding.ASCII.GetBytes(Magic));
    }

    public static bool TryGetOverlay(string selfPath, out long offset, out long size)
    {
        offset = 0;
        size = 0;
        var info = new FileInfo(selfPath);
        if (!info.Exists || info.Length < 16)
        {
            return false;
        }

        using var stream = new FileStream(selfPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        stream.Seek(-16, SeekOrigin.End);
        Span<byte> footer = stackalloc byte[16];
        stream.ReadExactly(footer);
        size = BitConverter.ToInt64(footer);
        string magic = Encoding.ASCII.GetString(footer.Slice(8, 8));
        if (magic != Magic || size <= 0 || size > info.Length - 16)
        {
            return false;
        }

        offset = info.Length - 16 - size;
        return offset > 0;
    }

    public static void ExtractOverlay(string selfPath, string destPath, long offset, long size)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
        if (File.Exists(destPath) && new FileInfo(destPath).Length == size)
        {
            return;
        }

        Exception? last = null;
        for (int attempt = 0; attempt < LaunchRetry.MaxAttempts; attempt++)
        {
            try
            {
                using var source = new FileStream(selfPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                source.Seek(offset, SeekOrigin.Begin);
                using var dest = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None);
                CopyExactly(source, dest, size);

                return;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                last = ex;
                Thread.Sleep(LaunchRetry.DelayMs);
            }
        }

        throw last ?? new IOException("Could not extract the app from the setup file.");
    }

    private static void CopyExactly(Stream source, Stream dest, long size)
    {
        byte[] buffer = new byte[1024 * 1024];
        long remaining = size;
        while (remaining > 0)
        {
            int read = source.Read(buffer, 0, (int)Math.Min(buffer.Length, remaining));
            if (read <= 0)
            {
                throw new InvalidOperationException("The setup file ended before the payload was complete.");
            }

            dest.Write(buffer, 0, read);
            remaining -= read;
        }
    }

    private static bool PathsEqual(string a, string b) =>
        string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);
}
