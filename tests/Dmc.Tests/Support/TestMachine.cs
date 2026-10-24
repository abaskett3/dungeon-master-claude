using Dmc.Install;
using Dmc.Kit;

namespace Dmc.Tests.Support;

/// <summary>A user <c>Path</c> held in memory, standing in for the registry: records what was written and whether the change was broadcast.</summary>
public sealed class FakeUserPath : IUserPath
{
    /// <summary>The stored value, as <see cref="IUserPath.Read"/> returns it; null for none.</summary>
    public string? Value { get; set; }

    /// <summary>How many times <see cref="IUserPath.Write"/> was called.</summary>
    public int Writes { get; private set; }

    /// <summary>How many times <see cref="IUserPath.Broadcast"/> was called.</summary>
    public int Broadcasts { get; private set; }

    /// <summary>When set, <see cref="IUserPath.Write"/> fails with this message, as a locked registry would.</summary>
    public string? FailWith { get; set; }

    /// <inheritdoc />
    public string? Read() => Value;

    /// <inheritdoc />
    public void Write(string value)
    {
        if (FailWith is not null)
            throw new UnauthorizedAccessException(FailWith);
        Value = value;
        Writes++;
    }

    /// <inheritdoc />
    public void Broadcast() => Broadcasts++;
}

/// <summary>
/// A machine for the tests of <c>dmc install</c>, <c>refresh</c>, <c>play</c> and <c>folder</c>: a temporary home folder, a
/// temporary folder the "running program" is in (with a stand-in <c>dmc</c> and a stand-in native library), an environment
/// held in a dictionary, a current folder, a fake user <c>Path</c> and a fixed clock. Nothing a test does through it can reach
/// the real home folder, PATH or registry. Deleted on dispose.
/// </summary>
public sealed class TestMachine : IDisposable
{
    /// <summary>The root of everything this machine owns.</summary>
    public string Root { get; } = DmcFolders.NewTemp();

    /// <summary>The user's home folder.</summary>
    public string Home => Path.Combine(Root, "home");

    /// <summary>The folder the "running program" is in: where <c>dmc.json</c> is read from and where its stand-in files are.</summary>
    public string RunningFolder => Path.Combine(Root, "running");

    /// <summary>The stand-in for the running program, a file in <see cref="RunningFolder"/>.</summary>
    public string RunningExecutable => Path.Combine(RunningFolder, OperatingSystem.IsWindows() ? "dmc.exe" : "dmc");

    /// <summary>The current folder.</summary>
    public string Cwd { get; set; }

    /// <summary>The environment: a variable that is not in it is not set. Changing it changes what the commands see.</summary>
    public Dictionary<string, string?> Environment { get; } = new();

    /// <summary>The user's <c>Path</c>, in memory.</summary>
    public FakeUserPath UserPath { get; } = new();

    /// <summary>The moment <c>.dmc/replaced/</c> folders are named after.</summary>
    public DateTime Now { get; set; } = new(2026, 10, 24, 12, 30, 45);

    /// <summary>True to act as Windows (the program folder, the user <c>Path</c>); false as Linux. The real OS by default.</summary>
    public bool Windows { get; set; } = OperatingSystem.IsWindows();

    /// <summary>The terminal the machine offers when standard input is not one; null for none (the answer then is the default).</summary>
    public TextReader? Terminal { get; set; }

    /// <summary>The stand-in native library's file name: <c>native.dll</c> on Windows, <c>native.so</c> elsewhere (the real system).</summary>
    public static string NativeLibrary => OperatingSystem.IsWindows() ? "native.dll" : "native.so";

    /// <summary>Makes the folders, the stand-in program and its stand-in library, with <c>PATH</c> set to something harmless.</summary>
    public TestMachine()
    {
        Directory.CreateDirectory(Home);
        Directory.CreateDirectory(RunningFolder);
        File.WriteAllText(RunningExecutable, "stand-in for dmc");
        // Named for the real system: the constructor runs before an initializer sets Windows, and the libraries beside
        // the running program are always the real system's.
        File.WriteAllText(Path.Combine(RunningFolder, NativeLibrary), "stand-in for a native library");
        File.WriteAllText(Path.Combine(RunningFolder, "notes.pdb"), "not a library: never copied");
        Cwd = Root;
        Environment["PATH"] = Path.Combine(Root, "elsewhere");
    }

    /// <summary>The program folder <c>dmc install</c> would use on this machine.</summary>
    public string ProgramFolder => ProgramInstall.ProgramFolder(Home, name => Environment.GetValueOrDefault(name), Windows);

    /// <summary>
    /// The machine as the commands take it. <c>LOCALAPPDATA</c> is set to a folder under <see cref="Home"/> on Windows, so the
    /// program folder is under the temporary home too.
    /// </summary>
    /// <param name="version">The version the "running program" says it is: <c>1.2.3</c> for a release, <c>0.0.0-dev</c> for a development build.</param>
    /// <param name="files">The generated files it writes; a small fixed set when null.</param>
    internal Machine Machine(string version = "1.2.3", IReadOnlyList<GeneratedFile>? files = null)
    {
        Environment.TryAdd("LOCALAPPDATA", Path.Combine(Home, "AppData", "Local"));
        return new Machine(name => Environment.GetValueOrDefault(name), Cwd, Home, Windows, RunningFolder, RunningExecutable,
            version, files ?? Files(), () => Now, Windows ? UserPath : null, () => Terminal);
    }

    /// <summary>A small set of generated files: the rules, the settings, a skill and a template.</summary>
    /// <param name="rules">The text of <c>CLAUDE.md</c>.</param>
    public static IReadOnlyList<GeneratedFile> Files(string rules = "rules v1\n") =>
    [
        new GeneratedFile(".claude/settings.json", "{}\n"u8.ToArray()),
        new GeneratedFile(".claude/skills/dmc-draw-map/SKILL.md", "draw map\n"u8.ToArray()),
        new GeneratedFile("CLAUDE.md", System.Text.Encoding.UTF8.GetBytes(rules)),
        new GeneratedFile("templates/feedback.md", "feedback\n"u8.ToArray()),
    ];

    /// <summary>Deletes everything the machine owns.</summary>
    public void Dispose() => DmcFolders.Delete(Root);
}
