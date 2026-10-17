using Dmc.Pdf;
using Dmc.Tests.Digest;

namespace Dmc.Tests.Pdf;

/// <summary>
/// <see cref="OutputStaging"/> on its own, in this process: where its temporary folder is, what a commit moves and what it
/// leaves, what an abandon removes, and what it refuses before it moves anything. The command-level behaviour (a whole
/// <c>dmc pdf</c> run that fails) is in <c>PdfCommandContract</c>.
/// </summary>
[TestClass]
public sealed class OutputStagingTests
{
    /// <summary>Writes a file, making its folders.</summary>
    static void Write(string path, string text)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
    }

    [TestMethod]
    public void TheTemporaryFolderIsInsideTheOutputFolderAndEverythingMissingIsMadeAtOnce()
    {
        using var temp = new TempFolder();
        string output = Path.Combine(temp.Path, "deep", "er", "out");
        using var staging = new OutputStaging(output);
        Assert.AreEqual(output, Path.GetDirectoryName(staging.Path));
        StringAssert.StartsWith(Path.GetFileName(staging.Path), OutputStaging.Prefix);
        Assert.IsTrue(Directory.Exists(staging.Path), "a bad output path would have failed by now, before any work");
    }

    [TestMethod]
    public void AbandoningRemovesExactlyWhatTheRunMade()
    {
        using var temp = new TempFolder();
        Directory.CreateDirectory(Path.Combine(temp.Path, "deep"));
        Write(Path.Combine(temp.Path, "deep", "mine.txt"), "already there");
        using var staging = new OutputStaging(Path.Combine(temp.Path, "deep", "er", "out"));
        Write(Path.Combine(staging.Path, "text", "page-001.txt"), "half done");

        Assert.IsNull(staging.Abandon());
        CollectionAssert.AreEqual(new[] { "mine.txt" }, Directory.GetFileSystemEntries(Path.Combine(temp.Path, "deep")).Select(Path.GetFileName).ToArray(),
            "the output folder, its missing parent and the temporary folder are gone, the folder that was there is not");
        Assert.IsNull(staging.Abandon(), "a second call does nothing");
    }

    [TestMethod]
    public void AbandoningLeavesAnOutputFolderThatWasThereAndWhatIsInIt()
    {
        using var temp = new TempFolder();
        string output = Path.Combine(temp.Path, "out");
        Write(Path.Combine(output, "notes.md"), "mine");
        using (var staging = new OutputStaging(output))
            Write(Path.Combine(staging.Path, "all-text.txt"), "new");
        CollectionAssert.AreEqual(new[] { "notes.md" }, Directory.GetFileSystemEntries(output).Select(Path.GetFileName).ToArray(), "disposing without a commit abandons");
        Assert.AreEqual("mine", File.ReadAllText(Path.Combine(output, "notes.md")));
    }

    [TestMethod]
    public void AnOutputPathThatIsAFileFailsAtOnceAndLeavesItAlone()
    {
        using var temp = new TempFolder();
        string output = Path.Combine(temp.Path, "out");
        File.WriteAllText(output, "a file");
        // An IOException on Windows, a DirectoryNotFoundException (an IOException too) on Linux and macOS.
        Assert.Throws<IOException>(() => new OutputStaging(output));
        Assert.AreEqual("a file", File.ReadAllText(output));
        CollectionAssert.AreEqual(new[] { "out" }, Directory.GetFileSystemEntries(temp.Path).Select(Path.GetFileName).ToArray());
    }

    [TestMethod]
    public void CommittingMovesTheFilesInReplacesTheSameNamesAndKeepsTheRest()
    {
        using var temp = new TempFolder();
        string output = Path.Combine(temp.Path, "out");
        Write(Path.Combine(output, "text", "page-001.txt"), "old");
        Write(Path.Combine(output, "text", "page-009.txt"), "kept");
        Write(Path.Combine(output, "notes.md"), "kept");
        using var staging = new OutputStaging(output);
        Write(Path.Combine(staging.Path, "text", "page-001.txt"), "new");
        Write(Path.Combine(staging.Path, "pages", "page-001.jpg"), "picture");

        Assert.IsNull(staging.Commit());
        Assert.AreEqual("new", File.ReadAllText(Path.Combine(output, "text", "page-001.txt")));
        Assert.AreEqual("picture", File.ReadAllText(Path.Combine(output, "pages", "page-001.jpg")));
        Assert.AreEqual("kept", File.ReadAllText(Path.Combine(output, "text", "page-009.txt")));
        Assert.AreEqual("kept", File.ReadAllText(Path.Combine(output, "notes.md")));
        Assert.IsFalse(Directory.Exists(staging.Path), "the temporary folder is gone");
        staging.Dispose();
        Assert.IsTrue(File.Exists(Path.Combine(output, "notes.md")), "disposing after a commit undoes nothing");
    }

    [TestMethod]
    public void CommittingRefusesBeforeMovingAnythingWhenAFileAndAFolderCollide()
    {
        using var temp = new TempFolder();
        string output = Path.Combine(temp.Path, "out");
        Write(Path.Combine(output, "pages"), "a file where the folder must go");
        Directory.CreateDirectory(Path.Combine(output, "all-text.txt"));
        using var staging = new OutputStaging(output);
        Write(Path.Combine(staging.Path, "a-fits.txt"), "would fit");
        Write(Path.Combine(staging.Path, "pages", "page-001.jpg"), "picture");
        Write(Path.Combine(staging.Path, "all-text.txt"), "text");

        var refused = Assert.ThrowsExactly<DmcException>(() => staging.Commit());
        Assert.IsTrue(refused.Message.Contains("is a file", StringComparison.Ordinal) || refused.Message.Contains("is a folder", StringComparison.Ordinal), refused.Message);
        Assert.IsFalse(File.Exists(Path.Combine(output, "a-fits.txt")), "not even the file that would have fitted was moved");

        Assert.IsNull(staging.Abandon());
        CollectionAssert.AreEquivalent(new[] { "pages", "all-text.txt" }, Directory.GetFileSystemEntries(output).Select(Path.GetFileName).ToArray());

        // Only the folder in the way: the message says so.
        File.Delete(Path.Combine(output, "pages"));
        using var second = new OutputStaging(output);
        Write(Path.Combine(second.Path, "all-text.txt"), "text");
        StringAssert.Contains(Assert.ThrowsExactly<DmcException>(() => second.Commit()).Message, "is a folder");
    }

    /// <summary>
    /// Where a held-open file stops the deletion (Windows; elsewhere a file can be deleted while open), abandoning cleans up
    /// what it can, one item at a time, and returns the path of the folder it could not remove.
    /// </summary>
    [TestMethod]
    public void AnAbandonThatCannotDeleteEverythingSaysWhatIsLeft()
    {
        using var temp = new TempFolder();
        string output = Path.Combine(temp.Path, "out");
        using var staging = new OutputStaging(output);
        string held = Path.Combine(staging.Path, "held.txt");
        File.WriteAllText(held, "open elsewhere");
        using (var handle = new FileStream(held, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            string? left = staging.Abandon();
            if (OperatingSystem.IsWindows())
                Assert.AreEqual(staging.Path, left, "the folder with the open file is reported");
            else
                Assert.IsNull(left);
        }
        if (OperatingSystem.IsWindows())
        {
            Assert.IsTrue(Directory.Exists(output), "the output folder is not empty, so it stays");
            Directory.Delete(staging.Path, recursive: true);
        }
    }
}
