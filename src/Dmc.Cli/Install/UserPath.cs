using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace Dmc.Install;

/// <summary>
/// The user's <c>Path</c> on Windows, as a seam: <see cref="ProgramInstall.AddToUserPath"/> works on this, and a test passes a
/// fake that holds the value in memory, so no test touches the real registry or the real PATH.
/// </summary>
public interface IUserPath
{
    /// <summary>The user's <c>Path</c> as stored, with its <c>%VARIABLES%</c> not expanded; null when the user has none.</summary>
    string? Read();

    /// <summary>Stores the user's <c>Path</c> (as an expandable string, so <c>%VARIABLES%</c> in it keep working).</summary>
    /// <param name="value">The new value.</param>
    void Write(string value);

    /// <summary>Tells running programs that the environment changed, so a new terminal sees it.</summary>
    void Broadcast();
}

/// <summary>
/// The real user <c>Path</c>: the value <c>Path</c> under <c>HKEY_CURRENT_USER\Environment</c>, followed by a
/// <c>WM_SETTINGCHANGE</c> broadcast so that Explorer and new terminals pick it up without signing out.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed partial class RegistryUserPath : IUserPath
{
    /// <summary>The registry key under <c>HKEY_CURRENT_USER</c> that holds a user's environment variables.</summary>
    const string Key = "Environment";

    /// <summary>The value in <see cref="Key"/> that is the user's <c>Path</c>.</summary>
    const string Name = "Path";

    /// <inheritdoc />
    public string? Read()
    {
        using var key = Registry.CurrentUser.OpenSubKey(Key);
        return key?.GetValue(Name, null, RegistryValueOptions.DoNotExpandEnvironmentNames) as string;
    }

    /// <inheritdoc />
    public void Write(string value)
    {
        using var key = Registry.CurrentUser.CreateSubKey(Key, writable: true);
        key.SetValue(Name, value, RegistryValueKind.ExpandString);
    }

    /// <inheritdoc />
    public void Broadcast() => SendMessageTimeout(HwndBroadcast, WmSettingChange, UIntPtr.Zero, "Environment", SmtoAbortIfHung, 5000, out _);

    /// <summary><c>HWND_BROADCAST</c>: every top-level window.</summary>
    static readonly IntPtr HwndBroadcast = new(0xffff);

    /// <summary><c>WM_SETTINGCHANGE</c>: the message that says the environment changed.</summary>
    const uint WmSettingChange = 0x001A;

    /// <summary><c>SMTO_ABORTIFHUNG</c>: do not wait for a window that is not responding.</summary>
    const uint SmtoAbortIfHung = 0x0002;

    /// <summary>
    /// <c>SendMessageTimeoutW</c> of <c>user32.dll</c>: sends a message to every window, waiting at most <paramref name="timeout"/>
    /// milliseconds for each, so that a window that has hung cannot hold the install up.
    /// </summary>
    /// <param name="hWnd">The window, or <c>HWND_BROADCAST</c>.</param>
    /// <param name="msg">The message.</param>
    /// <param name="wParam">The message's first parameter.</param>
    /// <param name="lParam">The message's second parameter: for <c>WM_SETTINGCHANGE</c>, the name of what changed.</param>
    /// <param name="flags">How to send it (<c>SMTO_*</c>).</param>
    /// <param name="timeout">The longest to wait for each window, in milliseconds.</param>
    /// <param name="result">The result of the message; not used.</param>
    /// <returns>Non-zero when the message was sent.</returns>
    [LibraryImport("user32.dll", EntryPoint = "SendMessageTimeoutW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial IntPtr SendMessageTimeout(IntPtr hWnd, uint msg, UIntPtr wParam, string lParam, uint flags, uint timeout, out UIntPtr result);
}
