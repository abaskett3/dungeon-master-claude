using Dmc.Commands;
using Dmc.Maps;
using Dmc.Tests.Support;
using static Dmc.Tests.Map.MapFixtures;

namespace Dmc.Tests.Map;

/// <summary><c>--open</c>: the address it opens, and how the opener is started. No test opens a real browser.</summary>
[TestClass]
public sealed class BrowserTests
{
    string _folder = "";

    [TestInitialize]
    public void MakeFolder()
    {
        _folder = Path.Combine(Path.GetTempPath(), "dmc-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
        WriteFile(_folder, "goblin-hall.json", Place());
        WriteFile(_folder, "live.json", Scene());
    }

    [TestCleanup]
    public void RemoveFolder() => Directory.Delete(_folder, recursive: true);

    [TestMethod]
    public void OpenGivesTheOpenerTheAddressOfThePage()
    {
        var opened = new List<string>();
        var result = DmcRunner.InProcess(["map", Path.Combine(_folder, "live.json"), "--open"], commands: [new MapCommand(opened.Add)]);
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        Assert.HasCount(1, opened);
        Assert.AreEqual(new Uri(Path.Combine(_folder, "live.html")).AbsoluteUri, new Uri(opened[0]).AbsoluteUri);
        StringAssert.StartsWith(opened[0], "file:///");
        StringAssert.EndsWith(opened[0], "/live.html");

        // Without --open nothing is opened.
        opened.Clear();
        DmcRunner.InProcess(["map", Path.Combine(_folder, "live.json")], commands: [new MapCommand(opened.Add)]);
        Assert.IsEmpty(opened);
    }

    [TestMethod]
    public void TheOpenerIsStartedWithItsOutputSentToDevNullAndNotThroughPipes()
    {
        var start = Browser.StartInfo("file:///x/live.html");
        if (OperatingSystem.IsWindows())
        {
            Assert.IsTrue(start.UseShellExecute);
            Assert.AreEqual("file:///x/live.html", start.FileName);
            return;
        }
        // Pipes that nobody reads, closed when this program ends, would kill the opener on its next write.
        Assert.IsFalse(start.RedirectStandardOutput);
        Assert.IsFalse(start.RedirectStandardError);
        Assert.IsFalse(start.RedirectStandardInput);
        Assert.AreEqual("/bin/sh", start.FileName);
        string script = start.ArgumentList[1];
        StringAssert.Contains(script, ">/dev/null 2>&1 </dev/null &");
        Assert.AreEqual(OperatingSystem.IsMacOS() ? "open" : "xdg-open", start.ArgumentList[2]);
        Assert.AreEqual("file:///x/live.html", start.ArgumentList[3]);
    }

    [TestMethod]
    public void TheBrowserStillOpens()
    {
        if (OperatingSystem.IsWindows())
            return; // shell execute: nothing to fake
        // A stand-in opener that writes the address it was given, and also writes plenty to stdout and stderr.
        string opener = Path.Combine(_folder, OperatingSystem.IsMacOS() ? "open" : "xdg-open");
        string marker = Path.Combine(_folder, "opened.txt");
        File.WriteAllText(opener, "#!/bin/sh\nsleep 1\necho noise\necho noise >&2\nprintf '%s' \"$1\" > \"" + marker + "\"\n");
        File.SetUnixFileMode(opener, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);

        string? path = Environment.GetEnvironmentVariable("PATH");
        Environment.SetEnvironmentVariable("PATH", _folder + Path.PathSeparator + path);
        try
        {
            Browser.Open("file:///x/live.html");
            // Open returns at once; the opener finishes in the background.
            var wait = System.Diagnostics.Stopwatch.StartNew();
            while (!File.Exists(marker) && wait.Elapsed < TimeSpan.FromSeconds(10))
                Thread.Sleep(50);
            Assert.IsTrue(File.Exists(marker), "the opener was not run");
            Assert.AreEqual("file:///x/live.html", File.ReadAllText(marker));
        }
        finally
        {
            Environment.SetEnvironmentVariable("PATH", path);
        }
    }
}
