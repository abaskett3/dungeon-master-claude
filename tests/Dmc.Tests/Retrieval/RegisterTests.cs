using System.Text;
using Dmc.Retrieval;
using Dmc.Size;

namespace Dmc.Tests.Retrieval;

/// <summary>The names-and-roles register inside canon's People entries (spec 013 FR-018, research R6, data-model.md § Register).</summary>
[TestClass]
public sealed class RegisterTests
{
    /// <summary>The lines of a canon text.</summary>
    static List<Line> Canon(string text) => Measurement.Split(Encoding.UTF8.GetBytes(text));

    /// <summary>The People section of a canon with some entries, between a Rulings and a Places section.</summary>
    static List<Line> People(params string[] entries) =>
        Canon("# Canon\n\n## Rulings\n\n- **Kole** rules nothing here (s1).\n\n## People\n\n<!-- Who exists. -->\n\n"
              + string.Join("\n", entries) + "\n\n## Places\n\n- **Marrow ford**: the ferry (s3).\n");

    [TestMethod]
    public void AnEntrysNamesAreItsBoldNamesBeforeTheColonAndItsAlsoItems()
    {
        var kole = Register.Of(People("- **Captain Emmajeen Kole** (also: Kole, Em; the captain): captain of the guard, daughter of **Aldric Kole** (s1).")).Single();
        Assert.AreEqual("Captain Emmajeen Kole", kole.Key);
        CollectionAssert.AreEquivalent(new[] { "Captain Emmajeen Kole", "Kole", "Em", "the captain" }, kole.Phrases.ToArray(),
            "Aldric Kole is after the colon, so he is not one of her names");
        CollectionAssert.AreEquivalent(new[] { "Emmajeen", "Kole" }, kole.Words.ToArray(), "Captain is a title, never a name alone");
    }

    /// <summary>A bare title in the also bracket is dropped: "Captain" would name every captain (seen in a model's migration, 2026-11-15).</summary>
    [TestMethod]
    public void ABareTitleInTheAlsoBracketIsNotAName()
    {
        var kole = Register.Of(People("- **Captain Emmajeen Kole** (also: Kole, Captain, Auntie, Auntie Em): captain of the guard (s1).")).Single();
        CollectionAssert.AreEquivalent(new[] { "Captain Emmajeen Kole", "Kole", "Auntie Em" }, kole.Phrases.ToArray());
        Assert.AreEqual(0, Register.Named("the captain says no", [kole]).Count, "a bare title names no one");
    }

    /// <summary>People come in the order the message names them, so the cap drops the last named, not the latest in canon (contracts/context-check-canon.md).</summary>
    [TestMethod]
    public void PeopleAreNamedInTheMessagesOrder()
    {
        var people = Register.Of(People("- **Captain Emmajeen Kole**: the captain (s1).", "- **Bruni Coalhand**: the smith (s2)."));
        CollectionAssert.AreEqual(new[] { "Bruni Coalhand", "Captain Emmajeen Kole" },
            Register.Named("I ask Bruni about Kole", people).Select(p => p.Key).ToArray());
    }

    /// <summary>An entry under a <c>*</c> bullet reads its also bracket as a <c>-</c> one does.</summary>
    [TestMethod]
    public void AStarBulletsAlsoNamesAreRead()
    {
        var oswin = Register.Of(People("* **Oswin Reed** (also: the ferryman): keeps the ferry (s3).")).Single();
        CollectionAssert.Contains(oswin.Phrases.ToArray(), "the ferryman");
    }

    /// <summary>An indented line is a detail of the entry above it: not a person, and not a shape failure.</summary>
    [TestMethod]
    public void AnIndentedLineIsADetailNotAnEntry()
    {
        var people = Register.Of(People("- **Oswin Reed**: keeps the ferry (s3).", "  - **His brother**: lost at the weir (s4).", "  - lives by the mill (s4)."));
        Assert.AreEqual("Oswin Reed", people.Single().Key);
        Assert.IsTrue(Register.IsNamedOrNotAnEntry("  - lives by the mill (s4)."));
    }

    [TestMethod]
    public void OnlyPeopleEntriesAreRead()
    {
        var people = Register.Of(People("- **Oswin Reed**: the ferryman (s3)."));
        Assert.AreEqual("Oswin Reed", people.Single().Key, "Rulings and Places are not the register");
    }

    [TestMethod]
    public void ABracketThatIsNotAlsoIsANoteNotNames()
    {
        var rorn = Register.Of(People("- **Rorn** (properly **Hysvearorn**, \"Soaring Silver\"): an adult silver dragon (s8).")).Single();
        CollectionAssert.AreEquivalent(new[] { "Rorn", "Hysvearorn" }, rorn.Phrases.ToArray(),
            "both bold names before the colon count; the quoted words in the note do not");
    }

    [TestMethod]
    public void AnEntryOfTwoPeopleWithNoColonNamesBoth()
    {
        var entry = Register.Of(People("- **Tam Birch** (a carter) and his mother **Dessa Birch** (the bakery). Sheets.")).Single();
        CollectionAssert.IsSubsetOf(new[] { "Tam Birch", "Dessa Birch" }, entry.Phrases.ToArray());
    }

    [TestMethod]
    public void LowercaseAndPossessiveWordsOfABoldNameAreNotNamesAlone()
    {
        var entry = Register.Of(People("- **Hala's two loves** (the player): Tam first (s41).")).Single();
        CollectionAssert.AreEqual(new[] { "Hala" }, entry.Words.ToArray(), "\"loves\" would match ordinary speech");
    }

    [TestMethod]
    public void AListItemWithNoBoldNameIsNotAnEntryAndFailsTheShape()
    {
        Assert.IsEmpty(Register.Of(People("- Oswin keeps the ferry (s3).")));
        Assert.IsFalse(Register.IsNamedOrNotAnEntry("- Oswin keeps the ferry (s3)."));
        Assert.IsTrue(Register.IsNamedOrNotAnEntry("- **Oswin Reed**: the ferryman (s3)."));
        Assert.IsTrue(Register.IsNamedOrNotAnEntry("<!-- Who exists. -->"));
        Assert.IsTrue(Register.IsNamedOrNotAnEntry(""));
    }

    [TestMethod]
    public void ANameMatchesWholeAndIgnoringCase()
    {
        var people = Register.Of(People("- **Oswin Reed** (also: the ferryman): keeps the ferry (s3)."));
        Assert.HasCount(1, Register.Named("we should ask OSWIN", people));
        Assert.HasCount(1, Register.Named("find the ferryman", people));
        Assert.IsEmpty(Register.Named("Oswinson is here", people), "not inside a longer word");
        Assert.IsEmpty(Register.Named("the reeds are high", people), "not inside a longer word");
    }

    [TestMethod]
    public void ALoneWordNamesOnlyTheFirstEntryItFits()
    {
        var people = Register.Of(People(
            "- **Captain Emmajeen Kole**: the captain (s1).",
            "- **Kole and the Fall Festival** (the player): her favourite holiday (s41).",
            "- **Kole's rule**: never alone (s2)."));
        CollectionAssert.AreEqual(new[] { "Captain Emmajeen Kole" }, Register.Named("Ask Kole", people).Select(p => p.Key).ToArray());
        CollectionAssert.AreEqual(new[] { "Captain Emmajeen Kole", "Kole and the Fall Festival" },
            Register.Named("Kole and the Fall Festival again", people).Select(p => p.Key).ToArray(), "a whole bold name still names its entry");
    }

    [TestMethod]
    public void TheRankingWordsLeaveOutTitles()
    {
        var kole = Register.Of(People("- **Captain Emmajeen Kole** (also: the captain): x (s1).")).Single();
        Assert.AreEqual("Emmajeen Kole", Register.QueryText(kole));
    }
}
