using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Dmc.Tests.Digest;
using Dmc.Tests.Map;
using Dmc.Tests.Pdf;
using Dmc.Tests.Support;

namespace Dmc.Tests.TokenCost;

/// <summary>
/// The token-cost guard of issue #3: everything <c>dmc</c> prints lands in DMC's context, so every error path
/// prints exactly one line and never a stack trace. The hook command prints nothing at all.
/// </summary>
[TestClass]
public sealed partial class ErrorPathTests
{
    [GeneratedRegex(@"^\s+at \S+|Unhandled exception|Stack trace|\.cs:line \d+", RegexOptions.Multiline)]
    private static partial Regex StackTrace();

    static string[] AllLines(ProcessResult result) => result.StdoutLines.Concat(result.StderrLines).ToArray();

    static void AssertOneLine(ProcessResult result, string what)
    {
        string[] lines = AllLines(result);
        Assert.HasCount(1, lines, $"{what} must print exactly one line, got:\n{result}");
        Assert.IsFalse(StackTrace().IsMatch(result.Stdout + result.Stderr), $"{what} printed a stack trace:\n{result}");
        Assert.AreNotEqual(0, result.ExitCode, what);
    }

    static void AssertSilentSuccess(ProcessResult result, string what)
    {
        Assert.AreEqual(0, result.ExitCode, $"{what}: {result}");
        Assert.AreEqual("", result.Stdout + result.Stderr, what);
    }

    public static IEnumerable<object[]> RollErrors { get; } =
    [
        [Array.Empty<string>()], // no command at all
        [new[] { "nope" }],
        [new[] { "roll" }],
        [new[] { "roll", "banana" }],
        [new[] { "roll", "1d" }],
        [new[] { "roll", "d" }],
        [new[] { "roll", "1d20+" }],
        [new[] { "roll", "1d20 +5" }],
        [new[] { "roll", "2d6+1d4" }],
        [new[] { "roll", "101d6" }],
        [new[] { "roll", "0d6" }],
        [new[] { "roll", "1d1" }],
        [new[] { "roll", "1d1001" }],
        [new[] { "roll", "99999999999d6" }],
        [new[] { "roll", "2d20kh3" }],
        [new[] { "roll", "2d20kl0" }],
        [new[] { "roll", "1d20+99999999999999999999" }], // a modifier too large for any number
        [new[] { "roll", "a\nb" }], // a line break in the echoed expression is written out, not printed
        [new[] { "roll", "a\r\nb" }],
        [new[] { "roll", "1d20\n" }],
    ];

    public static string Name(System.Reflection.MethodInfo method, object[] data) =>
        $"{method.Name}: dmc {string.Join(' ', ((string[])data[0]).Select(a => a.Replace("\n", "\\n")))}";

    [TestMethod]
    [DynamicData(nameof(RollErrors), DynamicDataDisplayName = nameof(Name))]
    public void EveryErrorOfTheCommandLinePrintsOneLine(string[] args)
    {
        // "roll" with several arguments prints one line per argument, so each case here is one bad expression.
        AssertOneLine(DmcRunner.BlackBox(args), $"dmc {string.Join(' ', args)}");
        AssertOneLine(DmcRunner.InProcess(args), $"dmc {string.Join(' ', args)} (in-process)");
    }

    public static IEnumerable<object[]> MapErrors { get; } = MapCases.ErrorCases.Select(c => new object[] { c }).ToArray();

    public static string CaseName(System.Reflection.MethodInfo method, object[] data) => $"{method.Name}: {data[0]}";

    /// <summary>
    /// Every bad map file, bad argument and bad cell prints one line and no stack trace. Every one of them must fail: a
    /// case that draws the map is a mistake in the list, and the test says so.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(MapErrors), DynamicDataDisplayName = nameof(CaseName))]
    public void EveryErrorOfMapPrintsOneLine(CommandCase test) =>
        test.Run("map", (result, _) => AssertOneLine(result, test.Describe("map"))); // which also needs a non-zero exit code

    public static IEnumerable<object[]> PdfErrors { get; } =
        PdfCases.ErrorCases().Concat(PdfCases.DmcOnlyErrorCases()).Select(c => new object[] { c }).ToArray();

    /// <summary>
    /// Every bad argument, missing file, bad page range, protected PDF and file that is not a PDF prints one line and no stack
    /// trace, and writes nothing.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(PdfErrors), DynamicDataDisplayName = nameof(CaseName))]
    public void EveryErrorOfPdfPrintsOneLine(CommandCase test) => test.Run("pdf", (result, folder) =>
    {
        AssertOneLine(result, test.Describe("pdf"));
        Assert.IsFalse(Directory.Exists(Path.Combine(folder, "out")) || Directory.Exists(Path.Combine(folder, "extracted")),
            "a failed run must not write anything");
    });

    public static IEnumerable<object[]> DigestErrors { get; } =
        DigestCases.ErrorCases().Concat(DigestCases.DmcOnlyErrorCases()).Select(c => new object[] { c }).ToArray();

    /// <summary>
    /// Every bad argument, missing folder, empty result and bad log (not UTF-8, a record of the wrong shape, an output path
    /// that is a file) prints one line and no stack trace. Every one of them must fail, and none may write an output folder.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(DigestErrors), DynamicDataDisplayName = nameof(CaseName))]
    public void EveryErrorOfDigestPrintsOneLine(CommandCase test) => test.Run("digest", (result, folder) =>
    {
        AssertOneLine(result, test.Describe("digest"));
        Assert.IsFalse(Directory.Exists(Path.Combine(folder, "out")), "a failed run must not write an output folder");
    });

    [TestMethod]
    public void PlayWithoutClaudePrintsOneLine()
    {
        string empty = Path.Combine(Path.GetTempPath(), "dmc-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(empty);
        try
        {
            var result = TestEnv.Run(DmcRunner.Binary(), ["play", "--continue"],
                environment: new Dictionary<string, string?> { ["PATH"] = empty });
            AssertOneLine(result, "dmc play with no claude on the PATH");
            Assert.AreEqual(127, result.ExitCode, "the shell's code for a command that is not found");
            StringAssert.StartsWith(result.Stderr, "dmc play: Claude Code (claude) is not on the PATH");
        }
        finally
        {
            Directory.Delete(empty, recursive: true);
        }
    }

    [TestMethod]
    public void TheHookNeverPrintsAnErrorOrAStackTrace()
    {
        string folder = Path.Combine(Path.GetTempPath(), "dmc-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            string badRecord = Path.Combine(folder, "bad-record.jsonl");
            File.WriteAllText(badRecord, "{\"type\": \"assistant\", \"message\": \"text\"}\n");
            string[] inputs =
            [
                "", "not json", "[1, 2]", "{\"transcript_path\": 5}", "{\"transcript_path\": \"\"}",
                new JsonObject { ["transcript_path"] = Path.Combine(folder, "missing.jsonl") }.ToJsonString(),
                new JsonObject { ["transcript_path"] = folder }.ToJsonString(),
                new JsonObject { ["transcript_path"] = badRecord }.ToJsonString(),
            ];
            foreach (string input in inputs)
            {
                AssertSilentSuccess(DmcRunner.BlackBox(["context-check"], input), $"context-check with '{input}'");
                AssertSilentSuccess(DmcRunner.InProcess(["context-check"], input), $"context-check (in-process) with '{input}'");
            }
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    // Failures the real commands cannot be made to produce on demand (a disk that fails, an exception from a
    // library) are made here by a command that throws, to test the wrapper that every command runs inside.
    sealed class Throws(Exception exception) : ICommand
    {
        public string Name => "boom";
        public string Summary => "";
        public int Run(string[] args, Io io) => throw exception;
    }

    [TestMethod]
    public void AnExpectedFailureIsOneLineWithItsExitCode()
    {
        var result = DmcRunner.InProcess(["boom"], commands: [new Throws(new DmcException("it broke", exitCode: 2))]);
        Assert.AreEqual(2, result.ExitCode);
        Assert.AreEqual("", result.Stdout);
        Assert.AreEqual("dmc boom: it broke\n", result.Stderr);
    }

    [TestMethod]
    public void AnUnexpectedExceptionIsOneLineWithoutAStackTrace()
    {
        var result = DmcRunner.InProcess(["boom"], commands: [new Throws(new InvalidOperationException("line one\nline two\r\nline three"))]);
        Assert.AreEqual(1, result.ExitCode);
        Assert.AreEqual("dmc boom: line one line two line three (InvalidOperationException)\n", result.Stderr);
        AssertOneLine(result, "an exception");
    }

    [TestMethod]
    public void AnExceptionWithADeepStackIsStillOneLine()
    {
        Exception deep;
        try
        {
            Recurse(5);
            throw new InvalidOperationException("unreachable");
        }
        catch (FormatException e)
        {
            deep = e;
        }
        var result = DmcRunner.InProcess(["boom"], commands: [new Throws(deep)]);
        AssertOneLine(result, "a deep exception");
        StringAssert.StartsWith(result.Stderr, "dmc boom: ");

        static void Recurse(int depth)
        {
            if (depth == 0)
                throw new FormatException("deep");
            Recurse(depth - 1);
        }
    }
}
