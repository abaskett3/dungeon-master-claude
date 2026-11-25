namespace Dmc.Size;

/// <summary>
/// Every size budget, limit and threshold <c>dmc size</c> checks, in tokens of file text (<see cref="Measurement.Tokens"/>,
/// 2 for 5 characters) unless a member says otherwise. Each budget is 1.6 times the number it had when tokens were bytes
/// ÷ 4, so it allows the same text and no file is newly over one (spec 021 research R3); a figure in a comment that was
/// measured in that old unit is given here times 1.6. This is the one home of these numbers: the skills and templates say "what <c>dmc size</c> checks" and
/// state none of them, and <c>.docs/campaign-files.md</c> lists them for readers (a toolkit test keeps that list equal to
/// this one). The reasons are in <c>.docs/decisions.md</c>, "Size rules are measured in tokens by <c>dmc size</c>", and in
/// <c>specs/006-dmc-size/research.md</c> R3 to R6, and for the unit in <c>specs/021-real-tokens-dmc-size/research.md</c>.
/// </summary>
internal static class Budgets
{
    /// <summary><c>game-state.md</c>, read whole at every resume: the present moment only (it replaced "about 100 lines").</summary>
    public const int GameState = 6400;

    /// <summary><c>story/dm-notes.md</c>, read whole at every resume: what is still ahead (it replaced "about 150 lines").</summary>
    public const int DmNotes = 16000;

    /// <summary>The <b>About this player</b> section of <c>dm-notes.md</c> (it replaced "about 40 lines").</summary>
    public const int DmNotesAboutThePlayer = 3200;

    /// <summary>
    /// The <b>Arc plan</b> section of <c>dm-notes.md</c> (it replaced "about 25 lines"), its content only: the template's
    /// comment is left out (spec 015), so the full save never deletes the guidance to fit. A finished stage kept in full
    /// shows as this section over budget.
    /// </summary>
    public const int DmNotesArcPlan = 2400;

    /// <summary><c>story/summary.md</c>, a briefing for a new DM (it replaced "about 600 words"; the review chose 4.8k against a summary of 8k).</summary>
    public const int Summary = 4800;

    /// <summary>The part of a PC's sheet the resume reads (down to its history). Higher than other sheets: a PC's features, spells and kit measured 8.6k on the long campaign.</summary>
    public const int PcSheet = 9600;

    /// <summary>The part of any other sheet the resume reads (down to its history); companions measured 2.9k to 5.6k.</summary>
    public const int OtherSheet = 6400;

    /// <summary>
    /// The part of a companion's sheet the resume reads when the sheet has a play card: from the top through the card
    /// (spec 013 FR-003, research R1). A card holds what a scene and a fight need; a hand estimate of one measured 800 to
    /// 960, so 1.6k leaves room and saves about 17.6k on four companions measured at 24.8k down to their histories.
    /// </summary>
    public const int PlayCard = 1600;

    /// <summary>
    /// Canon under this size is read whole at resume; at or over it, the resume reads its always-read sections and the
    /// scene's best matches (<c>dmc recall --scene</c>, spec 013 FR-007, research R3). Spec 013 set it at 6,000 (9,600
    /// now); its gate found canon slips rise (2 in Rorn's Landing's sessions 55 and 56 against a baseline of 0.04 a
    /// session), so FR-026's rollback puts it back at <see cref="CanonLimit"/>: canon is read whole until a full save
    /// archives it.
    /// </summary>
    public const int CanonWholeBelow = CanonLimit;

    /// <summary>
    /// The most bytes <c>dmc recall --scene</c> prints: about 4,800 tokens of canon lines ranked for the scene, the cut the
    /// user chose in spec 013's clarification (about 16k saved on Rorn's Landing's 20.8k outside the always-read
    /// sections). In bytes, so the unit of tokens does not move it.
    /// </summary>
    public const int SceneReadBytes = 12000;

    /// <summary>A full save archives canon over this size (it replaced "about 230 lines"). Since spec 013's canon rollback it is also <see cref="CanonWholeBelow"/>, under which the resume reads canon whole.</summary>
    public const int CanonLimit = 30400;

    /// <summary>A full save that finds canon over <see cref="CanonLimit"/> archives it down below this, so the next session's additions do not push it straight back over (it replaced "about 200 lines").</summary>
    public const int CanonArchiveMark = 25600;

    /// <summary>The <b>Prices, debts and promises</b> section of canon above this suggests paid debts or kept promises that were not deleted.</summary>
    public const int CanonPricesSection = 4800;

    /// <summary><c>legacy.md</c>, what changed in the shared world, which linked campaigns read once at resume (spec 011 FR-025): about a few pages of change lines. Its own resume never reads it.</summary>
    public const int Legacy = 4800;

    /// <summary>The Read tool returns about this many real tokens in one call; used only to say so in the near-cap line.</summary>
    public const int ReadCapTokens = 25000;

    /// <summary>
    /// A part whose Read costs this many tokens or more (<see cref="Measurement.ReadCost"/>) is near the Read cap. The
    /// estimate was at most 7.5% under a real count on whole campaign files, so 18,000 flags every part of 20,000 real
    /// tokens and leaves at least 5,600 under the cap, a few sessions' growth (spec 021 research R5). Under bytes ÷ 4 the
    /// mark was 20,000, about 32,000 real tokens: past the cap, so the flag came after the cut.
    /// </summary>
    public const int NearCapTokens = 18000;

    /// <summary>A part read of this many lines or more is near the Read tool's limit of 2,000 lines in one call (80% of it).</summary>
    public const int NearCapLines = 1600;

    /// <summary>
    /// A heading of an ingested module whose own text (to the next heading of any level) is over this many tokens holds
    /// more than one area or entry, so <c>dmc module check</c> reports it (spec 012 research R2). A stat block measured
    /// about 1,040; the largest section of a 638-page module written one heading per area was 3,885, and a hub chapter
    /// under one heading 22.1k.
    /// </summary>
    public const int ModuleSection = 4800;

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
