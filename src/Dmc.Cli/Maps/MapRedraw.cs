using System.Text.Json;

namespace Dmc.Maps;

/// <summary>
/// What <c>dmc redraw</c> needs to know: which campaign is active, which map file the player's page shows, and
/// whether that file has changed since it was drawn. Everything is found from the toolkit root and the files in the
/// active campaign's <c>maps/</c> folder.
/// </summary>
/// <remarks>
/// The page is <c>maps/live.html</c> with its picture in <c>maps/live-data.js</c>, whichever map file was drawn last:
/// <c>live.json</c>, or a file DMC named, as a fight is sometimes kept in another file. <see cref="Draw"/> records
/// that file's name in <see cref="SourceFile"/> next to the page. A page drawn before the record existed (by dmc
/// 0.1.2) was drawn from <c>live.json</c>, so that is the fallback.
/// </remarks>
public static class MapRedraw
{
    /// <summary>The file, next to <c>live.html</c>, that holds the name of the map file the page was last drawn from.</summary>
    public const string SourceFile = "live-source.txt";

    /// <summary>The map file assumed when <see cref="SourceFile"/> is missing: what dmc 0.1.2 always drew.</summary>
    public const string DefaultSource = "live.json";

    /// <summary>
    /// The map file to draw again, or null when there is nothing to do: no active campaign, no page drawn yet (DMC has
    /// not shown a map, so none is changed behind its back), no such source file, or the page is newer than the file
    /// and its base place. "Newer" is a strictly later modification time, so a file edited in the same clock tick as
    /// the drawing is not seen; the next edit is.
    /// </summary>
    /// <param name="root">The toolkit root, the folder that holds <c>adventures/</c>.</param>
    /// <returns>The full path of the map file, or null.</returns>
    public static string? Pending(string root)
    {
        string? campaign = ActiveCampaign(root);
        if (campaign is null)
            return null;
        string maps = Path.Combine(root, "adventures", campaign, "maps");
        string data = Path.Combine(maps, "live-data.js");
        if (!File.Exists(data))
            return null;

        string source = Path.Combine(maps, PlainName(ReadLine(Path.Combine(maps, SourceFile))) ?? DefaultSource);
        if (!File.Exists(source))
            return null;

        DateTime drawn = File.GetLastWriteTimeUtc(data);
        if (File.GetLastWriteTimeUtc(source) > drawn)
            return source;
        string? basePlace = BasePlace(source);
        return basePlace is not null && File.Exists(basePlace) && File.GetLastWriteTimeUtc(basePlace) > drawn ? source : null;
    }

    /// <summary>
    /// Checks and draws a map file: <c>live-data.js</c> and <c>live.html</c> beside it, and the name of the file in
    /// <see cref="SourceFile"/>. The record is written first, so the drawing is the newest of the files. Used by
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
        MapPage.WriteText(Path.Combine(folder, SourceFile), Path.GetFileName(mapPath) + "\n");
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

    /// <summary>The name in <c>adventures/.active-campaign</c> (its first line), or null when the file is missing or empty, or the name is not a plain folder name.</summary>
    /// <param name="root">The toolkit root.</param>
    static string? ActiveCampaign(string root) => PlainName(ReadLine(Path.Combine(root, "adventures", ".active-campaign")));

    /// <summary>The first non-blank line of a file, trimmed; null when the file is missing, unreadable or blank.</summary>
    static string? ReadLine(string path)
    {
        try
        {
            return File.ReadLines(path).Select(l => l.Trim()).FirstOrDefault(l => l.Length > 0);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>The text when it is one file or folder name, with no separator and not <c>.</c> or <c>..</c>; otherwise null, so a record can never point outside <c>maps/</c>.</summary>
    static string? PlainName(string? name) =>
        name is { Length: > 0 } && name is not ("." or "..") && name.IndexOfAny(['/', '\\', ':']) < 0 ? name : null;

    /// <summary>The place file a scene names in <c>"base"</c>, beside the scene; null when it names none or the file is not a JSON object.</summary>
    /// <param name="scene">The full path of the scene file.</param>
    static string? BasePlace(string scene)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllBytes(scene));
            if (document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("base", out var name) && name.ValueKind == JsonValueKind.String
                && PlainName(name.GetString()) is { } place)
                return Path.Combine(Path.GetDirectoryName(scene) ?? "", place + ".json");
        }
        catch (Exception e) when (e is JsonException or IOException or UnauthorizedAccessException)
        {
        }
        return null;
    }
}
