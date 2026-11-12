using Dmc.Campaign;
using Dmc.Tests.Support;
using static Dmc.Tests.Retrieval.RetrievalFixtures;

namespace Dmc.Tests.Campaign;

/// <summary>
/// <see cref="CampaignLinks"/>, called in this process on temporary DMC folders (spec 011 FR-001 to FR-005, research R2
/// and R3): reading the Continuity section and the old Predecessor line, following links both ways, distances, labels,
/// the closed state, missing folders, digest holders, the modules reached and the notices.
/// </summary>
[TestClass]
public sealed class CampaignLinksTests
{
    /// <summary>The test's DMC folder.</summary>
    string _root = "";

    /// <summary>Makes the DMC folder.</summary>
    [TestInitialize]
    public void MakeFolder() => _root = DmcFolders.MakeDmcFolder(DmcFolders.NewTemp());

    /// <summary>Deletes it.</summary>
    [TestCleanup]
    public void RemoveFolder() => DmcFolders.Delete(_root);

    /// <summary>A Continuity section with these three values.</summary>
    /// <param name="continues">The Continues value.</param>
    /// <param name="side">The Side campaign of value.</param>
    /// <param name="shares">The Shares a world with value.</param>
    static string Continuity(string continues = "none", string side = "none", string shares = "none") =>
        $"## Continuity\n\n- **Continues**: {continues}\n- **Side campaign of**: {side}\n- **Shares a world with**: {shares}\n";

    /// <summary>Writes a campaign whose <c>player-options.md</c> holds a Rules section and then <paramref name="body"/>.</summary>
    /// <param name="campaign">The folder name.</param>
    /// <param name="body">The text after the Rules section.</param>
    void Campaign(string campaign, string body = "") =>
        Write(_root, $"adventures/{campaign}/player-options.md",
            "# Player options (this campaign)\n\n## Rules\n\n- **Edition**: 5e 2014\n\n" + body + "\n## Difficulty\n\n- **Lethality**: balanced\n");

    /// <summary>Marks a campaign closed, as <c>dmc campaign close</c> does.</summary>
    /// <param name="campaign">The folder name.</param>
    void Close(string campaign) =>
        ClosedCampaign.Write(Path.Combine(_root, "adventures", campaign), new DateTime(2026, 11, 11, 9, 0, 0, DateTimeKind.Utc), $"{campaign}_closed_20261111090000.zip");

    /// <summary>Makes <c>digests/&lt;digested&gt;/</c> in a campaign, with an index.</summary>
    /// <param name="holder">The campaign that holds the digest.</param>
    /// <param name="digested">The campaign it digests.</param>
    void Digest(string holder, string digested) =>
        Write(_root, $"adventures/{holder}/digests/{digested}/index.md", $"# Digest of {digested}\n\n- **Format**: 1\n");

    /// <summary>The linked set of a campaign.</summary>
    /// <param name="campaign">The campaign.</param>
    LinkedSet Build(string campaign) => CampaignLinks.Build(_root, campaign);

    /// <summary>The members as <c>name label distance state</c> lines, in the set's order.</summary>
    /// <param name="set">The set.</param>
    static string[] Members(LinkedSet set) =>
        [.. set.Members.Select(m => $"{m.Name} {m.Label} {m.Distance} {(m.Present ? m.Closed ? "closed" : "open" : "missing")}")];

    /// <summary>The notices' texts.</summary>
    /// <param name="set">The set.</param>
    static string[] Notices(LinkedSet set) => [.. set.Notices.Select(n => n.Text)];

    [TestMethod]
    public void ACampaignAloneHasNoMembersNoModulesAndNoNotices()
    {
        Campaign("c", Continuity());
        Campaign("other", Continuity());
        var set = Build("c");
        Assert.IsEmpty(set.Members);
        Assert.IsEmpty(set.Modules);
        Assert.IsEmpty(set.Notices);
    }

    [TestMethod]
    public void TheThreeLinesAreReadWithCommentsBackticksFoldersAndSlashesRemoved()
    {
        Campaign("c", Continuity("`adventures/b/` <!-- the campaign before -->", "D", "e, `f` <!-- two -->"));
        Campaign("b");
        Campaign("d");
        Campaign("e");
        Campaign("f");
        CollectionAssert.AreEqual(new[] { "b continues 1 open", "d side 1 open", "e world 1 open", "f world 1 open" }, Members(Build("c")));
        Assert.IsEmpty(Build("c").Notices);
    }

    [TestMethod]
    public void APredecessorLineIsReadAsContinuesWhenThereIsNoContinuitySection()
    {
        Campaign("c", "- **Predecessor**: `b` <!-- a sequel -->\n");
        Campaign("b");
        CollectionAssert.AreEqual(new[] { "b continues 1 open" }, Members(Build("c")));
    }

    [TestMethod]
    public void TheContinuitySectionWinsOverALeftoverPredecessorLine()
    {
        Write(_root, "adventures/c/player-options.md", "# Options\n\n## Rules\n\n- **Predecessor**: old\n\n" + Continuity("b"));
        Campaign("b");
        Campaign("old");
        CollectionAssert.AreEqual(new[] { "b continues 1 open" }, Members(Build("c")));
    }

    [TestMethod]
    public void LinksAreFollowedBothWays()
    {
        Campaign("c", Continuity("b"));
        Campaign("b");
        Campaign("d", Continuity(side: "c"));
        Campaign("e", Continuity(shares: "c"));
        CollectionAssert.AreEqual(new[] { "b continues 1 open", "d side 1 open", "e world 1 open" }, Members(Build("c")));
        CollectionAssert.AreEqual(new[] { "c sequel 1 open", "d world 2 open", "e world 2 open" }, Members(Build("b")));
        CollectionAssert.AreEqual(new[] { "c side 1 open", "b world 2 open", "e world 2 open" }, Members(Build("d")));
    }

    [TestMethod]
    public void DistancesAndLabelsFollowTheFirstPathFound()
    {
        Campaign("a");
        Campaign("b", Continuity("a"));
        Campaign("c", Continuity("b"));
        Campaign("d", Continuity(side: "c", shares: "x"));
        Campaign("x");
        CollectionAssert.AreEqual(new[] { "b continues 1 open", "d side 1 open", "a continues 2 open", "x world 2 open" }, Members(Build("c")));
        CollectionAssert.AreEqual(new[] { "b sequel 1 open", "c sequel 2 open", "d world 3 open", "x world 4 open" }, Members(Build("a")));
    }

    [TestMethod]
    public void ARingOfLinksVisitsEachCampaignOnce()
    {
        Campaign("x", Continuity(shares: "y"));
        Campaign("y", Continuity(shares: "x, z"));
        Campaign("z", Continuity(shares: "x"));
        CollectionAssert.AreEqual(new[] { "y world 1 open", "z world 1 open" }, Members(Build("x")));
    }

    [TestMethod]
    public void TheStateComesFromEachCampaignsClosedMark()
    {
        Campaign("c", Continuity("b"));
        Campaign("b");
        Close("b");
        CollectionAssert.AreEqual(new[] { "b continues 1 closed" }, Members(Build("c")));
    }

    [TestMethod]
    public void AMissingFolderIsAMemberAndOneNotice()
    {
        Campaign("c", Continuity("gone"));
        var set = Build("c");
        CollectionAssert.AreEqual(new[] { "gone continues 1 missing" }, Members(set));
        Assert.HasCount(1, set.Notices);
        Assert.AreEqual("c: Continues gone is not in adventures/", set.Notices[0].Text);
        Assert.IsTrue(set.Notices[0].NotSearched);
    }

    [TestMethod]
    public void AMissingFolderWhoseDigestIsHeldGivesNoNotice()
    {
        Campaign("c", Continuity("gone"));
        Digest("c", "gone");
        var set = Build("c");
        Assert.IsEmpty(set.Notices);
        Assert.AreEqual("c", set.Members.Single().DigestHolder);
    }

    [TestMethod]
    public void TheDigestHolderIsTheNearestCampaignBeforeIt()
    {
        Campaign("a");
        Campaign("b", Continuity("a"));
        Campaign("c", Continuity("b"));
        Close("a");
        Close("b");
        Digest("b", "a");
        Digest("c", "a");
        Digest("c", "b");
        var fromC = Build("c");
        Assert.AreEqual("c", fromC.Members.Single(m => m.Name == "a").DigestHolder);
        Assert.AreEqual("c", fromC.Members.Single(m => m.Name == "b").DigestHolder);

        // Recalling in b, its own files are not a last resort because its sequel holds its digest: the holder must be nearer the start.
        var fromB = Build("b");
        Assert.AreEqual("b", fromB.Members.Single(m => m.Name == "a").DigestHolder);
        Assert.IsNull(fromB.Members.Single(m => m.Name == "c").DigestHolder);
    }

    [TestMethod]
    public void AValueThatIsNotAFolderNameIsANoticeAndIsNotFollowed()
    {
        Campaign("c", Continuity("../x", shares: "a/b, .hidden, D:\\camp"));
        Campaign("x");
        var set = Build("c");
        Assert.IsEmpty(set.Members);
        CollectionAssert.AreEqual(new[]
        {
            "c: Continues ../x is not a campaign folder name",
            "c: Shares a world with a/b is not a campaign folder name",
            "c: Shares a world with .hidden is not a campaign folder name",
            "c: Shares a world with D:/camp is not a campaign folder name",
        }, Notices(set));
        Assert.IsTrue(set.Notices.All(n => n.NotSearched));
    }

    [TestMethod]
    public void ASelfLinkIsANotice()
    {
        Campaign("c", Continuity(shares: "C"));
        var set = Build("c");
        Assert.IsEmpty(set.Members);
        CollectionAssert.AreEqual(new[] { "c: Shares a world with names the campaign itself" }, Notices(set));
        Assert.IsFalse(set.Notices[0].NotSearched);
    }

    [TestMethod]
    public void ASecondValueOnASingleValuedLineIsANoticeAndOnlyTheFirstIsFollowed()
    {
        Campaign("c", Continuity("b, a", side: "d, e"));
        Campaign("a");
        Campaign("b");
        Campaign("d");
        Campaign("e");
        var set = Build("c");
        CollectionAssert.AreEqual(new[] { "b continues 1 open", "d side 1 open" }, Members(set));
        CollectionAssert.AreEqual(new[]
        {
            "c: Continues has more than one name; only b is followed",
            "c: Side campaign of has more than one name; only d is followed",
        }, Notices(set));
    }

    [TestMethod]
    public void AContinuesLinkToAnOpenCampaignGivesNoNotice()
    {
        Campaign("c", Continuity("b"));
        Campaign("b");
        Assert.IsEmpty(Build("c").Notices);
    }

    [TestMethod]
    public void NoticesAreForEveryCampaignInTheSetAndNoOther()
    {
        Campaign("c", Continuity("b"));
        Campaign("b", Continuity("gone"));
        Campaign("elsewhere", Continuity("lost"));
        CollectionAssert.AreEqual(new[] { "b: Continues gone is not in adventures/" }, Notices(Build("c")));
    }

    [TestMethod]
    public void EachModuleIsReachedOnceAtTheSmallestDistance()
    {
        Campaign("c", Continuity("b"));
        Campaign("b");
        Campaign("d", Continuity(side: "c"));
        Write(_root, "adventures/c/module-progress.md", "# Module progress\n\n- **Module folder**: `modules/moon/`\n");
        Write(_root, "adventures/b/module-progress.md", "# Module progress\n\n- **Module folder**: `modules/keep/`\n");
        Write(_root, "adventures/d/module-progress.md", "# Module progress\n\n- **Module folder**: modules/keep/ <!-- also -->\n");
        Write(_root, "adventures/d/story/canon.md", "# Canon\n");
        Write(_root, "modules/moon/index.md", "# Moon\n");
        var set = Build("c");
        CollectionAssert.AreEqual(new[] { "moon 0 c True", "keep 1 b False" },
            set.Modules.Select(m => $"{m.Name} {m.Distance} {m.Via} {m.Present}").ToArray());
    }

    [TestMethod]
    public void TheListingNamesEachMemberEachModuleAndTheNotices()
    {
        Campaign("c", Continuity("b", shares: "gone"));
        Campaign("b");
        Campaign("d", Continuity(side: "c"));
        Close("b");
        Digest("c", "b");
        Write(_root, "adventures/c/module-progress.md", "# Module progress\n\n- **Module folder**: `modules/moon/`\n");
        Write(_root, "modules/moon/index.md", "# Moon\n");
        CollectionAssert.AreEqual(new[]
        {
            "b continues 1 closed digest",
            "d side 1 open",
            "gone world 1 missing",
            "modules/moon module 0",
            "notice: c: Shares a world with gone is not in adventures/",
        }, CampaignLinks.ListLines(Build("c")).ToArray());
    }

    [TestMethod]
    public void ACampaignAloneIsListedAsNoLinks()
    {
        Campaign("c", Continuity());
        CollectionAssert.AreEqual(new[] { "no links" }, CampaignLinks.ListLines(Build("c")).ToArray());
    }

    [TestMethod]
    public void MoreThanThreeNoticesAreCut()
    {
        Campaign("c", Continuity("x1", "x2", "x3, x4, x5"));
        var lines = CampaignLinks.NoticeLines(Build("c").Notices, "notice: ", "");
        CollectionAssert.AreEqual(new[]
        {
            "notice: c: Continues x1 is not in adventures/",
            "notice: c: Side campaign of x2 is not in adventures/",
            "notice: c: Shares a world with x3 is not in adventures/",
            "(+2 more)",
        }, lines.ToArray());
    }
}
