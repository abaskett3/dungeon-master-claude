using System.Globalization;
using Dmc.Maps;
using static Dmc.Digest.DigestText;

namespace Dmc.Digest;

/// <summary>
/// <c>list</c> and <c>show</c> (the script's <c>digest</c>) of <c>scripts/transcript_digest.py</c>: the same table,
/// statistics and files.
/// </summary>
internal static class Digester
{
    /// <summary>Runs the subcommand the request names.</summary>
    /// <param name="request">What to do, from <see cref="DigestArguments.Parse"/>.</param>
    /// <param name="io">Standard output gets the table or the statistics.</param>
    /// <returns>Always 0; a failure throws instead.</returns>
    /// <exception cref="DmcException">
    /// Every one has exit code 1. The log folder is missing; <c>show</c> matched no session that has a player message; or
    /// a log is unreadable, and the message names its file. <see cref="Session"/> finds a log that is not valid UTF-8
    /// (<c>name: not valid UTF-8</c>) or a record whose shape the digest cannot read (<c>name line N: ...</c>) when the
    /// sessions are loaded. A shape error that only shows when a session's figures are worked out (a token count that is
    /// not a finite number, such as NaN or Infinity, which a log can spell, or a token count that is not a number in a place
    /// the loading does not add up) is reported as <c>name: ...</c>, without a line number, by <see cref="Reporting"/>.
    /// Either way one bad log stops the whole run, and nothing is printed or written first: <c>list</c> builds every row,
    /// and <c>show</c> builds the text of every file, before the first line goes out or the first file is created.
    /// </exception>
    /// <exception cref="InvalidOperationException">The request names a subcommand that does not exist (a programming error).</exception>
    public static int Run(DigestRequest request, Io io)
    {
        switch (request.Command)
        {
            case DigestArguments.List:
                List(request, io);
                break;
            case DigestArguments.Show:
                Show(request, io);
                break;
            default:
                throw new InvalidOperationException($"dmc digest has no subcommand {request.Command}");
        }
        return 0;
    }

    /// <summary>
    /// Works out something from one session's log (a table row, its statistics, its files' text) and reports a log whose
    /// shape the digest could only judge now as a <see cref="DmcException"/> naming the file, as <see cref="Session"/> does
    /// for what it finds while reading. The line is not known here, so the form is <c>name: message</c>, like the
    /// "not valid UTF-8" error.
    /// </summary>
    /// <typeparam name="T">What is worked out.</typeparam>
    /// <param name="session">The session the work is about; its file name goes in the message.</param>
    /// <param name="work">The work. It must print and write nothing, so that a failure leaves no output behind.</param>
    /// <returns>What <paramref name="work"/> returns.</returns>
    /// <exception cref="DmcException"><paramref name="work"/> found a <see cref="LogShapeException"/>.</exception>
    static T Reporting<T>(Session session, Func<T> work)
    {
        try
        {
            return work();
        }
        catch (LogShapeException e)
        {
            throw new DmcException($"{Path.GetFileName(session.Path)}: {e.Message}");
        }
    }

    /// <summary>Python's string order: by code point, not by UTF-16 unit.</summary>
    static int CompareCodePoints(string a, string b)
    {
        int[] x = Py.CodePoints(a), y = Py.CodePoints(b);
        for (int i = 0; i < Math.Min(x.Length, y.Length); i++)
        {
            if (x[i] != y[i])
                return x[i] < y[i] ? -1 : 1;
        }
        return x.Length.CompareTo(y.Length);
    }

    /// <summary>The sessions the request asks for, oldest first (by the time the log was last written).</summary>
    /// <param name="request">Which sessions: by id prefix, by campaign touched, and by end date, each only if given; and which folder.</param>
    /// <exception cref="DmcException">The log folder does not exist.</exception>
    static List<Session> Load(DigestRequest request)
    {
        string folder = string.IsNullOrEmpty(request.Logs) ? LogFolder.Default() : request.Logs;
        if (!Directory.Exists(folder))
            throw new DmcException($"No session logs found at {folder}. Pass --logs DIR.");

        var paths = Directory.EnumerateFiles(folder)
            .Where(f => Path.GetFileName(f).EndsWith(".jsonl", StringComparison.Ordinal))
            .OrderBy(f => File.GetLastWriteTimeUtc(f)).ThenBy(f => f, StringComparer.Ordinal);
        var sessions = new List<Session>();
        foreach (string path in paths)
        {
            if (request.Sessions.Count > 0 && !request.Sessions.Any(w => Path.GetFileName(path).StartsWith(w, StringComparison.Ordinal)))
                continue;
            var session = new Session(path);
            if (!string.IsNullOrEmpty(request.Campaign) && session.Campaigns[request.Campaign] == 0)
                continue;
            if (!string.IsNullOrEmpty(request.After) && CompareCodePoints(Slice(session.End, 0, 10), request.After) < 0)
                continue;
            sessions.Add(session);
        }
        return sessions;
    }

    /// <summary>
    /// <c>list</c>: one row per session (short id, start and end, size in KB, player messages, peak context,
    /// campaign, and the start of the first message), under a heading row.
    /// </summary>
    static void List(DigestRequest request, Io io)
    {
        static string Row(string id, string start, string end, string kb, string messages, string peak, string campaign, string first) =>
            $"{PadRight(id, 9)} {PadRight(start, 17)} {PadRight(end, 17)} {PadLeft(kb, 6)} {PadLeft(messages, 5)} {PadLeft(peak, 8)}  {PadRight(campaign, 20)} {first}";

        // The script prints the heading first, so that a missing folder gives the heading and then the error. Here the
        // logs are read, and every row is made, first: a failed list prints the error alone, one line.
        var rows = Load(request).Select(s => Reporting(s, () =>
        {
            string first = s.Turns.Count > 0 ? OneLine(s.Turns[0].Text, 50) : "";
            return Row(Slice(s.Id, 0, 8), Report.Minute(s.Start), Report.Minute(s.End), Num(s.Size / 1024), Num(s.Turns.Count),
                Whole(s.Ctx.Count > 0 ? s.Ctx.Max() : 0), s.Campaign(), first);
        })).ToList();
        io.Out.WriteLine(Row("id", "start (UTC)", "end (UTC)", "KB", "msgs", "peak ctx", "campaign", "first message"));
        foreach (string row in rows)
            io.Out.WriteLine(row);
    }

    /// <summary>
    /// <c>show</c>: for each session that has player messages, writes <c>&lt;id&gt;-turns.txt</c> and
    /// <c>&lt;id&gt;-transcript.md</c> to the output folder, then <c>stats.md</c> with every session's statistics,
    /// which it also prints. Files from an earlier run with the same names are overwritten. All the text is made first, so
    /// a log that cannot be reported on (see <see cref="Reporting"/>) stops the run before the output folder is made or
    /// any file is written.
    /// </summary>
    /// <exception cref="DmcException">No session matched; or a log cannot be reported on; or the output folder or a file cannot be written.</exception>
    static void Show(DigestRequest request, Io io)
    {
        var sessions = Load(request).Where(s => s.Turns.Count > 0).ToList();
        if (sessions.Count == 0)
            throw new DmcException("No matching sessions with player messages.");

        string outDir = string.IsNullOrEmpty(request.Out) ? Path.Combine(Toolkit.Root, "adventures", ".transcript-review") : request.Out;
        string today = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var report = new List<string> { "# Transcript statistics", "", $"Generated {today} by dmc digest.", "" };
        var files = new List<(string Name, string Content)>();
        foreach (var s in sessions)
        {
            var (stats, turns) = Reporting(s, () => (Report.Stats(s), Report.TurnIndex(s)));
            report.Add(stats);
            string id = Slice(s.Id, 0, 8);
            files.Add(($"{id}-turns.txt", turns));
            files.Add(($"{id}-transcript.md", string.Join("\n", s.Lines) + "\n"));
        }
        string text = string.Join("\n", report);
        Directory.CreateDirectory(outDir);
        foreach (var (name, content) in files)
            File.WriteAllText(Path.Combine(outDir, name), content, Io.Utf8Replacing);
        File.WriteAllText(Path.Combine(outDir, "stats.md"), text, Io.Utf8Replacing);
        io.Out.Write(text);
        io.Out.WriteLine($"\nWrote stats.md and, for each session, <id>-turns.txt and <id>-transcript.md to {outDir}");
    }
}
