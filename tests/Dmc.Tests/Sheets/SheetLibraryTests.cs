using Dmc.Sheets;
using Dmc.Tests.Support;

namespace Dmc.Tests.Sheets;

/// <summary>
/// The code under <c>dmc sheet</c>, in this process: the layouts against the kit's templates (spec 031 FR-011), the
/// small Markdown renderer, and the readers of written numbers.
/// </summary>
[TestClass]
public sealed class SheetLibraryTests
{
    /// <summary>A template's lines, comments removed.</summary>
    /// <param name="relative">The path under <c>kit/templates/</c>.</param>
    static List<SheetSection> Template(string relative) =>
        SheetText.Sections(SheetText.LinesWithoutComments(File.ReadAllText(Path.Combine(TestEnv.Root, "kit", "templates", relative))));

    /// <summary>Every label of a template's sections, as (section, label).</summary>
    /// <param name="sections">The template's sections.</param>
    static IEnumerable<(string Section, string Label)> LabelsOf(IEnumerable<SheetSection> sections) =>
        sections.SelectMany(s => SheetText.Fields(s.Lines).Select(f => (s.Heading, f.Label)));

    /// <summary>The (section, label) pairs a set of layouts prints.</summary>
    /// <param name="layouts">The layouts.</param>
    /// <remarks>The skill grid prints the two skill fields of <c>## Abilities</c>.</remarks>
    static HashSet<(string, string)> Printed(params IReadOnlyList<Panel>[] layouts)
    {
        var sources = layouts.SelectMany(l => l).SelectMany(p => p.Boxes).Select(b => b.Source).ToList();
        var printed = sources.OfType<FieldSource>().Select(f => (f.Section.ToLowerInvariant(), f.Label.ToLowerInvariant())).ToHashSet();
        if (sources.OfType<SkillGridSource>().Any())
            printed.UnionWith([("abilities", "skills"), ("abilities", "other skills")]);
        return printed;
    }

    /// <summary>
    /// FR-011: every label of <c>kit/templates/pc-sheet.md</c> and <c>npc-sheet.md</c> is printed by a layout that uses
    /// the template, or named as not exported with a reason; and every structured section's label list in the code is the
    /// template's. A template change fails here until <c>Sheets/SheetLayouts.cs</c> follows.
    /// </summary>
    [TestMethod]
    public void EveryLabelOfTheSheetTemplatesIsPrintedOrNamedAsNotExported()
    {
        var problems = new List<string>();
        foreach (var (file, layouts, notExported, labels, sections) in new[]
                 {
                     ("pc-sheet.md", new[] { SheetLayouts.Pc, SheetLayouts.CompanionPc }, SheetLayouts.PcNotExported, SheetLayouts.PcLabels, SheetLayouts.PcSections),
                     ("npc-sheet.md", new[] { SheetLayouts.CompanionNpc }, SheetLayouts.NpcNotExported, SheetLayouts.NpcLabels, SheetLayouts.NpcSections),
                 })
        {
            var template = Template(file);
            var printed = Printed(layouts);
            var named = notExported.Select(n => (n.Section.ToLowerInvariant(), n.Label.ToLowerInvariant())).ToHashSet();
            foreach (var (section, label) in LabelsOf(template))
            {
                var key = (section.ToLowerInvariant(), label.ToLowerInvariant());
                if (!printed.Contains(key) && !named.Contains(key))
                    problems.Add($"{file}: {section}/{label} is neither printed nor named as not exported");
                if (!labels.TryGetValue(section, out string[]? known) || !known.Contains(label, StringComparer.OrdinalIgnoreCase))
                    problems.Add($"{file}: {section}/{label} is not in the code's label list");
            }
            foreach (var (section, known) in labels)
                foreach (string label in known.Where(l => !LabelsOf(template).Any(t => t.Section.Equals(section, StringComparison.OrdinalIgnoreCase) && t.Label.Equals(l, StringComparison.OrdinalIgnoreCase))))
                    if (!(section == SheetText.Top && label is "Companion")) // the marker the PC template names in a comment
                        problems.Add($"{file}: the code lists {section}/{label}, which the template does not have");
            CollectionAssert.AreEqual(sections.ToArray(), template.Select(s => s.Heading).ToArray(), $"{file}: the sections and their order");
            var inTemplate = LabelsOf(template).Select(t => (t.Section.ToLowerInvariant(), t.Label.ToLowerInvariant())).ToHashSet();
            foreach (var key in printed.Concat(named).Where(k => !inTemplate.Contains(k) && k != ("", "companion")))
                problems.Add($"{file}: a layout or the not-exported list names {key.Item1}/{key.Item2}, which the template does not have");
        }
        Assert.IsEmpty(problems, string.Join("\n", problems));
    }

    /// <summary>Every field of the Party status block of <c>campaign/game-state.md</c> is a box of the Now panel, in order.</summary>
    [TestMethod]
    public void EveryPartyStatusFieldOfTheTemplateIsPrinted()
    {
        var party = SheetText.Find(Template("campaign/game-state.md"), "Party status")!;
        var block = party.Subsections.First(s => s.Heading == "(character name)");
        CollectionAssert.AreEqual(SheetText.Fields(block.Lines).Select(f => f.Label).ToArray(), SheetLayouts.PartyLabels.ToArray());
    }

    /// <summary>The sections never printed are named, and no layout reads them (FR-014).</summary>
    [TestMethod]
    public void NoLayoutReadsASectionThatHoldsSpoilers()
    {
        var all = new[] { SheetLayouts.Pc, SheetLayouts.CompanionNpc, SheetLayouts.CompanionPc }.SelectMany(l => l).SelectMany(p => p.Boxes).ToList();
        string[] read = [.. all.Select(b => b.Source switch { FieldSource f => f.Section, FreeSource s => s.Section, _ => "" })];
        foreach (string section in new[] { "What they know", "Secrets" })
            CollectionAssert.DoesNotContain(read, section);
        var companions = SheetLayouts.CompanionNpc.Concat(SheetLayouts.CompanionPc).SelectMany(p => p.Boxes).ToList();
        Assert.IsFalse(companions.Any(b => b.Source is HistorySource or FreeSource { Section: "Relationships" or "History" }),
            "a companion's history and relationships are not printed");
    }

    [TestMethod]
    public void InlineMarkdownEscapesFirstThenMarksUp()
    {
        Assert.AreEqual("<strong>bold</strong> &lt;script&gt; <em>it</em> <code>a&lt;b</code> link", SheetMarkdown.Inline("**bold** <script> *it* `a<b` [link](javascript:x)"));
        Assert.AreEqual("a &amp; b &quot;c&quot;", SheetMarkdown.Inline("a & b \"c\""));
        Assert.AreEqual("snake_case_name", SheetMarkdown.Inline("snake_case_name"), "an underscore inside a word is not italics");
    }

    [TestMethod]
    public void BlocksMakeNestedListsTablesAndParagraphs()
    {
        string html = SheetMarkdown.Blocks(["- one", "  - one a", "- two", "", "| A | B |", "|---|---|", "| 1 | 2 |", "", "Some prose", "goes on."]);
        Assert.AreEqual("<ul><li>one<ul><li>one a</li></ul></li><li>two</li></ul>"
                        + "<table><thead><tr><th>A</th><th>B</th></tr></thead><tbody><tr><td>1</td><td>2</td></tr></tbody></table>"
                        + "<p>Some prose goes on.</p>", html);
    }

    [TestMethod]
    public void CommentsAreRemovedBeforeAnythingIsRead()
    {
        var lines = SheetText.LinesWithoutComments("# T\n<!-- one\ntwo -->\n- **A**: x <!-- hidden -->\nkept <!-- a\nb --> after\n");
        CollectionAssert.AreEqual(new[] { "# T", "- **A**: x", "kept", " after", "" }, lines);
    }

    [TestMethod]
    public void FieldsReadTheirLabelHintValueAndSubItems()
    {
        var others = new List<string>();
        var fields = SheetText.Fields(["- **Skills** (proficient, with total bonus): Athletics +5", "- **Attacks**:", "  - hook: +5", "- **Player character**", "stray text"], others);
        Assert.HasCount(3, fields);
        Assert.AreEqual("Skills", fields[0].Label);
        Assert.AreEqual("Athletics +5", fields[0].Value);
        Assert.IsFalse(fields[1].IsBlank, "a sub-item fills a field");
        Assert.IsFalse(fields[2].HasColon);
        CollectionAssert.AreEqual(new[] { "stray text" }, others);
    }

    [TestMethod]
    public void SkillListsReadNamesOfSeveralWordsBonusesAndNotes()
    {
        var unknown = new List<string>();
        var entries = SheetNumbers.SkillList("Sleight of Hand +5, Animal Handling −1, Stealth +4 (disadvantage in ring mail, loud), Thieves' tools +5, none", unknown);
        CollectionAssert.AreEqual(new[] { "Sleight of Hand", "Animal Handling", "Stealth" }, entries.Select(e => e.Skill).ToArray());
        CollectionAssert.AreEqual(new[] { "+5", "−1", "+4" }, entries.Select(e => e.Written).ToArray());
        Assert.AreEqual("disadvantage in ring mail, loud", entries[2].Note);
        CollectionAssert.AreEqual(new[] { "Thieves' tools +5" }, unknown);
        Assert.AreEqual(-1, SheetNumbers.ParseSigned("−1"));
        Assert.AreEqual(-1, SheetNumbers.Modifier(8));
        Assert.AreEqual(-1, SheetNumbers.Modifier(9));
        CollectionAssert.AreEquivalent(new[] { "STR", "CON" }, SheetNumbers.SaveAbilities("Strength, CON").ToArray());
    }
}
