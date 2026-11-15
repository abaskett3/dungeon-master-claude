namespace Dmc.Size;

/// <summary>
/// Every size budget, limit and threshold <c>dmc size</c> checks, in estimated tokens (bytes ÷ 4) unless a member says
/// otherwise. This is the one home of these numbers: the skills and templates say "what <c>dmc size</c> checks" and
/// state none of them, and <c>.docs/campaign-files.md</c> lists them for readers (a toolkit test keeps that list equal to
/// this one). The reasons are in <c>.docs/decisions.md</c>, "Size rules are measured in tokens by <c>dmc size</c>", and in
/// <c>specs/006-dmc-size/research.md</c> R3 to R6.
/// </summary>
internal static class Budgets
{
    /// <summary><c>game-state.md</c>, read whole at every resume: the present moment only (it replaced "about 100 lines").</summary>
    public const int GameState = 4000;

    /// <summary><c>story/dm-notes.md</c>, read whole at every resume: what is still ahead (it replaced "about 150 lines").</summary>
    public const int DmNotes = 10000;

    /// <summary>The <b>About this player</b> section of <c>dm-notes.md</c> (it replaced "about 40 lines").</summary>
    public const int DmNotesAboutThePlayer = 2000;

    /// <summary>The <b>Arc plan</b> section of <c>dm-notes.md</c> (it replaced "about 25 lines"); a finished stage kept in full shows as this section over budget.</summary>
    public const int DmNotesArcPlan = 1500;

    /// <summary><c>story/summary.md</c>, a briefing for a new DM (it replaced "about 600 words"; the review chose 3k against a summary of 5k).</summary>
    public const int Summary = 3000;

    /// <summary>The part of a PC's sheet the resume reads (down to its history). Higher than other sheets: a PC's features, spells and kit measured 5.4k on the long campaign.</summary>
    public const int PcSheet = 6000;

    /// <summary>The part of any other sheet the resume reads (down to its history); companions measured 1.8k to 3.5k.</summary>
    public const int OtherSheet = 4000;

    /// <summary>
    /// The part of a companion's sheet the resume reads when the sheet has a play card: from the top through the card
    /// (spec 013 FR-003, research R1). A card holds what a scene and a fight need; a hand estimate of one measured 500 to
    /// 600, so 1k leaves room and saves about 11k on four companions measured at 15.5k down to their histories.
    /// </summary>
    public const int PlayCard = 1000;

    /// <summary>
    /// Canon under this size is read whole at resume; at or over it, the resume reads its always-read sections and the
    /// scene's best matches (<c>dmc recall --scene</c>, spec 013 FR-007, research R3). At about this size the always-read
    /// sections and the cut cost what reading the whole file does.
    /// </summary>
    public const int CanonWholeBelow = 6000;

    /// <summary>
    /// The most bytes <c>dmc recall --scene</c> prints: about 3,000 estimated tokens of canon lines ranked for the scene,
    /// the cut the user chose in spec 013's clarification (about 10k saved on Rorn's Landing's 13k outside the always-read
    /// sections).
    /// </summary>
    public const int SceneReadBytes = 12000;

    /// <summary>A full save archives canon over this size (it replaced "about 230 lines"). Since spec 013 it no longer decides how the resume reads canon (<see cref="CanonWholeBelow"/> does).</summary>
    public const int CanonLimit = 19000;

    /// <summary>A full save that finds canon over <see cref="CanonLimit"/> archives it down below this, so the next session's additions do not push it straight back over (it replaced "about 200 lines").</summary>
    public const int CanonArchiveMark = 16000;

    /// <summary>The <b>Prices, debts and promises</b> section of canon above this suggests paid debts or kept promises that were not deleted.</summary>
    public const int CanonPricesSection = 3000;

    /// <summary><c>legacy.md</c>, what changed in the shared world, which linked campaigns read once at resume (spec 011 FR-025): about a few pages of change lines. Its own resume never reads it.</summary>
    public const int Legacy = 3000;

    /// <summary>The Read tool returns about this many tokens in one call; used only to say so in the near-cap line.</summary>
    public const int ReadCapTokens = 25000;

    /// <summary>A part read of this many estimated tokens or more is near the Read cap: 80% of it, since bytes ÷ 4 undercounts (a 23.5k file was cut).</summary>
    public const int NearCapTokens = 20000;

    /// <summary>A part read of this many lines or more is near the Read tool's limit of 2,000 lines in one call (80% of it).</summary>
    public const int NearCapLines = 1600;

    /// <summary>
    /// A heading of an ingested module whose own text (to the next heading of any level) is over this many estimated
    /// tokens holds more than one area or entry, so <c>dmc module check</c> reports it (spec 012 research R2). A stat
    /// block measured about 650; the largest section of a 638-page module written one heading per area was 2,428, and a
    /// hub chapter under one heading 13.8k.
    /// </summary>
    public const int ModuleSection = 3000;

    /// <summary>A line longer than this many characters breaks the rule of one fact per line.</summary>
    public const int LongLineCharacters = 400;

    /// <summary>The most lines <c>dmc size --check</c> prints; past it, the last line says how many more there are.</summary>
    public const int CheckLines = 12;

    /// <summary>The budget of a kind of file, or null for a file the full save does not trim (no budget).</summary>
    /// <param name="kind">The kind of file.</param>
    public static int? Of(BudgetKind kind) => kind switch
    {
        BudgetKind.GameState => GameState,
        BudgetKind.Summary => Summary,
        BudgetKind.DmNotes => DmNotes,
        BudgetKind.PcSheet => PcSheet,
        BudgetKind.OtherSheet => OtherSheet,
        BudgetKind.Legacy => Legacy,
        BudgetKind.PlayCard => PlayCard,
        _ => null,
    };
}

/// <summary>Which budget a file of the resume's read set has.</summary>
internal enum BudgetKind
{
    /// <summary>No budget: a file the full save never trims (the player options, the session log, the module files).</summary>
    None,

    /// <summary><see cref="Budgets.GameState"/>.</summary>
    GameState,

    /// <summary><see cref="Budgets.Summary"/>.</summary>
    Summary,

    /// <summary><see cref="Budgets.DmNotes"/>.</summary>
    DmNotes,

    /// <summary><see cref="Budgets.PcSheet"/>.</summary>
    PcSheet,

    /// <summary><see cref="Budgets.OtherSheet"/>.</summary>
    OtherSheet,

    /// <summary><see cref="Budgets.PlayCard"/>: a companion's sheet read through its play card.</summary>
    PlayCard,

    /// <summary><see cref="Budgets.Legacy"/>: a file other campaigns read, outside this campaign's resume total.</summary>
    Legacy,

    /// <summary>Canon, which has a limit (<see cref="Budgets.CanonLimit"/>) that decides how it is read, not a budget.</summary>
    Canon,
}
