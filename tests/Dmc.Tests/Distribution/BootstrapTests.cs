using System.Formats.Tar;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Dmc.Tests.Support;
using static Dmc.Tests.Distribution.Bootstrap;

namespace Dmc.Tests.Distribution;

/// <summary>
/// <c>tools/install.sh</c> and <c>tools/install.ps1</c>, the install line (contract <c>bootstrap.md</c>, FR-022), run with every
/// shell a player may have. The release is a local folder (<c>DMC_INSTALL_TEST_ASSETS</c>: <c>SHA256SUMS.txt</c>, the archive and
/// <c>latest.json</c> in the shape of the releases API's answer), which exists for the tests only; every hash check applies to it
/// as to a download. The program in the archive is the published development build, which installs the DMC folder only (it copies
/// itself as the program only with <c>--force</c>), and the child's home and temporary folders are temporary ones, so nothing
/// reaches the real home folder, PATH or registry. A shell that is missing skips the test locally and fails it on CI.
/// </summary>
[TestClass]
public sealed class BootstrapTests
{
    /// <summary>The runtime identifier of this computer, as the scripts pick it.</summary>
    static string Rid { get; } = (OperatingSystem.IsWindows() ? "win" : "linux") + "-" + (RuntimeInformation.OSArchitecture == Architecture.Arm64 ? "arm64" : "x64");

    /// <summary>The tag of the made-up release the tests serve.</summary>
    const string Tag = "dmc-v9.8.7";

    /// <summary>The name of the archive of the made-up release for a runtime identifier, as the release workflow names it.</summary>
    static string Asset(string rid) => $"{Tag}-{rid}.{(rid.StartsWith("win-", StringComparison.Ordinal) ? "zip" : "tar.gz")}";

    /// <summary>The shells that can run the whole install here: PowerShell 7 everywhere, Windows PowerShell 5.1 on Windows, bash on Linux (on Windows <c>install.sh</c> only refuses).</summary>
    public static IEnumerable<object[]> InstallShells { get; } =
        OperatingSystem.IsWindows() ? [["pwsh"], ["powershell"]] : [["bash"], ["pwsh"]];

    /// <summary>Every shell the scripts are run with: bash everywhere, PowerShell 7 everywhere, Windows PowerShell 5.1 on Windows.</summary>
    public static IEnumerable<object[]> AllShells { get; } =
        OperatingSystem.IsWindows() ? [["bash"], ["pwsh"], ["powershell"]] : [["bash"], ["pwsh"]];

    /// <summary>What to break in a release the tests make.</summary>
    /// <param name="Content">What the program in the archive holds; the published development build when null.</param>
    /// <param name="Corrupt">True to change a byte of the archive after its hashes were made.</param>
    /// <param name="WrongDigest">True to report a digest for the archive that is not its hash.</param>
    /// <param name="Latest">The text of <c>latest.json</c>; the API's answer for the release when null; no file at all when empty.</param>
    sealed record Release(byte[]? Content = null, bool Corrupt = false, bool WrongDigest = false, string? Latest = null);

    /// <summary>A folder holding a made-up release for this computer, and the folders a script run needs. Deleted on dispose.</summary>
    sealed class Assets : IDisposable
    {
        /// <summary>Everything this test owns.</summary>
        public string Root { get; } = DmcFolders.NewTemp();

        /// <summary>The release folder: <c>SHA256SUMS.txt</c>, the archive and <c>latest.json</c>.</summary>
        public string Folder => Path.Combine(Root, "release");

        /// <summary>The home folder the script's child sees.</summary>
        public string Home => Path.Combine(Root, "home");

        /// <summary>The temporary folder the script's child sees, which holds nothing once the script is done.</summary>
        public string Temp => Path.Combine(Root, "tmp");

        /// <summary>The DMC folder a test asks for.</summary>
        public string DmcFolder => Path.Combine(Root, "play", "dmc");

        /// <summary>The runtime identifier of the real archive made by <see cref="Make"/>.</summary>
        public string Rid => BootstrapTests.Rid;

        /// <summary>Makes the folders (empty).</summary>
        public Assets()
        {
            Directory.CreateDirectory(Folder);
            Directory.CreateDirectory(Home);
            Directory.CreateDirectory(Temp);
        }

        /// <summary>
        /// Writes the release: the archive for this computer (the program, and a stand-in for the native libraries), a checksum list
        /// that names an archive for each of the four platforms (the other three are junk files), and the API's answer.
        /// </summary>
        /// <param name="release">What to break.</param>
        public void Make(Release release)
        {
            string rid = Rid;
            string archive = Path.Combine(Folder, Asset(rid));
            byte[] program = release.Content ?? File.ReadAllBytes(DmcRunner.Binary());
            byte[] standIn = "not a library"u8.ToArray();
            if (OperatingSystem.IsWindows())
            {
                using var zip = ZipFile.Open(archive, ZipArchiveMode.Create);
                foreach (var (name, bytes) in new[] { (ExeName, program), ("native-library-stand-in.txt", standIn) })
                {
                    using var stream = zip.CreateEntry(name).Open();
                    stream.Write(bytes);
                }
            }
            else
            {
                using var file = File.Create(archive);
                using var gzip = new GZipStream(file, CompressionLevel.Fastest);
                using var tar = new TarWriter(gzip);
                foreach (var (name, bytes) in new[] { (ExeName, program), ("native-library-stand-in.txt", standIn) })
                    tar.WriteEntry(new PaxTarEntry(TarEntryType.RegularFile, name) { DataStream = new MemoryStream(bytes), Mode = (UnixFileMode)0b111_101_101 });
            }
            string hash = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(archive)));

            var sums = new StringBuilder().Append($"{hash}  {Asset(rid)}\n");
            var assets = new List<(string Name, string Digest)> { (Asset(rid), release.WrongDigest ? new string('b', 64) : hash) };
            foreach (string other in new[] { "win-x64", "win-arm64", "linux-x64", "linux-arm64" }.Where(r => r != rid))
            {
                sums.Append($"{new string('a', 64)}  {Asset(other)}\n");
                assets.Add((Asset(other), new string('a', 64)));
                File.WriteAllText(Path.Combine(Folder, Asset(other)), "junk");
            }
            File.WriteAllText(Path.Combine(Folder, "SHA256SUMS.txt"), sums.ToString(), new UTF8Encoding(false));
            if (release.Corrupt)
                File.AppendAllText(archive, "x");

            // The shape of GitHub's answer, pretty-printed as GitHub prints it (the scripts must read it either way).
            var assetJson = assets.Select(a => string.Join("\n",
                "    {",
                "      \"url\": \"https://api.github.com/repos/abaskett3/dungeon-master-claude/releases/assets/1\",",
                $"      \"name\": \"{a.Name}\",",
                "      \"label\": null,",
                "      \"uploader\": { \"login\": \"github-actions[bot]\", \"id\": 41898282 },",
                "      \"content_type\": \"application/octet-stream\",",
                "      \"size\": 123,",
                $"      \"digest\": \"sha256:{a.Digest}\",",
                "      \"download_count\": 0",
                "    }"));
            string json = release.Latest ?? string.Join("\n",
                "{",
                "  \"url\": \"https://api.github.com/repos/abaskett3/dungeon-master-claude/releases/1\",",
                $"  \"tag_name\": \"{Tag}\",",
                "  \"name\": \"dmc 9.8.7\",",
                "  \"author\": { \"login\": \"github-actions[bot]\", \"id\": 41898282 },",
                "  \"assets\": [",
                string.Join(",\n", assetJson),
                "  ]",
                "}");
            if (json.Length > 0)
                File.WriteAllText(Path.Combine(Folder, "latest.json"), json, new UTF8Encoding(false));
        }

        /// <summary>The environment for a script run: the release folder, a temporary home and temporary folders, and no Claude Code session.</summary>
        /// <param name="more">Further variables; a null value removes one.</param>
        public Dictionary<string, string?> Environment(params (string Name, string? Value)[] more)
        {
            var environment = new Dictionary<string, string?>
            {
                ["DMC_INSTALL_TEST_ASSETS"] = Folder,
                ["CLAUDECODE"] = null,
                ["USERPROFILE"] = Home, ["HOME"] = Home, ["LOCALAPPDATA"] = Path.Combine(Home, "AppData", "Local"), ["XDG_DATA_HOME"] = Path.Combine(Home, ".local", "share"),
                ["TEMP"] = Temp, ["TMP"] = Temp,
            };
            if (!OperatingSystem.IsWindows())
                environment["TMPDIR"] = Temp;
            foreach (var (name, value) in more)
                environment[name] = value;
            return environment;
        }

        /// <summary>Deletes everything the test owns.</summary>
        public void Dispose() => DmcFolders.Delete(Root);
    }

    /// <summary>Runs an install script with a shell: <c>bash tools/install.sh</c>, or PowerShell on <c>tools/install.ps1</c>. A folder is passed as <c>--path</c> or <c>-Path</c>.</summary>
    static Dmc.Tests.Support.ProcessResult Run(string shell, Assets assets, string? folder, params (string Name, string? Value)[] more) =>
        RunWith(shell, assets.Environment(more), folder);

    /// <summary>As <see cref="Run"/>, with an environment already made.</summary>
    static Dmc.Tests.Support.ProcessResult RunWith(string shell, Dictionary<string, string?> environment, string? folder)
    {
        string program = Bootstrap.Shell(shell);
        var args = new List<string>();
        switch (shell)
        {
            case "bash":
                args.Add(Path.Combine(TestEnv.Root, "tools", "install.sh"));
                if (folder is not null)
                    args.AddRange(["--path", folder]);
                break;
            case "pwsh":
                args.AddRange(["-NoProfile", "-File", Path.Combine(TestEnv.Root, "tools", "install.ps1")]);
                if (folder is not null)
                    args.AddRange(["-Path", folder]);
                break;
            default:
                args.AddRange(["-NoProfile", "-ExecutionPolicy", "Bypass", "-File", Path.Combine(TestEnv.Root, "tools", "install.ps1")]);
                if (folder is not null)
                    args.AddRange(["-Path", folder]);
                break;
        }
        return TestEnv.Run(program, args, environment: environment);
    }

    /// <summary>Asserts a refusal: exit code 1, nothing on standard output, one line starting <c>install: </c> on standard error with the message, no DMC folder and nothing left in the temporary folder.</summary>
    static void AssertRefused(Dmc.Tests.Support.ProcessResult result, Assets assets, string message, bool checkTemp = true)
    {
        Assert.AreEqual(1, result.ExitCode, result.ToString());
        Assert.AreEqual("", result.Stdout, result.ToString());
        Assert.HasCount(1, result.StderrLines, result.ToString());
        StringAssert.StartsWith(result.StderrLines[0], "install: ");
        StringAssert.Contains(result.StderrLines[0], message);
        Assert.IsFalse(Directory.Exists(assets.DmcFolder), "nothing may be installed");
        if (checkTemp)
            Assert.IsEmpty(Directory.EnumerateFileSystemEntries(assets.Temp, "dmc-install*"), "the temporary folder is removed on every exit");
    }

    [TestMethod]
    [DynamicData(nameof(InstallShells))]
    public void InstallsTheLatestReleaseThenRunsDmcInstallWithThePath(string shell)
    {
        using var assets = new Assets();
        assets.Make(new Release());
        var result = Run(shell, assets, assets.DmcFolder);
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        Assert.AreEqual("", result.Stderr, result.ToString());
        Assert.AreEqual($"dmc install: DMC folder ready at {assets.DmcFolder}", string.Join("\n", result.StdoutLines), "all the output is dmc install's");
        foreach (string file in new[] { "CLAUDE.md", ".dmc/manifest.json", ".claude/settings.json" })
            Assert.IsTrue(File.Exists(Path.Combine(assets.DmcFolder, file.Replace('/', Path.DirectorySeparatorChar))), file);
        foreach (string folder in new[] { "adventures", "feedback", "modules", "rules" })
            Assert.IsEmpty(Directory.EnumerateFileSystemEntries(Path.Combine(assets.DmcFolder, folder)));
        Assert.IsFalse(Directory.Exists(Path.Combine(assets.Home, "dmc")), "the folder asked for is the one made, not the default");
        Assert.IsEmpty(Directory.EnumerateFileSystemEntries(assets.Temp, "dmc-install*"), "the temporary folder is removed");
    }

    [TestMethod]
    [DynamicData(nameof(InstallShells))]
    public void AnArchiveThatDoesNotMatchTheChecksumListIsRefused(string shell)
    {
        using var assets = new Assets();
        assets.Make(new Release(Corrupt: true));
        var result = Run(shell, assets, assets.DmcFolder);
        AssertRefused(result, assets, $"{Asset(Rid)} does not match the release's SHA-256 checksums");
    }

    [TestMethod]
    [DynamicData(nameof(InstallShells))]
    public void AnArchiveThatDoesNotMatchGitHubsDigestIsRefusedEvenWhenTheListMatches(string shell)
    {
        using var assets = new Assets();
        assets.Make(new Release(WrongDigest: true));
        AssertRefused(Run(shell, assets, assets.DmcFolder), assets, "does not match the release's SHA-256 checksums (or GitHub's digest)");
    }

    [TestMethod]
    [DynamicData(nameof(InstallShells))]
    public void WhenGitHubsApiLimitIsReachedOnlyTheChecksumListIsCheckedAndOneLineSaysSo(string shell)
    {
        using var assets = new Assets();
        const string limited = "{\"message\":\"API rate limit exceeded for 203.0.113.9. (But here's the good news: Authenticated requests get a higher rate limit.)\",\"documentation_url\":\"https://docs.github.com/rest\"}";
        assets.Make(new Release(Latest: limited));
        var result = Run(shell, assets, assets.DmcFolder);
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        Assert.AreEqual("install: GitHub's API limit was reached, so only the release's checksum list was checked", string.Join("\n", result.StderrLines), result.ToString());
        Assert.IsTrue(Directory.Exists(assets.DmcFolder));

        // The list is still checked: a corrupted archive is refused even then.
        using var corrupted = new Assets();
        corrupted.Make(new Release(Corrupt: true, Latest: limited));
        var refused = Run(shell, corrupted, corrupted.DmcFolder);
        Assert.AreEqual(1, refused.ExitCode, refused.ToString());
        StringAssert.Contains(refused.Stderr, "does not match the release's SHA-256 checksums");
        Assert.IsFalse(Directory.Exists(corrupted.DmcFolder));
    }

    [TestMethod]
    [DynamicData(nameof(InstallShells))]
    public void AnyOtherApiFailureIsAnErrorAndAReleaseWithNoDigestForTheArchiveIsRefused(string shell)
    {
        using var assets = new Assets();
        assets.Make(new Release(Latest: ""));
        AssertRefused(Run(shell, assets, assets.DmcFolder), assets, "latest.json is not in");

        using var noDigest = new Assets();
        noDigest.Make(new Release(Latest: "{\"tag_name\":\"" + Tag + "\",\"assets\":[{\"name\":\"" + Asset(Rid) + "\"}]}"));
        AssertRefused(Run(shell, noDigest, noDigest.DmcFolder), noDigest, $"GitHub reports no digest for {Asset(Rid)}");
    }

    [TestMethod]
    [DynamicData(nameof(InstallShells))]
    public void ADownloadedDmcThatDoesNotRunIsNeverInstalled(string shell)
    {
        using var assets = new Assets();
        assets.Make(new Release(Content: "this is not a program"u8.ToArray()));
        AssertRefused(Run(shell, assets, assets.DmcFolder), assets, "the downloaded dmc does not run on this computer, so nothing was installed");
    }

    /// <summary>What every script says on macOS, which dmc is not built for.</summary>
    const string MacOsRefusal = "dmc is not supported on macOS: it runs on Windows and Linux";

    [TestMethod]
    [DynamicData(nameof(AllShells))]
    public void MacOsIsRefusedBeforeAnythingIsRead(string shell)
    {
        using var assets = new Assets();
        // No release in the folder: nothing is looked at.
        var result = Run(shell, assets, assets.DmcFolder, ("DMC_INSTALL_TEST_SYSTEM", shell == "bash" ? "Darwin" : "macOS"));
        AssertRefused(result, assets, MacOsRefusal);
    }

    /// <summary>
    /// Which archive install.sh asks for on each system and processor (<c>DMC_INSTALL_TEST_SYSTEM</c> and <c>DMC_INSTALL_TEST_MACHINE</c>
    /// stand in for <c>uname</c>). Each archive of the made-up release is junk, so the refusal names the archive picked.
    /// </summary>
    [TestMethod]
    [DataRow("Linux", "x86_64", "linux-x64")]
    [DataRow("Linux", "aarch64", "linux-arm64")]
    [DataRow("Linux", "armv7l", "not built for armv7l processors")]
    [DataRow("FreeBSD", "amd64", "not built for FreeBSD")]
    [DataRow("MINGW64_NT-10.0", "x86_64", "this is Windows: use the PowerShell install line")]
    public void BashPicksTheArchiveForThisSystem(string system, string machine, string expected)
    {
        using var assets = new Assets();
        assets.Make(new Release());
        // The release holds a real archive for this computer's runtime identifier and junk for the others: for another
        // system the script must ask for the junk one, whose hash does not match.
        var result = Run("bash", assets, assets.DmcFolder, ("DMC_INSTALL_TEST_SYSTEM", system), ("DMC_INSTALL_TEST_MACHINE", machine));
        string message = expected.Contains(' ') ? expected : $"{Asset(expected)} does not match the release's SHA-256 checksums";
        if (!expected.Contains(' ') && expected == Rid)
        {
            // This computer's own archive is real: it installs.
            Assert.AreEqual(0, result.ExitCode, result.ToString());
            return;
        }
        AssertRefused(result, assets, message, checkTemp: !OperatingSystem.IsWindows());
    }

    /// <summary>On Windows, the machine types the system can name (through <c>DMC_INSTALL_TEST_MACHINE</c>, which stands in for the registry), and this computer as the registry says. Elsewhere PowerShell asks .NET, which cannot be made to lie, so the one case is this computer.</summary>
    public static IEnumerable<object?[]> Machines { get; } = OperatingSystem.IsWindows()
        ?
        [
            ["AMD64", "win-x64"],
            ["ARM64", "win-arm64"],
            ["x86", "not built for x86 processors on Windows"],
            [null, Rid],
        ]
        : [[null, Rid]];

    [TestMethod]
    [DynamicData(nameof(Machines))]
    public void PowerShellPicksTheArchiveForThisComputer(string? machine, string expected)
    {
        using var assets = new Assets();
        assets.Make(new Release());
        // On Windows the process says it is AMD64 every time, as one emulated on Arm64 does, and must not be believed.
        var result = Run(OperatingSystem.IsWindows() ? "powershell" : "pwsh", assets, assets.DmcFolder,
            ("DMC_INSTALL_TEST_MACHINE", machine), ("PROCESSOR_ARCHITECTURE", "AMD64"), ("PROCESSOR_ARCHITEW6432", null));
        if (expected == Rid)
        {
            Assert.AreEqual(0, result.ExitCode, result.ToString());
            return;
        }
        AssertRefused(result, assets, expected.Contains(' ') ? expected : $"{Asset(expected)} does not match the release's SHA-256 checksums");
    }

    [TestMethod]
    public void ThePowerShellLineDoesNotEndTheSessionItWasRunIn()
    {
        string pwsh = Bootstrap.Shell("pwsh");
        using var assets = new Assets();
        assets.Make(new Release(Corrupt: true));
        string script = Path.Combine(TestEnv.Root, "tools", "install.ps1");
        foreach (string command in new[]
                 {
                     $"Get-Content -Raw -LiteralPath '{script}' | iex; 'session still open'; \"exit code: $LASTEXITCODE\"",
                     $"& ([scriptblock]::Create((Get-Content -Raw -LiteralPath '{script}'))) -Path '{assets.DmcFolder}'; 'session still open'; \"exit code: $LASTEXITCODE\"",
                 })
        {
            var result = TestEnv.Run(pwsh, ["-NoProfile", "-Command", command], environment: assets.Environment());
            Assert.AreEqual(0, result.ExitCode, result.ToString());
            CollectionAssert.AreEqual(new[] { "session still open", "exit code: 1" }, result.StdoutLines, result.ToString());
            Assert.HasCount(1, result.StderrLines, result.ToString());
            StringAssert.Contains(result.StderrLines[0], "does not match the release's SHA-256 checksums");
        }
    }

    [TestMethod]
    public void AnUnknownOptionOfInstallShIsRefusedInOneLine()
    {
        using var assets = new Assets();
        var result = TestEnv.Run(Bootstrap.Shell("bash"), [Path.Combine(TestEnv.Root, "tools", "install.sh"), "--bogus"], environment: assets.Environment());
        Assert.AreEqual(1, result.ExitCode, result.ToString());
        Assert.AreEqual("install: unknown option --bogus: the only one is --path FOLDER\n", result.Stderr);
        Assert.AreEqual("", result.Stdout);
    }
}
