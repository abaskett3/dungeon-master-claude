using Dmc.Commands;
using Dmc.Tests.Support;

namespace Dmc.Tests.Distribution;

/// <summary>
/// <c>dmc folder</c> and the lookup behind it, <see cref="DmcFolder.Find()"/> (research R10): <c>CLAUDE_PROJECT_DIR</c> if it is a
/// DMC folder, else the current folder if it is one, else the first recorded folder that still is, else none; and the record
/// of folders in <c>dmc.json</c> beside the program.
/// </summary>
[TestClass]
public sealed class FolderTests
{
    /// <summary>Runs <c>dmc folder</c> in-process over a machine.</summary>
    static (int Exit, string Out, string Err) Folder(TestMachine machine, params string[] args)
    {
        using var output = new StringWriter { NewLine = "\n" };
        using var errors = new StringWriter { NewLine = "\n" };
        int exit = CommandLine.Run(["folder", .. args], new Io(new StringReader(""), output, errors), [new FolderCommand(machine.Machine())]);
        return (exit, output.ToString(), errors.ToString());
    }

    /// <summary>Makes a DMC folder (by its manifest) under the machine's root.</summary>
    static string Dmc(TestMachine machine, string name) => DmcFolders.MakeDmcFolder(Path.Combine(machine.Root, name));

    [TestMethod]
    public void TheProjectFolderComesFirstWhenItIsADmcFolder()
    {
        using var machine = new TestMachine();
        string project = Dmc(machine, "project"), current = Dmc(machine, "current"), recorded = Dmc(machine, "recorded");
        machine.Cwd = current;
        DmcFolder.Record(machine.RunningFolder, recorded);
        machine.Environment["CLAUDE_PROJECT_DIR"] = project;
        var (exit, output, error) = Folder(machine);
        Assert.AreEqual(0, exit, error);
        Assert.AreEqual(project + "\n", output);
        Assert.AreEqual("", error);
    }

    [TestMethod]
    public void AProjectFolderThatIsNotADmcFolderIsPassedOver()
    {
        using var machine = new TestMachine();
        string current = Dmc(machine, "current");
        machine.Cwd = current;
        machine.Environment["CLAUDE_PROJECT_DIR"] = machine.Root; // the repository checkout of a developer, say
        Assert.AreEqual(current + "\n", Folder(machine).Out);
        machine.Environment["CLAUDE_PROJECT_DIR"] = "";
        Assert.AreEqual(current + "\n", Folder(machine).Out);
    }

    [TestMethod]
    public void TheCurrentFolderComesBeforeTheRecordedOnes()
    {
        using var machine = new TestMachine();
        string current = Dmc(machine, "current"), recorded = Dmc(machine, "recorded");
        DmcFolder.Record(machine.RunningFolder, recorded);
        machine.Cwd = current;
        Assert.AreEqual(current + "\n", Folder(machine).Out);
    }

    [TestMethod]
    public void OnlyTheCurrentFolderItselfCountsNotAFolderBelowIt()
    {
        using var machine = new TestMachine();
        string current = Dmc(machine, "current");
        machine.Cwd = Path.Combine(current, "adventures");
        Directory.CreateDirectory(machine.Cwd);
        var (exit, _, error) = Folder(machine);
        Assert.AreEqual(1, exit);
        Assert.AreEqual("dmc folder: no DMC folder: run dmc install\n", error);
    }

    [TestMethod]
    public void TheFirstRecordedFolderThatIsStillADmcFolderIsTheLastResort()
    {
        using var machine = new TestMachine();
        string gone = Dmc(machine, "gone"), notDmc = Path.Combine(machine.Root, "plain"), first = Dmc(machine, "first"), second = Dmc(machine, "second");
        Directory.CreateDirectory(notDmc);
        foreach (string folder in new[] { gone, notDmc, first, second })
            DmcFolder.Record(machine.RunningFolder, folder);
        DmcFolders.Delete(gone);
        Assert.AreEqual(first + "\n", Folder(machine).Out);
        DmcFolders.Delete(first);
        Assert.AreEqual(second + "\n", Folder(machine).Out);
    }

    [TestMethod]
    public void WithNoFolderAtAllItIsOneLineAndExitOne()
    {
        using var machine = new TestMachine();
        var (exit, output, error) = Folder(machine);
        Assert.AreEqual(1, exit);
        Assert.AreEqual("", output);
        Assert.AreEqual("dmc folder: no DMC folder: run dmc install\n", error);
    }

    [TestMethod]
    public void AnArgumentIsAUsageMistake()
    {
        using var machine = new TestMachine();
        var (exit, _, error) = Folder(machine, "x");
        Assert.AreEqual(2, exit);
        Assert.AreEqual("dmc folder: unrecognized arguments: x\n", error);
    }

    [TestMethod]
    public void TheRecordKeepsOrderAddsOnceAndForgivesABadFile()
    {
        using var machine = new TestMachine();
        string program = machine.RunningFolder;
        Assert.IsEmpty(DmcFolder.Recorded(program), "no file");
        File.WriteAllText(Path.Combine(program, "dmc.json"), "not json");
        Assert.IsEmpty(DmcFolder.Recorded(program), "not JSON");
        File.WriteAllText(Path.Combine(program, "dmc.json"), "{ \"folders\": 5 }");
        Assert.IsEmpty(DmcFolder.Recorded(program), "not a list");
        File.WriteAllText(Path.Combine(program, "dmc.json"), "{ \"folders\": [ 1, \"\", \"" + Path.Combine(machine.Root, "x").Replace("\\", "\\\\") + "\" ] }");
        CollectionAssert.AreEqual(new[] { Path.Combine(machine.Root, "x") }, DmcFolder.Recorded(program).ToArray(), "only the strings that name a folder");

        DmcFolder.Record(program, Path.Combine(machine.Root, "y"));
        DmcFolder.Record(program, Path.Combine(machine.Root, "x"));
        DmcFolder.Record(program, Path.Combine(machine.Root, "y"));
        CollectionAssert.AreEqual(new[] { Path.Combine(machine.Root, "x"), Path.Combine(machine.Root, "y") }, DmcFolder.Recorded(program).ToArray());
        Assert.IsFalse(File.Exists(Path.Combine(program, "dmc.json.tmp")));
    }

    [TestMethod]
    public void ThePublishedDmcPrintsTheFolderItWasRunIn()
    {
        DmcFolders.NeedNoRecordedFolders();
        string folder = DmcFolders.MakeDmcFolder(DmcFolders.NewTemp()), plain = DmcFolders.NewTemp();
        try
        {
            var found = DmcRunner.BlackBox(["folder"], cwd: folder);
            Assert.AreEqual(0, found.ExitCode, found.ToString());
            Assert.AreEqual(Path.GetFullPath(folder), Path.GetFullPath(found.Stdout.Trim()));
            Assert.AreEqual("", found.Stderr);

            var none = DmcRunner.BlackBox(["folder"], cwd: plain);
            Assert.AreEqual(1, none.ExitCode, none.ToString());
            Assert.AreEqual("dmc folder: no DMC folder: run dmc install\n", none.Stderr);
        }
        finally
        {
            DmcFolders.Delete(folder);
            DmcFolders.Delete(plain);
        }
    }
}
