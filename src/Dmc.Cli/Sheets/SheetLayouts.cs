namespace Dmc.Sheets;

/// <summary>Which template a sheet follows, and so which fixed layout prints it (spec 031 FR-008).</summary>
internal enum SheetKind
{
    /// <summary>A player character: <c>character-sheets/pc-*.md</c>, from <c>templates/pc-sheet.md</c>.</summary>
    Pc,

    /// <summary>A companion whose sheet follows <c>templates/npc-sheet.md</c>.</summary>
    CompanionNpc,

    /// <summary>A companion whose sheet follows <c>templates/pc-sheet.md</c> (marked <c>- **Companion**</c>).</summary>
    CompanionPc,
}

/// <summary>Where a box of a layout takes what it prints from. Each box reads exactly one place (spec 031 FR-009).</summary>
internal abstract record BoxSource;

/// <summary>One labelled field of a section of the sheet.</summary>
/// <param name="Section">The section's heading without <c>## </c>; <see cref="SheetText.Top"/> for the opening fields.</param>
/// <param name="Label">The field's bold label, as the template writes it.</param>
internal sealed record FieldSource(string Section, string Label) : BoxSource;

/// <summary>A free-form section of the sheet, printed as it is written.</summary>
/// <param name="Section">The section's heading without <c>## </c>.</param>
internal sealed record FreeSource(string Section) : BoxSource;

/// <summary>The attack table: the <c>### Attacks</c> subsection of <c>## Combat</c>.</summary>
internal sealed record AttackTableSource : BoxSource;

/// <summary>The ability table of <c>## Abilities</c>: scores, modifiers and saves.</summary>
internal sealed record AbilityTableSource : BoxSource;

/// <summary>All eighteen skills, from the <c>Skills</c> and <c>Other skills</c> fields of <c>## Abilities</c>.</summary>
internal sealed record SkillGridSource : BoxSource;

/// <summary>One labelled field of the character's own block under <c>## Party status</c> in <c>game-state.md</c>.</summary>
/// <param name="Label">The field's label, as the template writes it.</param>
internal sealed record PartySource(string Label) : BoxSource;

/// <summary>The PC's history: the sheet's history section and <c>&lt;sheet name&gt;-history.md</c>, with <c>--history</c> only.</summary>
internal sealed record HistorySource : BoxSource;

/// <summary>One box of a layout.</summary>
/// <param name="Title">What the printed box is called.</param>
/// <param name="Source">The one place it is filled from.</param>
/// <param name="CardHeld">
/// True for a field whose number a companion's play card holds instead (migration 006 step 3): on a companion's sheet it
/// may be missing, and the box then says it is on the play card. Always required on a PC's sheet.
/// </param>
/// <param name="Wide">True for a box that takes the panel's whole width (lists, tables, free-form text).</param>
internal sealed record Box(string Title, BoxSource Source, bool CardHeld = false, bool Wide = false);

/// <summary>A titled group of boxes, printed together.</summary>
/// <param name="Title">The panel's heading.</param>
/// <param name="Boxes">Its boxes, in order.</param>
internal sealed record Panel(string Title, IReadOnlyList<Box> Boxes);

/// <summary>A label of a template that no layout prints, and why (spec 031 FR-011, FR-014).</summary>
/// <param name="Section">The section, without <c>## </c>.</param>
/// <param name="Label">The label.</param>
/// <param name="Reason">Why it is not exported: a spoiler, DMC's own notes, or a marker.</param>
internal sealed record NotExported(string Section, string Label, string Reason);

/// <summary>
/// The fixed layouts of <c>dmc sheet</c> and the map from every template label to a box or to a reason it is not
/// printed. This is the allow-list (spec 031 FR-014): a layout prints only the places named here, so a section or a
/// label not named (Secrets, What they know, Role in the story) never reaches the page. <c>SheetLayoutTests</c> checks
/// that every label of <c>kit/templates/pc-sheet.md</c>, <c>npc-sheet.md</c> and the Party status block of
/// <c>campaign/game-state.md</c> is mapped here, so a template change fails the build until this file follows.
/// </summary>
internal static class SheetLayouts
{
    /// <summary>The heading of a companion's play card, without <c>## </c>.</summary>
    public const string PlayCard = "Play card";

    /// <summary>The Party status fields of a character's block, in the template's order: the current-state panel of every layout.</summary>
    public static IReadOnlyList<string> PartyLabels { get; } =
        ["HP", "Hit dice left", "Spell slots left", "Class resources", "Conditions", "Concentrating on", "Exhaustion",
         "Death saves", "Inspiration", "Gold", "Consumables"];

    /// <summary>The eighteen skills of the 5e rules (2014 and 2024 name the same ones), each with its ability.</summary>
    public static IReadOnlyList<(string Skill, string Ability)> Skills { get; } =
    [
        ("Acrobatics", "DEX"), ("Animal Handling", "WIS"), ("Arcana", "INT"), ("Athletics", "STR"), ("Deception", "CHA"),
        ("History", "INT"), ("Insight", "WIS"), ("Intimidation", "CHA"), ("Investigation", "INT"), ("Medicine", "WIS"),
        ("Nature", "INT"), ("Perception", "WIS"), ("Performance", "CHA"), ("Persuasion", "CHA"), ("Religion", "INT"),
        ("Sleight of Hand", "DEX"), ("Stealth", "DEX"), ("Survival", "WIS"),
    ];

    /// <summary>The six abilities, in the table's order.</summary>
    public static IReadOnlyList<string> Abilities { get; } = ["STR", "DEX", "CON", "INT", "WIS", "CHA"];

    /// <summary>The sections of the PC template, in its order. A PC's or a PC-template companion's sheet may have only these (and an older history heading).</summary>
    public static IReadOnlyList<string> PcSections { get; } =
        [SheetText.Top, PlayCard, "Abilities", "Combat", "Features and traits", "Spellcasting", "Inventory", "Story",
         "Relationships", "Wardrobe and keepsakes", "History"];

    /// <summary>The sections of the NPC template, in its order.</summary>
    public static IReadOnlyList<string> NpcSections { get; } =
        [SheetText.Top, PlayCard, "Playing them", "What they know", "Secrets", "Relationships", "Stat block",
         "Wardrobe and keepsakes", "History"];

    /// <summary>The older names of a sheet's history section, which the resume also accepts (<c>Size/ReadSet.cs</c>).</summary>
    public static IReadOnlyList<string> OlderHistoryHeadings { get; } = ["Notes", "History with the PC"];

    /// <summary>
    /// The free-form sections: their lines are not checked for labels, because they hold prose or one item a line (the
    /// templates' comments say so). The rest are structured: every line is a labelled field of the template.
    /// </summary>
    public static IReadOnlySet<string> FreeSections { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { "Features and traits", "Relationships", "Wardrobe and keepsakes", "History", "Notes", "History with the PC", "What they know", "Secrets" };

    /// <summary>The template's labels of each structured section of the PC template (all of them, exported or not).</summary>
    public static IReadOnlyDictionary<string, string[]> PcLabels { get; } = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        [SheetText.Top] = ["Player character", "Companion", "Species and background", "Class and level", "Alignment", "XP or milestone progress"],
        [PlayCard] = CardLabels,
        ["Abilities"] = ["Proficiency bonus", "Saving throw proficiencies", "Skills", "Other skills", "Expertise", "Passive Perception", "Languages", "Tool and other proficiencies"],
        ["Combat"] = ["Max HP", "Hit dice", "Armor Class", "Initiative", "Speed", "Senses", "Resistances, immunities, vulnerabilities"],
        ["Spellcasting"] = ["Casting ability, save DC, attack bonus", "Slots per level", "Cantrips", "Prepared or known spells", "Spellbook or other spells available"],
        ["Inventory"] = ["Equipped", "Carried", "Magic items", "Stored elsewhere"],
        ["Story"] = ["Appearance", "Personality", "Names they use", "Ideals, bonds, flaws", "Backstory", "Current objectives", "Long-term goals"],
    };

    /// <summary>The template's labels of each structured section of the NPC template.</summary>
    public static IReadOnlyDictionary<string, string[]> NpcLabels { get; } = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        [SheetText.Top] = ["Role in the story", "Species, occupation, age", "Usually found", "First appeared", "Status"],
        [PlayCard] = CardLabels,
        ["Playing them"] = ["Appearance", "Voice", "Names they use", "Personality", "What they want", "What they fear", "Attitude to the PC, and why"],
        ["Stat block"] = ["Based on", "AC", "Max HP", "Speed", "STR / DEX / CON / INT / WIS / CHA", "Saves and skills", "Senses and languages", "Traits", "Actions", "Spells or special abilities", "Gear"],
    };

    /// <summary>The play card's labels, the same on both templates.</summary>
    static string[] CardLabels =>
        ["Who", "Looks", "Voice", "Names they use", "Toward the PC, and what they want now", "AC, max HP, speed, saves, senses",
         "Attacks", "Recharge and limited abilities", "Standing traits that change play", "Items used in play"];

    /// <summary>The labels of the PC template that no layout prints, and why.</summary>
    public static IReadOnlyList<NotExported> PcNotExported { get; } =
    [
        new(SheetText.Top, "Player character", "a marker of the sheet's kind"),
        new(SheetText.Top, "Companion", "a marker of the sheet's kind"),
        new(PlayCard, "Who", "DMC's own summary, which may hold what the PC has not learned"),
        new(PlayCard, "Names they use", "DMC's notes for voicing them"),
        new(PlayCard, "Toward the PC, and what they want now", "their motives, which may be a spoiler"),
        new("Story", "Names they use", "DMC's notes for voicing them"),
    ];

    /// <summary>The labels of the PC template that only a companion's layout leaves out, and why: what a companion wants and where they come from may be a spoiler.</summary>
    public static IReadOnlyList<NotExported> PcCompanionNotExported { get; } =
    [
        new("Story", "Ideals, bonds, flaws", "a companion's inner life, which may be a spoiler"),
        new("Story", "Backstory", "a companion's past, which may be a spoiler"),
        new("Story", "Current objectives", "a companion's aims, which may be a spoiler"),
        new("Story", "Long-term goals", "a companion's aims, which may be a spoiler"),
    ];

    /// <summary>The labels of the NPC template that no layout prints, and why.</summary>
    public static IReadOnlyList<NotExported> NpcNotExported { get; } =
    [
        new(SheetText.Top, "Role in the story", "a spoiler: what they are for in the story"),
        new(SheetText.Top, "Usually found", "DMC's notes"),
        new(SheetText.Top, "First appeared", "DMC's notes"),
        new(PlayCard, "Who", "DMC's own summary, which may hold what the PC has not learned"),
        new(PlayCard, "Names they use", "DMC's notes for voicing them"),
        new(PlayCard, "Toward the PC, and what they want now", "their motives, which may be a spoiler"),
        new("Playing them", "Voice", "the play card's Voice is printed instead"),
        new("Playing them", "Names they use", "DMC's notes for voicing them"),
        new("Playing them", "What they want", "their motives, which may be a spoiler"),
        new("Playing them", "What they fear", "their motives, which may be a spoiler"),
        new("Playing them", "Attitude to the PC, and why", "their motives, which may be a spoiler"),
    ];

    /// <summary>The sections no layout prints, and why (on top of the labels above).</summary>
    public static IReadOnlyList<(string Section, string Reason)> SectionsNotExported { get; } =
    [
        ("What they know", "a spoiler"),
        ("Secrets", "a spoiler"),
        ("Relationships", "on a companion's sheet: ties the PC may not know of (a PC's are printed)"),
        ("History", "on a companion's sheet, never; on a PC's, only with --history"),
    ];

    /// <summary>The panel every layout ends its numbers with: the character's block under Party status.</summary>
    static Panel Now => new("Now (from game-state.md)", [.. PartyLabels.Select(l => new Box(l, new PartySource(l)))]);

    /// <summary>The layout of a player character's sheet, in its fixed order (spec 031 FR-008).</summary>
    public static IReadOnlyList<Panel> Pc { get; } =
    [
        new("Character", [
            F("Species and background", SheetText.Top), F("Class and level", SheetText.Top), F("Alignment", SheetText.Top),
            F("XP or milestone progress", SheetText.Top)]),
        new("Abilities", [
            new Box("Ability scores, modifiers and saves", new AbilityTableSource(), Wide: true),
            F("Proficiency bonus", "Abilities"), F("Saving throw proficiencies", "Abilities"), F("Passive Perception", "Abilities"),
            F("Expertise", "Abilities")]),
        new("Skills", [new Box("Skills", new SkillGridSource(), Wide: true)]),
        new("Combat", [
            F("Armor Class", "Combat"), F("Initiative", "Combat"), F("Speed", "Combat"), F("Max HP", "Combat"),
            F("Hit dice", "Combat"), F("Senses", "Combat"), F("Resistances, immunities, vulnerabilities", "Combat", wide: true),
            new Box("Attacks", new AttackTableSource(), Wide: true)]),
        Now,
        new("Proficiencies", [F("Languages", "Abilities", wide: true), F("Tool and other proficiencies", "Abilities", wide: true)]),
        new("Features and traits", [new Box("Features and traits", new FreeSource("Features and traits"), Wide: true)]),
        new("Spellcasting", [
            F("Casting ability, save DC, attack bonus", "Spellcasting", wide: true), F("Slots per level", "Spellcasting", wide: true),
            F("Cantrips", "Spellcasting", wide: true), F("Prepared or known spells", "Spellcasting", wide: true),
            F("Spellbook or other spells available", "Spellcasting", wide: true)]),
        new("Equipment", [
            F("Equipped", "Inventory", wide: true), F("Carried", "Inventory", wide: true), F("Magic items", "Inventory", wide: true),
            F("Stored elsewhere", "Inventory", wide: true),
            new Box("Wardrobe and keepsakes", new FreeSource("Wardrobe and keepsakes"), Wide: true)]),
        new("Story", [
            F("Appearance", "Story", wide: true), F("Personality", "Story", wide: true), F("Ideals, bonds, flaws", "Story", wide: true),
            F("Backstory", "Story", wide: true), F("Current objectives", "Story", wide: true), F("Long-term goals", "Story", wide: true),
            new Box("Allies and relationships", new FreeSource("Relationships"), Wide: true)]),
        new("History", [new Box("History", new HistorySource(), Wide: true)]),
    ];

    /// <summary>The layout of a companion whose sheet follows the NPC template: the play card first, then the stat block.</summary>
    public static IReadOnlyList<Panel> CompanionNpc { get; } =
    [
        new("Character", [F("Species, occupation, age", SheetText.Top), F("Status", SheetText.Top)]),
        CardPanel,
        new("Stat block", [
            F("Based on", "Stat block"), F("STR / DEX / CON / INT / WIS / CHA", "Stat block", wide: true),
            F("AC", "Stat block", card: true), F("Max HP", "Stat block", card: true), F("Speed", "Stat block", card: true),
            F("Saves and skills", "Stat block", card: true, wide: true), F("Senses and languages", "Stat block", card: true, wide: true),
            F("Traits", "Stat block", card: true, wide: true), F("Actions", "Stat block", card: true, wide: true),
            F("Spells or special abilities", "Stat block", wide: true), F("Gear", "Stat block", wide: true)]),
        Now,
        new("About them", [
            F("Appearance", "Playing them", wide: true), F("Personality", "Playing them", wide: true),
            new Box("Wardrobe and keepsakes", new FreeSource("Wardrobe and keepsakes"), Wide: true)]),
    ];

    /// <summary>The layout of a companion whose sheet follows the PC template: the play card, then the PC's panels, its story cut to what is not a spoiler.</summary>
    public static IReadOnlyList<Panel> CompanionPc { get; } =
    [
        new("Character", [
            F("Species and background", SheetText.Top), F("Class and level", SheetText.Top), F("Alignment", SheetText.Top),
            F("XP or milestone progress", SheetText.Top)]),
        CardPanel,
        new("Abilities", [
            new Box("Ability scores, modifiers and saves", new AbilityTableSource(), Wide: true),
            F("Proficiency bonus", "Abilities"), F("Saving throw proficiencies", "Abilities", card: true),
            F("Passive Perception", "Abilities", card: true), F("Expertise", "Abilities", card: true)]),
        new("Skills", [new Box("Skills", new SkillGridSource(), CardHeld: true, Wide: true)]),
        new("Combat", [
            F("Armor Class", "Combat", card: true), F("Initiative", "Combat", card: true), F("Speed", "Combat", card: true),
            F("Max HP", "Combat", card: true), F("Hit dice", "Combat"), F("Senses", "Combat", card: true),
            F("Resistances, immunities, vulnerabilities", "Combat", card: true, wide: true),
            new Box("Attacks", new AttackTableSource(), CardHeld: true, Wide: true)]),
        Now,
        new("Proficiencies", [F("Languages", "Abilities", wide: true), F("Tool and other proficiencies", "Abilities", wide: true)]),
        new("Features and traits", [new Box("Features and traits", new FreeSource("Features and traits"), Wide: true)]),
        new("Spellcasting", [
            F("Casting ability, save DC, attack bonus", "Spellcasting", wide: true), F("Slots per level", "Spellcasting", wide: true),
            F("Cantrips", "Spellcasting", wide: true), F("Prepared or known spells", "Spellcasting", wide: true),
            F("Spellbook or other spells available", "Spellcasting", wide: true)]),
        new("Equipment", [
            F("Equipped", "Inventory", wide: true), F("Carried", "Inventory", wide: true), F("Magic items", "Inventory", wide: true),
            F("Stored elsewhere", "Inventory", wide: true),
            new Box("Wardrobe and keepsakes", new FreeSource("Wardrobe and keepsakes"), Wide: true)]),
        new("About them", [F("Appearance", "Story", wide: true), F("Personality", "Story", wide: true)]),
    ];

    /// <summary>The play card's exported fields, the same on both companion layouts.</summary>
    static Panel CardPanel => new("Play card", [
        F("Looks", PlayCard, wide: true), F("Voice", PlayCard, wide: true),
        F("AC, max HP, speed, saves, senses", PlayCard, wide: true), F("Attacks", PlayCard, wide: true),
        F("Recharge and limited abilities", PlayCard, wide: true), F("Standing traits that change play", PlayCard, wide: true),
        F("Items used in play", PlayCard, wide: true)]);

    /// <summary>The layout of a kind of sheet.</summary>
    /// <param name="kind">The sheet's kind.</param>
    public static IReadOnlyList<Panel> Of(SheetKind kind) => kind switch
    {
        SheetKind.Pc => Pc,
        SheetKind.CompanionNpc => CompanionNpc,
        _ => CompanionPc,
    };

    /// <summary>The sections a kind of sheet may have.</summary>
    /// <param name="kind">The sheet's kind.</param>
    public static IReadOnlyList<string> SectionsOf(SheetKind kind) => kind == SheetKind.CompanionNpc ? NpcSections : PcSections;

    /// <summary>The template labels of each structured section, for a kind of sheet.</summary>
    /// <param name="kind">The sheet's kind.</param>
    public static IReadOnlyDictionary<string, string[]> LabelsOf(SheetKind kind) => kind == SheetKind.CompanionNpc ? NpcLabels : PcLabels;

    /// <summary>A box filled from one labelled field.</summary>
    /// <param name="label">The field's label, which is also the box's title.</param>
    /// <param name="section">The field's section.</param>
    /// <param name="card">Whether a companion's play card may hold it instead (<see cref="Box.CardHeld"/>).</param>
    /// <param name="wide">Whether the box takes the panel's width.</param>
    static Box F(string label, string section, bool card = false, bool wide = false) =>
        new(label, new FieldSource(section, label), card, wide);
}
