using System.Formats.Tar;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Dmc.Commands;
using Dmc.Tests.Support;

namespace Dmc.Tests.Distribution;

/// <summary>
/// What the tests of <c>tools/get-dmc.*</c>, the launchers and <c>dmc play</c> start from: a copy of the toolkit's
/// scripts in a temporary folder with a pin of the test's choosing, a release archive made from the published
/// <c>dmc</c>, the shells to run the scripts with, and a fake <c>claude</c> that prints where it was started and
/// with what.
/// </summary>
public static class Bootstrap
{
    /// <summary>The shells the scripts are run with: bash everywhere, PowerShell 7 everywhere, Windows PowerShell 5.1 on Windows.</summary>
    public static IEnumerable<object[]> Shells { get; } =
        OperatingSystem.IsWindows() ? [["bash"], ["pwsh"], ["powershell"]] : [["bash"], ["pwsh"]];

    /// <summary>The runtime identifier of this computer, as get-dmc picks it.</summary>
    public static string Rid { get; } =
        (OperatingSystem.IsWindows() ? "win" : OperatingSystem.IsMacOS() ? "osx" : "linux") + "-"
        + (RuntimeInformation.OSArchitecture == Architecture.Arm64 ? "arm64" : "x64");

    /// <summary>The file name of the program: <c>dmc.exe</c> on Windows, <c>dmc</c> elsewhere.</summary>
    public static string ExeName { get; } = OperatingSystem.IsWindows() ? "dmc.exe" : "dmc";

    /// <summary>The name of the archive of a release for a runtime identifier, as the release workflow names it.</summary>
    public static string Asset(string tag, string rid) => $"{tag}-{rid}.{(rid.StartsWith("win-", StringComparison.Ordinal) ? "zip" : "tar.gz")}";

    /// <summary>A file the test archive holds next to dmc, standing in for its native libraries.</summary>
    public const string NativeStandIn = "native-library-stand-in.txt";

    /// <summary>
    /// A fresh folder holding <c>play.ps1</c>, <c>play.sh</c> and <c>tools/get-dmc.*</c> from the repository, and a
    /// pin of the given tag and hashes (one key per line, as the release workflow writes it). Deleted on dispose.
    /// </summary>
    public sealed class ToolkitCopy : IDisposable
    {
        /// <summary>The copy's folder, which the test may write into (the pin, <c>bin/</c>).</summary>
        public string Root { get; } = Path.Combine(Path.GetTempPath(), "dmc-bootstrap-" + Guid.NewGuid().ToString("N"));

        /// <summary>Copies the scripts and writes the pin.</summary>
        /// <param name="tag">The release the pin names; empty for a toolkit that pins none yet.</param>
        /// <param name="sha256">The archive hashes in the pin, by archive name.</param>
        public ToolkitCopy(string tag, IReadOnlyDictionary<string, string> sha256)
        {
            Directory.CreateDirectory(Path.Combine(Root, "tools"));
            foreach (string file in new[] { "play.ps1", "play.sh", "tools/get-dmc.ps1", "tools/get-dmc.sh" })
                File.Copy(Path.Combine(TestEnv.Root, file), Path.Combine(Root, file));
            string entries = string.Join(",\n", sha256.Select(e => $"    \"{e.Key}\": \"{e.Value}\""));
            File.WriteAllText(Path.Combine(Root, "tools", "dmc-release.json"),
                $"{{\n  \"about\": \"test pin\",\n  \"tag\": \"{tag}\",\n  \"sha256\": {{\n{entries}\n  }}\n}}\n");
        }

        /// <summary>Where get-dmc installs the program: <c>bin/</c> in the copy.</summary>
        public string Bin => Path.Combine(Root, "bin");

        /// <summary>The installed program's path, which exists only once get-dmc has run.</summary>
        public string Exe => Path.Combine(Bin, ExeName);

        /// <summary>Deletes the copy. A folder that cannot be deleted (still in use) is left behind without an error.</summary>
        public void Dispose()
        {
            try
            {
                Directory.Delete(Root, recursive: true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    /// <summary>A release of the published dmc: its tag and a folder holding its archive for this computer.</summary>
    /// <param name="Tag">The release tag, <c>dmc-v9.9.9</c>.</param>
    /// <param name="Version">The version the program prints.</param>
    /// <param name="Assets">The folder holding the archive, named as the release workflow names it.</param>
    /// <param name="Sha256">The archive's SHA-256, as the pin holds it.</param>
    public sealed record Release(string Tag, string Version, string Assets, string Sha256);

    /// <summary>The version the test release is built with. A build made here says 0.0.0-dev, which is never a release's.</summary>
    const string ReleaseVersion = "9.9.9";

    /// <summary>
    /// A folder holding a dmc published with <c>-p:Version=9.9.9</c> as a release is (Native AOT, on CI). CI makes it
    /// once and names it here; without it, <see cref="PublishedRelease"/> builds its own.
    /// </summary>
    const string ReleaseFolderVariable = "DMC_TEST_RELEASE_DIR";

    /// <summary>
    /// A release of dmc packed the way the release workflow packs it (a zip on Windows, a tar.gz elsewhere, the files at
    /// the top), with <see cref="NativeStandIn"/> in place of the native libraries. The program is one published with
    /// <c>-p:Version=</c>, as the workflow does, since a build of the sources says 0.0.0-dev: the one in the folder named
    /// by <c>DMC_TEST_RELEASE_DIR</c>, or else one built here from a copy of the sources. Made once.
    /// </summary>
    public static Release PublishedRelease() => _release.Value;

    /// <summary>The release, built on first use and shared by every test in the run.</summary>
    static readonly Lazy<Release> _release = new(MakeRelease);

    /// <summary>
    /// Builds the release: gets the 9.9.9 program (from <c>DMC_TEST_RELEASE_DIR</c> or by publishing), checks its
    /// version, and packs it with the stand-in file into an archive in a new folder. Its working folder is deleted; the
    /// archive folder is left for the tests, and removed only if the build fails.
    /// </summary>
    static Release MakeRelease()
    {
        string work = Path.Combine(Path.GetTempPath(), "dmc-release-build-" + Guid.NewGuid().ToString("N"));
        string assets = Path.Combine(Path.GetTempPath(), "dmc-release-" + Guid.NewGuid().ToString("N"));
        bool made = false;
        try
        {
            string? given = Environment.GetEnvironmentVariable(ReleaseFolderVariable);
            string binary = string.IsNullOrEmpty(given) ? PublishFromCopy(work) : Path.Combine(given, ExeName);
            Assert.IsTrue(File.Exists(binary), $"the release program {binary} (from {ReleaseFolderVariable}) is not there");
            var answer = TestEnv.Run(binary, ["--version"]);
            Assert.AreEqual(0, answer.ExitCode, answer.ToString());
            Assert.AreEqual($"dmc {ReleaseVersion}\n", answer.Stdout, "a release gets its version from -p:Version, not from the project");

            string tag = "dmc-v" + ReleaseVersion;
            Directory.CreateDirectory(assets);
            string archive = Path.Combine(assets, Asset(tag, Rid));
            byte[] standIn = "not a library"u8.ToArray();
            if (OperatingSystem.IsWindows())
            {
                using var zip = ZipFile.Open(archive, ZipArchiveMode.Create);
                zip.CreateEntryFromFile(binary, ExeName, CompressionLevel.NoCompression);
                using var entry = zip.CreateEntry(NativeStandIn).Open();
                entry.Write(standIn);
            }
            else
            {
                using var file = File.Create(archive);
                using var gzip = new GZipStream(file, CompressionLevel.Fastest);
                using var tar = new TarWriter(gzip);
                tar.WriteEntry(binary, ExeName);
                tar.WriteEntry(new PaxTarEntry(TarEntryType.RegularFile, NativeStandIn) { DataStream = new MemoryStream(standIn) });
            }
            made = true;
            return new Release(tag, ReleaseVersion, assets, Sha256(archive));
        }
        finally
        {
            DeleteQuietly(work);
            if (!made)
                DeleteQuietly(assets);
        }
    }

    /// <summary>Publish dmc with <c>-p:Version=</c> from a copy of the sources in <paramref name="work"/>, so the build leaves src/Dmc.Cli/bin and obj as they were. Returns the program.</summary>
    static string PublishFromCopy(string work)
    {
        Directory.CreateDirectory(work);
        foreach (string file in new[] { "Directory.Build.props", "Directory.Packages.props", "global.json" })
            File.Copy(Path.Combine(TestEnv.Root, file), Path.Combine(work, file), overwrite: true);
        string source = Path.Combine(TestEnv.Root, "src", "Dmc.Cli");
        string copy = Path.Combine(work, "src", "Dmc.Cli");
        foreach (string file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(source, file);
            if (relative.StartsWith("bin" + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                || relative.StartsWith("obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                continue;
            Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(copy, relative))!);
            File.Copy(file, Path.Combine(copy, relative));
        }
        string output = Path.Combine(work, "out");
        var published = TestEnv.Run(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet",
            ["publish", copy, "-c", "Release", "-r", Rid, "-o", output, $"-p:Version={ReleaseVersion}"],
            cwd: work, timeoutSeconds: 600, environment: new Dictionary<string, string?> { ["DOTNET_NOLOGO"] = "1", ["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1" });
        Assert.AreEqual(0, published.ExitCode, published.ToString());
        return Path.Combine(output, ExeName);
    }

    /// <summary>Deletes a folder, and says nothing if it cannot (a file still in use).</summary>
    static void DeleteQuietly(string folder)
    {
        try
        {
            Directory.Delete(folder, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    /// <summary>The SHA-256 of a file as lower-case hex, the form the pin holds.</summary>
    public static string Sha256(string file)
    {
        using var stream = File.OpenRead(file);
        return Convert.ToHexStringLower(SHA256.HashData(stream));
    }

    /// <summary>
    /// A folder with a fake <c>claude</c> in it, to put first on the PATH. It prints the folder it runs in and each
    /// argument on its own line, and exits with 7. On Windows it is the three shims npm installs: <c>claude.ps1</c>,
    /// which dmc and PowerShell run; a <c>claude</c> shell script, which Git Bash runs; and <c>claude.cmd</c>, which
    /// nothing may run (it says so and exits with 9).
    /// </summary>
    public static string FakeClaude()
    {
        string folder = Path.Combine(Path.GetTempPath(), "dmc-fake-claude-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string script = Path.Combine(folder, "claude");
        File.WriteAllText(script, "#!/bin/sh\necho \"fake claude in $(pwd)\"\nfor a in \"$@\"; do echo \"arg: $a\"; done\nexit 7\n");
        if (OperatingSystem.IsWindows())
        {
            File.WriteAllText(Path.Combine(folder, "claude.ps1"),
                "\"fake claude in $((Get-Location).Path)\"\r\nforeach ($a in $args) { \"arg: $a\" }\r\nexit 7\r\n");
            File.WriteAllText(Path.Combine(folder, "claude.cmd"), "@echo off\r\necho the cmd shim ran\r\nexit /b 9\r\n");
        }
        else
        {
            File.SetUnixFileMode(script, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
        return folder;
    }

    /// <summary>
    /// The PATH with <paramref name="folder"/> first, and without the folders of a real Claude Code: dmc takes a
    /// <c>claude.exe</c> anywhere on the PATH before npm's shims, which is what the fake is on Windows.
    /// </summary>
    public static string PathWith(string folder) =>
        string.Join(Path.PathSeparator, (Environment.GetEnvironmentVariable("PATH") ?? "")
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Where(dir => !HasClaude(dir))
            .Prepend(folder));

    /// <summary>True when a PATH folder holds something named <c>claude</c> with any extension.</summary>
    static bool HasClaude(string dir)
    {
        try
        {
            return Directory.Exists(dir.Trim('"')) && Directory.EnumerateFiles(dir.Trim('"'), "claude*").Any(
                f => Path.GetFileNameWithoutExtension(f).Equals("claude", StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return false;
        }
    }

    /// <summary>What the fake claude printed: the folder it ran in, and its arguments.</summary>
    public static (string Folder, string[] Args) ClaudeSaw(ProcessResult result)
    {
        string[] lines = result.StdoutLines;
        string? folder = lines.FirstOrDefault(l => l.StartsWith("fake claude in ", StringComparison.Ordinal));
        Assert.IsNotNull(folder, $"claude did not start:\n{result}");
        return (folder["fake claude in ".Length..], lines.Where(l => l.StartsWith("arg: ", StringComparison.Ordinal)).Select(l => l["arg: ".Length..]).ToArray());
    }

    /// <summary>What claude must be started with: the launchers' arguments, then the player's.</summary>
    public static string[] ClaudeArgs(params string[] players) => [.. PlayCommand.FixedArguments, .. players];

    /// <summary>
    /// Run a script of the toolkit copy with a shell. <paramref name="script"/> is relative to the toolkit root, with
    /// forward slashes and no extension (<c>tools/get-dmc</c>, <c>play</c>); the shell picks <c>.sh</c> or <c>.ps1</c>.
    /// <paramref name="pathFirst"/> is a folder to put first on the PATH. For bash that is done inside bash, because
    /// Git Bash puts its own folders (with its uname) before the PATH it is given.
    /// </summary>
    public static ProcessResult RunScript(string shell, ToolkitCopy toolkit, string script, string[]? args = null,
        IReadOnlyDictionary<string, string?>? environment = null, string? pathFirst = null)
    {
        string program = Shell(shell);
        var env = new Dictionary<string, string?>(environment ?? new Dictionary<string, string?>());
        string[] arguments;
        if (shell == "bash")
        {
            string file = Path.Combine(toolkit.Root, script + ".sh").Replace('\\', '/');
            arguments = pathFirst is null
                ? [file]
                : ["-c", "PATH=\"$(cygpath -u \"$0\" 2>/dev/null || printf '%s' \"$0\"):$PATH\"; exec bash \"$@\"", pathFirst, file];
        }
        else
        {
            arguments = ["-NoProfile", "-ExecutionPolicy", "Bypass", "-File", Path.Combine(toolkit.Root, script + ".ps1")];
            if (pathFirst is not null)
                env["PATH"] = PathWith(pathFirst);
        }
        return TestEnv.Run(program, [.. arguments, .. args ?? []], cwd: Path.GetTempPath(), timeoutSeconds: 180,
            environment: env);
    }

    /// <summary>The full path of a shell, after checking it is there (a skip locally, a failure on CI).</summary>
    public static string Shell(string name)
    {
        string? found = name == "bash" ? Bash.Value : Find(name);
        TestEnv.Need(found is not null, name);
        return found!;
    }

    /// <summary>The full path of a program on the PATH, found the way <c>dmc play</c> finds Claude Code; null if absent.</summary>
    static string? Find(string name) =>
        PlayCommand.FindOnPath(name, Environment.GetEnvironmentVariable("PATH"), ".EXE", OperatingSystem.IsWindows());

    /// <summary>
    /// On Windows, Git Bash: a bare "bash" can be the WSL launcher in the Windows folder, so Git's own bash is taken,
    /// found from <c>git</c> or Program Files.
    /// </summary>
    static readonly Lazy<string?> Bash = new(() =>
    {
        if (!OperatingSystem.IsWindows())
            return Find("bash");
        var candidates = new List<string>();
        if (Find("git") is { } git)
            candidates.Add(Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(git))!, "bin", "bash.exe"));
        if (Environment.GetEnvironmentVariable("ProgramFiles") is { } programFiles)
            candidates.Add(Path.Combine(programFiles, "Git", "bin", "bash.exe"));
        return candidates.FirstOrDefault(File.Exists);
    });
}
