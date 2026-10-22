using Dmc.Campaign;

namespace Dmc.Tests.Campaign;

/// <summary>
/// The portable-path rules of research R6. They do not depend on the operating system, so these cases run the same on every
/// system, and an archive made on Linux that Windows could not store is refused on Linux too.
/// </summary>
[TestClass]
public sealed class ArchivePathsTests
{
    [TestMethod]
    [DataRow("game-state.md")]
    [DataRow("story/sessions/session-001.md")]
    [DataRow("Château/Écu.md")] // not ASCII
    [DataRow("notes/two words.md")]
    [DataRow("maps/.gitkeep")] // a leading dot is not a trailing one
    [DataRow("a.b.c")]
    [DataRow("console.md")] // starts like a reserved name but is not one
    [DataRow("com10.txt")]
    public void APortablePathIsAccepted(string path) => Assert.IsNull(ArchivePaths.Refusal(path));

    [TestMethod]
    [DataRow("", "empty path")]
    [DataRow("/abs", "absolute path")]
    [DataRow("C:/x", "has a drive")]
    [DataRow("c:x", "has a drive")]
    [DataRow("a\\b", "contains a backslash")]
    [DataRow("a//b", "has an empty segment")]
    [DataRow("a/", "has an empty segment")]
    [DataRow("./x", "contains .")]
    [DataRow("a/./b", "contains .")]
    [DataRow("../x", "contains ..")]
    [DataRow("a/../b", "contains ..")]
    [DataRow("a:b", "has a drive")] // Windows reads it as drive A, relative path b
    [DataRow("ab:c", "contains :")]
    [DataRow("story/a:b", "contains :")]
    [DataRow("a<b", "contains <")]
    [DataRow("a>b", "contains >")]
    [DataRow("a\"b", "contains \"")]
    [DataRow("a|b", "contains |")]
    [DataRow("a?b", "contains ?")]
    [DataRow("a*b", "contains *")]
    [DataRow("a\u0001b", "contains a control character")]
    [DataRow("a\tb", "contains a control character")]
    [DataRow("trailing.", "ends with a dot or a space")]
    [DataRow("trailing ", "ends with a dot or a space")]
    [DataRow("dir./file", "ends with a dot or a space")]
    [DataRow("CON", "reserved name CON")]
    [DataRow("con.txt", "reserved name con")]
    [DataRow("NUL", "reserved name NUL")]
    [DataRow("Com1.md", "reserved name Com1")]
    [DataRow("aux", "reserved name aux")]
    [DataRow("story/PRN.md", "reserved name PRN")]
    [DataRow("lpt9.tar.gz", "reserved name lpt9")]
    [DataRow("con .txt", "reserved name con")] // Windows ignores the spaces before the dot
    public void AnUnportablePathIsRefusedWithItsReason(string path, string reason) =>
        Assert.AreEqual(reason, ArchivePaths.Refusal(path));

    [TestMethod]
    public void TwoPathsThatDifferOnlyByCaseClash()
    {
        var clash = ArchivePaths.CaseClash(["story/canon.md", "Notes.md", "x/y.md", "notes.md"]);
        Assert.IsNotNull(clash);
        Assert.AreEqual("Notes.md", clash.Value.First);
        Assert.AreEqual("notes.md", clash.Value.Second);
    }

    [TestMethod]
    public void PathsThatDifferInMoreThanCaseDoNotClash()
    {
        Assert.IsNull(ArchivePaths.CaseClash(["Notes.md", "notes2.md", "a/Notes.md", "Écu.md"]));
        Assert.IsNull(ArchivePaths.CaseClash([]));
    }

    [TestMethod]
    public void CombineResolvesUnderTheFolder()
    {
        string folder = Path.Combine(Path.GetTempPath(), "dmc-tests-archive-paths");
        string full = ArchivePaths.Combine(folder, "story/sessions/session-001.md");
        Assert.AreEqual(Path.GetFullPath(Path.Combine(folder, "story", "sessions", "session-001.md")), full);
    }

    [TestMethod]
    public void CombineRefusesToResolveOutsideTheFolder()
    {
        string folder = Path.Combine(Path.GetTempPath(), "dmc-tests-archive-paths");
        foreach (string path in new[] { "../evil.txt", "a/../../evil.txt", "..", "a/../.." })
            Assert.ThrowsExactly<DmcException>(() => ArchivePaths.Combine(folder, path), path);
    }

    [TestMethod]
    public void CombineDoesNotMistakeASiblingFolderForTheFolder()
    {
        string parent = Path.Combine(Path.GetTempPath(), "dmc-tests-archive-paths");
        string folder = Path.Combine(parent, "staging");
        // "../staging-evil/x" ends up in a folder whose name starts with the folder's name.
        Assert.ThrowsExactly<DmcException>(() => ArchivePaths.Combine(folder, "../staging-evil/x"));
    }
}
