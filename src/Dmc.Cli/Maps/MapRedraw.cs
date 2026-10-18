namespace Dmc.Maps;

/// <summary>
/// What <c>dmc redraw</c> needs to know: which campaign is active, whether the map the player's page shows has changed
/// since it was drawn, and how to draw it again. Everything is found from the toolkit root and the files in the active
/// campaign's <c>maps/</c> folder.
/// </summary>
/// <remarks>
/// <para>
/// The page is <c>maps/live.html</c> with its picture in <c>maps/live-data.js</c>. A fight is kept in
/// <see cref="LiveFile"/>; a redraw draws it whenever it is newer than the picture, whatever the page showed before.
/// A map file DMC drew by hand (a place the player asked to see) is followed too while <c>live.json</c> is unchanged:
/// <see cref="Draw"/> records, in <see cref="SourceFile"/>, the name of the file it drew and the place file that file
/// names in <c>"base"</c>, so that telling whether anything changed takes only modification times, not a parse.
/// </para>
/// <para>
/// The record is two lines: the file's name, then the path of its base place relative to <c>maps/</c> (empty for a
/// place). A page with no record (drawn by an older <c>dmc</c>) follows <see cref="LiveFile"/> alone, and a change to
/// that scene's base place is not noticed until the scene itself changes.
/// </para>
/// <para>
/// A fight's first map is drawn here too, so that the player sees it after the reply and not before. When there is no
/// page yet, <see cref="LiveFile"/> is drawn if it exists. <see cref="NeedsOpening"/> says when the page must also be
/// opened in the browser: when <see cref="LiveFile"/> is drawn and the page did not show it (no page, or the record names
/// another file). A redraw of a page already showing the scene opens nothing, so no tab is opened twice.
/// </para>
/// </remarks>
public static class MapRedraw
{
    /// <summary>The file, next to <c>live.html</c>, that records what the page was last drawn from: the map file's name, then its base place's path.</summary>
    public const string SourceFile = "live-source.txt";

    /// <summary>The scene file a fight is kept in, the one a redraw always checks.</summary>
    public const string LiveFile = "live.json";

    /// <summary>
    /// The map file to draw again, or null when there is nothing to do: no active campaign, no page drawn yet (DMC has
    /// not shown a map and there is no <see cref="LiveFile"/>), or nothing newer than the picture. Checked in order:
    /// <see cref="LiveFile"/>; then the file the record names, and its base place. "Newer" is a strictly later
    /// modification time than <c>live-data.js</c>, so a file edited in the same clock tick as the drawing is not seen;
    /// the next edit is. With nothing changed this costs the reads of two tiny files and a few <c>stat</c> calls.
    /// </summary>
    /// <param name="root">The toolkit root, the folder that holds <c>adventures/</c>.</param>
    /// <returns>The full path of the map file, or null.</returns>
    public static string? Pending(string root)
    {
        string? campaign = PlainName(ReadLines(Path.Combine(root, "adventures", ".active-campaign")).FirstOrDefault());
        if (campaign is null)
            return null;
        string maps = Path.Combine(root, "adventures", campaign, "maps");
        string data = Path.Combine(maps, "live-data.js");
        string live = Path.Combine(maps, LiveFile);
        if (!File.Exists(data))
            return File.Exists(live) ? live : null;
        DateTime drawn = File.GetLastWriteTimeUtc(data);

        if (File.Exists(live) && File.GetLastWriteTimeUtc(live) > drawn)
            return live;

        string[] record = ReadLines(Path.Combine(maps, SourceFile)).ToArray();
        if (PlainName(record.FirstOrDefault()) is not { } name || name == LiveFile)
            name = LiveFile;
        string source = Path.Combine(maps, name);
        if (name != LiveFile && File.Exists(source) && File.GetLastWriteTimeUtc(source) > drawn)
            return source;
        if (File.Exists(source) && record.Length > 1 && record[1].Length > 0)
        {
            string basePlace = Path.Combine(maps, record[1]);
            if (File.Exists(basePlace) && File.GetLastWriteTimeUtc(basePlace) > drawn)
                return source;
        }
        return null;
    }

    /// <summary>
    /// Whether drawing this map file should also open the page in the browser: it is <see cref="LiveFile"/> and the
    /// page did not show it, because no page has been drawn or the record names another file. Must be asked before
    /// <see cref="Draw"/>, which rewrites the record. A page with no record is taken to show <see cref="LiveFile"/>.
    /// </summary>
    /// <param name="mapPath">The map file about to be drawn.</param>
    public static bool NeedsOpening(string mapPath)
    {
        if (Path.GetFileName(mapPath) != LiveFile)
            return false;
        string folder = Path.GetDirectoryName(Path.GetFullPath(mapPath)) ?? "";
        if (!File.Exists(Path.Combine(folder, "live-data.js")))
            return true;
        string? name = PlainName(ReadLines(Path.Combine(folder, SourceFile)).FirstOrDefault());
        return name is not null && name != LiveFile;
    }

    /// <summary>
    /// Checks and draws a map file: <c>live-data.js</c> and <c>live.html</c> beside it, and what the drawing came from
    /// in <see cref="SourceFile"/>. The record is written first, so the drawing is the newest of the files. Used by
    /// <c>dmc map</c> and <c>dmc redraw</c>, so both leave the page and the record in step.
    /// </summary>
    /// <param name="mapPath">The map file to draw.</param>
    /// <param name="png">Whether to write <c>live.png</c> too.</param>
    /// <returns>The checked map, the full path of the page, and the picture's path (null without <paramref name="png"/>).</returns>
    /// <exception cref="DmcException">The file cannot be read or does not pass <see cref="MapChecker"/>; nothing is written.</exception>
    public static (MapModel Model, string Page, string? Png) Draw(string mapPath, bool png)
    {
        var model = MapChecker.Check(MapChecker.LoadMap(mapPath));
        string svg = SvgDrawing.Render(model);
        string folder = Path.GetDirectoryName(Path.GetFullPath(mapPath)) ?? "";
        string basePlace = MapChecker.BaseFile(mapPath) is { } b ? Path.GetRelativePath(folder, b).Replace('\\', '/') : "";
        MapPage.WriteText(Path.Combine(folder, SourceFile), Path.GetFileName(mapPath) + "\n" + basePlace + "\n");
        MapPage.WriteText(Path.Combine(folder, "live-data.js"), MapPage.Data(DateTime.Now, model.Title, svg));
        string page = Path.Combine(folder, "live.html");
        MapPage.WriteText(page, MapPage.Html);
        string? picture = null;
        if (png)
        {
            picture = Path.Combine(folder, "live.png");
            MapPng.Write(svg, picture);
        }
        return (model, page, picture);
    }

    /// <summary>The non-blank lines of a file, trimmed; none when the file is missing or unreadable.</summary>
    static IEnumerable<string> ReadLines(string path)
    {
        try
        {
            return File.ReadLines(path).Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    /// <summary>The text when it is one file or folder name, with no separator and not <c>.</c> or <c>..</c>; otherwise null, so a record can never point outside <c>maps/</c>.</summary>
    static string? PlainName(string? name) =>
        name is { Length: > 0 } && name is not ("." or "..") && name.IndexOfAny(['/', '\\', ':']) < 0 ? name : null;
}
