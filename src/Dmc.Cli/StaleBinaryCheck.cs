using System.Text.Json;
using System.Text.RegularExpressions;

namespace Dmc;

/// <summary>
/// The stale-binary warning. <c>tools/dmc-release.json</c> pins the release the toolkit expects (its <c>tag</c>,
/// <c>dmc-vX.Y.Z</c>). When this <c>dmc</c> is a different version, for example after a <c>git pull</c> in a session
/// not started through a launcher, it prints one line to stderr naming the command that fetches the pinned one, and
/// carries on.
/// </summary>
/// <remarks>
/// It says nothing when the file is missing, unreadable, or pins no release yet (an empty tag), as in a developer's
/// checkout before the first release. Commands with <see cref="ICommand.NeverFails"/> (the hook) never reach it.
/// </remarks>
public static partial class StaleBinaryCheck
{
    public static string ReleaseFile { get; } = Path.Combine(Toolkit.Root, "tools", "dmc-release.json");

    public const string TagPrefix = "dmc-v";

    public static void Run(TextWriter err)
    {
        string? pin;
        try
        {
            pin = File.ReadAllText(ReleaseFile);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return;
        }
        string? warning = Warning(pin, CommandLine.Version);
        if (warning is not null)
            err.WriteLine(warning);
    }

    /// <summary>
    /// The warning for a pin file's text and this program's version, or null when there is nothing to say. Only a
    /// release (X.Y.Z, the form the release tags, get-dmc and write-pin.sh use) can be out of date. A build made on this
    /// computer says <c>0.0.0-dev</c>, is not a release, and is left alone: whoever built it did so on purpose (and it
    /// does not live in <c>bin/</c>, which the launchers own).
    /// </summary>
    internal static string? Warning(string pin, string version)
    {
        string? tag = PinnedTag(pin);
        if (tag is null || !tag.StartsWith(TagPrefix, StringComparison.Ordinal) || tag[TagPrefix.Length..] == version)
            return null;
        if (!ReleaseVersion().IsMatch(version))
            return null;
        return $"dmc: this is dmc {version}, but the toolkit pins {tag}: run bash tools/get-dmc.sh (in PowerShell, powershell -ExecutionPolicy Bypass -File tools/get-dmc.ps1) to update it";
    }

    /// <summary>A release version: three numbers, as in a release tag <c>dmc-vX.Y.Z</c>.</summary>
    [GeneratedRegex(@"^[0-9]+\.[0-9]+\.[0-9]+$")]
    private static partial Regex ReleaseVersion();

    /// <summary>The <c>tag</c> of a pin file; null when the text is not a pin, or the tag is empty.</summary>
    internal static string? PinnedTag(string pin)
    {
        try
        {
            using var document = JsonDocument.Parse(pin);
            if (document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("tag", out var tag)
                && tag.ValueKind == JsonValueKind.String)
            {
                string? value = tag.GetString();
                return string.IsNullOrEmpty(value) ? null : value;
            }
        }
        catch (JsonException)
        {
        }
        return null;
    }
}
