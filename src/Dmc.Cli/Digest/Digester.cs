using System.Globalization;
using Dmc.Maps;
using static Dmc.Digest.DigestText;

namespace Dmc.Digest;

/// <summary>
/// <c>list</c> and <c>digest</c> of <c>scripts/transcript_digest.py</c>: the same table, statistics and files.
/// </summary>
internal static class Digester
{
    public static int Run(DigestRequest request, Io io)
    {
        if (request.Command == "list")
            List(request, io);
        else
            Digest(request, io);
        return 0;
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

    static void List(DigestRequest request, Io io)
    {
        static string Row(string id, string start, string end, string kb, string messages, string peak, string campaign, string first) =>
            $"{PadRight(id, 9)} {PadRight(start, 17)} {PadRight(end, 17)} {PadLeft(kb, 6)} {PadLeft(messages, 5)} {PadLeft(peak, 8)}  {PadRight(campaign, 20)} {first}";

        // The script prints the heading first, so that a missing folder gives the heading and then the error. Here the
        // logs are read first: a failed list prints the error alone, one line.
        var sessions = Load(request);
        io.Out.WriteLine(Row("id", "start (UTC)", "end (UTC)", "KB", "msgs", "peak ctx", "campaign", "first message"));
        foreach (var s in sessions)
        {
            string first = s.Turns.Count > 0 ? OneLine(s.Turns[0].Text, 50) : "";
            io.Out.WriteLine(Row(Slice(s.Id, 0, 8), Report.Minute(s.Start), Report.Minute(s.End), Num(s.Size / 1024), Num(s.Turns.Count),
                Num(s.Ctx.Count > 0 ? s.Ctx.Max() : 0), s.Campaign(), first));
        }
    }

    static void Digest(DigestRequest request, Io io)
    {
        var sessions = Load(request).Where(s => s.Turns.Count > 0).ToList();
        if (sessions.Count == 0)
            throw new DmcException("No matching sessions with player messages.");

        string outDir = string.IsNullOrEmpty(request.Out) ? Path.Combine(Toolkit.Root, "adventures", ".transcript-review") : request.Out;
        Directory.CreateDirectory(outDir);
        string today = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var report = new List<string> { "# Transcript statistics", "", $"Generated {today} by dmc digest.", "" };
        foreach (var s in sessions)
        {
            report.Add(Report.Stats(s));
            string id = Slice(s.Id, 0, 8);
            File.WriteAllText(Path.Combine(outDir, $"{id}-turns.txt"), Report.TurnIndex(s), Utf8);
            File.WriteAllText(Path.Combine(outDir, $"{id}-transcript.md"), string.Join("\n", s.Lines) + "\n", Utf8);
        }
        string text = string.Join("\n", report);
        File.WriteAllText(Path.Combine(outDir, "stats.md"), text, Utf8);
        io.Out.Write(text);
        io.Out.WriteLine($"\nWrote stats.md and, for each session, <id>-turns.txt and <id>-transcript.md to {outDir}");
    }
}
