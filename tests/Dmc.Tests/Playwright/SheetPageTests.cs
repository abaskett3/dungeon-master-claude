using Dmc.Tests.Sheets;
using Dmc.Tests.Support;
using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;

namespace Dmc.Tests.Playwright;

/// <summary>
/// The page <c>dmc sheet</c> writes, in a real browser (spec 031 FR-005): opened from disk in Chromium, printed with a
/// sheet per page, and readable at a phone's width with no sideways scrolling. Skipped locally while Chromium is
/// missing, failed on CI (<see cref="TestEnv.Need"/>), as the map page tests are.
/// </summary>
[TestClass]
public sealed class SheetPageTests : PageTest
{
    /// <summary>The test's temporary DMC folder, with the fixture campaign and its party page.</summary>
    string _root = "";

    /// <summary>Launches Chromium once, to find out whether it is installed.</summary>
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

    /// <summary>Writes the party page with the published <c>dmc</c>.</summary>
    [TestInitialize]
    public void WritePage()
    {
        _root = SheetFixtures.NewDmcFolder();
        var result = DmcRunner.BlackBox(["sheet", "--party", "--folder", _root]);
        Assert.AreEqual(0, result.ExitCode, result.ToString());
    }

    [TestCleanup]
    public void RemoveFolder() => DmcFolders.Delete(_root);

    /// <summary>The page's <c>file:///</c> address.</summary>
    string Address => new Uri(SheetFixtures.Page(_root, "party.html")).AbsoluteUri;

    [TestMethod]
    public async Task EachSheetStartsANewPrintedPage()
    {
        await Page.GotoAsync(Address);
        await Page.EmulateMediaAsync(new PageEmulateMediaOptions { Media = Media.Print });
        await Expect(Page).ToHaveTitleAsync("Ana Reed, Bram, Cora · DMC sheets");
        string[] breaks = [.. await Page.EvalOnSelectorAllAsync<string[]>("section.sheet", "els => els.map(e => getComputedStyle(e).breakAfter)")];
        CollectionAssert.AreEqual(new[] { "page", "page", "auto" }, breaks);
        string[] boxBreaks = [.. (await Page.EvalOnSelectorAllAsync<string[]>(".box", "els => els.map(e => getComputedStyle(e).breakInside)")).Distinct()];
        CollectionAssert.AreEqual(new[] { "avoid" }, boxBreaks, "a box is never cut across two pages");
    }

    [TestMethod]
    public async Task APhoneShowsThePageWithNoSidewaysScrolling()
    {
        await Page.SetViewportSizeAsync(375, 800);
        await Page.GotoAsync(Address);
        int overflow = await Page.EvaluateAsync<int>("() => document.documentElement.scrollWidth - document.documentElement.clientWidth");
        Assert.IsLessThanOrEqualTo(0, overflow, "the page is wider than a phone");
        await Expect(Page.Locator("section.sheet h1").First).ToHaveTextAsync("Ana Reed");
        await Expect(Page.Locator("table.skills tbody tr").First).ToBeVisibleAsync();
        Assert.AreEqual(18, await Page.Locator("section.sheet").First.Locator("table.skills tbody tr").CountAsync(), "all eighteen skills");
    }
}
