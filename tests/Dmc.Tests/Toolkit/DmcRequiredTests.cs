using Dmc.Tests.Support;

namespace Dmc.Tests.Toolkit;

/// <summary>
/// Spec 017 (issue #208): <c>dmc</c> is a required part of DMC. The kit carries no instruction for playing without it
/// (FR-001), and a missing <c>dmc</c> stops play until <c>dmc --version</c> runs (FR-003).
/// </summary>
public sealed partial class ToolkitTests
{
    /// <summary>
    /// The wording spec 017 removed from the kit. It is the removed wording itself, not a looser "if `dmc` fails", which
    /// would also match the resume's closed-campaign guard that stays (FR-010).
    /// </summary>
    static readonly string[] RemovedFallbackWording =
    [
        "If `dmc` is unavailable",
        "Without `dmc`, skip",
        "If `dmc` fails or the recall",
        "Otherwise, or when it finds nothing",
        "carry on without dice and maps",
    ];

    /// <summary>
    /// No kit file tells DMC how to play without <c>dmc</c>, and <c>kit/dm-rules.md</c> says to play nothing until it
    /// runs (spec 017, FR-001, FR-003).
    /// </summary>
    [TestMethod]
    public void TheKitCarriesNoRuleForPlayingWithoutDmc()
    {
        string kit = Path.Combine(TestEnv.Root, "kit");
        var found = new List<string>();
        foreach (string file in Directory.EnumerateFiles(kit, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            string text = File.ReadAllText(file);
            foreach (string phrase in RemovedFallbackWording)
            {
                if (text.Contains(phrase, StringComparison.OrdinalIgnoreCase))
                {
                    found.Add($"{Path.GetRelativePath(TestEnv.Root, file)}: \"{phrase}\"");
                }
            }
        }

        Assert.AreEqual(0, found.Count, "Fallback wording for a missing dmc is back in the kit:\n" + string.Join("\n", found));
        StringAssert.Contains(Read("kit/dm-rules.md"), "then play nothing until `dmc --version` runs");
    }
}
