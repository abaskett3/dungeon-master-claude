using System.Diagnostics;
using System.Text.RegularExpressions;
using Dmc.Tests.Map;
using Dmc.Tests.Support;
using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;
using static Dmc.Tests.Map.MapFixtures;

namespace Dmc.Tests.Playwright;

/// <summary>
/// The map page in a real browser: <c>live.html</c> opened from disk (<c>file://</c>, no server) in Chromium, as the
/// player has it. Install the browser once with <c>pwsh bin/&lt;configuration&gt;/net10.0/playwright.ps1 install chromium</c>
/// (in <c>tests/Dmc.Tests</c>, after a build). Locally the tests are skipped while it is missing; on CI they fail,
/// like every other optional tool (<see cref="TestEnv.Need"/>).
/// </summary>
[TestClass]
public sealed class MapPageTests : PageTest
{
    string _folder = "";

    [ClassInitialize]
    public static async Task BrowserIsInstalled(TestContext _)
    {
        string? problem = null;
        try
        {
            using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync();
        }
        catch (PlaywrightException e)
        {
            problem = e.Message.Split('\n')[0];
        }
        TestEnv.Need(problem is null,
            "Playwright's Chromium (run: pwsh tests/Dmc.Tests/bin/<configuration>/net10.0/playwright.ps1 install chromium; " + problem + ")");
    }

    public override BrowserNewContextOptions ContextOptions() => new() { ViewportSize = new ViewportSize { Width = 1600, Height = 1200 } };

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

    string Draw(params string[] extra)
    {
        var result = DmcRunner.BlackBox(["map", "live.json", .. extra], cwd: _folder);
        Assert.AreEqual(0, result.ExitCode, result.ToString());
        return new Uri(Path.Combine(_folder, "live.html")).AbsoluteUri;
    }

    [TestMethod]
    public async Task TheSvgAndTheTooltipsRender()
    {
        string url = Draw();
        Assert.StartsWith("file:///", url);
        await Page.GotoAsync(url);

        var svg = Page.Locator("#map svg");
        await Expect(svg).ToBeVisibleAsync();
        await Expect(Page).ToHaveTitleAsync("Goblin hall \u00b7 DMC map");
        await Expect(Page.Locator("#stamp")).ToHaveTextAsync(new Regex(@"^updated \d\d:\d\d:\d\d$"));

        // A tooltip is the <title> of a creature: Thorin and the Goblin boss, each with its cell, and in the legend panel.
        var tooltips = Page.Locator("#map svg title");
        string[] titles = (await tooltips.AllTextContentsAsync()).ToArray();
        CollectionAssert.Contains(titles, "Thorin (B2)");
        CollectionAssert.Contains(titles, "Goblin boss (E2)");
        Assert.HasCount(4, titles); // each creature twice: on the grid and in the panel
        await Expect(Page.Locator("#map svg text", new() { HasText = "Goblin hall" })).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task ThePageUpdatesWhenTheDataFileIsRewritten()
    {
        await Page.GotoAsync(Draw());
        await Expect(Page.Locator("#map svg")).ToBeVisibleAsync();
        await Expect(Page.Locator("#stamp")).ToHaveTextAsync(new Regex("^updated "));

        // Note whether #stamp ever gets the class "fresh": it only keeps it for 0.4 s.
        await Page.EvaluateAsync("""
            () => {
              window.__fresh = false;
              const stamp = document.getElementById('stamp');
              new MutationObserver(() => { if (stamp.classList.contains('fresh')) window.__fresh = true; })
                .observe(stamp, { attributes: true, attributeFilter: ['class'] });
            }
            """);

        const string svg = "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 120 40' width='120' height='40'>"
                           + "<rect width='100%' height='100%' fill='#123456'/><text x='6' y='24' fill='#fff'>Rewritten</text>"
                           + "<g><title>Dragon (A1)</title><circle cx='100' cy='20' r='10' fill='#d0342c'/></g></svg>";
        string data = "dmcMap({\"stamp\": \"2099-01-01T00:00:00\", \"time\": \"12:34:56\", \"title\": \"Rewritten\", \"svg\": "
                      + System.Text.Json.JsonSerializer.Serialize(svg) + "});\n";
        var clock = Stopwatch.StartNew();
        File.WriteAllText(Path.Combine(_folder, "live-data.js"), data);

        // The page polls every 2 seconds, and loading the script takes a moment: 4 s is the longest it may take.
        await Expect(Page.Locator("#stamp")).ToHaveTextAsync("updated 12:34:56", new() { Timeout = 4000 });
        Assert.IsLessThan(4500, clock.ElapsedMilliseconds, "the page took longer than its poll to update");
        await Expect(Page).ToHaveTitleAsync("Rewritten \u00b7 DMC map");
        await Expect(Page.Locator("#map svg text")).ToHaveTextAsync("Rewritten");
        CollectionAssert.Contains((await Page.Locator("#map svg title").AllTextContentsAsync()).ToArray(), "Dragon (A1)");
        Assert.IsTrue(await Page.EvaluateAsync<bool>("() => window.__fresh"), "#stamp never got the class \"fresh\"");
        await Expect(Page.Locator("#stamp")).Not.ToHaveClassAsync(new Regex("fresh")); // and it fades again
    }

    [TestMethod]
    public async Task ThePageIgnoresARewriteWithTheSameStamp()
    {
        await Page.GotoAsync(Draw());
        string before = (await Page.Locator("#stamp").TextContentAsync())!;
        await Page.EvaluateAsync("() => { window.__fresh = false; const s = document.getElementById('stamp'); new MutationObserver(() => { window.__fresh = true; }).observe(s, { attributes: true }); }");
        File.SetLastWriteTimeUtc(Path.Combine(_folder, "live-data.js"), DateTime.UtcNow); // same contents, same stamp
        await Task.Delay(2600); // longer than a poll
        Assert.AreEqual(before, await Page.Locator("#stamp").TextContentAsync());
        Assert.IsFalse(await Page.EvaluateAsync<bool>("() => window.__fresh"));
    }

    [TestMethod]
    public async Task ThePngIsTheSameSizeAsTheSvg()
    {
        string url = Draw("--png");
        await Page.GotoAsync(url);
        var box = await Page.Locator("#map svg").BoundingBoxAsync();
        Assert.IsNotNull(box);

        var (width, height) = MapCases.PngSizeOf(Path.Combine(_folder, "live.png"));
        // The page shows the SVG at its own size when the window is big enough.
        Assert.AreEqual(width, (int)Math.Round(box.Width));
        Assert.AreEqual(height, (int)Math.Round(box.Height));
        Assert.AreEqual(MapCases.SvgSizeOf(_folder), (width, height));
    }
}
