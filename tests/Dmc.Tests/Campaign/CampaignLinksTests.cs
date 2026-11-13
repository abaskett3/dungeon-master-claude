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

    /// <summary>
    /// A campaign that names no other and that no other names has an empty set: no members, no modules, no notices.
    /// </summary>
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

    /// <summary>
    /// Each of the three lines is read, its values cleaned of comments, backticks, a leading <c>adventures/</c> and
    /// slashes, and matched to folders ignoring case.
    /// </summary>
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

    /// <summary>
    /// A file with no Continuity section is read through its old Predecessor line, as Continues (FR-005).
    /// </summary>
    [TestMethod]
    public void APredecessorLineIsReadAsContinuesWhenThereIsNoContinuitySection()
    {
        Campaign("c", "- **Predecessor**: `b` <!-- a sequel -->\n");
        Campaign("b");
        CollectionAssert.AreEqual(new[] { "b continues 1 open" }, Members(Build("c")));
    }

    /// <summary>When a file has both, the Continuity section is read and a leftover Predecessor line is not.</summary>
    [TestMethod]
    public void TheContinuitySectionWinsOverALeftoverPredecessorLine()
    {
        Write(_root, "adventures/c/player-options.md", "# Options\n\n## Rules\n\n- **Predecessor**: old\n\n" + Continuity("b"));
        Campaign("b");
        Campaign("old");
        CollectionAssert.AreEqual(new[] { "b continues 1 open" }, Members(Build("c")));
    }

    /// <summary>
    /// Links are followed both ways: the set is the same campaigns from wherever it is built, each with its label from
    /// there.
    /// </summary>
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

    /// <summary>
    /// Distances and labels are those of the first path found, breadth first: steps back are <c>continues</c>, steps
    /// forward <c>sequel</c>, one side step <c>side</c>, anything else <c>world</c>.
    /// </summary>
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

    /// <summary>A ring of links ends: each campaign is visited once.</summary>
    [TestMethod]
    public void ARingOfLinksVisitsEachCampaignOnce()
    {
        Campaign("x", Continuity(shares: "y"));
        Campaign("y", Continuity(shares: "x, z"));
        Campaign("z", Continuity(shares: "x"));
        CollectionAssert.AreEqual(new[] { "y world 1 open", "z world 1 open" }, Members(Build("x")));
    }

    /// <summary>A member is closed when its own folder holds the closed mark.</summary>
    [TestMethod]
    public void TheStateComesFromEachCampaignsClosedMark()
    {
        Campaign("c", Continuity("b"));
        Campaign("b");
        Close("b");
        CollectionAssert.AreEqual(new[] { "b continues 1 closed" }, Members(Build("c")));
    }

    /// <summary>
    /// A linked folder that is missing is a member, <c>missing</c>, and one notice that marks it not searched.
    /// </summary>
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

    /// <summary>A missing folder whose digest the start holds gives no notice: the digest stands in for it.</summary>
    [TestMethod]
    public void AMissingFolderWhoseDigestIsHeldGivesNoNotice()
    {
        Campaign("c", Continuity("gone"));
        Digest("c", "gone");
        var set = Build("c");
        Assert.IsEmpty(set.Notices);
        Assert.AreEqual("c", set.Members.Single().DigestHolder);
    }

    /// <summary>
    /// A digest's holder is the campaign nearest the start, the start included, that holds it, and must be nearer the
    /// start than the campaign it digests.
    /// </summary>
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

    /// <summary>
    /// A value with a separator, a drive or a leading dot is a notice that marks it not searched, and is not followed.
    /// </summary>
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

    /// <summary>A link to the campaign itself, in any case, is a notice and adds no member.</summary>
    [TestMethod]
    public void ASelfLinkIsANotice()
    {
        Campaign("c", Continuity(shares: "C"));
        var set = Build("c");
        Assert.IsEmpty(set.Members);
        CollectionAssert.AreEqual(new[] { "c: Shares a world with names the campaign itself" }, Notices(set));
        Assert.IsFalse(set.Notices[0].NotSearched);
    }

    /// <summary>
    /// A second name on Continues or Side campaign of is a notice, and only the first name is followed.
    /// </summary>
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

    /// <summary>
    /// A Continues link to an open campaign is no notice (FR-004): the skills that write a link check it.
    /// </summary>
    [TestMethod]
    public void AContinuesLinkToAnOpenCampaignGivesNoNotice()
    {
        Campaign("c", Continuity("b"));
        Campaign("b");
        Assert.IsEmpty(Build("c").Notices);
    }

    /// <summary>The notices cover the lines of every campaign in the set, and of no campaign outside it.</summary>
    [TestMethod]
    public void NoticesAreForEveryCampaignInTheSetAndNoOther()
    {
        Campaign("c", Continuity("b"));
        Campaign("b", Continuity("gone"));
        Campaign("elsewhere", Continuity("lost"));
        CollectionAssert.AreEqual(new[] { "b: Continues gone is not in adventures/" }, Notices(Build("c")));
    }

    /// <summary>
    /// Each module the set's campaigns name is reached once, at the distance of the nearest campaign that names it,
    /// whether its folder is there or not.
    /// </summary>
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

    /// <summary>
    /// The listing <c>dmc campaign links</c> prints: each member with its label, distance, state and digest mark, each
    /// module whose folder exists, then the notices.
    /// </summary>
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

    /// <summary>A campaign with no links is listed as <c>no links</c>.</summary>
    [TestMethod]
    public void ACampaignAloneIsListedAsNoLinks()
    {
        Campaign("c", Continuity());
        CollectionAssert.AreEqual(new[] { "no links" }, CampaignLinks.ListLines(Build("c")).ToArray());
    }

    /// <summary>More than three notices are cut to three and a <c>(+N more)</c> line.</summary>
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

    /// <summary>The suffix ends the <c>(+N more)</c> line too, so the full save leaves it as it leaves the notices.</summary>
    [TestMethod]
    public void TheSuffixEndsTheMoreLineToo()
    {
        Campaign("c", Continuity("x1", "x2", "x3, x4"));
        var lines = CampaignLinks.NoticeLines(Build("c").Notices, "notice: ", " (mark)");
        Assert.AreEqual("(+1 more) (mark)", lines[^1]);
    }

    /// <summary>A second Continues line is a notice, as a second name on one line is; only the first is followed.</summary>
    [TestMethod]
    public void ASecondLineOfASingleValuedNameIsANoticeAndOnlyTheFirstIsFollowed()
    {
        Campaign("c", Continuity("b") + "- **Continues**: a\n- **Continues**: B\n");
        Campaign("a");
        Campaign("b");
        var set = Build("c");
        CollectionAssert.AreEqual(new[] { "b continues 1 open" }, Members(set));
        CollectionAssert.AreEqual(new[] { "c: Continues has more than one name; only b is followed" }, Notices(set));
    }

    /// <summary>A line inside an HTML comment over several lines is not read, nor is a Continuity heading inside one.</summary>
    [TestMethod]
    public void LinesInsideACommentOverSeveralLinesAreNotRead()
    {
        Campaign("c", "## Continuity\n\n- **Continues**: none\n- **Side campaign of**: none\n<!--\n- **Shares a world with**: old-west\n-->\n- **Shares a world with**: none\n");
        Campaign("d", "<!--\n## Continuity\n\n- **Continues**: c\n-->\n- **Predecessor**: b\n");
        Campaign("b");
        Assert.IsEmpty(Build("c").Members);
        Assert.IsEmpty(Build("c").Notices);
        CollectionAssert.AreEqual(new[] { "b continues 1 open" }, Members(Build("d")));
    }

    /// <summary>An empty <c>digests/&lt;name&gt;/</c> folder holds no digest: the missing campaign is still a notice.</summary>
    [TestMethod]
    public void AnEmptyDigestFolderHoldsNoDigest()
    {
        Campaign("c", Continuity("gone"));
        Directory.CreateDirectory(Path.Combine(_root, "adventures", "c", "digests", "gone"));
        var set = Build("c");
        Assert.IsNull(set.Members.Single().DigestHolder);
        CollectionAssert.AreEqual(new[] { "c: Continues gone is not in adventures/" }, Notices(set));
    }

    /// <summary>A notice remembers whether its line is in a closed campaign's file, the start's included.</summary>
    [TestMethod]
    public void ANoticeKnowsWhetherItsLineIsInAClosedCampaign()
    {
        Campaign("c", Continuity("b", shares: "lost"));
        Campaign("b", Continuity("gone"));
        Close("b");
        var notices = Build("c").Notices;
        CollectionAssert.AreEqual(new[] { "c: Shares a world with lost is not in adventures/", "b: Continues gone is not in adventures/" }, notices.Select(n => n.Text).ToArray());
        CollectionAssert.AreEqual(new[] { false, true }, notices.Select(n => n.InClosed).ToArray());
    }

    // ---------- legacy files (PR 3: spec 011 FR-025, FR-026, research R12) ----------

    /// <summary>The suffix the tests pass, as <c>dmc size --check</c> does.</summary>
    const string Suffix = " (not the full save's to trim)";

    /// <summary>Writes a campaign's <c>legacy.md</c> with a Written line of the given value.</summary>
    /// <param name="campaign">The writer.</param>
    /// <param name="written">The Written line's value, as written.</param>
    void Legacy(string campaign, string written) =>
        Write(_root, $"adventures/{campaign}/legacy.md",
            $"# Legacy of {campaign}\n\n- **Written**: {written}\n\n## What changed in the world\n\n- The ford bridge is burned (s3.2).\n");

    /// <summary>Writes a campaign's <c>game-state.md</c>, with a Legacies folded line when one is given.</summary>
    /// <param name="campaign">The reader.</param>
    /// <param name="folded">The line's value, or null for no line.</param>
    void Folded(string campaign, string? folded) =>
        Write(_root, $"adventures/{campaign}/game-state.md",
            "# Game state\n\n- **Campaign**: Test\n- **Current session**: 12\n- **Last full save**: never\n"
            + (folded is null ? "" : $"- **Legacies folded**: {folded}\n") + "\n## Scene\n\n- **Location**: a road\n");

    /// <summary>The legacy lines of a campaign's set.</summary>
    /// <param name="campaign">The campaign.</param>
    string[] LegacyLines(string campaign) => [.. CampaignLinks.LegacyLines(Build(campaign), Suffix)];

    /// <summary>A campaign alone prints no legacy line, even with a legacy file of its own (SC-007).</summary>
    [TestMethod]
    public void ACampaignAlonePrintsNoLegacyLine()
    {
        Campaign("c", Continuity());
        Legacy("c", "session 3, 2026-11-13");
        Assert.IsEmpty(LegacyLines("c"));
    }

    /// <summary>
    /// A member's legacy written in a later session than the one this campaign records for it is named; the reader's own
    /// legacy is not, and the reader is told to bring its own up to date, keeping every line, since the writer has never
    /// folded the reader's.
    /// </summary>
    [TestMethod]
    public void ALegacyLaterThanTheOneFoldedIsNamed()
    {
        Campaign("c", Continuity());
        Campaign("d", Continuity(side: "c"));
        Legacy("d", "session 5, 2026-11-13");
        Legacy("c", "session 11, 2026-11-12");
        Folded("c", "d s4");
        CollectionAssert.AreEqual(new[]
        {
            "legacy: d legacy.md (written session 5) (not the full save's to trim)",
            "legacy due: open linked campaigns d: update legacy.md (keep all) (not the full save's to trim)",
        }, LegacyLines("c"));
    }

    /// <summary>The same or an earlier session than the one recorded names nothing, whatever the dates say.</summary>
    [TestMethod]
    public void ALegacyAlreadyFoldedIsNotNamed()
    {
        Campaign("c", Continuity());
        Campaign("d", Continuity(side: "c"));
        Legacy("d", "session 5, 2026-11-13");
        Folded("c", "other s9, D s5");
        Assert.IsFalse(LegacyLines("c").Any(l => l.StartsWith("legacy: ", StringComparison.Ordinal)));
        Folded("c", "d s7");
        Assert.IsFalse(LegacyLines("c").Any(l => l.StartsWith("legacy: ", StringComparison.Ordinal)));
    }

    /// <summary>A legacy is named when nothing is recorded for its campaign, or the reader has no Legacies folded line.</summary>
    [TestMethod]
    public void ALegacyNeverFoldedIsNamed()
    {
        Campaign("c", Continuity());
        Campaign("d", Continuity(side: "c"));
        Legacy("d", "session 2, 2026-11-13");
        Folded("c", null);
        Assert.AreEqual("legacy: d legacy.md (written session 2) (not the full save's to trim)", LegacyLines("c")[0]);
        Folded("c", "other s3");
        Assert.AreEqual("legacy: d legacy.md (written session 2) (not the full save's to trim)", LegacyLines("c")[0]);
    }

    /// <summary>An unreadable Written line is named as <c>written unknown</c>: a false alarm costs a read.</summary>
    [TestMethod]
    public void AnUnreadableWrittenLineIsNamedAsUnknown()
    {
        Campaign("c", Continuity());
        Campaign("d", Continuity(side: "c"));
        Legacy("d", "last week");
        Folded("c", "d s9");
        Assert.AreEqual("legacy: d legacy.md (written unknown) (not the full save's to trim)", LegacyLines("c")[0]);
    }

    /// <summary>
    /// Two campaigns played the same day, one after the other: the legacy's session decides, and both carrying the same
    /// date changes nothing.
    /// </summary>
    [TestMethod]
    public void TheSameDateDoesNotHideALegacyFromALaterSession()
    {
        Campaign("c", Continuity());
        Campaign("d", Continuity(side: "c"));
        Legacy("d", "session 6, 2026-11-13");
        Write(_root, "adventures/c/game-state.md",
            "# Game state\n\n- **Current session**: 12\n- **Last full save**: 2026-11-13 18:40, session 12, through scene 3\n- **Legacies folded**: d s5\n");
        Assert.AreEqual("legacy: d legacy.md (written session 6) (not the full save's to trim)", LegacyLines("c")[0]);
    }

    /// <summary>A closed member's last legacy is still named until it is folded; a closed member never makes one due.</summary>
    [TestMethod]
    public void AClosedMembersLegacyIsNamedButMakesNothingDue()
    {
        Campaign("c", Continuity());
        Campaign("d", Continuity(side: "c"));
        Legacy("d", "session 6, 2026-11-13");
        Close("d");
        CollectionAssert.AreEqual(new[] { "legacy: d legacy.md (written session 6) (not the full save's to trim)" }, LegacyLines("c"));
    }

    /// <summary>
    /// <c>legacy due</c> is printed at every check while the set holds an open campaign, whatever this campaign's own
    /// legacy says, so two full saves in one session each bring it up to date.
    /// </summary>
    [TestMethod]
    public void LegacyDueIsPrintedWhateverTheOwnLegacySays()
    {
        Campaign("c", Continuity());
        Campaign("d", Continuity(side: "c"));
        Folded("c", null);
        Assert.AreEqual("legacy due: open linked campaigns d: update legacy.md (keep all) (not the full save's to trim)", LegacyLines("c").Single());
        Legacy("c", "session 12, 2026-11-13");
        Assert.AreEqual("legacy due: open linked campaigns d: update legacy.md (keep all) (not the full save's to trim)", LegacyLines("c").Single());
    }

    /// <summary>
    /// The keep part: one after the lowest session the open readers record for this campaign (two readers at s3 and s5
    /// give s4); <c>keep all</c> when any open reader records nothing or has no line; a closed reader is ignored.
    /// </summary>
    [TestMethod]
    public void TheKeepPartFollowsTheSlowestOpenReader()
    {
        Campaign("c", Continuity(shares: "d, e, f"));
        Campaign("d", Continuity());
        Campaign("e", Continuity());
        Campaign("f", Continuity());
        Folded("d", "c s3");
        Folded("e", "x s1, c s5");
        Folded("f", "c s1");
        Close("f");
        Assert.AreEqual("legacy due: open linked campaigns d, e: update legacy.md (keep from s4) (not the full save's to trim)", LegacyLines("c").Single());
        Folded("e", "x s1");
        Assert.AreEqual("legacy due: open linked campaigns d, e: update legacy.md (keep all) (not the full save's to trim)", LegacyLines("c").Single());
        Folded("e", null);
        Assert.AreEqual("legacy due: open linked campaigns d, e: update legacy.md (keep all) (not the full save's to trim)", LegacyLines("c").Single());
    }

    /// <summary>A set that holds only closed campaigns makes nothing due; a missing member counts as neither.</summary>
    [TestMethod]
    public void ASetOfOnlyClosedOrMissingCampaignsMakesNothingDue()
    {
        Campaign("c", Continuity("b", shares: "lost"));
        Campaign("b", Continuity());
        Close("b");
        Assert.IsEmpty(LegacyLines("c"));
    }

    /// <summary>A closed start, which can no longer be written, is told nothing is due.</summary>
    [TestMethod]
    public void AClosedStartMakesNothingDue()
    {
        Campaign("c", Continuity());
        Campaign("d", Continuity(side: "c"));
        Close("c");
        Assert.IsEmpty(LegacyLines("c"));
    }

    /// <summary>The Written and Legacies folded lines are read as the template and the full save write them, comments left out.</summary>
    [TestMethod]
    public void TheWrittenAndFoldedLinesAreParsed()
    {
        Assert.AreEqual(14, CampaignLinks.WrittenSession("# Legacy of x\n\n- **Written**: session 14, 2026-11-20\n"));
        Assert.AreEqual(3, CampaignLinks.WrittenSession("- **Written**: Session 3\n"));
        Assert.IsNull(CampaignLinks.WrittenSession("- **Written**: soon\n"));
        Assert.IsNull(CampaignLinks.WrittenSession("<!--\n- **Written**: session 4\n-->\n"));
        var folded = CampaignLinks.LegaciesFolded("- **Legacies folded**: side-quest s14, old-west s3, Side-Quest s9, junk <!-- a note -->\n");
        Assert.AreEqual(14, folded["side-quest"]);
        Assert.AreEqual(3, folded["OLD-WEST"]);
        Assert.HasCount(2, folded);
    }
}
