using System.Text;
using Dmc.Retrieval;
using Dmc.Size;
using Dmc.Tests.Support;
using static Dmc.Tests.Size.SizeFixtures;

namespace Dmc.Tests.Retrieval;

/// <summary>
/// The scene read under <c>dmc recall NAME --scene</c> (spec 013 FR-007 to FR-009, research R3,
/// contracts/dmc-recall-scene.md): the words it takes from <c>game-state.md</c>, the canon lines it searches, its ranking
/// and its cap.
/// </summary>
[TestClass]
public sealed class SceneReadTests
{
    /// <summary>The test's DMC folder, with the fixture campaign.</summary>
    string _root = "";

    /// <summary>Makes the DMC folder.</summary>
    [TestInitialize]
    public void MakeFolder() => _root = NewDmcFolderWithCampaign();

    /// <summary>Deletes it.</summary>
    [TestCleanup]
    public void RemoveFolder() => DmcFolders.Delete(_root);

    /// <summary>A canon over the whole mark: Rulings and Prices holding <paramref name="always"/>, a People and a Timeline holding <paramref name="searched"/>, padded with filler lines that match nothing.</summary>
    /// <param name="always">Lines of the always-read sections.</param>
    /// <param name="searched">Lines of the searched sections.</param>
    static string BigCanon(string always, string searched)
    {
        var text = new StringBuilder("# Canon\n\n## Rulings\n\n").Append(always)
            .Append("\n## Prices, debts and promises\n\n- none\n\n## People\n\n").Append(searched).Append("\n## Timeline\n\n");
        while (Encoding.UTF8.GetByteCount(text.ToString()) < Budgets.CanonWholeBelow * 4 + 100)
            text.Append("- zzzz zzzz zzzz zzzz zzzz zzzz zzzz zzzz zzzz zzzz zzzz zzzz zzzz zzzz (s1).\n");
        return text.ToString();
    }

    /// <summary>The words come from the Scene block's four fields, the Active quests and Pending, never from labels, other fields, finished quests or comments.</summary>
    [TestMethod]
    public void TheWordsComeFromTheSceneTheActiveQuestsAndPending()
    {
        string gameState = "# Game state\n\n## Scene\n\n- **Location**: Marrow ford\n- **In-world date and time**: Midwinter\n"
            + "- **Conditions** (weather, light): sleet\n- **Present**: Oswin\n- **What is happening**: haggling\n"
            + "- **The player was about to**: cross <!-- not this -->\n\n## Party status\n\n### Ada\n\n- **HP**: 9\n\n"
            + "## Quests\n\n### Active\n\n- Find the lantern\n\n### Completed\n\n- Burn the mill\n\n## Pending\n\n- Kole's letter\n";
        string text = SceneRead.Text(Measurement.Split(Encoding.UTF8.GetBytes(gameState)));
        foreach (string word in new[] { "Marrow ford", "Oswin", "haggling", "cross", "Find the lantern", "Kole's letter" })
            StringAssert.Contains(text, word);
        foreach (string word in new[] { "Location", "Midwinter", "sleet", "not this", "Burn the mill", "HP" })
            Assert.DoesNotContain(word, text);
    }

    [TestMethod]
    public void ASmallCanonIsToBeReadWhole()
    {
        CollectionAssert.AreEqual(new[] { SceneRead.SmallLine }, SceneRead.Run(_root, Name));
    }

    /// <summary>The scene's lines are found and ranked; the always-read sections, read whole anyway, are never printed.</summary>
    [TestMethod]
    public void TheScenesLinesComeFirstAndTheAlwaysReadSectionsAreNotPrinted()
    {
        Write(_root, "story/canon.md", BigCanon(
            "- Oswin's ford toll is waived for the militia (ruling s2).\n",
            "- Oswin keeps the ferry at Marrow ford and owes Ada a crossing (s3.1).\n- Brenna runs the mill on the hill road (s1.2).\n"));
        Write(_root, "game-state.md", GameState([], present: "Oswin").Replace("a road", "Marrow ford"));
        var lines = SceneRead.Run(_root, Name);
        StringAssert.StartsWith(lines[0], $"{Name} story/canon.md:");
        StringAssert.Contains(lines[0], "Oswin keeps the ferry");
        Assert.IsFalse(lines.Any(l => l.Contains("toll is waived", StringComparison.Ordinal)), "Rulings are read whole, not printed again");
        Assert.IsFalse(lines.Any(l => l.Contains("Brenna", StringComparison.Ordinal)), "a line with no word of the scene is not printed");
    }

    /// <summary>The output stays within the cap and ends with the count of lines that did not fit.</summary>
    [TestMethod]
    public void TheOutputStaysWithinItsCapAndCountsTheRest()
    {
        var people = new StringBuilder();
        for (int i = 0; i < 400; i++)
            people.Append($"- Oswin's cousin number {i} lives by Marrow ford and mends nets for the ferry, every day but feast days (s{i + 1}).\n");
        Write(_root, "story/canon.md", BigCanon("- none\n", people.ToString()));
        Write(_root, "game-state.md", GameState([], present: "Oswin"));
        var lines = SceneRead.Run(_root, Name);
        Assert.IsLessThanOrEqualTo(Budgets.SceneReadBytes, lines.Sum(Recall.Bytes));
        StringAssert.EndsWith(lines[^1], "more lines matched)");
    }

    [TestMethod]
    public void AGameStateThatNamesNothingHasNothingToSearchFor()
    {
        Write(_root, "story/canon.md", BigCanon("- none\n", "- Oswin keeps the ferry (s3.1).\n"));
        Write(_root, "game-state.md", "# Game state\n\n## Scene\n\n- **Location**:\n- **Present**:\n\n## Pending\n\n");
        CollectionAssert.AreEqual(new[] { SceneRead.NothingToSearch }, SceneRead.Run(_root, Name));
    }

    [TestMethod]
    public void ASceneNoSearchedLineHoldsMatchesNothing()
    {
        Write(_root, "story/canon.md", BigCanon("- Quillon rules the bridge (s1).\n", "- Oswin keeps the ferry (s3.1).\n"));
        Write(_root, "game-state.md", GameState([], present: "Quillon").Replace("a road", "the bridge").Replace("walking", "standing"));
        CollectionAssert.AreEqual(new[] { SceneRead.NothingMatched }, SceneRead.Run(_root, Name));
    }

    [TestMethod]
    public void ACampaignWithNoCanonFails()
    {
        File.Delete(Path.Combine(CampaignFolder(_root), "story", "canon.md"));
        var e = Assert.ThrowsExactly<DmcException>(() => SceneRead.Run(_root, Name));
        Assert.AreEqual($"no story/canon.md in {Name}", e.Message);
    }
}
