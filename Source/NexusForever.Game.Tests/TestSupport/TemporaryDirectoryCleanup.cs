using System.Diagnostics;

namespace NexusForever.Game.Tests.TestSupport;

internal static class TemporaryDirectoryCleanup
{
    internal static void Delete(string path)
    {
        var clock = Stopwatch.StartNew();
        Run(() => Directory.Delete(path, true), () => clock.Elapsed, () => Thread.Sleep(25), OperatingSystem.IsWindows());
    }

    internal static void Run(Action cleanup, Func<TimeSpan> elapsed, Action delay, bool windows)
    {
        while (true)
        {
            try { cleanup(); return; }
            catch (IOException error) when (windows && (error.HResult & 0xffff) is 32 or 33 && elapsed() < TimeSpan.FromSeconds(2))
            {
                delay();
            }
        }
    }
}
