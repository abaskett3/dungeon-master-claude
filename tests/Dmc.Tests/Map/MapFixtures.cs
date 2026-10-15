using System.Text;
using Dmc.Maps;

namespace Dmc.Tests.Map;

/// <summary>
/// The maps the tests of <c>tests/test_map_render.py</c> start from (<c>PLACE</c>, <c>SCENE</c>), and short ways to
/// write a JSON value in a test: <see cref="Obj"/>, <see cref="Arr"/> and <see cref="Tokens"/>.
/// </summary>
public static class MapFixtures
{
    /// <summary>A JSON object. A whole number may be an <c>int</c>; it is stored as a <c>long</c>, as the reader gives it.</summary>
    public static PyDict Obj(params (string Key, object? Value)[] items)
    {
        var dict = new PyDict();
        foreach (var (key, value) in items)
            dict[key] = Normalize(value);
        return dict;
    }

    public static List<object?> Arr(params object?[] items) => items.Select(Normalize).ToList();

    static object? Normalize(object? value) => value switch
    {
        int i => (long)i,
        string[] strings => strings.Cast<object?>().ToList(),
        _ => value,
    };

    /// <summary>The goblin hall: the place file of the Python tests.</summary>
    public static PyDict Place() => Obj(
        ("title", "Goblin hall"),
        ("grid", Arr("##########",
                     "#........#",
                     "+...o...>#",
                     "#........#",
                     "##########")),
        ("labels", Arr(Obj(("at", "I3"), ("text", "to the cellar")))));

    /// <summary>Two creatures on the hall: the scene file of the Python tests.</summary>
    public static PyDict Scene() => Obj(
        ("base", "goblin-hall"),
        ("tokens", SceneTokens()));

    public static List<object?> SceneTokens() => Arr(
        Obj(("id", "@"), ("name", "Thorin"), ("kind", "pc"), ("at", "B2")),
        Obj(("id", "1"), ("name", "Goblin boss"), ("kind", "enemy"), ("at", "E2")));

    /// <summary>A valid map with the given keys replaced, passed through <see cref="MapChecker.Check"/> (the Python <c>checked</c>).</summary>
    public static MapModel Checked(params (string Key, object? Value)[] changes)
    {
        var data = Place();
        if (changes.Any(c => c.Key == "grid") && changes.All(c => c.Key != "labels"))
            data["labels"] = Arr(); // the hall's label may not fit another grid
        foreach (var (key, value) in changes)
            data[key] = Normalize(value);
        return MapChecker.Check(data);
    }

    /// <summary>
    /// Tokens numbered "0", "1", ... each given as a cell and the other keys (the Python <c>tokens(...)</c>).
    /// </summary>
    public static List<object?> Tokens(params (string At, (string Key, object? Value)[] Extra)[] specs) =>
        specs.Select((spec, n) =>
        {
            var token = Obj(("id", n.ToString()), ("at", spec.At));
            foreach (var (key, value) in spec.Extra)
                token[key] = Normalize(value);
            return (object?)token;
        }).ToList();

    public static (string, (string, object?)[]) Spec(string at, params (string, object?)[] extra) => (at, extra);

    /// <summary>The text in the SVG, with the tags gone: parsing it also proves that the picture is well formed.</summary>
    public static string SvgText(string svg) =>
        string.Join(" ", System.Xml.Linq.XElement.Parse(svg).DescendantNodes().OfType<System.Xml.Linq.XText>().Select(t => t.Value));

    /// <summary>The same JSON as text, for a file on disk.</summary>
    public static string Json(object? value) => PyJson.Dumps(value);

    public static void WriteFile(string folder, string name, object? data) =>
        File.WriteAllText(Path.Combine(folder, name), data as string ?? Json(data), new UTF8Encoding(false));
}
