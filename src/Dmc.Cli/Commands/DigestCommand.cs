using Dmc.Digest;

namespace Dmc.Commands;

/// <summary>
/// <c>dmc digest list|show [SESSION ...] [--campaign NAME] [--after YYYY-MM-DD] [--logs DIR] [--out DIR]</c>: replaced
/// <c>scripts/transcript_digest.py</c>. Claude Code records every session of this project as a JSONL file that is far too big
/// to read; this turns the sessions into a readable transcript plus the numbers needed to judge how the toolkit's prompts
/// performed, for the <c>review-transcript</c> skill.
/// </summary>
/// <remarks>
/// Arguments are read the way Python's <c>argparse</c> reads them (see <see cref="DigestArguments"/>), with its messages, but
/// a mistake is one line and exit code 2, never the usage text.
/// </remarks>
public sealed class DigestCommand : ICommand
{
    /// <inheritdoc />
    public string Name => "digest";

    /// <inheritdoc />
    public string Summary => "condense Claude Code session logs for review: dmc digest list|show [SESSION ...] [--campaign NAME] [--after YYYY-MM-DD] [--logs DIR] [--out DIR]";

    /// <summary>The text <c>--help</c> prints: usage, what the output files are, and the units used in them.</summary>
    // A checkout with CRLF line endings makes the literal CRLF too; the program always prints "\n".
    static readonly string Help = """
usage: dmc digest list [--campaign NAME] [--after YYYY-MM-DD] [--logs DIR]
       dmc digest show [SESSION ...] [--campaign NAME] [--after YYYY-MM-DD] [--logs DIR] [--out DIR]

Condenses Claude Code session logs into something small enough to review. Claude Code records every session of
this project as a JSONL file under ~/.claude/projects/<project>/ (or $CLAUDE_CONFIG_DIR/projects/<project>/).
Those files are far too big to read directly. "show" turns them into a readable transcript plus the numbers
needed to judge how the toolkit's prompts performed. The review-transcript skill uses it.

  SESSION is the start of a session id, as shown by "list". With no SESSION, "show" takes every session that
  touched the campaign (or every session).

  --campaign  only sessions that touched adventures/<NAME>/
  --after     only sessions that ended on or after this date (YYYY-MM-DD, UTC)
  --logs      folder of session .jsonl files (default: this project's Claude Code logs)
  --out       output folder of "show" (default adventures/.transcript-review, overwritten on each run, not in the repository)

Output of "show":
  DIR/stats.md              the numbers for each session (also printed)
  DIR/<id>-turns.txt        one line per player message: time, context size, the message, then the size and tool
                            calls of the reply. Lines marked "!" may show friction.
  DIR/<id>-transcript.md    the whole session in order, with tool calls and results cut down to a line each

Times are UTC. "ctx" is the number of tokens sent to the model on that call.
""".Replace("\r\n", "\n");

    /// <summary>Parses the arguments with <see cref="DigestArguments.Parse"/> and hands the request to <see cref="Digester.Run"/>; <c>--help</c> prints <see cref="Help"/> instead.</summary>
    /// <param name="args">The arguments after <c>digest</c>.</param>
    /// <param name="io">Standard output gets the report, or the help text.</param>
    /// <returns>
    /// 0 on success or <c>--help</c>. A usage mistake throws <see cref="DmcException"/> with exit code 2. The other
    /// failures exit 1: a missing log folder, no matching session with a player message, a log that is not valid UTF-8,
    /// a record of a shape the digest cannot read (see <see cref="Digester.Run"/>, which lists them).
    /// </returns>
    public int Run(string[] args, Io io)
    {
        var request = DigestArguments.Parse(args);
        if (request is null)
        {
            io.Out.WriteLine(Help);
            return 0;
        }
        return Digester.Run(request, io);
    }
}
