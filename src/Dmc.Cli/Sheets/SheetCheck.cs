namespace Dmc.Sheets;

/// <summary>What the check found in the characters chosen for an export.</summary>
/// <param name="Findings">What is out of shape: an export with any of these is refused (spec 031 FR-010).</param>
/// <param name="Warnings">What does not stop the export: an arithmetic difference, a Party status line off its template (FR-012, FR-013). Each starts with <c>warning: </c>.</param>
internal sealed record CheckResult(IReadOnlyList<string> Findings, IReadOnlyList<string> Warnings);

/// <summary>
/// The structure check of <c>dmc sheet</c> (spec 031 FR-010 to FR-013): the templates are the schema, one labelled field
/// per box. A sheet whose structured sections hold a line the template has no label for, that lacks a field a box
/// prints, or that leaves one blank, is a finding, and the export is refused until DMC fixes the file; the arithmetic
/// of saves and skills and the Party status block's shape only warn. Nothing here reads a file or writes one.
/// </summary>
internal static class SheetCheck
{
    /// <summary>Checks every character of an export, in order.</summary>
    /// <param name="sheets">The characters, read.</param>
    public static CheckResult Run(IEnumerable<CharacterSheet> sheets)
    {
        var findings = new List<string>();
        var warnings = new List<string>();
        foreach (var sheet in sheets)
        {
            Structure(sheet, findings);
            if (sheet.Kind is SheetKind.Pc or SheetKind.CompanionPc)
                Arithmetic(sheet, warnings);
            Party(sheet, warnings);
        }
        return new CheckResult(findings, warnings);
    }

    /// <summary>The template a kind of sheet follows, as a DMC folder names it.</summary>
    /// <param name="kind">The sheet's kind.</param>
    static string Template(SheetKind kind) => kind == SheetKind.CompanionNpc ? "templates/npc-sheet.md" : "templates/pc-sheet.md";

    /// <summary>How a finding names a section: its heading, or "the opening fields" for the part before the first heading.</summary>
    /// <param name="section">The heading without <c>## </c>.</param>
    static string Where(string section) => section == SheetText.Top ? "the opening fields" : "## " + section;

    /// <summary>A line cut to a length that fits a finding.</summary>
    /// <param name="line">The line.</param>
    static string Cut(string line)
    {
        string text = line.Trim();
        return text.Length <= 60 ? text : text[..57].TrimEnd() + "...";
    }

    /// <summary>The structure findings of one sheet.</summary>
    /// <param name="sheet">The character.</param>
    /// <param name="findings">Receives the findings.</param>
    static void Structure(CharacterSheet sheet, List<string> findings)
    {
        string file = sheet.File;
        if (sheet.Kind is not { } kind)
        {
            findings.Add($"{file}: follows neither templates/pc-sheet.md nor templates/npc-sheet.md: rewrite it on the one it is closest to");
            return;
        }
        if (sheet.IsCompanion && !sheet.HasCard)
            findings.Add($"{file}: a companion's sheet needs its ## Play card (see the dmc-character-sheet-update skill)");

        var allowed = SheetLayouts.SectionsOf(kind).Concat(SheetLayouts.OlderHistoryHeadings).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var section in sheet.Sections)
        {
            if (section.Heading == SheetText.Top && section.Lines.All(l => l.Trim().Length == 0) && section.Subsections.Count == 0)
                continue;
            if (!allowed.Contains(section.Heading))
            {
                findings.Add($"{file}: {Where(section.Heading)} is not a section of {Template(kind)}: move its lines into the template's sections");
                continue;
            }
            if (!seen.Add(section.Heading))
                findings.Add($"{file}: {Where(section.Heading)} is there twice: merge them");
            if (SheetLayouts.LabelsOf(kind).TryGetValue(section.Heading, out string[]? labels))
                Labels(file, section, labels, kind, findings);
        }

        var layout = SheetLayouts.Of(kind);
        foreach (var box in layout.SelectMany(p => p.Boxes))
        {
            switch (box.Source)
            {
                case FieldSource f:
                    if (f.Section == SheetLayouts.PlayCard && !sheet.HasCard)
                        break; // the missing card is one finding, above
                    var field = sheet.Field(f.Section, f.Label);
                    if (field is null)
                    {
                        if (!(box.CardHeld && sheet.IsCompanion))
                            findings.Add($"{file}: no {f.Label} line in {Where(f.Section)}: add it from {Template(kind)}");
                    }
                    else if (field.IsBlank)
                        findings.Add($"{file}: {f.Label} is blank: write its value, or none");
                    break;
                case AbilityTableSource:
                    AbilityTable(sheet, findings);
                    break;
                case AttackTableSource:
                    if (!(box.CardHeld && sheet.IsCompanion) && AttackRows(sheet) is null)
                        findings.Add($"{file}: no ### Attacks table in ## Combat: add it from {Template(kind)}");
                    break;
                case SkillGridSource:
                    SkillGrid(sheet, box.CardHeld && sheet.IsCompanion, findings);
                    break;
            }
        }
    }

    /// <summary>The findings of one structured section: a label the template does not have, a line that is not a field, a field twice, a subsection the template does not have.</summary>
    /// <param name="file">The sheet's file name.</param>
    /// <param name="section">The section.</param>
    /// <param name="labels">The template's labels for it.</param>
    /// <param name="kind">The sheet's kind.</param>
    /// <param name="findings">Receives the findings.</param>
    static void Labels(string file, SheetSection section, string[] labels, SheetKind kind, List<string> findings)
    {
        var others = new List<string>();
        var tables = new List<string>();
        var fields = SheetText.Fields(section.Lines, others, tables);
        var counted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in fields)
        {
            if (!labels.Contains(field.Label, StringComparer.OrdinalIgnoreCase))
                findings.Add($"{file}: {Where(section.Heading)}: \"{Cut(field.Line)}\" has no label of {Template(kind)}: move it to the field it belongs to");
            else if (!counted.Add(field.Label))
                findings.Add($"{file}: {Where(section.Heading)}: two {field.Label} lines: merge them");
        }
        foreach (string line in others)
            findings.Add($"{file}: {Where(section.Heading)}: \"{Cut(line)}\" is not a labelled field: move it to the field it belongs to");
        bool tablesAllowed = kind != SheetKind.CompanionNpc && section.Heading.Equals("Abilities", StringComparison.OrdinalIgnoreCase);
        if (tables.Count > 0 && !tablesAllowed)
            findings.Add($"{file}: {Where(section.Heading)}: a table the template does not have: write its rows as the section's fields");
        foreach (var (heading, lines) in section.Subsections)
        {
            bool attacks = kind != SheetKind.CompanionNpc && section.Heading.Equals("Combat", StringComparison.OrdinalIgnoreCase)
                           && heading.Equals("Attacks", StringComparison.OrdinalIgnoreCase);
            if (!attacks)
                findings.Add($"{file}: {Where(section.Heading)}: ### {heading} is not part of {Template(kind)}: move its lines into the section's fields");
            else if (lines.Any(l => l.Trim().Length > 0 && !l.TrimStart().StartsWith('|')))
                findings.Add($"{file}: ## Combat: ### Attacks holds a line that is not a table row: write each attack as a row");
        }
    }

    /// <summary>The ability table's cells, header first, or none when <c>## Abilities</c> has no table.</summary>
    /// <param name="sheet">The character.</param>
    public static List<string[]> AbilityCells(CharacterSheet sheet)
    {
        if (SheetText.Find(sheet.Sections, "Abilities") is not { } section)
            return [];
        var tables = new List<string>();
        SheetText.Fields(section.Lines, tables: tables);
        return SheetText.TableCells(tables);
    }

    /// <summary>The findings of the ability table: each of Score, Modifier and Save written for all six abilities (a companion's saves may be on its play card instead).</summary>
    /// <param name="sheet">The character.</param>
    /// <param name="findings">Receives the findings.</param>
    static void AbilityTable(CharacterSheet sheet, List<string> findings)
    {
        var rows = SheetNumbers.AbilityRows(AbilityCells(sheet));
        if (rows.Count == 0)
        {
            findings.Add($"{sheet.File}: no ability table in ## Abilities: add it from {Template(sheet.Kind!.Value)}");
            return;
        }
        foreach (string row in new[] { "Score", "Modifier", "Save" })
        {
            if (row == "Save" && sheet.IsCompanion && !rows.ContainsKey(row))
                continue; // a companion's play card may hold the saves (migration 006 step 3)
            if (!rows.TryGetValue(row, out string[]? cells))
            {
                findings.Add($"{sheet.File}: the ability table has no {row} row");
                continue;
            }
            var blank = SheetLayouts.Abilities.Where((_, i) => cells[i].Length == 0).ToList();
            if (blank.Count > 0)
                findings.Add($"{sheet.File}: the ability table's {row} row is blank for {string.Join(", ", blank)}");
        }
    }

    /// <summary>The attack table's cells, header first, or null when <c>## Combat</c> has no <c>### Attacks</c> subsection.</summary>
    /// <param name="sheet">The character.</param>
    public static List<string[]>? AttackRows(CharacterSheet sheet)
    {
        if (SheetText.Find(sheet.Sections, "Combat") is not { } combat)
            return null;
        foreach (var (heading, lines) in combat.Subsections)
            if (heading.Equals("Attacks", StringComparison.OrdinalIgnoreCase))
                return SheetText.TableCells(lines.Where(l => l.TrimStart().StartsWith('|')));
        return null;
    }

    /// <summary>
    /// The findings of the eighteen skills: <c>Skills</c> and <c>Other skills</c> both written, every skill in exactly one
    /// of them, each with its bonus.
    /// </summary>
    /// <param name="sheet">The character.</param>
    /// <param name="mayLack">True for a companion whose play card may hold the skills instead: no finding when both fields are missing.</param>
    /// <param name="findings">Receives the findings.</param>
    static void SkillGrid(CharacterSheet sheet, bool mayLack, List<string> findings)
    {
        string file = sheet.File;
        var skills = sheet.Field("Abilities", "Skills");
        var other = sheet.Field("Abilities", "Other skills");
        if (skills is null && other is null && mayLack)
            return;
        if (skills is null)
            findings.Add($"{file}: no Skills line in ## Abilities: add it from {Template(sheet.Kind!.Value)}");
        if (other is null)
        {
            findings.Add($"{file}: no Other skills line in ## Abilities: write the skills it is not proficient in, each with its bonus (templates/pc-sheet.md)");
            return;
        }
        var unknown = new List<string>();
        var entries = SheetNumbers.SkillList(skills?.Value ?? "", unknown).Concat(SheetNumbers.SkillList(other.Value, unknown)).ToList();
        foreach (string piece in unknown)
            findings.Add($"{file}: Skills or Other skills: \"{Cut(piece)}\" is not one of the eighteen skills: move it to the field it belongs to");
        var twice = entries.GroupBy(e => e.Skill).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (twice.Count > 0)
            findings.Add($"{file}: Skills and Other skills both name {string.Join(", ", twice)}: keep each skill in one");
        var missing = SheetLayouts.Skills.Select(s => s.Skill).Where(s => entries.All(e => e.Skill != s)).ToList();
        if (missing.Count > 0)
            findings.Add($"{file}: Skills and Other skills do not name {string.Join(", ", missing)}: write every skill with its bonus");
        var noBonus = entries.Where(e => e.Written.Length == 0).Select(e => e.Skill).Distinct().ToList();
        if (noBonus.Count > 0)
            findings.Add($"{file}: no bonus written for {string.Join(", ", noBonus)}: write each skill's total bonus");
    }

    /// <summary>
    /// The arithmetic check (FR-012): each modifier, save and skill, and passive Perception, worked out from the scores,
    /// the proficiency bonus and the proficiency and expertise lists, compared with the written value. A difference is
    /// one warning; the written value is what prints, because a feature or an item can rightly change it.
    /// </summary>
    /// <param name="sheet">The character.</param>
    /// <param name="warnings">Receives the warnings.</param>
    static void Arithmetic(CharacterSheet sheet, List<string> warnings)
    {
        string file = sheet.File;
        var rows = SheetNumbers.AbilityRows(AbilityCells(sheet));
        if (!rows.TryGetValue("Score", out string[]? scores))
            return;
        var mods = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < 6; i++)
        {
            if (SheetNumbers.ParseSigned(scores[i]) is not { } score)
                continue;
            string ability = SheetLayouts.Abilities[i];
            int mod = SheetNumbers.Modifier(score);
            mods[ability] = mod;
            if (rows.TryGetValue("Modifier", out string[]? written) && SheetNumbers.ParseSigned(written[i]) is { } w && w != mod)
                warnings.Add($"warning: {file}: the {ability} modifier is {written[i]} on the sheet; a score of {score} makes {SheetNumbers.WithSign(mod)} (the sheet's value is printed)");
        }

        int? pb = sheet.Field("Abilities", "Proficiency bonus") is { } p ? SheetNumbers.ParseSigned(p.Value) : null;
        if (pb is null)
            return;
        var saveProf = SheetNumbers.SaveAbilities(sheet.Field("Abilities", "Saving throw proficiencies")?.Value ?? "");
        if (rows.TryGetValue("Save", out string[]? saves))
        {
            for (int i = 0; i < 6; i++)
            {
                string ability = SheetLayouts.Abilities[i];
                if (!mods.TryGetValue(ability, out int mod) || SheetNumbers.ParseSigned(saves[i]) is not { } written)
                    continue;
                bool prof = saveProf.Contains(ability);
                int expected = mod + (prof ? pb.Value : 0);
                if (written != expected)
                    warnings.Add($"warning: {file}: the {ability} save is {saves[i]} on the sheet; {ability} {SheetNumbers.WithSign(mod)}"
                                 + (prof ? $" and proficiency {SheetNumbers.WithSign(pb.Value)}" : ", not proficient,")
                                 + $" make {SheetNumbers.WithSign(expected)} (the sheet's value is printed)");
            }
        }

        var expertise = SheetNumbers.SkillNames(sheet.Field("Abilities", "Expertise")?.Value ?? "");
        int? perception = null;
        foreach (var (field, proficient) in new[] { ("Skills", true), ("Other skills", false) })
        {
            foreach (var entry in SheetNumbers.SkillList(sheet.Field("Abilities", field)?.Value ?? ""))
            {
                string ability = SheetLayouts.Skills.First(s => s.Skill == entry.Skill).Ability;
                if (SheetNumbers.ParseSigned(entry.Written) is not { } written || !mods.TryGetValue(ability, out int mod))
                    continue;
                if (entry.Skill == "Perception")
                    perception = written;
                bool doubled = proficient && expertise.Contains(entry.Skill);
                int expected = mod + (proficient ? pb.Value * (doubled ? 2 : 1) : 0);
                if (written != expected)
                {
                    string why = proficient
                        ? $"{ability} {SheetNumbers.WithSign(mod)} and proficiency {SheetNumbers.WithSign(pb.Value)}{(doubled ? " doubled for expertise" : "")}"
                        : $"{ability} {SheetNumbers.WithSign(mod)}, not proficient,";
                    warnings.Add($"warning: {file}: {entry.Skill} is {entry.Written} on the sheet; {why} make {SheetNumbers.WithSign(expected)} (the sheet's value is printed)");
                }
            }
        }
        if (perception is { } per && sheet.Field("Abilities", "Passive Perception") is { } passive
            && SheetNumbers.ParseSigned(passive.Value) is { } pp && pp != 10 + per)
            warnings.Add($"warning: {file}: passive Perception is {pp} on the sheet; 10 and Perception {SheetNumbers.WithSign(per)} make {10 + per} (the sheet's value is printed)");
    }

    /// <summary>The warnings of the character's Party status block (FR-013): none for them, or a line off its template, which is not printed.</summary>
    /// <param name="sheet">The character.</param>
    /// <param name="warnings">Receives the warnings.</param>
    static void Party(CharacterSheet sheet, List<string> warnings)
    {
        if (sheet.Party is null)
        {
            warnings.Add($"warning: game-state.md: no Party status block for {sheet.Title}: the Now panel says not recorded");
            return;
        }
        foreach (var field in sheet.Party.Where(f => !SheetLayouts.PartyLabels.Contains(f.Label, StringComparer.OrdinalIgnoreCase)))
            warnings.Add($"warning: game-state.md: {sheet.Title}'s block: \"{Cut(field.Line)}\" is not a field of the template and is not printed");
        foreach (string line in sheet.PartyOthers)
            warnings.Add($"warning: game-state.md: {sheet.Title}'s block: \"{Cut(line)}\" is not a labelled field and is not printed");
    }
}
