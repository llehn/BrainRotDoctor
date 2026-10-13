using System.Runtime.InteropServices;

namespace BrainRotDoctor.Starter;

internal static partial class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            string self = Environment.ProcessPath
                ?? throw new InvalidOperationException("Cannot resolve the setup path.");
            string payload = SetupPayload.Materialize(self);
            LaunchRetry.RunUntilAlive(new ExeStarter(payload, args));
            return 0;
        }
        catch (Exception ex)
        {
            MessageBoxW(IntPtr.Zero, ex.Message, "BrainRotDoctor", 0x00000010);
            return 1;
        }
    }

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);
}
