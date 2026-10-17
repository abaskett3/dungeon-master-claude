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
        int exit = PlayCommand.Run(["--continue", "two words"], new PlayCommand.Launch("/somewhere/pwsh", ["-File", "claude.ps1"]), info =>
        {
            started = info;
            return 7;
        });
        Assert.AreEqual(7, exit, "claude's exit code is dmc's");
        Assert.IsNotNull(started);
        Assert.AreEqual("/somewhere/pwsh", started.FileName);
        Assert.AreEqual(Dmc.Toolkit.Root, started.WorkingDirectory);
        Assert.IsFalse(started.UseShellExecute);
        Assert.IsFalse(started.RedirectStandardInput || started.RedirectStandardOutput || started.RedirectStandardError,
            "claude gets the terminal");
        CollectionAssert.AreEqual(new[] { "-File", "claude.ps1" }.Concat(ClaudeArgs("--continue", "two words")).ToArray(),
            started.ArgumentList.ToArray());
    }

    /// <summary>Folders holding empty files of the given names, joined as a PATH; deleted afterwards.</summary>
    static void WithPath(string[][] folders, Action<string[], string> test)
    {
        string[] dirs = folders.Select(_ => Path.Combine(Path.GetTempPath(), "dmc-path-" + Guid.NewGuid().ToString("N"))).ToArray();
        try
        {
            for (int i = 0; i < dirs.Length; i++)
            {
                Directory.CreateDirectory(dirs[i]);
                foreach (string name in folders[i])
                    File.WriteAllText(Path.Combine(dirs[i], name), "");
            }
            test(dirs, string.Join(Path.PathSeparator, dirs));
        }
        finally
        {
            foreach (string dir in dirs)
                Directory.Delete(dir, recursive: true);
        }
    }

    // The three cases of Windows, which FindClaude takes as a flag so that they run on every OS.

    [TestMethod]
    public void OnWindowsClaudeExeAnywhereOnThePathComesBeforeNpmsShims()
    {
        WithPath([["claude.cmd", "claude.ps1"], ["claude.exe"]], (dirs, path) =>
        {
            var launch = PlayCommand.FindClaude(path, windows: true);
            Assert.AreEqual(Path.Combine(dirs[1], "claude.exe"), launch.File);
            Assert.IsEmpty(launch.Prefix);
        });
    }

    [TestMethod]
    public void OnWindowsNpmsShimRunsThroughPowerShellNotCmd()
    {
        WithPath([["claude.cmd", "claude.ps1"], ["pwsh.exe"]], (dirs, path) =>
        {
            var launch = PlayCommand.FindClaude(path, windows: true);
            Assert.AreEqual(Path.Combine(dirs[1], "pwsh.exe"), launch.File);
            CollectionAssert.AreEqual(new[] { "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", Path.Combine(dirs[0], "claude.ps1") },
                launch.Prefix.ToArray());
        });
    }

    [TestMethod]
    public void OnWindowsALoneCmdShimIsOneClearLine()
    {
        WithPath([["claude.cmd"]], (dirs, path) =>
        {
            var error = Assert.ThrowsExactly<DmcException>(() => PlayCommand.FindClaude(path, windows: true));
            StringAssert.StartsWith(error.Message, $"only {Path.Combine(dirs[0], "claude.cmd")} was found, and dmc does not start Claude Code through cmd.exe");
        });
    }

    /// <summary>
    /// The published binary finds claude on the PATH (on Windows npm's claude.ps1, never its claude.cmd) and starts it
    /// in the toolkit root.
    /// </summary>
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

    /// <summary>A pin file with a <c>TAG</c> placeholder, which the tests below replace with the tag they want.</summary>
    const string Pin = """{ "about": "x", "tag": "TAG", "sha256": { } }""";

    [TestMethod]
    [DataRow("", "0.1.0", null, DisplayName = "no release pinned yet")]
    [DataRow("dmc-v0.1.0", "0.1.0", null, DisplayName = "the pinned version")]
    [DataRow("dmc-v0.2.0", "0.1.0", "dmc: this is dmc 0.1.0, but the toolkit pins dmc-v0.2.0: run bash tools/get-dmc.sh (in PowerShell, powershell -ExecutionPolicy Bypass -File tools/get-dmc.ps1) to update it", DisplayName = "another version")]
    [DataRow("v0.2.0", "0.1.0", null, DisplayName = "not a dmc tag")]
    [DataRow("dmc-v0.2.0", "0.0.0-dev", null, DisplayName = "a build made on this computer is not a release")]
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
    /// The pin is in the form get-dmc.sh reads. The project's version stays the development one, which no release has:
    /// the release workflow gets a release's version from its tag and never writes it into the project, so a build
    /// made here cannot pass for the pinned release.
    /// </summary>
    [TestMethod]
    public void ThePinIsReadableAndTheProjectsVersionIsTheDevelopmentOne()
    {
        string text = File.ReadAllText(Path.Combine(TestEnv.Root, "tools", "dmc-release.json"));
        using var pin = JsonDocument.Parse(text);
        string project = File.ReadAllText(Path.Combine(TestEnv.Root, "src", "Dmc.Cli", "Dmc.Cli.csproj"));
        StringAssert.Contains(project, "<Version>0.0.0-dev</Version>");
        string tag = pin.RootElement.GetProperty("tag").GetString()!;
        var hashes = pin.RootElement.GetProperty("sha256").EnumerateObject().ToArray();
        if (tag.Length == 0)
        {
            Assert.IsEmpty(hashes, "no hashes without a tag");
            return;
        }
        StringAssert.Matches(tag, new Regex(@"^dmc-v\d+\.\d+\.\d+$"));
        var lines = text.ReplaceLineEndings("\n").Split('\n');
        Assert.IsTrue(lines.Any(l => Regex.IsMatch(l, $"^\\s*\"tag\"\\s*:\\s*\"{tag}\"")), "the tag on a line of its own");
        foreach (var hash in hashes)
        {
            Assert.IsTrue(lines.Any(l => Regex.IsMatch(l, $"^\\s*\"{hash.Name}\"\\s*:\\s*\"[0-9a-f]{{64}}\"")),
                $"the SHA-256 of {hash.Name} on a line of its own");
        }
    }
}
