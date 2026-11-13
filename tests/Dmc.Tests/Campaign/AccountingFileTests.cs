using Dmc.Campaign;

namespace Dmc.Tests.Campaign;

/// <summary>
/// The accounting files DMC writes, a migration's (spec 005 data-model.md § Accounting file) and a digest's (spec 011
/// research R9), as unit tests of their one reader.
/// </summary>
[TestClass]
public sealed class AccountingFileTests
{
    /// <summary>The message of the error a text makes.</summary>
    static string ErrorOf(string text) =>
        Assert.ThrowsExactly<DmcException>(() => AccountingFile.Parse(text, "acc.tsv")).Message;

    [TestMethod]
    public void EveryKindOfLineIsRead()
    {
        string text = "# accounting for rorn\n\n0123456789ab\treworded\tMara runs the ferry\r\n"
            + "ABCDEF012345\tdropped:mix-tracker\tthe log of sessions\n"
            + "skipped\tstory/odd.md\thand-edited, shape not recognised\n";
        var file = AccountingFile.Parse(text, "acc.tsv");
        Assert.HasCount(2, file.Entries);
        var reworded = file.Entries["0123456789ab"];
        Assert.AreEqual(Disposition.Reworded, reworded.Disposition);
        Assert.AreEqual("Mara runs the ferry", reworded.Detail);
        Assert.AreEqual(3, reworded.Line);
        var dropped = file.Entries["abcdef012345"];
        Assert.AreEqual(Disposition.Dropped, dropped.Disposition);
        Assert.AreEqual("mix-tracker", dropped.Class);
        var skipped = file.Skipped.Single();
        Assert.AreEqual("story/odd.md", skipped.File);
        Assert.AreEqual("hand-edited, shape not recognised", skipped.Reason);
    }

    [TestMethod]
    public void AMissingFileIsEmpty()
    {
        var file = AccountingFile.Read(Path.Combine(Path.GetTempPath(), "dmc-no-such-" + Guid.NewGuid().ToString("N")), "x");
        Assert.IsEmpty(file.Entries);
        Assert.IsEmpty(file.Skipped);
    }

    [TestMethod]
    [DataRow("0123456789ab\treworded\tshort", "acc.tsv line 1: a reworded unit needs an anchor of at least 8 characters")]
    [DataRow("0123456789ab\tdropped:Mix\tx", "acc.tsv line 1: Mix is not a drop class (lowercase words joined by hyphens)")]
    [DataRow("0123456789ab\tdropped:mix-tracker\t", "acc.tsv line 1: a dropped unit needs a reason")]
    [DataRow("0123456789ab\tmoved\tsomewhere else", "acc.tsv line 1: moved is not a disposition: use reworded or dropped:<class>")]
    [DataRow("0123456789ab\treworded", "acc.tsv line 1: expected three fields separated by tabs: id, disposition, detail")]
    [DataRow("0123456789a\treworded\tlong enough anchor", "acc.tsv line 1: 0123456789a is not an id of 12 hex digits (take it from dmc campaign verify --list)")]
    [DataRow("0123456789ag\treworded\tlong enough anchor", "acc.tsv line 1: 0123456789ag is not an id of 12 hex digits (take it from dmc campaign verify --list)")]
    [DataRow("skipped\t../x.md\twhy", "acc.tsv line 1: skipped file ../x.md is not a campaign path (contains ..)")]
    [DataRow("skipped\t/x.md\twhy", "acc.tsv line 1: skipped file /x.md is not a campaign path (absolute path)")]
    [DataRow("skipped\tbackups/x.zip\twhy", "acc.tsv line 1: skipped file backups/x.zip is not a campaign file")]
    [DataRow("skipped\tstory/x.md\t", "acc.tsv line 1: a skipped file needs a reason")]
    public void ABadLineIsAnErrorNamingItsNumber(string line, string message) => Assert.AreEqual(message, ErrorOf(line));

    [TestMethod]
    public void TheLineNumberCountsCommentsAndBlankLines() =>
        StringAssert.StartsWith(ErrorOf("# a\n\n# b\nbad line\n"), "acc.tsv line 4:");

    /// <summary>A digest's accounting takes <c>merged</c> with an anchor of 8 characters or more, and drops as a migration's does.</summary>
    [TestMethod]
    public void ADigestsAccountingTakesMerged()
    {
        var file = AccountingFile.Parse("0123456789ab\tmerged\tKibil fell through\nabcdef012345\tdropped:structure\ta heading\n", "acc.tsv", AccountingFile.Merged);
        Assert.AreEqual(Disposition.Reworded, file.Entries["0123456789ab"].Disposition, "merged is the digest's word for a unit kept in changed form");
        Assert.AreEqual("Kibil fell through", file.Entries["0123456789ab"].Detail);
        Assert.AreEqual("structure", file.Entries["abcdef012345"].Class);
    }

    /// <summary>A digest's accounting refuses <c>reworded</c>, a short anchor and a skipped file, each naming its line and the word it takes.</summary>
    [TestMethod]
    [DataRow("0123456789ab\treworded\tlong enough anchor", "acc.tsv line 1: reworded is not a disposition: use merged or dropped:<class>")]
    [DataRow("# digest\n0123456789ab\tmerged\tshort", "acc.tsv line 2: a merged unit needs an anchor of at least 8 characters")]
    [DataRow("skipped\tstory/x.md\twhy", "acc.tsv line 1: a digest's accounting has no skipped lines: account each line, or copy it")]
    public void ADigestsAccountingRefusesAMigrationsWords(string line, string message) =>
        Assert.AreEqual(message, Assert.ThrowsExactly<DmcException>(() => AccountingFile.Parse(line, "acc.tsv", AccountingFile.Merged)).Message);

    /// <summary>A migration's accounting refuses <c>merged</c>, naming the word it takes.</summary>
    [TestMethod]
    public void AMigrationsAccountingRefusesMerged() =>
        Assert.AreEqual("acc.tsv line 1: merged is not a disposition: use reworded or dropped:<class>", ErrorOf("0123456789ab\tmerged\tlong enough anchor"));
}
