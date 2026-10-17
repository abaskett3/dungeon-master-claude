using static Dmc.Tests.Distribution.Bootstrap;

namespace Dmc.Tests.Distribution;

/// <summary>
/// <c>play.ps1</c> and <c>play.sh</c>, with a fake <c>claude</c>. Play must never break: with no release pinned, or
/// when dmc cannot be had, a launcher starts Claude Code itself as it always has; with one pinned, it fetches dmc and
/// hands over to <c>bin/dmc play</c>. Either way Claude Code starts in the toolkit root with the same arguments.
/// </summary>
[TestClass]
public sealed class LauncherTests
{
    /// <summary>The arguments a player gives the launcher; one holds a space, to show it is passed on as one argument.</summary>
    static readonly string[] PlayersArgs = ["--continue", "two words"];

    /// <summary>Runs the launcher with <see cref="PlayersArgs"/>, with the fake claude folder first on the PATH and the archives taken from <paramref name="assets"/>.</summary>
    static Dmc.Tests.Support.ProcessResult Launch(string shell, ToolkitCopy toolkit, string claude, string assets) =>
        RunScript(shell, toolkit, "play", PlayersArgs,
            new Dictionary<string, string?> { ["DMC_GET_TEST_ASSETS"] = assets, ["PATH"] = PathWith(claude) });

    /// <summary>Asserts that claude ran with exit code 7 (which the launcher passes on), in the toolkit copy's folder, with the launchers' arguments and then the player's.</summary>
    static void AssertClaudeStarted(Dmc.Tests.Support.ProcessResult result, ToolkitCopy toolkit)
    {
        Assert.AreEqual(7, result.ExitCode, $"the launcher passes on claude's exit code:\n{result}");
        var (folder, args) = ClaudeSaw(result);
        // Git Bash prints the folder as /c/..., everything else as the OS writes it: compare by the folder's unique name.
        StringAssert.EndsWith(folder.TrimEnd('/', '\\'), Path.GetFileName(toolkit.Root), result.ToString());
        CollectionAssert.AreEqual(ClaudeArgs(PlayersArgs), args, result.ToString());
    }

    [TestMethod]
    [DynamicData(nameof(Shells), typeof(Bootstrap))]
    public void WithNoReleasePinnedTheLauncherStartsClaudeItself(string shell)
    {
        string claude = FakeClaude();
        using var toolkit = new ToolkitCopy("", new Dictionary<string, string>());
        try
        {
            var result = Launch(shell, toolkit, claude, Path.GetTempPath());
            AssertClaudeStarted(result, toolkit);
            Assert.AreEqual("", result.Stderr, "nothing but Claude Code's own output");
            Assert.IsFalse(Directory.Exists(toolkit.Bin), "get-dmc is not run");
        }
        finally
        {
            Directory.Delete(claude, recursive: true);
        }
    }

    /// <summary>
    /// With no <c>claude</c> on the PATH (an empty folder stands in for the fake's), no release pinned, a launcher must
    /// start nothing and exit 127. play.ps1 once exited 0 here: a command that cannot be found sets no exit code.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(Shells), typeof(Bootstrap))]
    public void WhenClaudeIsNotInstalledTheLauncherExits127(string shell)
    {
        string noClaude = Path.Combine(Path.GetTempPath(), "dmc-no-claude-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(noClaude);
        using var toolkit = new ToolkitCopy("", new Dictionary<string, string>());
        try
        {
            var result = Launch(shell, toolkit, noClaude, Path.GetTempPath());
            Assert.AreEqual(127, result.ExitCode, $"no claude, no session:\n{result}");
            Assert.IsFalse(result.Stdout.Contains("fake claude in ", StringComparison.Ordinal), result.ToString());
            StringAssert.Contains(result.Stderr, "claude", result.ToString());
        }
        finally
        {
            Directory.Delete(noClaude, recursive: true);
        }
    }

    [TestMethod]
    [DynamicData(nameof(Shells), typeof(Bootstrap))]
    public void WithAReleasePinnedTheLauncherFetchesDmcAndHandsOverToIt(string shell)
    {
        var release = PublishedRelease();
        string claude = FakeClaude();
        using var toolkit = new ToolkitCopy(release.Tag, new Dictionary<string, string> { [Rid] = release.Sha256 });
        try
        {
            var result = Launch(shell, toolkit, claude, release.Assets);
            AssertClaudeStarted(result, toolkit);
            Assert.AreEqual($"get-dmc: installed dmc {release.Version} ({Rid}) in bin/", result.StdoutLines[0], result.ToString());
            // No "dmc is not available" line (so the launcher took bin/dmc), and no stale-binary warning from dmc.
            Assert.AreEqual("", result.Stderr, result.ToString());
            Assert.IsTrue(File.Exists(toolkit.Exe));

            var again = Launch(shell, toolkit, claude, release.Assets);
            AssertClaudeStarted(again, toolkit);
            Assert.AreEqual("", again.Stderr, again.ToString());
            StringAssert.StartsWith(again.StdoutLines[0], "fake claude in ", "get-dmc says nothing the second time");
        }
        finally
        {
            Directory.Delete(claude, recursive: true);
        }
    }

    [TestMethod]
    [DynamicData(nameof(Shells), typeof(Bootstrap))]
    public void WhenDmcCannotBeHadTheLauncherStartsClaudeItself(string shell) => StartsClaudeWithoutDmc(shell, brokenDmc: false);

    /// <summary>A bin/dmc that cannot run (blocked, damaged, its executable bit lost) does not get the session either.</summary>
    [TestMethod]
    [DynamicData(nameof(Shells), typeof(Bootstrap))]
    public void WhenBinDmcDoesNotRunTheLauncherStartsClaudeItself(string shell) => StartsClaudeWithoutDmc(shell, brokenDmc: true);

    /// <summary>
    /// A release is pinned but its archive is not available. The launcher must still start claude, after two lines
    /// on stderr that say what went wrong; with <paramref name="brokenDmc"/>, an existing <c>bin/dmc</c> that cannot run is there too.
    /// </summary>
    static void StartsClaudeWithoutDmc(string shell, bool brokenDmc)
    {
        string claude = FakeClaude();
        // A release is pinned, but its archive cannot be had (no network, say): here, it is not in the folder.
        using var toolkit = new ToolkitCopy("dmc-v9.8.7", new Dictionary<string, string> { [Rid] = new string('a', 64) });
        if (brokenDmc)
        {
            Directory.CreateDirectory(toolkit.Bin);
            File.WriteAllText(toolkit.Exe, "not a program\n");
            if (!OperatingSystem.IsWindows())
                File.SetUnixFileMode(toolkit.Exe, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
        try
        {
            var result = Launch(shell, toolkit, claude, Path.GetTempPath());
            AssertClaudeStarted(result, toolkit);
            Assert.HasCount(2, result.StderrLines, result.ToString());
            StringAssert.StartsWith(result.StderrLines[0], $"get-dmc: {Asset("dmc-v9.8.7", Rid)} is not in ");
            Assert.AreEqual("play: dmc is not available, so Claude Code starts without it, and DMC cannot roll dice, draw maps or read PDFs until get-dmc succeeds.", result.StderrLines[1]);
            Assert.AreEqual(brokenDmc, File.Exists(toolkit.Exe));
        }
        finally
        {
            Directory.Delete(claude, recursive: true);
        }
    }
}
