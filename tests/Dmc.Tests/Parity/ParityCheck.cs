using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Dmc.Tests.Support;

namespace Dmc.Tests.Parity;

/// <summary>
/// One input for the parity check: the same arguments, stdin and starting files given to an old script and to the
/// matching <c>dmc</c> subcommand.
/// </summary>
/// <param name="Name">Shown in the test name and in failure messages.</param>
/// <param name="Args">The arguments after the script name; they get the working folder, to name files in it.</param>
/// <param name="Stdin">The standard input, given the working folder; null for none.</param>
/// <param name="Seed">Writes the starting files into a fresh working folder.</param>
public sealed record ParityCase(string Name, Func<string, string[]> Args, Func<string, string>? Stdin = null,
    Action<string>? Seed = null)
{
    public ParityCase(string name, params string[] args) : this(name, _ => args)
    {
    }

    public override string ToString() => Name;
}

/// <summary>
/// An old script and the <c>dmc</c> subcommand that replaces it. Adding a subcommand in a later phase means one
/// more <see cref="ParityTool"/> and a list of <see cref="ParityCase"/>s: nothing else here changes.
/// </summary>
/// <param name="Name">The script, for messages ("roll.sh").</param>
/// <param name="DmcCommand">The subcommand ("roll").</param>
/// <param name="RunLegacy">Runs the script in a folder with the arguments and stdin; null when its tool is missing.</param>
/// <param name="Normalize">
/// Rewrites text output before it is compared, to remove what may legitimately differ. Line endings and the
/// working folder's name are always normalized first.
/// </param>
public sealed record ParityTool(string Name, string DmcCommand,
    Func<string, string[], string?, ProcessResult> RunLegacy, Func<string, string>? Normalize = null);

/// <summary>
/// The parity check of issue #3 ("Parity check during phases 1-5"): run the old script and the matching
/// <c>dmc</c> subcommand on the same fixtures, and compare stdout, stderr, the exit code and the files written.
/// Delete this folder in Phase 6, together with the scripts.
/// </summary>
public static class ParityCheck
{
    public static void Run(ParityTool tool, ParityCase test)
    {
        using var legacyFolder = new Folder();
        using var dmcFolder = new Folder();
        test.Seed?.Invoke(legacyFolder.Path);
        test.Seed?.Invoke(dmcFolder.Path);

        string[] legacyArgs = test.Args(legacyFolder.Path);
        string[] dmcArgs = test.Args(dmcFolder.Path);
        var legacy = tool.RunLegacy(legacyFolder.Path, legacyArgs, test.Stdin?.Invoke(legacyFolder.Path));
        var dmc = DmcRunner.BlackBox([tool.DmcCommand, .. dmcArgs], test.Stdin?.Invoke(dmcFolder.Path), dmcFolder.Path);

        string Normalize(string text, string folder)
        {
            text = text.Replace("\r\n", "\n").Replace(folder, "<work>").Replace(folder.Replace('\\', '/'), "<work>");
            return tool.Normalize is null ? text : tool.Normalize(text);
        }

        var problems = new List<string>();
        if (legacy.ExitCode != dmc.ExitCode)
            problems.Add($"exit code: {tool.Name} {legacy.ExitCode}, dmc {dmc.ExitCode}");
        Compare("stdout", Normalize(legacy.Stdout, legacyFolder.Path), Normalize(dmc.Stdout, dmcFolder.Path), problems);
        Compare("stderr", Normalize(legacy.Stderr, legacyFolder.Path), Normalize(dmc.Stderr, dmcFolder.Path), problems);

        var legacyFiles = Snapshot(legacyFolder.Path);
        var dmcFiles = Snapshot(dmcFolder.Path);
        foreach (string file in legacyFiles.Keys.Union(dmcFiles.Keys).Order())
        {
            legacyFiles.TryGetValue(file, out string? a);
            dmcFiles.TryGetValue(file, out string? b);
            if (a != b)
                problems.Add($"file {file}: {(a is null ? "only dmc wrote it" : b is null ? $"only {tool.Name} wrote it" : "contents differ")}");
        }

        if (problems.Count > 0)
        {
            Assert.Fail($"{tool.Name} and dmc {tool.DmcCommand} differ on '{test.Name}' (args: {string.Join(' ', dmcArgs)}):\n  "
                        + string.Join("\n  ", problems)
                        + $"\n--- {tool.Name} stdout:\n{legacy.Stdout}\n--- dmc stdout:\n{dmc.Stdout}");
        }
    }

    static void Compare(string what, string legacy, string dmc, List<string> problems)
    {
        if (legacy != dmc)
            problems.Add($"{what}: {Quote(legacy)} != {Quote(dmc)}");

        static string Quote(string text) => "\"" + text.Replace("\n", "\\n") + "\"";
    }

    /// <summary>Every file under a folder, by relative path, with a hash of its contents.</summary>
    static Dictionary<string, string> Snapshot(string folder) =>
        Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories).ToDictionary(
            f => Path.GetRelativePath(folder, f).Replace('\\', '/'),
            f => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(f))));

    /// <summary>A fresh working folder, removed afterwards.</summary>
    sealed class Folder : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "dmc-parity-" + Guid.NewGuid().ToString("N"));

        public Folder() => Directory.CreateDirectory(Path);

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}

/// <summary>
/// Normalizing <c>roll</c> output: the dice differ from run to run, so a rolled line is checked for format and
/// range, and replaced by its shape. Error lines are compared as they are.
/// </summary>
public static partial class RollOutput
{
    [GeneratedRegex(@"^(\S+): \[([\d, ]+)\](?: keep \[([\d, ]+)\])?(?: ([+-]\d+))? = (-?\d+)$")]
    private static partial Regex RolledLine();

    [GeneratedRegex(@"^(\d*)d(\d+)(?:(kh|kl)(\d+))?([+-]\d+)?$", RegexOptions.IgnoreCase)]
    private static partial Regex Expression();

    /// <summary>The scripts say "usage: roll.sh ..." and dmc says "usage: dmc roll ...": the only difference allowed.</summary>
    [GeneratedRegex(@"^usage: (roll\.sh|roll\.ps1|dmc roll) ", RegexOptions.Multiline)]
    private static partial Regex Usage();

    public static string Normalize(string text) =>
        string.Join("\n", Usage().Replace(text, "usage: <roller> ").Split('\n').Select(Shape));

    static string Shape(string line)
    {
        var rolled = RolledLine().Match(line);
        if (!rolled.Success)
            return line;

        var expr = Expression().Match(rolled.Groups[1].Value);
        if (!expr.Success)
            Assert.Fail($"a rolled line for something that is not a dice expression: {line}");

        int count = expr.Groups[1].Value.Length == 0 ? 1 : int.Parse(expr.Groups[1].Value);
        int sides = int.Parse(expr.Groups[2].Value);
        int[] dice = Numbers(rolled.Groups[2].Value);
        Assert.HasCount(count, dice, $"dice rolled: {line}");
        Assert.IsTrue(dice.All(d => d >= 1 && d <= sides), $"a die is out of range 1..{sides}: {line}");

        int[] kept = dice;
        string keepShape = "";
        if (expr.Groups[3].Success)
        {
            int keep = int.Parse(expr.Groups[4].Value);
            kept = (expr.Groups[3].Value.ToLowerInvariant() == "kh" ? dice.OrderDescending() : dice.Order()).Take(keep).ToArray();
            Assert.IsTrue(rolled.Groups[3].Success, $"missing 'keep': {line}");
            CollectionAssert.AreEqual(kept, Numbers(rolled.Groups[3].Value), $"wrong dice kept: {line}");
            keepShape = $" keep [{keep} dice]";
        }
        else
        {
            Assert.IsFalse(rolled.Groups[3].Success, $"unexpected 'keep': {line}");
        }

        int mod = expr.Groups[5].Success ? int.Parse(expr.Groups[5].Value) : 0;
        Assert.AreEqual(mod == 0 ? "" : mod.ToString("+0;-0"), rolled.Groups[4].Value, $"modifier shown: {line}");
        Assert.AreEqual(kept.Sum() + mod, int.Parse(rolled.Groups[5].Value), $"total: {line}");

        string modShape = mod == 0 ? "" : $" {mod:+0;-0}";
        return $"{rolled.Groups[1].Value}: [{count} dice in 1..{sides}]{keepShape}{modShape} = <total>";
    }

    static int[] Numbers(string text) => text.Split(", ").Select(int.Parse).ToArray();
}
