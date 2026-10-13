using System.IO;
using System.Text;
using BrainRotDoctor.Starter;
using Xunit;

namespace BrainRotDoctor.App.Tests;

public sealed class SetupPayloadTests : IDisposable
{
    private readonly string _dir =
        Path.Combine(Path.GetTempPath(), "brd-setup-payload-tests", Guid.NewGuid().ToString("N"));

    public SetupPayloadTests() => Directory.CreateDirectory(_dir);

    [Fact]
    public void Append_then_extract_round_trips_the_payload_bytes()
    {
        string starter = Path.Combine(_dir, "starter.exe");
        string app = Path.Combine(_dir, "app.exe");
        string packed = Path.Combine(_dir, "setup.exe");
        string extracted = Path.Combine(_dir, "out", "BrainRotDoctor.exe");
        File.WriteAllBytes(starter, Encoding.ASCII.GetBytes("STARTER"));
        File.WriteAllBytes(app, Encoding.ASCII.GetBytes("THE-REAL-APP-BYTES"));

        SetupPayload.Append(starter, app, packed);

        Assert.True(SetupPayload.TryGetOverlay(packed, out long offset, out long size));
        Assert.Equal("STARTER".Length, offset);
        Assert.Equal("THE-REAL-APP-BYTES".Length, size);

        SetupPayload.ExtractOverlay(packed, extracted, offset, size);
        Assert.Equal("THE-REAL-APP-BYTES", File.ReadAllText(extracted));
    }

    [Fact]
    public void TryGetOverlay_rejects_a_bare_starter_with_no_footer()
    {
        string starter = Path.Combine(_dir, "bare.exe");
        File.WriteAllBytes(starter, Encoding.ASCII.GetBytes("just-a-starter-no-payload"));

        Assert.False(SetupPayload.TryGetOverlay(starter, out _, out _));
    }

    [Fact]
    public void Materialize_uses_the_payload_environment_variable()
    {
        string payload = Path.Combine(_dir, "BrainRotDoctor.exe");
        File.WriteAllText(payload, "payload");
        string previous = Environment.GetEnvironmentVariable(SetupPayload.PayloadEnvVar) ?? "";
        try
        {
            Environment.SetEnvironmentVariable(SetupPayload.PayloadEnvVar, payload);
            Assert.Equal(Path.GetFullPath(payload), SetupPayload.Materialize(Path.Combine(_dir, "missing-self.exe")));
        }
        finally
        {
            Environment.SetEnvironmentVariable(
                SetupPayload.PayloadEnvVar,
                string.IsNullOrEmpty(previous) ? null : previous);
        }
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_dir, recursive: true);
        }
        catch (Exception)
        {
        }
    }
}
