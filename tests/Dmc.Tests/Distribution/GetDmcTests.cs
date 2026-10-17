using static Dmc.Tests.Distribution.Bootstrap;

namespace Dmc.Tests.Distribution;

/// <summary>
/// <c>tools/get-dmc.sh</c> and <c>tools/get-dmc.ps1</c> (issue #3, "How a player gets dmc"), run with every shell a
/// player may have. The archives come from a local folder through <c>DMC_GET_TEST_ASSETS</c>, which exists for the
/// tests only; the hash check applies to them as to a download.
/// </summary>
[TestClass]
public sealed class GetDmcTests
{
    /// <summary>The environment that points get-dmc at a local folder of archives, plus any further variables (a null value removes one).</summary>
    static Dictionary<string, string?> From(string assets, params (string Name, string? Value)[] more)
    {
        var environment = new Dictionary<string, string?> { ["DMC_GET_TEST_ASSETS"] = assets };
        foreach (var (name, value) in more)
            environment[name] = value;
        return environment;
    }

    /// <summary>
    /// Asserts that get-dmc refused: exit code 1, nothing on stdout, one stderr line starting "get-dmc: " and holding
    /// <paramref name="message"/>, and nothing installed or left in <c>bin/</c>.
    /// </summary>
    static void AssertRefused(Dmc.Tests.Support.ProcessResult result, ToolkitCopy toolkit, string message)
    {
        Assert.AreEqual(1, result.ExitCode, result.ToString());
        Assert.AreEqual("", result.Stdout, result.ToString());
        Assert.HasCount(1, result.StderrLines, result.ToString());
        StringAssert.StartsWith(result.StderrLines[0], "get-dmc: ");
        StringAssert.Contains(result.StderrLines[0], message);
        Assert.IsFalse(File.Exists(toolkit.Exe), "nothing may be installed");
        Assert.IsFalse(Directory.Exists(toolkit.Bin) && Directory.EnumerateFileSystemEntries(toolkit.Bin).Any(),
            "nothing may be left in bin/");
    }

    [TestMethod]
    [DynamicData(nameof(Shells), typeof(Bootstrap))]
    public void InstallsThePinnedReleaseThenSaysNothing(string shell)
    {
        var release = PublishedRelease();
        using var toolkit = new ToolkitCopy(release.Tag, new Dictionary<string, string> { [Rid] = release.Sha256 });

        // A build made on this computer is already in bin/. It says 0.0.0-dev, not the pinned version, so it is replaced.
        Directory.CreateDirectory(toolkit.Bin);
        File.Copy(Dmc.Tests.Support.DmcRunner.Binary(), toolkit.Exe);
        var local = Dmc.Tests.Support.TestEnv.Run(toolkit.Exe, ["--version"]);
        Assert.AreEqual("dmc 0.0.0-dev\n", local.Stdout, local.ToString());

        var first = RunScript(shell, toolkit, "tools/get-dmc", environment: From(release.Assets));
        Assert.AreEqual(0, first.ExitCode, first.ToString());
        Assert.AreEqual($"get-dmc: installed dmc {release.Version} ({Rid}) in bin/", string.Join("\n", first.StdoutLines), first.ToString());
        Assert.AreEqual("", first.Stderr, first.ToString());
        Assert.IsTrue(File.Exists(Path.Combine(toolkit.Bin, NativeStandIn)), "every file of the archive goes into bin/");
        Assert.AreEqual(0, Directory.EnumerateDirectories(toolkit.Bin).Count(), "no temporary folder is left in bin/");
        var installed = Dmc.Tests.Support.TestEnv.Run(toolkit.Exe, ["--version"]);
        Assert.AreEqual($"dmc {release.Version}\n", installed.Stdout, installed.ToString());

        var again = RunScript(shell, toolkit, "tools/get-dmc", environment: From(release.Assets));
        Assert.AreEqual(0, again.ExitCode, again.ToString());
        Assert.AreEqual("", again.Stdout + again.Stderr, "nothing is printed when dmc is current");
    }

    [TestMethod]
    [DynamicData(nameof(Shells), typeof(Bootstrap))]
    public void RefusesAnArchiveThatDoesNotMatchThePin(string shell)
    {
        var release = PublishedRelease();
        using var toolkit = new ToolkitCopy(release.Tag, new Dictionary<string, string> { [Rid] = new string('0', 64) });
        var result = RunScript(shell, toolkit, "tools/get-dmc", environment: From(release.Assets));
        AssertRefused(result, toolkit, $"{Asset(release.Tag, Rid)} does not match its SHA-256");
    }

    [TestMethod]
    [DynamicData(nameof(Shells), typeof(Bootstrap))]
    public void SaysSoWhenNoReleaseIsPinned(string shell)
    {
        using var toolkit = new ToolkitCopy("", new Dictionary<string, string>());
        var result = RunScript(shell, toolkit, "tools/get-dmc", environment: From(Path.GetTempPath()));
        AssertRefused(result, toolkit, "no dmc release is pinned yet");
    }

    /// <summary>
    /// Which archive get-dmc.sh asks for on each system, from <c>uname -s</c> and <c>uname -m</c> (a fake uname first on
    /// the PATH). Each archive in the folder is junk that matches no pinned hash, so the refusal names the archive picked.
    /// </summary>
    [TestMethod]
    [DataRow("Linux", "x86_64", "linux-x64")]
    [DataRow("Linux", "aarch64", "linux-arm64")]
    [DataRow("Darwin", "arm64", "osx-arm64")]
    [DataRow("Darwin", "x86_64", "not built for Intel Macs")]
    [DataRow("Linux", "armv7l", "not built for armv7l processors")]
    [DataRow("FreeBSD", "amd64", "not built for FreeBSD")]
    public void BashPicksTheArchiveForThisSystem(string system, string machine, string expected)
    {
        string fake = Path.Combine(Path.GetTempPath(), "dmc-fake-uname-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(fake);
        try
        {
            string uname = Path.Combine(fake, "uname");
            File.WriteAllText(uname, $"#!/bin/sh\ncase \"$1\" in -s) echo {system} ;; -m) echo {machine} ;; esac\n");
            if (!OperatingSystem.IsWindows())
                File.SetUnixFileMode(uname, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            var (toolkit, assets) = JunkReleases();
            using (toolkit)
            {
                var result = RunScript("bash", toolkit, "tools/get-dmc", environment: From(assets), pathFirst: fake);
                AssertRefused(result, toolkit, expected.Contains(' ') ? expected : $"{Asset(JunkTag, expected)} does not match its SHA-256");
            }
        }
        finally
        {
            Directory.Delete(fake, recursive: true);
        }
    }

    /// <summary>
    /// On Windows, the machine types the system can name (through <c>DMC_GET_TEST_MACHINE</c>, which stands in for the
    /// registry), and this computer as the registry says. Elsewhere PowerShell 7 asks .NET, which cannot be made to lie,
    /// so the one case is this computer.
    /// </summary>
    public static IEnumerable<object?[]> Machines { get; } = OperatingSystem.IsWindows()
        ?
        [
            ["AMD64", "win-x64"],
            ["ARM64", "win-arm64"],
            ["x86", "not built for x86 processors on Windows"],
            [null, Rid],
        ]
        : [[null, Rid]];

    /// <summary>
    /// Which archive get-dmc.ps1 asks for: Windows PowerShell 5.1 on Windows, PowerShell 7 elsewhere. On Windows the
    /// process says it is AMD64 every time, as one emulated on Arm64 does, and must not be believed.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(Machines))]
    public void PowerShellPicksTheArchiveForThisComputer(string? machine, string expected)
    {
        var (toolkit, assets) = JunkReleases();
        using (toolkit)
        {
            var result = OperatingSystem.IsWindows()
                ? RunScript("powershell", toolkit, "tools/get-dmc", environment: From(assets,
                    ("DMC_GET_TEST_MACHINE", machine), ("PROCESSOR_ARCHITECTURE", "AMD64"), ("PROCESSOR_ARCHITEW6432", null)))
                : RunScript("pwsh", toolkit, "tools/get-dmc", environment: From(assets));
            AssertRefused(result, toolkit, expected.Contains(' ') ? expected : $"{Asset(JunkTag, expected)} does not match its SHA-256");
        }
    }

    /// <summary>The tag of the made-up release of <see cref="JunkReleases"/>.</summary>
    const string JunkTag = "dmc-v9.8.7";

    /// <summary>A toolkit pinning every runtime identifier to a hash that its archive, which is junk, does not have.</summary>
    static (ToolkitCopy, string Assets) JunkReleases()
    {
        string[] rids = ["win-x64", "win-arm64", "linux-x64", "linux-arm64", "osx-arm64"];
        var toolkit = new ToolkitCopy(JunkTag, rids.ToDictionary(r => r, _ => new string('a', 64)));
        string assets = Path.Combine(toolkit.Root, "assets");
        Directory.CreateDirectory(assets);
        foreach (string rid in rids)
            File.WriteAllText(Path.Combine(assets, Asset(JunkTag, rid)), "junk");
        return (toolkit, assets);
    }
}
