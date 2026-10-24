using System.Formats.Tar;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dmc.Commands;
using Dmc.Install;
using Dmc.Kit;
using Dmc.Tests.Support;

namespace Dmc.Tests.Distribution;

/// <summary>
/// <c>dmc update</c> (contract <c>dmc-update.md</c>, FR-014, FR-026 to FR-028, research R9 and R16). In-process over a
/// <see cref="TestMachine"/> (temporary home, program folder and environment, so nothing reaches the real ones), with a throwaway
/// signing key made in each test, passed to the command through its internal seams; a release is a local folder
/// (<c>DMC_UPDATE_TEST_ASSETS</c>) or, for the GitHub path, a fake HTTP handler. The program that would run is a stand-in whose
/// content says what it prints. Through the published binary, which is a development build, only the refusals that come before a
/// release is read can be tested; the signature checks of a release build are exercised in the release workflow's dry run.
/// </summary>
[TestClass]
public sealed class UpdateTests
{
    /// <summary>The version the running program says it is in most tests.</summary>
    const string Running = "1.2.3";

    /// <summary>The version of the release the tests publish.</summary>
    const string Newer = "1.3.0";

    /// <summary>What went wrong with a release the tests make; each field breaks one thing.</summary>
    /// <param name="Version">The release's version (its tag is <c>dmc-v</c> and this).</param>
    /// <param name="Content">What the program in the archive holds: <c>new dmc X.Y.Z</c> makes the stand-in print <c>dmc X.Y.Z</c>; anything else makes it fail to run.</param>
    /// <param name="TamperArchive">True to change a byte of the archive after the list and the digest were made.</param>
    /// <param name="WrongDigest">True to report a digest for the archive that is not its hash.</param>
    /// <param name="NoSignature">True to leave the signature file out.</param>
    /// <param name="Signature">How the checksum list is signed: by the key the command trusts, by another key, by a good key over other data, or as garbage.</param>
    /// <param name="OmitArchiveFromList">True to leave the archive out of the checksum list.</param>
    /// <param name="OmitArchiveFromApi">True to leave the archive out of the release's asset list.</param>
    sealed record Options(string Version = Newer, string? Content = null, bool TamperArchive = false, bool WrongDigest = false,
        bool NoSignature = false, Signed Signature = Signed.Trusted, bool OmitArchiveFromList = false, bool OmitArchiveFromApi = false)
    {
        /// <summary>The text the stand-in program holds.</summary>
        public string ProgramText => Content ?? "new dmc " + Version;
    }

    /// <summary>Who signed the checksum list.</summary>
    public enum Signed
    {
        /// <summary>The key the command is given.</summary>
        Trusted,

        /// <summary>A key it is not given.</summary>
        Stranger,

        /// <summary>The trusted key, over other bytes.</summary>
        OtherData,

        /// <summary>Not a signature at all.</summary>
        Garbage,
    }

    /// <summary>A throwaway release signing key, made for one test.</summary>
    static ECDsa NewKey() => ECDsa.Create(ECCurve.NamedCurves.nistP256);

    /// <summary>The PEM text of a key's public half.</summary>
    /// <param name="key">The key.</param>
    internal static string Pem(ECDsa key) => key.ExportSubjectPublicKeyInfoPem();

    /// <summary>The runtime identifier a machine asks for.</summary>
    /// <param name="machine">The machine.</param>
    static string Rid(TestMachine machine) => ReleaseSource.Rid(machine.Windows);

    /// <summary>
    /// Writes a release into a folder as a local test source reads it: <c>latest.json</c> (the API's shape), the archive for the
    /// machine's runtime identifier, <c>SHA256SUMS.txt</c> and <c>SHA256SUMS.txt.sig</c>, broken as <paramref name="options"/> says.
    /// </summary>
    /// <param name="machine">The machine, which decides the archive's name and kind.</param>
    /// <param name="folder">The folder to write into (made if missing).</param>
    /// <param name="key">The key the command trusts.</param>
    /// <param name="stranger">A key it does not trust.</param>
    /// <param name="options">What to break.</param>
    static void WriteRelease(TestMachine machine, string folder, ECDsa key, ECDsa stranger, Options options)
    {
        Directory.CreateDirectory(folder);
        string tag = "dmc-v" + options.Version, rid = Rid(machine), name = ReleaseSource.ArchiveName(tag, rid);
        string archive = Path.Combine(folder, name);
        string exeName = ProgramInstall.ExeName(machine.Windows);
        byte[] program = Encoding.UTF8.GetBytes(options.ProgramText), library = "new native library"u8.ToArray();
        if (name.EndsWith(".zip", StringComparison.Ordinal))
        {
            using var zip = ZipFile.Open(archive, ZipArchiveMode.Create);
            foreach (var (entry, bytes) in new[] { (exeName, program), (TestMachine.NativeLibrary, library) })
            {
                using var stream = zip.CreateEntry(entry).Open();
                stream.Write(bytes);
            }
        }
        else
        {
            using var file = File.Create(archive);
            using var gzip = new GZipStream(file, CompressionLevel.Fastest);
            using var tar = new TarWriter(gzip);
            foreach (var (entry, bytes) in new[] { (exeName, program), (TestMachine.NativeLibrary, library) })
                tar.WriteEntry(new PaxTarEntry(TarEntryType.RegularFile, entry) { DataStream = new MemoryStream(bytes) });
        }

        string hash = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(archive)));
        byte[] list = Encoding.UTF8.GetBytes(options.OmitArchiveFromList
            ? $"{new string('0', 64)}  some-other-file.zip\n"
            : $"{hash}  {name}\n{new string('a', 64)}  {tag}-other-rid.zip\n");
        File.WriteAllBytes(Path.Combine(folder, "SHA256SUMS.txt"), list);
        if (!options.NoSignature)
        {
            byte[] signature = options.Signature switch
            {
                Signed.Trusted => key.SignData(list, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence),
                Signed.Stranger => stranger.SignData(list, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence),
                Signed.OtherData => key.SignData("something else"u8.ToArray(), HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence),
                _ => "not a signature"u8.ToArray(),
            };
            File.WriteAllBytes(Path.Combine(folder, "SHA256SUMS.txt.sig"), signature);
        }
        if (options.TamperArchive)
            File.AppendAllText(archive, "x");

        using var json = new MemoryStream();
        using (var writer = new Utf8JsonWriter(json))
        {
            writer.WriteStartObject();
            writer.WriteString("tag_name", tag);
            writer.WriteStartArray("assets");
            if (!options.OmitArchiveFromApi)
            {
                writer.WriteStartObject();
                writer.WriteString("name", name);
                writer.WriteString("digest", "sha256:" + (options.WrongDigest ? new string('b', 64) : hash));
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        File.WriteAllBytes(Path.Combine(folder, "latest.json"), json.ToArray());
    }

    /// <summary>
    /// A machine with a DMC folder recorded and filled in (the rules at version 1, a player's files in every personal folder, a
    /// settings file and a skill of the player's own), the stand-in program in its program folder, and a release folder named in its
    /// environment.
    /// </summary>
    sealed class Setup : IDisposable
    {
        /// <summary>The machine, as the running program (version <see cref="Running"/>).</summary>
        public TestMachine Machine { get; } = new();

        /// <summary>The DMC folder, installed with the rules at version 1.</summary>
        public string Folder => Path.Combine(Machine.Root, "dmc");

        /// <summary>The folder the release is in.</summary>
        public string Release => Path.Combine(Machine.Root, "release");

        /// <summary>The key the command trusts.</summary>
        public ECDsa Key { get; } = NewKey();

        /// <summary>A key it does not trust.</summary>
        public ECDsa Stranger { get; } = NewKey();

        /// <summary>The program folder, and what is in it before the update: its stand-in files.</summary>
        public string[] ProgramBefore { get; }

        /// <summary>Makes the machine, the DMC folder and its player files.</summary>
        public Setup()
        {
            Machine.Environment[ReleaseSource.TestAssetsVariable] = Release;
            Refresher.Refresh(Folder, TestMachine.Files("rules v1\n"), Running, Machine.Now);
            DmcFolder.Record(Machine.RunningFolder, Folder);
            foreach (string file in new[] { "adventures/player-options.md", "adventures/silver/game-state.md", "feedback/2026-10-01-a.md", "modules/lmop/index.md", "rules/srd.txt",
                         "exports/silver.zip", ".claude/settings.local.json", ".claude/skills/my-skill/SKILL.md", "my-notes.txt" })
            {
                string path = Path.Combine(Folder, file.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, "content of " + file);
            }
            ProgramBefore = DmcFolders.SnapshotAll(Machine.RunningFolder);
        }

        /// <summary>Everything the player owns in the DMC folder, as a hash per file.</summary>
        public string[] Player() =>
        [
            .. DmcFolders.Snapshot(Folder, "adventures", "feedback", "modules", "rules", "exports", ".claude/skills/my-skill"),
            .. new[] { ".claude/settings.local.json", "my-notes.txt" }.Select(f => f + " " + Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Path.Combine(Folder, f))))),
        ];

        /// <summary>Releases the keys and deletes the machine.</summary>
        public void Dispose()
        {
            Key.Dispose();
            Stranger.Dispose();
            Machine.Dispose();
        }
    }

    /// <summary>What an update did: its exit code, standard output and standard error, and the programs it ran.</summary>
    /// <param name="Exit">The exit code.</param>
    /// <param name="Out">Standard output.</param>
    /// <param name="Err">Standard error.</param>
    /// <param name="Ran">Each program run, as the file's name and its arguments.</param>
    sealed record Updated(int Exit, string Out, string Err, IReadOnlyList<string> Ran)
    {
        /// <summary>The non-empty lines of standard output.</summary>
        public string[] Lines => Out.Split('\n', StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Runs <c>dmc update</c> in-process. The programs it runs are stand-ins: <c>--version</c> prints <c>dmc X.Y.Z</c> for a program
    /// whose content is <c>new dmc X.Y.Z</c> and fails for any other; <c>refresh --all</c> runs the refresh of the new program (its
    /// version and rules, as <paramref name="newRules"/> says) in-process.
    /// </summary>
    /// <param name="setup">The machine and the release.</param>
    /// <param name="version">The version the running program says it is.</param>
    /// <param name="keys">The keys the command trusts; the setup's key when null.</param>
    /// <param name="http">A fake HTTP handler for the GitHub path (the local release is not used when the environment lacks the test folder).</param>
    /// <param name="afterMove">The swap hook.</param>
    /// <param name="newRules">The text of <c>CLAUDE.md</c> the new program would write.</param>
    /// <param name="refreshFails">True to make the refresh of the new program fail.</param>
    static Updated Update(Setup setup, string version = Running, ReleaseKeys? keys = null, HttpMessageHandler? http = null, Action<int>? afterMove = null,
        string newRules = "rules v2\n", bool refreshFails = false)
    {
        var ran = new List<string>();
        string exeName = ProgramInstall.ExeName(setup.Machine.Windows);
        ProgramSwap.Runner runner = (file, arguments) =>
        {
            ran.Add(Path.GetFileName(file) + " " + string.Join(' ', arguments));
            if (arguments is ["--version"])
            {
                string text = File.ReadAllText(file);
                return text.StartsWith("new dmc ", StringComparison.Ordinal) ? (0, "dmc " + text["new dmc ".Length..]) : (1, "cannot execute: " + text);
            }
            Assert.AreEqual(Path.Combine(setup.Machine.RunningFolder, exeName), file, "the refresh runs the new program in the program folder");
            Assert.IsTrue(arguments is ["refresh", "--all"], string.Join(' ', arguments));
            if (refreshFails)
                return (1, "dmc refresh: cannot write the folder: access denied");
            string text2 = File.ReadAllText(file);
            var fresh = setup.Machine.Machine(text2["new dmc ".Length..], TestMachine.Files(newRules));
            using var output = new StringWriter { NewLine = "\n" };
            int exit = new RefreshCommand(fresh).Run(["--all"], new Io(new StringReader(""), output, TextWriter.Null));
            return (exit, output.ToString());
        };
        var command = new UpdateCommand(setup.Machine.Machine(version), new UpdateSeams(keys ?? new ReleaseKeys([Pem(setup.Key)]), runner, http, afterMove));
        using var stdout = new StringWriter { NewLine = "\n" };
        using var stderr = new StringWriter { NewLine = "\n" };
        int code = CommandLine.Run(["update"], new Io(new StringReader(""), stdout, stderr), [command]);
        return new Updated(code, stdout.ToString(), stderr.ToString(), ran);
    }

    /// <summary>Asserts a refusal: exit code 1, nothing on standard output, one line on standard error with the message, the program folder and the DMC folder as they were and no staging folder left.</summary>
    static void AssertRefused(Setup setup, Updated result, string message, string[]? playerBefore = null, string? folderRules = "rules v1\n")
    {
        Assert.AreEqual(1, result.Exit, result.Err);
        Assert.AreEqual("", result.Out);
        Assert.AreEqual($"dmc update: {message}\n", result.Err);
        CollectionAssert.AreEqual(setup.ProgramBefore, DmcFolders.SnapshotAll(setup.Machine.RunningFolder), "nothing in the program folder changed");
        Assert.IsEmpty(Directory.EnumerateFileSystemEntries(setup.Machine.RunningFolder, ".update-*"), "no staging folder is left");
        if (playerBefore is not null)
            CollectionAssert.AreEqual(playerBefore, setup.Player());
        if (folderRules is not null)
            Assert.AreEqual(folderRules, File.ReadAllText(Path.Combine(setup.Folder, "CLAUDE.md")), "the generated files were not touched");
    }

    [TestMethod]
    public void ANewerReleaseIsInstalledAndTheFolderRefreshedByTheNewProgram()
    {
        using var setup = new Setup();
        WriteRelease(setup.Machine, setup.Release, setup.Key, setup.Stranger, new Options());
        string[] player = setup.Player();

        var result = Update(setup);

        Assert.AreEqual(0, result.Exit, result.Err);
        Assert.AreEqual("", result.Err);
        Assert.AreEqual($"dmc update: installed dmc {Newer} (was {Running})", result.Lines[0]);
        Assert.AreEqual($"dmc refresh: {setup.Folder}: 1 files updated for dmc {Newer}", result.Lines[1]);
        Assert.HasCount(2, result.Lines);
        CollectionAssert.AreEqual(new[] { $"{ProgramInstall.ExeName(setup.Machine.Windows)} --version", $"{ProgramInstall.ExeName(setup.Machine.Windows)} refresh --all" }, result.Ran.ToArray());
        Assert.AreEqual("new dmc " + Newer, File.ReadAllText(setup.Machine.RunningExecutable), "the program was replaced");
        Assert.AreEqual("new native library", File.ReadAllText(Path.Combine(setup.Machine.RunningFolder, TestMachine.NativeLibrary)), "and its library");
        Assert.AreEqual("rules v2\n", File.ReadAllText(Path.Combine(setup.Folder, "CLAUDE.md")), "the new program rewrote the generated files");
        Assert.AreEqual(Newer, Manifest.Read(setup.Folder)!.Version);
        CollectionAssert.AreEqual(player, setup.Player(), "player files are identical");
        CollectionAssert.AreEqual(new[] { setup.Folder }, DmcFolder.Recorded(setup.Machine.RunningFolder).ToArray(), "dmc.json beside the program was kept");
        Assert.IsEmpty(Directory.EnumerateFileSystemEntries(setup.Machine.RunningFolder, ".update-*"));
        Assert.IsEmpty(Directory.EnumerateFileSystemEntries(setup.Machine.RunningFolder, "*.old"), "leftovers of the swap are removed once nothing runs them");
    }

    [TestMethod]
    public void TheSameVersionIsAlreadyTheLatestAndAStaleFolderIsStillRefreshed()
    {
        using var setup = new Setup();
        WriteRelease(setup.Machine, setup.Release, setup.Key, setup.Stranger, new Options(Version: Running));
        // The folder holds what an older program wrote: this program's rules differ, so the refresh has one file to replace.
        Refresher.Refresh(setup.Folder, TestMachine.Files("rules v0\n"), "1.0.0", setup.Machine.Now);
        string[] player = setup.Player();

        var result = Update(setup);

        Assert.AreEqual(0, result.Exit, result.Err);
        Assert.AreEqual($"dmc update: dmc {Running} is already the latest", result.Lines[0]);
        Assert.AreEqual($"dmc refresh: {setup.Folder}: 1 files updated for dmc {Running}", result.Lines[1]);
        Assert.HasCount(2, result.Lines);
        Assert.IsEmpty(result.Ran, "no program was run");
        Assert.AreEqual("rules v1\n", File.ReadAllText(Path.Combine(setup.Folder, "CLAUDE.md")));
        CollectionAssert.AreEqual(setup.ProgramBefore, DmcFolders.SnapshotAll(setup.Machine.RunningFolder));
        CollectionAssert.AreEqual(player, setup.Player());

        // A second run has nothing to refresh either.
        var again = Update(setup);
        Assert.AreEqual($"dmc update: dmc {Running} is already the latest\n", again.Out);
    }

    [TestMethod]
    public void AnOlderReleaseIsNotInstalled()
    {
        using var setup = new Setup();
        WriteRelease(setup.Machine, setup.Release, setup.Key, setup.Stranger, new Options(Version: "1.2.0"));
        var result = Update(setup, version: "1.3.0");
        Assert.AreEqual(0, result.Exit, result.Err);
        Assert.AreEqual("dmc update: dmc 1.3.0 is already the latest\n", result.Out);
        Assert.IsEmpty(result.Ran);
        CollectionAssert.AreEqual(setup.ProgramBefore, DmcFolders.SnapshotAll(setup.Machine.RunningFolder));
    }

    [TestMethod]
    public void AVersionIsComparedAsNumbersNotAsText()
    {
        using var setup = new Setup();
        WriteRelease(setup.Machine, setup.Release, setup.Key, setup.Stranger, new Options(Version: "1.10.0"));
        var result = Update(setup, version: "1.9.9");
        Assert.AreEqual(0, result.Exit, result.Err);
        StringAssert.StartsWith(result.Out, "dmc update: installed dmc 1.10.0 (was 1.9.9)");
    }

    [TestMethod]
    public void ADevelopmentBuildIsRefusedAtOnceWithoutReadingAnyRelease()
    {
        using var setup = new Setup();
        // No release exists in the folder: the refusal comes first.
        var result = Update(setup, version: "0.0.0-dev");
        AssertRefused(setup, result, "this is a development build (0.0.0-dev): install a release to update");
        Assert.IsEmpty(result.Ran);
    }

    [TestMethod]
    public void InsideAClaudeCodeSessionNothingIsReadOrWrittenAndOneLineSaysWhy()
    {
        using var setup = new Setup();
        WriteRelease(setup.Machine, setup.Release, setup.Key, setup.Stranger, new Options());
        setup.Machine.Environment["CLAUDECODE"] = "1";
        string[] all = DmcFolders.SnapshotAll(setup.Machine.Root);
        var result = Update(setup);
        AssertRefused(setup, result, "cannot run inside a Claude Code session: run it in a terminal");
        CollectionAssert.AreEqual(all, DmcFolders.SnapshotAll(setup.Machine.Root));
        // It comes before the development-build refusal.
        AssertRefused(setup, Update(setup, version: "0.0.0-dev"), "cannot run inside a Claude Code session: run it in a terminal");
    }

    [TestMethod]
    public void AHashThatDoesNotMatchTheSignedListRefusesAndInstallsNothing()
    {
        using var setup = new Setup();
        WriteRelease(setup.Machine, setup.Release, setup.Key, setup.Stranger, new Options(TamperArchive: true));
        string[] player = setup.Player();
        var result = Update(setup);
        AssertRefused(setup, result, "the download does not match the signed checksums (or GitHub's digest): nothing was installed", player);
        Assert.IsEmpty(result.Ran, "nothing from the archive was run");
    }

    [TestMethod]
    public void ADigestThatDoesNotMatchTheDownloadRefusesEvenWhenTheSignedListMatches()
    {
        using var setup = new Setup();
        WriteRelease(setup.Machine, setup.Release, setup.Key, setup.Stranger, new Options(WrongDigest: true));
        AssertRefused(setup, Update(setup), "the download does not match the signed checksums (or GitHub's digest): nothing was installed");
    }

    [TestMethod]
    public void AnArchiveTheSignedListDoesNotNameRefuses()
    {
        using var setup = new Setup();
        WriteRelease(setup.Machine, setup.Release, setup.Key, setup.Stranger, new Options(OmitArchiveFromList: true));
        AssertRefused(setup, Update(setup), "the download does not match the signed checksums (or GitHub's digest): nothing was installed");
    }

    [TestMethod]
    public void AReleaseWithoutASignatureRefuses()
    {
        using var setup = new Setup();
        WriteRelease(setup.Machine, setup.Release, setup.Key, setup.Stranger, new Options(NoSignature: true));
        var result = Update(setup);
        AssertRefused(setup, result, $"release {Newer} has no signature: nothing was installed");
        Assert.IsEmpty(result.Ran);
    }

    [TestMethod]
    [DataRow(Signed.Garbage)]
    [DataRow(Signed.OtherData)]
    [DataRow(Signed.Stranger)]
    public void ASignatureThatIsBadOrByAnUnknownKeyRefuses(Signed how)
    {
        using var setup = new Setup();
        WriteRelease(setup.Machine, setup.Release, setup.Key, setup.Stranger, new Options(Signature: how));
        var result = Update(setup);
        AssertRefused(setup, result, $"release {Newer} is not signed by the DMC release key: nothing was installed");
        Assert.IsEmpty(result.Ran);
    }

    [TestMethod]
    [DataRow(Signed.Trusted)]
    [DataRow(Signed.Stranger)]
    public void AReleaseSignedByEitherKeyOfTheSetIsAccepted(Signed by)
    {
        using var setup = new Setup();
        WriteRelease(setup.Machine, setup.Release, setup.Key, setup.Stranger, new Options(Signature: by));
        // The set holds two keys, as the built-in set holds the active key and the backup key.
        var keys = new ReleaseKeys([Pem(setup.Key), Pem(setup.Stranger)]);
        var result = Update(setup, keys: keys);
        Assert.AreEqual(0, result.Exit, result.Err);
        StringAssert.StartsWith(result.Out, $"dmc update: installed dmc {Newer}");
    }

    [TestMethod]
    public void ANewProgramThatDoesNotRunIsNeverInstalled()
    {
        using var setup = new Setup();
        WriteRelease(setup.Machine, setup.Release, setup.Key, setup.Stranger, new Options(Content: "this is not a program"));
        var result = Update(setup);
        AssertRefused(setup, result, "the new dmc does not run: nothing was installed");
        Assert.HasCount(1, result.Ran);
    }

    [TestMethod]
    public void ANewProgramThatSaysAnotherVersionIsNeverInstalled()
    {
        using var setup = new Setup();
        WriteRelease(setup.Machine, setup.Release, setup.Key, setup.Stranger, new Options(Content: "new dmc 9.9.9"));
        AssertRefused(setup, Update(setup), "the new dmc does not run: nothing was installed");
    }

    [TestMethod]
    public void AFailureInTheMiddleOfTheSwapMovesBackWhatWasMoved()
    {
        using var setup = new Setup();
        WriteRelease(setup.Machine, setup.Release, setup.Key, setup.Stranger, new Options());
        string[] player = setup.Player();
        int moves = 0;
        var result = Update(setup, afterMove: n =>
        {
            moves = n;
            if (n == 1)
                throw new IOException("the disk is full");
        });
        Assert.AreEqual(1, moves, "the first file was moved before the failure");
        AssertRefused(setup, result, $"cannot write {setup.Machine.RunningFolder}: the disk is full", player);
        Assert.AreEqual("stand-in for dmc", File.ReadAllText(setup.Machine.RunningExecutable));
        Assert.HasCount(1, result.Ran, "the new refresh was never run");
    }

    [TestMethod]
    public void ARefreshThatFailsAfterTheSwapIsReportedInOneLineAndTheProgramStaysInstalled()
    {
        using var setup = new Setup();
        WriteRelease(setup.Machine, setup.Release, setup.Key, setup.Stranger, new Options());
        var result = Update(setup, refreshFails: true);
        Assert.AreEqual(1, result.Exit);
        Assert.AreEqual("", result.Out);
        Assert.AreEqual($"dmc update: installed dmc {Newer}, but the DMC folder was not refreshed: cannot write the folder: access denied; dmc play will refresh it\n", result.Err);
        Assert.AreEqual("new dmc " + Newer, File.ReadAllText(setup.Machine.RunningExecutable));
        Assert.AreEqual("rules v1\n", File.ReadAllText(Path.Combine(setup.Folder, "CLAUDE.md")), "the folder is left for dmc play to refresh");
    }

    [TestMethod]
    public void AReleaseWithNoArchiveForThisMachineRefuses()
    {
        using var setup = new Setup();
        WriteRelease(setup.Machine, setup.Release, setup.Key, setup.Stranger, new Options(OmitArchiveFromApi: true));
        AssertRefused(setup, Update(setup), $"the latest release has no archive for {Rid(setup.Machine)}");
    }

    [TestMethod]
    public void LeftoversOfAnEarlierUpdateAreRemoved()
    {
        using var setup = new Setup();
        WriteRelease(setup.Machine, setup.Release, setup.Key, setup.Stranger, new Options(Version: Running));
        Directory.CreateDirectory(Path.Combine(setup.Machine.RunningFolder, ".update-1234abcd", "new"));
        File.WriteAllText(Path.Combine(setup.Machine.RunningFolder, "dmc.old"), "old");
        Assert.AreEqual(0, Update(setup).Exit);
        Assert.IsEmpty(Directory.EnumerateFileSystemEntries(setup.Machine.RunningFolder, ".update-*"));
        Assert.IsEmpty(Directory.EnumerateFileSystemEntries(setup.Machine.RunningFolder, "*.old"));
    }

    [TestMethod]
    public void AnUnknownArgumentIsAUsageMistakeAndHelpPrintsTheUsage()
    {
        using var setup = new Setup();
        var command = new UpdateCommand(setup.Machine.Machine(Running), new UpdateSeams(new ReleaseKeys([]), ProgramSwap.Start));
        using var stdout = new StringWriter { NewLine = "\n" };
        using var stderr = new StringWriter { NewLine = "\n" };
        Assert.AreEqual(2, CommandLine.Run(["update", "--bogus"], new Io(new StringReader(""), stdout, stderr), [command]));
        Assert.AreEqual("dmc update: unrecognized arguments: --bogus\n", stderr.ToString());
        Assert.AreEqual(0, CommandLine.Run(["update", "--help"], new Io(new StringReader(""), stdout, stderr), [command]));
        StringAssert.StartsWith(stdout.ToString(), "usage: dmc update\n");
        Assert.DoesNotContain("\r", stdout.ToString());
    }

    // ---------- the GitHub path, through a fake HTTP handler ----------

    /// <summary>A stand-in for GitHub: answers each address with a stored status and body, and records every address asked for.</summary>
    sealed class FakeGitHub : HttpMessageHandler
    {
        /// <summary>The answer for each address; an address that is not here answers 404.</summary>
        public Dictionary<string, (HttpStatusCode Status, byte[] Body)> Answers { get; } = new();

        /// <summary>When set, every request fails with this message, as a machine with no network does.</summary>
        public string? Offline { get; set; }

        /// <summary>The addresses asked for, in order.</summary>
        public List<string> Requests { get; } = [];

        /// <summary>The user agent of the first request.</summary>
        public string? UserAgent { get; private set; }

        /// <summary>Answers a request from <see cref="Answers"/>.</summary>
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri!.AbsoluteUri);
            UserAgent ??= request.Headers.UserAgent.ToString();
            if (Offline is not null)
                throw new HttpRequestException(Offline);
            var (status, body) = Answers.GetValueOrDefault(request.RequestUri!.AbsoluteUri, (HttpStatusCode.NotFound, Array.Empty<byte>()));
            return Task.FromResult(new HttpResponseMessage(status) { Content = new ByteArrayContent(body), ReasonPhrase = status.ToString() });
        }
    }

    /// <summary>Loads a local release folder into a fake GitHub under the addresses the real one uses.</summary>
    /// <param name="github">The fake.</param>
    /// <param name="folder">A release written by <see cref="WriteRelease"/>.</param>
    /// <param name="tag">The release's tag.</param>
    static void Serve(FakeGitHub github, string folder, string tag)
    {
        github.Answers[ReleaseSource.LatestUrl] = (HttpStatusCode.OK, File.ReadAllBytes(Path.Combine(folder, "latest.json")));
        foreach (string file in Directory.EnumerateFiles(folder).Where(f => Path.GetFileName(f) != "latest.json"))
            github.Answers[$"https://github.com/abaskett3/dungeon-master-claude/releases/download/{tag}/{Path.GetFileName(file)}"] = (HttpStatusCode.OK, File.ReadAllBytes(file));
    }

    [TestMethod]
    public void TheGitHubPathAsksTheApiThenTheReleaseDownloadAddressesAndInstalls()
    {
        using var setup = new Setup();
        string served = Path.Combine(setup.Machine.Root, "served");
        WriteRelease(setup.Machine, served, setup.Key, setup.Stranger, new Options());
        var github = new FakeGitHub();
        Serve(github, served, "dmc-v" + Newer);
        setup.Machine.Environment.Remove(ReleaseSource.TestAssetsVariable);

        var result = Update(setup, http: github);

        Assert.AreEqual(0, result.Exit, result.Err);
        StringAssert.StartsWith(result.Out, $"dmc update: installed dmc {Newer} (was {Running})");
        string rid = Rid(setup.Machine), archive = ReleaseSource.ArchiveName("dmc-v" + Newer, rid), download = "https://github.com/abaskett3/dungeon-master-claude/releases/download/dmc-v" + Newer + "/";
        CollectionAssert.AreEqual(new[] { ReleaseSource.LatestUrl, download + "SHA256SUMS.txt.sig", download + "SHA256SUMS.txt", download + archive }, github.Requests);
        Assert.IsTrue(github.Requests.All(r => r.StartsWith("https://", StringComparison.Ordinal)));
        Assert.AreEqual("dmc/" + Running, github.UserAgent);
    }

    [TestMethod]
    public void ANetworkFailureIsOneLineAndChangesNothing()
    {
        using var setup = new Setup();
        setup.Machine.Environment.Remove(ReleaseSource.TestAssetsVariable);
        var result = Update(setup, http: new FakeGitHub { Offline = "No such host is known." });
        AssertRefused(setup, result, "could not reach GitHub: No such host is known.");
    }

    [TestMethod]
    public void AnApiRefusalIsReportedWithItsStatus()
    {
        using var setup = new Setup();
        setup.Machine.Environment.Remove(ReleaseSource.TestAssetsVariable);
        var github = new FakeGitHub();
        github.Answers[ReleaseSource.LatestUrl] = (HttpStatusCode.Forbidden, "{\"message\":\"API rate limit exceeded\"}"u8.ToArray());
        AssertRefused(setup, Update(setup, http: github), $"could not reach GitHub: {ReleaseSource.LatestUrl} answered 403 Forbidden");
    }

    [TestMethod]
    public void ATagThatIsNotADmcReleaseIsRefused()
    {
        using var setup = new Setup();
        setup.Machine.Environment.Remove(ReleaseSource.TestAssetsVariable);
        var github = new FakeGitHub();
        github.Answers[ReleaseSource.LatestUrl] = (HttpStatusCode.OK, "{\"tag_name\":\"v2.0.0\",\"assets\":[]}"u8.ToArray());
        AssertRefused(setup, Update(setup, http: github), "could not reach GitHub: the latest release's tag \"v2.0.0\" is not a dmc release");
    }

    [TestMethod]
    public void AMissingSignatureOnGitHubIsTheNoSignatureRefusal()
    {
        using var setup = new Setup();
        string served = Path.Combine(setup.Machine.Root, "served");
        WriteRelease(setup.Machine, served, setup.Key, setup.Stranger, new Options(NoSignature: true));
        var github = new FakeGitHub();
        Serve(github, served, "dmc-v" + Newer);
        setup.Machine.Environment.Remove(ReleaseSource.TestAssetsVariable);
        AssertRefused(setup, Update(setup, http: github), $"release {Newer} has no signature: nothing was installed");
    }

    // ---------- through the published binary (a development build) ----------

    [TestMethod]
    public void ThePublishedDevelopmentBuildRefusesAndTheSessionRefusalComesFirst()
    {
        string root = DmcFolders.NewTemp();
        try
        {
            var environment = new Dictionary<string, string?>
            {
                ["CLAUDECODE"] = null, ["USERPROFILE"] = root, ["HOME"] = root, ["LOCALAPPDATA"] = root, ["XDG_DATA_HOME"] = root,
                [ReleaseSource.TestAssetsVariable] = root,
            };
            var dev = TestEnv.Run(DmcRunner.Binary(), ["update"], environment: environment);
            Assert.AreEqual(1, dev.ExitCode, dev.ToString());
            Assert.AreEqual("", dev.Stdout);
            Assert.AreEqual("dmc update: this is a development build (0.0.0-dev): install a release to update\n", dev.Stderr);

            environment["CLAUDECODE"] = "1";
            var session = TestEnv.Run(DmcRunner.Binary(), ["update"], environment: environment);
            Assert.AreEqual(1, session.ExitCode, session.ToString());
            Assert.AreEqual("dmc update: cannot run inside a Claude Code session: run it in a terminal\n", session.Stderr);

            var help = TestEnv.Run(DmcRunner.Binary(), ["update", "--help"], environment: new Dictionary<string, string?> { ["CLAUDECODE"] = null });
            Assert.AreEqual(0, help.ExitCode, help.ToString());
            StringAssert.StartsWith(help.Stdout, "usage: dmc update\n");
        }
        finally
        {
            DmcFolders.Delete(root);
        }
    }
}
