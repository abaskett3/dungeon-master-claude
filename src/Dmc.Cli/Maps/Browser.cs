using System.Diagnostics;

namespace Dmc.Maps;

/// <summary>Opens a page in the default browser: shell execute on Windows, <c>open</c> on macOS, <c>xdg-open</c> on Linux.</summary>
public static class Browser
{
    /// <summary>The program and arguments that open an address on this OS.</summary>
    public static ProcessStartInfo StartInfo(string address)
    {
        if (OperatingSystem.IsWindows())
            return new ProcessStartInfo(address) { UseShellExecute = true };
        var start = new ProcessStartInfo(OperatingSystem.IsMacOS() ? "open" : "xdg-open")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        start.ArgumentList.Add(address);
        return start;
    }

    /// <summary>Open the address. Like the script, it says nothing if there is no browser to open.</summary>
    public static void Open(string address)
    {
        try
        {
            using var process = Process.Start(StartInfo(address));
        }
        catch (Exception e) when (e is System.ComponentModel.Win32Exception or InvalidOperationException or PlatformNotSupportedException)
        {
        }
    }
}
