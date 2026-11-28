namespace Dmc.Sheets;

/// <summary>
/// A character chosen for export, read: their sheet cut into sections, and their own block under Party status. Built by
/// <see cref="SheetExport"/>; checked by <see cref="SheetCheck"/>; printed by <see cref="SheetHtml"/>.
/// </summary>
/// <param name="File">The sheet's file name in <c>character-sheets/</c>.</param>
/// <param name="Title">The sheet's <c># </c> title, or its file name without <c>.md</c> when it has none.</param>
/// <param name="Kind">The template it follows, or null when it follows neither (a finding).</param>
/// <param name="Sections">The sheet's sections, comments removed.</param>
/// <param name="Party">The fields of the character's Party status block, or null when they have none.</param>
/// <param name="PartyOthers">The lines of that block that are not fields, for the check's warning.</param>
/// <param name="HistoryFile">The lines of <c>character-sheets/&lt;sheet name&gt;-history.md</c> without its title, or null when there is none or it was not asked for.</param>
/// <param name="Limits">
/// The texts of the limits on the character's record (<c>records/&lt;slug&gt;.json</c>, spec 032 FR-009), in the record's
/// order; empty when they have no record or it has none. The record's rows (who calls them what) are never read here:
/// they may hold names the PC has not heard.
/// </param>
internal sealed record CharacterSheet(
    string File, string Title, SheetKind? Kind, IReadOnlyList<SheetSection> Sections,
    IReadOnlyList<SheetField>? Party, IReadOnlyList<string> PartyOthers, IReadOnlyList<string>? HistoryFile,
    IReadOnlyList<string> Limits)
{
    /// <summary>Whether the sheet has a <c>## Play card</c> section.</summary>
    public bool HasCard => SheetText.Find(Sections, SheetLayouts.PlayCard) is not null;

    /// <summary>Whether this is a companion's sheet (either companion kind).</summary>
    public bool IsCompanion => Kind is SheetKind.CompanionNpc or SheetKind.CompanionPc;

    /// <summary>The fields of a section, or none when the section is missing.</summary>
    /// <param name="section">The section's heading without <c>## </c>.</param>
    public List<SheetField> FieldsOf(string section) =>
        SheetText.Find(Sections, section) is { } s ? SheetText.Fields(s.Lines) : [];

    /// <summary>The field with this label in this section (label compared ignoring case), or null.</summary>
    /// <param name="section">The section's heading without <c>## </c>.</param>
    /// <param name="label">The label.</param>
    public SheetField? Field(string section, string label) =>
        FieldsOf(section).FirstOrDefault(f => string.Equals(f.Label, label, StringComparison.OrdinalIgnoreCase));

    /// <summary>The history section's lines: <c>## History</c>, or an older name of it; null when there is none.</summary>
    public IReadOnlyList<string>? HistoryLines =>
        (SheetText.Find(Sections, "History") ?? SheetLayouts.OlderHistoryHeadings.Select(h => SheetText.Find(Sections, h)).FirstOrDefault(s => s is not null))
            ?.AllLines.ToList();

    /// <summary>
    /// The kind of a sheet that is not a PC's, from its opening fields: the NPC template's <c>Role in the story</c>, or the
    /// PC template's <c>Species and background</c> or <c>Companion</c> marker; then from its sections. Null for neither.
    /// </summary>
    /// <param name="sections">The sheet's sections.</param>
    public static SheetKind? CompanionKind(IReadOnlyList<SheetSection> sections)
    {
        var top = SheetText.Find(sections, SheetText.Top) is { } t ? SheetText.Fields(t.Lines) : [];
        bool Has(string label) => top.Any(f => string.Equals(f.Label, label, StringComparison.OrdinalIgnoreCase));
        if (Has("Role in the story") || Has("Species, occupation, age"))
            return SheetKind.CompanionNpc;
        if (Has("Companion") || Has("Species and background") || Has("Class and level"))
            return SheetKind.CompanionPc;
        if (SheetText.Find(sections, "Playing them") is not null || SheetText.Find(sections, "Stat block") is not null)
            return SheetKind.CompanionNpc;
        if (SheetText.Find(sections, "Abilities") is not null || SheetText.Find(sections, "Combat") is not null)
            return SheetKind.CompanionPc;
        return null;
    }
}
