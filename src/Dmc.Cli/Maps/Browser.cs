using System.Diagnostics;

namespace Dmc.Maps;

/// <summary>Opens a page in the default browser: shell execute on Windows, <c>xdg-open</c> on Linux.</summary>
public static class Browser
{
    /// <summary>The program and arguments that open an address on this OS.</summary>
    /// <remarks>
    /// On Linux the opener runs under <c>sh</c>, in the background, with all three of its streams sent to
    /// <c>/dev/null</c>: it must not write into the pipes of the program that started it (whose reader may be gone by
    /// then, and a write to a closed pipe kills the writer), and it must not leave anything on this program's output.
    /// </remarks>
    /// <param name="address">What to open: the <c>file:///</c> address of the page.</param>
    public static ProcessStartInfo StartInfo(string address)
    {
        if (OperatingSystem.IsWindows())
            return new ProcessStartInfo(address) { UseShellExecute = true };
        var start = new ProcessStartInfo("/bin/sh") { UseShellExecute = false };
        start.ArgumentList.Add("-c");
        start.ArgumentList.Add("\"$0\" \"$1\" >/dev/null 2>&1 </dev/null &");
        start.ArgumentList.Add("xdg-open");
        start.ArgumentList.Add(address);
        return start;
    }

    /// <summary>
    /// Open the address. Like the script, it says nothing if there is no browser to open: a failure to start the
    /// opener (no <c>xdg-open</c>, no shell association) is swallowed, and so the exit code stays 0.
    /// </summary>
    /// <param name="address">What to open.</param>
    public static void Open(string address)
    {
        try
        {
            using var process = Process.Start(StartInfo(address));
            process?.WaitForExit(); // sh returns at once: the opener is left running in the background
        }
        catch (Exception e) when (e is System.ComponentModel.Win32Exception or InvalidOperationException or PlatformNotSupportedException)
        {
        }
    }
}
