using System.Numerics;

namespace Dmc.Maps;

/// <summary>
/// Reads a map file and checks it, with the messages of <c>map_render.py</c>. A file is a JSON object whose values
/// are used as Python uses them (a number written <c>5.0</c> is the scale 5; a name that is not text is written the
/// way Python would write it), so a file means the same to <c>dmc map</c> as it did to the script.
/// </summary>
public static class MapChecker
{
    // ---------- reading ----------

    /// <summary>Read a JSON object from a file. A scale in it must be a standard one; the error names the file.</summary>
    /// <param name="path">The file to read: UTF-8 text, which must hold a JSON object.</param>
    /// <exception cref="DmcException">The file is missing, unreadable, not valid UTF-8 or JSON, not an object, or has a <c>cell_ft</c> that is not a standard scale.</exception>
    public static PyDict LoadJson(string path)
    {
        byte[] bytes;
        try
        {
            bytes = File.ReadAllBytes(path);
        }
        catch (Exception e) when (e is FileNotFoundException or DirectoryNotFoundException or ArgumentException)
        {
            throw new DmcException($"no such file: {path}");
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            throw new DmcException($"cannot read {path}: {e.Message}");
        }

        string text;
        try
        {
            text = new System.Text.UTF8Encoding(false, throwOnInvalidBytes: true).GetString(bytes);
        }
        catch (System.Text.DecoderFallbackException)
        {
            throw new DmcException($"{path} is not valid UTF-8 text.");
        }
        // Python reads the file in text mode, where "\r\n" and a lone "\r" are both a newline.
        text = text.Replace("\r\n", "\n").Replace('\r', '\n');

        object? data;
        try
        {
            data = PyJson.Parse(text);
        }
        catch (JsonDecodeError e)
        {
            throw new DmcException($"{path} is not valid JSON: {e.Message}");
        }
        if (data is not PyDict dict)
            throw new DmcException($"{path} must hold a JSON object.");
        if (dict.TryGetValue("cell_ft", out var cellFt))
            CheckScale(cellFt, path);
        return dict;
    }

    /// <summary>Reject a scale that is not standard. <paramref name="where"/> names the file that holds it.</summary>
    /// <param name="cellFt">The <c>cell_ft</c> value from the file, of any JSON type. Any number equal to a standard scale counts, so <c>5.0</c> is 5; <c>true</c> is not accepted.</param>
    /// <param name="where">What to call the source in the error message.</param>
    /// <returns>The scale, one of <see cref="MapRules.Scales"/>.</returns>
    /// <exception cref="DmcException">The value is not a standard scale; the message lists them.</exception>
    public static int CheckScale(object? cellFt, string where = "the map")
    {
        if (Py.TryExactInt(cellFt, out var number) && cellFt is not bool)
        {
            foreach (int scale in MapRules.Scales)
            {
                if (number == scale)
                    return scale;
            }
        }
        string allowed = string.Join(", ", MapRules.Scales.Select(s =>
            MapRules.IsOverland(s) ? $"{s} ({Distances.FormatDistance(1, s)})" : s.ToString()));
        throw new DmcException($"{where}: \"cell_ft\" must be a standard map scale in feet ({allowed}), "
                               + $"not {PyJson.Dumps(cellFt)}: every map is drawn to scale.");
    }

    /// <summary>
    /// Read a place or scene file; a scene's "base" is merged underneath it. A scene is a file with a <c>base</c>, the
    /// name of a place file in the same folder (without <c>.json</c>): every key of the scene replaces the place's key of
    /// the same name, except that the place's <c>initiative</c> and <c>current</c> are dropped, because a turn order
    /// belongs to the scene that holds it. A scene may repeat its place's scale but not change it, and a place cannot
    /// itself have a base.
    /// </summary>
    /// <param name="path">The place or scene file.</param>
    /// <returns>The merged object, which has a <c>grid</c>. Nothing else about it is checked yet: see <see cref="Check"/>.</returns>
    /// <exception cref="DmcException">The file or its base cannot be read, the base is itself a scene or does not exist, the scale disagrees, or there is no grid.</exception>
    public static PyDict LoadMap(string path)
    {
        var data = LoadJson(path);
        data.TryGetValue("base", out var baseValue);
        if (Py.Truthy(baseValue))
        {
            if (baseValue is not string baseName)
                throw new DmcException($"{path}: \"base\" must be the name of a place file, not {PyJson.Dumps(baseValue)}.");
            string basePath = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(path)) ?? "", baseName + ".json");
            if (!File.Exists(basePath) && !Directory.Exists(basePath))
                throw new DmcException($"\"base\": \"{baseName}\" names {basePath}, which does not exist.");
            var merged = LoadJson(basePath);
            if (merged.TryGetValue("base", out var chained) && Py.Truthy(chained))
                throw new DmcException($"{basePath} is a scene (it has its own \"base\"); base must be a place file.");
            merged.TryGetValue("cell_ft", out var baseFt);
            if (!merged.ContainsKey("cell_ft"))
                baseFt = 5L;
            if (data.TryGetValue("cell_ft", out var ownFt))
            {
                bool same = Py.TryExactInt(ownFt, out var own) && Py.TryExactInt(baseFt, out var theirs) && own == theirs;
                if (!same)
                {
                    throw new DmcException($"{path} sets \"cell_ft\": {PyJson.Dumps(ownFt)}, but its base \"{baseName}\" "
                                           + $"is drawn at {PyJson.Dumps(baseFt)}: a scene cannot change its place's scale.");
                }
            }
            // A fight's turn order belongs to the scene that holds it, never to the place it stands on.
            merged.Remove("initiative");
            merged.Remove("current");
            foreach (var pair in data)
            {
                if (pair.Key != "base")
                    merged[pair.Key] = pair.Value;
            }
            data = merged;
        }
        if (!data.ContainsKey("grid"))
            throw new DmcException($"{path} has no \"grid\" and no \"base\" to take one from.");
        return data;
    }

    // ---------- checking ----------

    /// <summary>The error for a map that does not pass the check: a plain <see cref="DmcException"/> with exit code 1.</summary>
    static DmcException Fail(string message) => new(message);

    /// <summary>What <c>for x in value</c> gives in Python for a value that came from JSON.</summary>
    /// <param name="value">A list (its items), an object (its keys) or a string (its characters). Anything else is an error.</param>
    /// <param name="what">The field's name, for the error message.</param>
    /// <exception cref="DmcException">The value cannot be looped over.</exception>
    static IEnumerable<object?> Items(object? value, string what) => value switch
    {
        List<object?> list => list,
        PyDict dict => dict.Keys,
        string text => Py.CodePoints(text).Select(c => (object?)Py.FromCodePoint(c)),
        _ => throw Fail($"\"{what}\" must be a list."),
    };

    /// <summary>Python's <c>dict.get(key, fallback)</c>. A key whose value is a JSON null gives null, not the fallback.</summary>
    static object? Get(PyDict dict, string key, object? fallback) => dict.TryGetValue(key, out var value) ? value : fallback;

    /// <summary>
    /// Resolve every symbol used in the grid to its terrain, name and colour: the built-in meaning (<see cref="MapRules.Builtin"/>),
    /// overridden by the file's <c>legend</c>, whose entry is a name or an object with <c>name</c>, <c>as</c> and <c>color</c>.
    /// A symbol that is neither built in nor in the legend is an error, and a legend entry with no terrain is a <c>feature</c>.
    /// </summary>
    /// <param name="data">The map object.</param>
    /// <param name="grid">The rows as code points. Spaces are empty space and need no entry.</param>
    /// <exception cref="DmcException">The legend is malformed or lacks a symbol the grid uses.</exception>
    static SortedDictionary<int, LegendEntry> BuildLegend(PyDict data, int[][] grid)
    {
        object? custom = Get(data, "legend", new PyDict());
        if (custom is not PyDict customLegend)
            throw Fail("\"legend\" must be an object of symbol: name.");
        foreach (string symbol in customLegend.Keys)
        {
            if (Py.Len(symbol) != 1 || symbol == " ")
                throw Fail($"legend key \"{symbol}\" must be a single character, not a space.");
        }

        var legend = new SortedDictionary<int, LegendEntry>();
        foreach (int symbol in grid.SelectMany(row => row).Where(c => c != ' ').Distinct().Order())
        {
            string symbolText = Py.FromCodePoint(symbol);
            object? terrain = null, name = null, color = null;
            if (MapRules.Builtin.TryGetValue(symbol, out var builtin))
                (terrain, name) = (builtin.Terrain, builtin.Name);
            customLegend.TryGetValue(symbolText, out var entry);
            if (entry is string entryName)
            {
                name = entryName;
            }
            else if (entry is PyDict entryDict)
            {
                name = Get(entryDict, "name", name);
                terrain = Get(entryDict, "as", terrain);
                color = Get(entryDict, "color", null);
            }
            else if (entry is not null)
            {
                throw Fail($"legend entry for \"{symbolText}\" must be a name or an object.");
            }

            if (terrain is null)
            {
                if (entry is null)
                    throw Fail($"the grid uses \"{symbolText}\", which is not a built-in symbol and is not in \"legend\".");
                terrain = "feature";
            }
            if (terrain is not string terrainName || !MapRules.Terrains.Contains(terrainName))
            {
                throw Fail($"legend entry for \"{symbolText}\": unknown terrain \"{Py.Str(terrain)}\". "
                           + $"Use one of: {string.Join(", ", MapRules.Terrains.Order(StringComparer.Ordinal))}.");
            }
            if (!Py.Truthy(name))
                throw Fail($"legend entry for \"{symbolText}\" needs a name.");
            legend[symbol] = new LegendEntry(terrainName, Py.Str(name), Py.Truthy(color) ? Py.Str(color) : null);
        }
        return legend;
    }

    /// <summary>
    /// Validate the map and return it in the form the drawing code uses. Checks the grid (rectangular, every symbol
    /// known), the scale and the <c>north</c> direction; each creature (id of one or two characters and unique, a
    /// known kind, a size from 1 to 4, a cell on the map and not in a wall or empty space, and, at 5 ft, a cell of its
    /// own); each label; that <c>edges</c> has only top, bottom, left and right; and, when drawing, the turn order
    /// (<c>initiative</c> and <c>current</c>).
    /// </summary>
    /// <param name="data">The object from <see cref="LoadMap"/>.</param>
    /// <param name="turnOrder">
    /// Whether to check the turn order. Drawing does; measuring a distance does not depend on it, so a stale order
    /// never blocks <c>--measure</c>.
    /// </param>
    /// <exception cref="DmcException">The first thing found wrong, worded as <c>map_render.py</c> worded it.</exception>
    public static MapModel Check(PyDict data, bool turnOrder = true)
    {
        if (!data.TryGetValue("grid", out var gridValue))
            throw Fail("the map has no \"grid\".");
        if (gridValue is not List<object?> gridRows || gridRows.Count == 0 || !gridRows.All(r => r is string))
            throw Fail("\"grid\" must be a list of strings, one per row.");
        int[][] grid = gridRows.Select(r => Py.CodePoints((string)r!)).ToArray();
        int cols = grid[0].Length;
        if (cols == 0)
            throw Fail("the first grid row is empty.");
        for (int n = 0; n < grid.Length; n++)
        {
            if (grid[n].Length != cols)
            {
                throw Fail($"grid row {n + 1} is {grid[n].Length} cells wide but row 1 is {cols}. "
                           + "Every row must be the same length (pad with spaces).");
            }
        }
        int rows = grid.Length;
        var legend = BuildLegend(data, grid);

        string TerrainAt(int col, int row)
        {
            int symbol = grid[row][col];
            return symbol == ' ' ? "void" : legend[symbol].Terrain;
        }

        // load_json has already checked a scale by file name; this one guards direct callers.
        int cellFt = CheckScale(Get(data, "cell_ft", 5L));

        object? northValue = Get(data, "north", "up");
        string? north = null;
        if (northValue is not null)
        {
            if (northValue is not string northName || !MapRules.NorthTurn.ContainsKey(northName))
                throw Fail("\"north\" must be \"up\", \"down\", \"left\", \"right\" or null.");
            north = northName;
        }

        var tokens = new List<Token>();
        var seenIds = new HashSet<string>();
        var taken = new Dictionary<(int, int), string>();
        foreach (var item in Items(Get(data, "tokens", new List<object?>()), "tokens"))
        {
            if (item is not PyDict token || !token.ContainsKey("id") || !token.ContainsKey("at"))
                throw Fail("every token needs at least \"id\" and \"at\".");
            string id = Py.Str(token["id"]);
            int idLength = Py.Len(id);
            if (idLength is < 1 or > 2)
                throw Fail($"token id \"{id}\" must be one or two characters.");
            if (!seenIds.Add(id))
                throw Fail($"two tokens share the id \"{id}\".");
            string name = Py.Str(Get(token, "name", id));
            object? kindValue = Get(token, "kind", "neutral");
            if (kindValue is not string kind || MapRules.Kinds.All(k => k.Kind != kind))
            {
                throw Fail($"token {id}: kind \"{Py.Str(kindValue)}\" must be one of "
                           + $"{string.Join(", ", MapRules.Kinds.Select(k => k.Kind))}.");
            }
            object? sizeValue = Get(token, "size", 1L);
            if (!Py.TryExactInt(sizeValue, out var sizeNumber) || sizeNumber < 1 || sizeNumber > 4)
                throw Fail($"token {id}: size must be 1, 2, 3 or 4 (cells across).");
            int size = (int)sizeNumber;
            int span = MapRules.IsCoarse(cellFt) ? 1 : size; // cells across on this map
            var (col, row) = Cells.Parse(token["at"], cols, rows, $"token {id} ({name})");
            var cells = new List<(int, int)>();
            for (int dy = 0; dy < span; dy++)
            {
                for (int dx = 0; dx < span; dx++)
                    cells.Add((col + dx, row + dy));
            }
            foreach (var (c, r) in cells)
            {
                if (c >= cols || r >= rows)
                {
                    throw Fail($"token {id} ({name}) at {Cells.CellName(col, row)} with size {Py.Str(sizeValue)} "
                               + "runs off the map.");
                }
                string terrain = TerrainAt(c, r);
                if (terrain is "wall" or "void")
                {
                    throw Fail($"token {id} ({name}) is on {Cells.CellName(c, r)}, which is "
                               + $"{(terrain == "void" ? "empty space" : "a wall")}.");
                }
                if (taken.TryGetValue((c, r), out var other) && !MapRules.IsCoarse(cellFt))
                    throw Fail($"tokens {other} and {id} are both on {Cells.CellName(c, r)}.");
                taken[(c, r)] = id;
            }
            tokens.Add(new Token(id, name, kind, size, Py.Str(sizeValue), span, col, row, cells,
                Py.Str(Get(token, "note", "")), Py.Truthy(Get(token, "down", false))));
        }

        var labels = new List<Label>();
        foreach (var item in Items(Get(data, "labels", new List<object?>()), "labels"))
        {
            if (item is not PyDict label || !label.ContainsKey("at") || !label.ContainsKey("text"))
                throw Fail("every label needs \"at\" and \"text\".");
            var (col, row) = Cells.Parse(label["at"], cols, rows, $"label \"{Py.Str(label["text"])}\"");
            labels.Add(new Label(col, row, Py.Str(label["text"])));
        }

        object? edgesValue = Get(data, "edges", new PyDict());
        if (edgesValue is not PyDict edges || edges.Keys.Any(k => k is not ("top" or "bottom" or "left" or "right")))
            throw Fail("\"edges\" may only have the keys top, bottom, left and right.");
        var edgeText = new Dictionary<string, string>();
        foreach (var pair in edges)
        {
            if (Py.Truthy(pair.Value))
                edgeText[pair.Key] = Py.Str(pair.Value);
        }

        var (initiative, current) = turnOrder ? CheckTurnOrder(data, tokens) : ([], null);

        return new MapModel(Py.Str(Get(data, "title", "Map")), Py.Str(Get(data, "notes", "")), grid, cols, rows, legend,
            cellFt, Py.Str(Get(data, "scale_note", "")), north, tokens, labels, edgeText, initiative, current);
    }

    /// <summary>
    /// Check the optional <c>"initiative"</c> (the creatures in turn order) and <c>"current"</c> (whose turn it is).
    /// Each entry is a token's id or, if no token has that id, the name of exactly one token; a name shared by several
    /// tokens is refused, with their ids. A creature appears once, and <c>"current"</c> must be in the order. Null and
    /// an empty list mean there is no turn order. A fight that is over simply drops both keys.
    /// </summary>
    /// <returns>The tokens in turn order (empty with no turn order) and the token whose turn it is, or null.</returns>
    static (IReadOnlyList<Token> Initiative, Token? Current) CheckTurnOrder(PyDict data, List<Token> tokens)
    {
        var order = new List<Token>();
        object? initiative = Get(data, "initiative", null);
        if (initiative is not null)
        {
            if (initiative is not List<object?> entries || entries.Any(n => n is not string { Length: > 0 }))
                throw Fail("\"initiative\" must be a list of creatures (token ids or names), in turn order.");
            foreach (string entry in entries.Cast<string>())
            {
                var token = ResolveCreature("initiative", entry, tokens);
                if (order.Contains(token))
                    throw Fail($"\"initiative\" lists \"{entry}\" twice.");
                order.Add(token);
            }
        }

        object? current = Get(data, "current", null);
        if (current is null)
            return (order, null);
        if (current is not string { Length: > 0 } currentText)
            throw Fail("\"current\" must be the id or name of the creature whose turn it is.");
        var now = ResolveCreature("current", currentText, tokens);
        if (!order.Contains(now))
            throw Fail($"\"current\" is \"{currentText}\", who is not in \"initiative\".");
        return (order, now);
    }

    /// <summary>
    /// The token an <c>"initiative"</c> or <c>"current"</c> entry means: the one whose id it is, else the only one
    /// whose name it is. <paramref name="key"/> names the key in the error.
    /// </summary>
    static Token ResolveCreature(string key, string text, List<Token> tokens)
    {
        var byId = tokens.FirstOrDefault(t => t.Id == text);
        if (byId is not null)
            return byId;
        var named = tokens.Where(t => t.Name == text).ToList();
        if (named.Count == 1)
            return named[0];
        if (named.Count == 0)
            throw Fail($"\"{key}\" names \"{text}\", which is neither the id nor the name of any token on the map.");
        throw Fail($"\"{key}\" names \"{text}\", the name of {named.Count} tokens: use their ids "
                   + $"({string.Join(", ", named.Select(t => t.Id))}).");
    }
}
