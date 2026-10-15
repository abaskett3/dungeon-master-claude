using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Dmc.Commands;
using Dmc.Tests.Support;
using static Dmc.Tests.Distribution.Bootstrap;

namespace Dmc.Tests.Distribution;

/// <summary><c>dmc play</c>, the stale-binary warning, and the pin they rely on.</summary>
[TestClass]
public sealed class PlayTests
{
    [TestMethod]
    public void PlayStartsClaudeFromTheToolkitRootWithThePlayersArguments()
    {
        ProcessStartInfo? started = null;
        int exit = PlayCommand.Run(["--continue", "two words"], "/somewhere/claude", info =>
        {
            started = info;
            return 7;
        });
        Assert.AreEqual(7, exit, "claude's exit code is dmc's");
        Assert.IsNotNull(started);
        Assert.AreEqual("/somewhere/claude", started.FileName);
        Assert.AreEqual(Dmc.Toolkit.Root, started.WorkingDirectory);
        Assert.IsFalse(started.UseShellExecute);
        Assert.IsFalse(started.RedirectStandardInput || started.RedirectStandardOutput || started.RedirectStandardError,
            "claude gets the terminal");
        CollectionAssert.AreEqual(ClaudeArgs("--continue", "two words"), started.ArgumentList.ToArray());
    }

    /// <summary>The published binary finds claude on the PATH (claude.cmd on Windows) and starts it in the toolkit root.</summary>
    [TestMethod]
    public void ThePublishedDmcFindsClaudeOnThePath()
    {
        string claude = FakeClaude();
        try
        {
            var result = TestEnv.Run(DmcRunner.Binary(), ["play", "--continue", "two words"], cwd: Path.GetTempPath(),
                environment: new Dictionary<string, string?> { ["PATH"] = PathWith(claude) });
            Assert.AreEqual(7, result.ExitCode, result.ToString());
            Assert.AreEqual("", result.Stderr, result.ToString());
            var (folder, args) = ClaudeSaw(result);
            Assert.AreEqual(TestEnv.Root.TrimEnd(Path.DirectorySeparatorChar), folder.TrimEnd(Path.DirectorySeparatorChar));
            CollectionAssert.AreEqual(ClaudeArgs("--continue", "two words"), args);
        }
        finally
        {
            Directory.Delete(claude, recursive: true);
        }
    }

    const string Pin = """{ "about": "x", "tag": "TAG", "sha256": { } }""";

    [TestMethod]
    [DataRow("", "0.1.0", null, DisplayName = "no release pinned yet")]
    [DataRow("dmc-v0.1.0", "0.1.0", null, DisplayName = "the pinned version")]
    [DataRow("dmc-v0.2.0", "0.1.0", "dmc: this is dmc 0.1.0, but the toolkit pins dmc-v0.2.0: run ./tools/get-dmc.ps1 (PowerShell) or bash tools/get-dmc.sh to update it", DisplayName = "another version")]
    [DataRow("v0.2.0", "0.1.0", null, DisplayName = "not a dmc tag")]
    public void TheStaleBinaryWarningComparesTheVersionWithThePin(string tag, string version, string? expected)
    {
        Assert.AreEqual(expected, StaleBinaryCheck.Warning(Pin.Replace("TAG", tag), version));
    }

    [TestMethod]
    public void TheStaleBinaryCheckIsSilentWhenThePinIsNotJson()
    {
        foreach (string text in new[] { "", "not json", "[]", """{ "tag": 5 }""" })
            Assert.IsNull(StaleBinaryCheck.Warning(text, "0.1.0"), text);
    }

    /// <summary>
    /// The pin and the project's version agree once a release is pinned (the release workflow's pull request sets both),
    /// so that a build from this checkout is not reported as stale. And the pin is in the form get-dmc.sh reads.
    /// </summary>
    [TestMethod]
    public void ThePinIsReadableAndMatchesTheProjectsVersion()
    {
        string text = File.ReadAllText(Path.Combine(TestEnv.Root, "tools", "dmc-release.json"));
        using var pin = JsonDocument.Parse(text);
        string tag = pin.RootElement.GetProperty("tag").GetString()!;
        var hashes = pin.RootElement.GetProperty("sha256").EnumerateObject().ToArray();
        if (tag.Length == 0)
        {
            Assert.IsEmpty(hashes, "no hashes without a tag");
            return;
        }
        StringAssert.Matches(tag, new Regex(@"^dmc-v\d+\.\d+\.\d+$"));
        string project = File.ReadAllText(Path.Combine(TestEnv.Root, "src", "Dmc.Cli", "Dmc.Cli.csproj"));
        StringAssert.Contains(project, $"<Version>{tag["dmc-v".Length..]}</Version>");
        var lines = text.ReplaceLineEndings("\n").Split('\n');
        Assert.IsTrue(lines.Any(l => Regex.IsMatch(l, $"^\\s*\"tag\"\\s*:\\s*\"{tag}\"")), "the tag on a line of its own");
        foreach (var hash in hashes)
        {
            Assert.IsTrue(lines.Any(l => Regex.IsMatch(l, $"^\\s*\"{hash.Name}\"\\s*:\\s*\"[0-9a-f]{{64}}\"")),
                $"the SHA-256 of {hash.Name} on a line of its own");
        }
    }
}
