using Dmc.Campaign;

namespace Dmc.Tests.Campaign;

/// <summary>The accounting file DMC writes during a migration (data-model.md § Accounting file), as unit tests of its reader.</summary>
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
}
