using Dmc.PersonRecords;
using Dmc.Retrieval;
using Dmc.Size;
using Dmc.Tests.PersonRecords;
using Dmc.Tests.Support;

namespace Dmc.Tests.ContextCheck;

/// <summary>
/// A check on this computer, not in CI (spec 032 T055): at every fork point of a slip-test trial, the hook's output
/// on the player's message, run on a DMC folder holding the migrated campaign, prints each chosen speaker's "says" line
/// whenever that person is present or named and has one, and the whole output stays within
/// <see cref="Commands.ContextCheckCommand.OutputCapBytes"/>. Found by SC-003's run (2026-11-27), where one person's
/// long block kept another's from ever printing. Play's names and text never enter the repository, so the folder, the
/// messages and the persons come from the environment; without them the test is inconclusive:
/// <list type="bullet">
/// <item><c>DMC_ROWS_CHECK_FOLDER</c>: the DMC folder (only read).</item>
/// <item><c>DMC_ROWS_CHECK_MESSAGES</c>: a file of sections, <c>## &lt;point&gt;</c> then the player's message.</item>
/// <item><c>DMC_ROWS_CHECK_PERSONS</c>: the persons' full names, separated by <c>|</c>.</item>
/// </list>
/// It prints who is present, and per person at how many points they were in the scene and at how many they had a
/// "says" line (a person in the scene has none when they use no word for anyone else in it).
/// </summary>
[TestClass]
public sealed class RowsAtSlipPointsCheck
{
    /// <summary>The test's context, for its output.</summary>
    public TestContext TestContext { get; set; } = null!;

    /// <summary>The check; inconclusive without its three variables.</summary>
    [TestMethod]
    public void EveryChosenSpeakersSaysLinePrintsAtEveryPointAndTheOutputStaysWithinTheCap()
    {
        string? folder = Environment.GetEnvironmentVariable("DMC_ROWS_CHECK_FOLDER");
        string? messages = Environment.GetEnvironmentVariable("DMC_ROWS_CHECK_MESSAGES");
        string? persons = Environment.GetEnvironmentVariable("DMC_ROWS_CHECK_PERSONS");
        if (string.IsNullOrEmpty(folder) || string.IsNullOrEmpty(messages) || string.IsNullOrEmpty(persons))
        {
            Assert.Inconclusive("a check on this computer: set DMC_ROWS_CHECK_FOLDER, DMC_ROWS_CHECK_MESSAGES and DMC_ROWS_CHECK_PERSONS");
            return;
        }
        string campaign = CanonReminder.ActiveCampaign(folder) ?? throw new InvalidOperationException("no active campaign in " + folder);
        var index = RecordIndex.Build(folder, campaign);
        string gameState = Path.Combine(folder, "adventures", campaign, "game-state.md");
        string presentText = ReadSet.PresentText(Measurement.ReadLines(gameState));
        var chosen = persons.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        foreach (string person in chosen)
            Assert.IsNotNull(index.Record(person), "no record for " + person);

        TestContext.WriteLine("present: " + string.Join("; ", RowsReminder.Present(index, presentText)));
        var points = Points(File.ReadAllText(messages));
        Assert.IsNotEmpty(points, "no points in " + messages);
        var counts = chosen.ToDictionary(p => p, _ => 0);
        var inScene = chosen.ToDictionary(p => p, _ => 0);
        var failures = new List<string>();
        int largest = 0;
        foreach (var (point, prompt) in points)
        {
            var result = DmcRunner.InProcess(["context-check"], RecordFixtures.Prompt(folder, prompt));
            int bytes = System.Text.Encoding.UTF8.GetByteCount(result.Stdout);
            largest = Math.Max(largest, bytes);
            if (bytes > Commands.ContextCheckCommand.OutputCapBytes)
                failures.Add($"{point}: {bytes} bytes");
            var scene = RowsReminder.Present(index, presentText).Concat(index.Named(prompt)).Distinct(StringComparer.Ordinal).ToList();
            foreach (string person in chosen.Where(scene.Contains))
            {
                inScene[person]++;
                string? says = RowsReminder.Block(index, person, scene).FirstOrDefault(l => l.StartsWith($"- {person} says: ", StringComparison.Ordinal));
                if (says is null)
                    continue;
                counts[person]++;
                if (!result.StdoutLines.Contains(says))
                    failures.Add($"{point}: no \"says\" line for {person}");
            }
        }
        foreach (var (person, n) in counts)
            TestContext.WriteLine($"{person}: in the scene at {inScene[person]} of {points.Count} points, with a \"says\" line at {n}");
        TestContext.WriteLine($"largest output: {largest} bytes");
        Assert.IsEmpty(failures, string.Join("\n", failures));
    }

    /// <summary>The points of a messages file: each <c>## &lt;point&gt;</c> and the text under it, trimmed.</summary>
    /// <param name="text">The file's text.</param>
    static List<(string Point, string Prompt)> Points(string text)
    {
        var points = new List<(string, string)>();
        foreach (string section in text.Replace("\r\n", "\n").Split("\n## ").Skip(1))
        {
            int end = section.IndexOf('\n');
            if (end > 0)
                points.Add((section[..end].Trim(), section[(end + 1)..].Trim()));
        }
        return points;
    }
}
