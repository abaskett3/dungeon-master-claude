using System.Text.RegularExpressions;
using static Dmc.Digest.DigestText;

namespace Dmc.Digest;

/// <summary>
/// The numbers and the turn index of a session: <c>stats</c>, <c>turn_index</c> and the functions under them in
/// <c>transcript_digest.py</c>, line for line, because the <c>review-transcript</c> skill reads their output.
/// </summary>
internal static partial class Report
{
    /// <summary>
    /// Player messages matching this are marked "!" in the turn index. It only points at places to read; most
    /// friction has to be found by reading. (Known difference: .NET's <c>\b</c> counts nonspacing marks, connector
    /// punctuation and the zero-width joiners as word characters, and Python's does not, so exotic text next to a trigger word
    /// can be marked differently.)
    /// </summary>
    [GeneratedRegex(
        @"request interrupted|\bwrong\b|\bagain\b|\brewind\b|\bconsisten|\bconfus|" +
        @"\bwhy (are|did|is|do)\b|you said|\bi (said|told|asked)\b|\bdon'?t\b|\bstop\b|" +
        @"\bnot what\b|\bimpossible\b|\bunfair\b|\bno fun\b|\bnever mind\b|" +
        @"\bactually\b|\bincorrect\b|\bmistake\b|\bforgot\b|\bsorry\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    public static partial Regex Friction();

    /// <summary>Why a call after the second one in a player message was needed (see <see cref="ExtraCalls"/>).</summary>
    public static readonly (string Class, string Label)[] ExtraClasses =
    [
        ("save", "Full save"),
        ("dm-roll", "A turn with a DM roll: roll, then write, then narrate"),
        ("write", "A second write call in one reply"),
        ("lookup", "A Read, Grep or Glob as its own call"),
        ("roll", "A second roll call in one reply"),
        ("resume", "Resume"),
        ("other", "Anything else"),
    ];

    /// <summary>
    /// (class, context) for each call after the second in one player message.
    /// A reply that uses a tool needs two calls; a third and later call is needed because an earlier call made a tool call
    /// of its own. A message that ran the full save or resume is classed by that skill. Otherwise a call is classed by the
    /// call just before it and those earlier still: a write after a write is a second write, a write after a roll a
    /// DM-roll turn, a roll after a roll a second roll, and a call after a lookup a lookup.
    /// </summary>
    public static List<(string Class, double Ctx)> ExtraCalls(Turn turn)
    {
        var result = new List<(string, double)>();
        var calls = turn.Calls;
        for (int i = 2; i < calls.Count; i++)
        {
            var prev = calls[i - 1].Kinds;
            var before = new HashSet<string>(calls.Take(i - 1).SelectMany(c => c.Kinds));
            string cls;
            if (turn.Skills.Contains(Session.SaveSkill))
                cls = "save";
            else if (turn.Skills.Contains(Session.ResumeSkill))
                cls = "resume";
            else if (prev.Contains("write") && before.Contains("write"))
                cls = "write";
            else if (prev.Contains("write") && before.Contains("roll"))
                cls = "dm-roll";
            else if (prev.Contains("roll") && before.Contains("roll"))
                cls = "roll";
            else if (prev.Contains("lookup") || before.Contains("lookup"))
                cls = "lookup";
            else
                cls = "other";
            result.Add((cls, calls[i].Ctx));
        }
        return result;
    }

    /// <summary>Files read again after being written, in the order of their first re-read.</summary>
    public static List<string> RereadAfterWrite(Turn turn)
    {
        var written = new HashSet<string>();
        var reread = new List<string>();
        foreach (var (kind, target) in turn.Files)
        {
            if (kind == "write")
                written.Add(target);
            else if (written.Contains(target) && !reread.Contains(target))
                reread.Add(target);
        }
        return reread;
    }

    /// <summary>Lines on calls per message, game-state.md edits and full saves.</summary>
    static List<string> CallFigures(Session s)
    {
        var lines = new List<string>();
        double total = s.Ctx.Sum();
        var extra = new Dictionary<string, double>();
        foreach (var turn in s.Turns)
        {
            foreach (var (cls, ctx) in ExtraCalls(turn))
                extra[cls] = extra.GetValueOrDefault(cls) + ctx;
        }
        if (total != 0)
        {
            double extraTotal = extra.Values.Sum();
            lines.Add($"- **Context read by third and later calls of a message**: {Percent(extraTotal, total)} of all context read ({Whole(extraTotal)} of {Whole(total)}).");
            lines.AddRange(ExtraClasses.Where(c => extra.GetValueOrDefault(c.Class) != 0)
                .Select(c => $"  - {c.Label}: {Percent(extra[c.Class], total)}"));
        }
        var edits = s.Turns.Where(t => t.StateChars != 0).Select(t => t.StateChars).ToList();
        string editFigures = edits.Count > 0
            ? $", {edits.Sum()} chars sent; per editing message mean {FloorDiv(edits.Sum(), edits.Count)}, median {(long)(Median(edits) + 0.5)}"
            : "";
        lines.Add($"- **{Session.StateFile}**: read {s.Reads[Session.StateFile]} times. Edited in {edits.Count} player messages{editFigures}.");
        lines.Add($"- **Failed edits**: {s.FailedEdits}.");
        for (int n = 1; n <= s.Turns.Count; n++)
        {
            var turn = s.Turns[n - 1];
            if (turn.Skills.Contains(Session.SaveSkill))
            {
                string reread = string.Join(", ", RereadAfterWrite(turn));
                lines.Add($"- **Full save at player message {n}**: {turn.Calls.Count} calls; files read again after writing: {(reread.Length > 0 ? reread : "none")}.");
            }
        }
        return lines;
    }

    /// <summary>"2026-01-02T10:00:00.000Z" as "2026-01-02 10:00".</summary>
    public static string Minute(string stamp) => Slice(stamp, 0, 16).Replace("T", " ", StringComparison.Ordinal);

    /// <summary>
    /// The section of <c>stats.md</c> for one session, ending in a newline: when it ran, models, player messages and
    /// API calls, context growth, token totals, reply lengths, tool, skill and file counts, the figures from
    /// <see cref="CallFigures"/>, the five biggest context jumps and the first ten tool errors. A line is left out when
    /// its data does not exist (no context readings, no replies).
    /// </summary>
    public static string Stats(Session s)
    {
        var tok = s.Tokens();
        var replies = s.Turns.Where(t => t.ReplyChars != 0).Select(t => t.ReplyChars).ToList();
        var o = new List<string> { $"## Session {Slice(s.Id, 0, 8)}", "" };
        o.Add($"- **When**: {Minute(s.Start)} to {Minute(s.End)} UTC. Campaign: {s.Campaign()}. Log size {s.Size / 1024} KB.");
        o.Add($"- **Models**: {Table(s.Models)}");
        o.Add($"- **Player messages**: {s.Turns.Count} ({s.Turns.Count(t => Friction().IsMatch(t.Text))} marked \"!\", {s.Interrupts} interrupted). **API calls**: {s.Calls.Count}.");
        if (s.Ctx.Count > 0)
        {
            double growth = FloorDiv(s.Ctx[^1] - s.Ctx[0], Math.Max(1, s.Turns.Count));
            o.Add($"- **Context**: starts at {Whole(s.Ctx[0])} tokens, ends at {Whole(s.Ctx[^1])}, peak {Whole(s.Ctx.Max())}. About {Whole(growth)} tokens added per player message. Compactions: {s.Compactions}.");
        }
        o.Add($"- **Tokens**: output {Whole(tok.Output)}; input read from cache {Whole(tok.CacheRead)}, written to cache {Whole(tok.CacheCreation)}, uncached {Whole(tok.Input)}.");
        if (replies.Count > 0)
            o.Add($"- **Replies to the player**: average {FloorDiv(replies.Sum(), replies.Count)} chars, longest {replies.Max()}; {replies.Count(r => r > 1500)} of {replies.Count} over 1,500 chars.");
        o.Add($"- **Tool calls**: {Table(s.Tools, top: 12)}");
        o.Add($"- **Skills run**: {Table(s.Skills)}");
        o.Add($"- **Files written** (calls, chars sent): {Table(s.Writes, s.WriteChars)}");
        o.Add($"- **Files read** (calls, chars returned): {Table(s.Reads, s.ReadChars)}");
        string output = string.Join(", ", s.ResultChars.MostCommon(8).Select(p => $"{p.Key.Display} {p.Value}"));
        o.Add($"- **Tool output by tool** (chars): {(output.Length > 0 ? output : "none")}");
        o.AddRange(CallFigures(s));

        // Pairs of neighbouring messages where the context grew, the biggest growth first (equal growth in message order).
        var jumps = s.Turns.Zip(s.Turns.Skip(1), (a, b) => (Size: b.Ctx - a.Ctx, Turn: a))
            .Where(j => j.Size > 0).OrderByDescending(j => j.Size).Take(5).ToList();
        if (jumps.Count > 0)
        {
            o.Add("- **Biggest context jumps** (tokens added answering one message):");
            foreach (var (size, turn) in jumps)
            {
                string tools = string.Join(" ", turn.Tools.Take(10));
                o.Add($"  - +{Whole(size)} at {turn.Time}: \"{OneLine(turn.Text, 70)}\" -> {(tools.Length > 0 ? tools : "no tools")}");
            }
        }
        if (s.Errors.Count > 0)
        {
            o.Add($"- **Tool errors** ({s.Errors.Count}):");
            o.AddRange(s.Errors.Take(10).Select(e => $"  - {e}"));
        }
        return string.Join("\n", o) + "\n";
    }

    /// <summary>
    /// The contents of <c>&lt;id&gt;-turns.txt</c>: two lines for each player message. The first has a "!" if
    /// <see cref="Friction"/> matches, the message's number and time, the context size in thousands of tokens, and the
    /// message cut to 600 characters. The second has the reply's length and its tool calls.
    /// </summary>
    public static string TurnIndex(Session s)
    {
        var o = new List<string>();
        for (int n = 1; n <= s.Turns.Count; n++)
        {
            var turn = s.Turns[n - 1];
            string mark = Friction().IsMatch(turn.Text) ? "!" : " ";
            o.Add($"{mark}{PadLeft(Num(n), 3)} {turn.Time} ctx={Whole(FloorDiv(turn.Ctx, 1000))}k | {OneLine(turn.Text, 600)}");
            string tools = string.Join(" ", turn.Tools);
            o.Add($"       reply {turn.ReplyChars} chars; tools: {(tools.Length > 0 ? tools : "none")}");
        }
        return string.Join("\n", o) + "\n";
    }
}
