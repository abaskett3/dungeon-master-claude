using System.Globalization;
using System.Text;

namespace Dmc.Maps;

/// <summary>
/// Draws a checked map as one SVG picture, exactly as <c>map_render.py</c> does: the same layout constants, the
/// same elements in the same order, and numbers written the way Python writes them, so the text is identical.
/// </summary>
public static class SvgDrawing
{
    /// <summary>Pixels per cell.</summary>
    public const int Cell = 40;

    /// <summary>The outer margin, in pixels.</summary>
    public const int Pad = 16;

    /// <summary>Room for the coordinates along each edge, in pixels.</summary>
    public const int Gutter = 26;

    /// <summary>The width of the legend panel beside the map, in pixels.</summary>
    public const int Panel = 290;

    /// <summary>The font family of every text in the picture, and of the page around it.</summary>
    public const string Font = "Segoe UI, Helvetica, Arial, sans-serif";

    /// <summary>The background colour of the picture and of the page.</summary>
    public const string PageBg = "#1e2227";

    /// <summary>The main text colour.</summary>
    public const string Ink = "#e6e8eb";

    /// <summary>The colour of secondary text: notes, coordinates, headings.</summary>
    public const string Muted = "#9aa3ad";

    /// <summary>The default colour of a floor tile.</summary>
    const string FloorColor = "#ebe6d9";

    /// <summary>The outline of a floor tile.</summary>
    const string GridLine = "#cbc4b3";

    /// <summary>The default colour of a wall, and of a window's frame.</summary>
    const string WallColor = "#3b4149";

    /// <summary>Tokens sharing a cell: two across, so four fit in a 40 px cell.</summary>
    public const double SlotRadius = 8.5;

    /// <summary>and their ids are never drawn smaller than this.</summary>
    public const int SlotFont = 10;

    /// <summary>When more share a cell, the last place is a "+N" badge.</summary>
    public const int ShownInACell = 4;

    /// <summary>The colour of a kind of creature (<see cref="MapRules.Kinds"/>).</summary>
    static string Kinded(string kind) => MapRules.Kinds.First(k => k.Kind == kind).Color;

    /// <summary>A whole number as an SVG attribute value.</summary>
    static string Num(int value) => value.ToString(CultureInfo.InvariantCulture);

    /// <summary>A number as Python's <c>str(float)</c> writes it, such as <c>8.5</c>, or <c>12.0</c> for a whole one.</summary>
    static string Num(double value) => Py.FloatRepr(value);

    /// <summary>A coordinate or length with one decimal, as Python's <c>f"{value:.1f}"</c> writes it.</summary>
    static string F(double value) => Py.F1(value);

    // ---------- text ----------

    /// <summary>
    /// Break text into lines of at most <paramref name="width"/> characters, on spaces. A word longer than the width
    /// gets a line to itself. Runs of whitespace become one space.
    /// </summary>
    /// <param name="text">The text to break.</param>
    /// <param name="width">The most characters (code points) a line may hold.</param>
    /// <returns>The lines; one empty line when the text has no words.</returns>
    public static List<string> Wrap(string text, int width)
    {
        var lines = new List<string>();
        string line = "";
        foreach (string word in Py.Split(text))
        {
            if (line.Length > 0 && Py.Len(line) + 1 + Py.Len(word) > width)
            {
                lines.Add(line);
                line = word;
            }
            else
            {
                line = Py.Strip($"{line} {word}");
            }
        }
        if (line.Length > 0)
            lines.Add(line);
        return lines.Count > 0 ? lines : [""];
    }

    /// <summary>One <c>&lt;text&gt;</c> element, with <paramref name="content"/> escaped for HTML. The overloads below take the font size as a number.</summary>
    /// <param name="x">The anchor's x position.</param>
    /// <param name="y">The text's baseline.</param>
    /// <param name="content">The text.</param>
    /// <param name="size">The font size, already written as it goes into the attribute.</param>
    /// <param name="fill">The colour.</param>
    /// <param name="anchor"><c>start</c>, <c>middle</c> or <c>end</c>: which side of the text is at <paramref name="x"/>.</param>
    /// <param name="weight"><c>normal</c> or <c>bold</c>.</param>
    static string Text(double x, double y, string content, string size, string fill = Ink, string anchor = "start",
        string weight = "normal") =>
        $"<text x=\"{F(x)}\" y=\"{F(y)}\" font-family=\"{Font}\" font-size=\"{size}\" fill=\"{fill}\" "
        + $"text-anchor=\"{anchor}\" font-weight=\"{weight}\">{Py.HtmlEscape(content)}</text>";

    /// <summary><see cref="Text(double, double, string, string, string, string, string)"/> with a whole-number font size, written without a decimal point.</summary>
    static string Text(double x, double y, string content, int size, string fill = Ink, string anchor = "start",
        string weight = "normal") => Text(x, y, content, Num(size), fill, anchor, weight);

    /// <summary><see cref="Text(double, double, string, string, string, string, string)"/> with a fractional font size, written as Python writes a float.</summary>
    static string Text(double x, double y, string content, double size, string fill = Ink, string anchor = "start",
        string weight = "normal") => Text(x, y, content, Num(size), fill, anchor, weight);

    // ---------- terrain ----------

    /// <summary>SVG for one cell of the given terrain with its top-left corner at x, y.</summary>
    /// <param name="terrain">One of <see cref="MapRules.Terrains"/>; anything else is drawn as a <c>feature</c>.</param>
    /// <param name="symbol">The grid symbol (a code point). A feature is drawn as a floor with this character on it.</param>
    /// <param name="x">The left edge.</param>
    /// <param name="y">The top edge.</param>
    /// <param name="s">The side of the square: <see cref="Cell"/> on the map, smaller in the key.</param>
    /// <param name="color">The legend entry's colour, which replaces the terrain's main one; null for the default.</param>
    public static string Tile(string terrain, int symbol, double x, double y, double s, string? color = null)
    {
        var @out = new StringBuilder();

        void Rect(string fill, double inset = 0.0, int rx = 0, string? stroke = null)
        {
            string extra = stroke is not null ? $" stroke=\"{stroke}\" stroke-width=\"1\"" : "";
            @out.Append($"<rect x=\"{F(x + inset)}\" y=\"{F(y + inset)}\" width=\"{F(s - 2 * inset)}\" "
                        + $"height=\"{F(s - 2 * inset)}\" rx=\"{rx}\" fill=\"{fill}\"{extra}/>");
        }

        void Floor(string fill = FloorColor) => Rect(fill, stroke: GridLine);

        void Glyph(string character, string fill, double size = 0.5) =>
            @out.Append(Text(x + s / 2, y + s / 2 + s * size * 0.36, character, s * size, fill, "middle", "bold"));

        double cx = x + s / 2, cy = y + s / 2;
        switch (terrain)
        {
            case "wall":
                Rect(color ?? WallColor);
                break;
            case "floor":
                Floor(color ?? FloorColor);
                break;
            case "door":
                Floor();
                Rect(color ?? "#8a5a2b", inset: s * 0.14, rx: 3, stroke: "#5b3a1a");
                @out.Append($"<circle cx=\"{F(x + s * 0.68)}\" cy=\"{F(cy)}\" r=\"{F(s * 0.06)}\" fill=\"#e8c56a\"/>");
                break;
            case "window":
                Rect(WallColor);
                @out.Append($"<rect x=\"{F(x + s * 0.1)}\" y=\"{F(y + s * 0.36)}\" width=\"{F(s * 0.8)}\" "
                            + $"height=\"{F(s * 0.28)}\" fill=\"{color ?? "#9fd3f0"}\"/>");
                break;
            case "water":
                Rect(color ?? "#6fb1e0", stroke: "#4f93c4");
                foreach (double dy in new[] { 0.38, 0.66 })
                {
                    double yy = y + s * dy;
                    @out.Append($"<path d=\"M{F(x + s * 0.15)} {F(yy)} q{F(s * 0.09)} {F(-s * 0.1)} "
                                + $"{F(s * 0.175)} 0 t{F(s * 0.175)} 0 t{F(s * 0.175)} 0 t{F(s * 0.175)} 0\" "
                                + "fill=\"none\" stroke=\"#e8f4fc\" stroke-width=\"1.6\"/>");
                }
                break;
            case "hazard":
                Floor(color ?? "#f3c98b");
                @out.Append($"<path d=\"M{F(cx)} {F(y + s * 0.2)} L{F(x + s * 0.82)} {F(y + s * 0.8)} "
                            + $"L{F(x + s * 0.18)} {F(y + s * 0.8)} Z\" fill=\"#d9541e\" stroke=\"#7a2c0c\" "
                            + "stroke-width=\"1\"/>");
                @out.Append(Text(cx, y + s * 0.74, "!", s * 0.4, "#ffffff", "middle", "bold"));
                break;
            case "stairs_down" or "stairs_up":
                Floor(color ?? "#d6d0c2");
                for (int i = 0; i < 4; i++)
                {
                    double xx = x + s * (0.2 + i * 0.2);
                    @out.Append($"<line x1=\"{F(xx)}\" y1=\"{F(y + s * 0.15)}\" x2=\"{F(xx)}\" "
                                + $"y2=\"{F(y + s * 0.85)}\" stroke=\"#6a6558\" stroke-width=\"1.5\"/>");
                }
                Glyph(terrain == "stairs_down" ? "▼" : "▲", "#2b2f36", 0.42);
                break;
            case "tree":
                Floor();
                @out.Append($"<circle cx=\"{F(cx)}\" cy=\"{F(cy)}\" r=\"{F(s * 0.4)}\" fill=\"{color ?? "#4c9a52"}\" "
                            + "stroke=\"#2c6331\" stroke-width=\"1.5\"/>");
                @out.Append($"<circle cx=\"{F(cx)}\" cy=\"{F(cy)}\" r=\"{F(s * 0.1)}\" fill=\"#6b4a2a\"/>");
                break;
            case "pillar":
                Floor();
                @out.Append($"<circle cx=\"{F(cx)}\" cy=\"{F(cy)}\" r=\"{F(s * 0.32)}\" fill=\"{color ?? "#7b828c"}\" "
                            + "stroke=\"#454b53\" stroke-width=\"2\"/>");
                break;
            case "cover":
                Floor();
                Rect(color ?? "#b98952", inset: s * 0.12, rx: 4, stroke: "#6b4a2a");
                break;
            case "difficult":
                Floor(color ?? "#dccfb0");
                foreach (var (dx, dy) in new[] { (0.25, 0.3), (0.6, 0.22), (0.75, 0.6), (0.4, 0.68), (0.2, 0.8), (0.55, 0.45) })
                {
                    @out.Append($"<circle cx=\"{F(x + s * dx)}\" cy=\"{F(y + s * dy)}\" r=\"{F(s * 0.055)}\" "
                                + "fill=\"#8f7f5c\"/>");
                }
                break;
            case "unknown":
                Rect(color ?? "#2a2f36", stroke: "#3a4048");
                Glyph("?", "#6d7682", 0.45);
                break;
            default: // feature: floor with the map's own symbol on it
                Floor(color ?? "#e0d6bd");
                Glyph(Py.FromCodePoint(symbol), "#4a4233", 0.5);
                break;
        }
        return @out.ToString();
    }

    // ---------- creatures ----------

    /// <summary>
    /// Lay out tokens that share a cell. A token's slot is its (dx, dy) in pixels from the cell's top-left corner,
    /// or null for a token left out because the cell is crowded. A token that shares no cell has no entry.
    /// Badges is one entry for each crowded cell, drawn as "+N" in the last place.
    /// </summary>
    /// <param name="tokens">Every creature on the map, in the order the file lists them (the order that decides who is shown and who is hidden).</param>
    public static (Dictionary<string, (double Dx, double Dy)?> Slots, List<(int Col, int Row, double Dx, double Dy, List<Token> Hidden)> Badges)
        SharedSlots(IReadOnlyList<Token> tokens)
    {
        var order = new List<(int, int)>();
        var groups = new Dictionary<(int, int), List<Token>>();
        foreach (var token in tokens)
        {
            var key = (token.Col, token.Row);
            if (!groups.TryGetValue(key, out var group))
            {
                groups[key] = group = [];
                order.Add(key);
            }
            group.Add(token);
        }

        var slots = new Dictionary<string, (double, double)?>();
        var badges = new List<(int, int, double, double, List<Token>)>();
        double half = Cell / 2.0;
        foreach (var key in order)
        {
            var group = groups[key];
            if (group.Count < 2)
                continue;
            int places = Math.Min(group.Count, ShownInACell);
            int rows = (places + 1) / 2;
            for (int i = 0; i < places; i++)
            {
                int r = i / 2, c = i % 2;
                int across = r == rows - 1 && places % 2 == 1 ? 1 : 2;
                var place = ((Cell - across * half) / 2 + (c + 0.5) * half, (Cell - rows * half) / 2 + (r + 0.5) * half);
                if (group.Count > ShownInACell && i == places - 1)
                    badges.Add((key.Item1, key.Item2, place.Item1, place.Item2, group.GetRange(places - 1, group.Count - (places - 1))));
                else
                    slots[group[i].Id] = place;
            }
            if (group.Count > ShownInACell)
            {
                foreach (var token in group.GetRange(places - 1, group.Count - (places - 1)))
                    slots[token.Id] = null;
            }
        }
        return (slots, badges);
    }

    /// <summary>The "+N" that stands for the tokens that did not fit in a crowded cell. Hovering it lists their names.</summary>
    /// <param name="col">The crowded cell's column.</param>
    /// <param name="row">The crowded cell's row.</param>
    /// <param name="dx">The badge's x offset in pixels from the cell's left edge.</param>
    /// <param name="dy">The badge's y offset in pixels from the cell's top edge.</param>
    /// <param name="hidden">The creatures that are not drawn. The badge counts them.</param>
    /// <param name="x0">The left edge of the grid.</param>
    /// <param name="y0">The top edge of the grid.</param>
    static string DrawBadge(int col, int row, double dx, double dy, List<Token> hidden, int x0, int y0)
    {
        double cx = x0 + col * Cell + dx, cy = y0 + row * Cell + dy;
        string tip = "Also here: " + string.Join(", ", hidden.Select(t => t.Name));
        return $"<g><title>{Py.HtmlEscape(tip)}</title>"
               + $"<circle cx=\"{F(cx)}\" cy=\"{F(cy)}\" r=\"{Num(SlotRadius)}\" fill=\"{Muted}\" stroke=\"#ffffff\" stroke-width=\"2.5\"/>"
               + Text(cx, cy + SlotFont * 0.36, $"+{hidden.Count}", SlotFont, "#15181c", "middle", "bold") + "</g>";
    }

    /// <summary>
    /// SVG for a creature. With <paramref name="legendScale"/>, a small sample at x0, y0. With
    /// <paramref name="slot"/>, drawn small inside its cell, which it shares with others. Otherwise it fills the
    /// cells it covers. A creature that is down is drawn faded with a line through it.
    /// </summary>
    /// <param name="token">The creature.</param>
    /// <param name="x0">The left edge of the grid, or the sample's centre when <paramref name="legendScale"/> is given.</param>
    /// <param name="y0">The top edge of the grid, or the sample's centre when <paramref name="legendScale"/> is given.</param>
    /// <param name="legendScale">The radius of the small sample drawn in the key; null to draw the creature on the map.</param>
    /// <param name="slot">Where in its cell to draw it, in pixels from the cell's top-left corner, when the cell is shared (see <see cref="SharedSlots"/>); null to draw it across the cells it covers.</param>
    static string DrawToken(Token token, double x0, double y0, int? legendScale = null, (double Dx, double Dy)? slot = null)
    {
        string color = Kinded(token.Kind);
        double cx, cy, radius;
        if (slot is { } place)
        {
            cx = x0 + token.Col * Cell + place.Dx;
            cy = y0 + token.Row * Cell + place.Dy;
            radius = SlotRadius;
        }
        else if (legendScale is null)
        {
            double width = token.Span * Cell;
            cx = x0 + token.Col * Cell + width / 2;
            cy = y0 + token.Row * Cell + width / 2;
            radius = width / 2 - 4;
        }
        else
        {
            cx = x0;
            cy = y0;
            radius = legendScale.Value;
        }

        string opacity = token.Down ? "0.45" : "1";
        // min(a, b) keeps a unless b is smaller. A cap (a whole number in the script) prints without a ".0".
        double cap = Py.Len(token.Id) == 1 ? 26 : 22;
        double fontSize = radius * (Py.Len(token.Id) == 1 ? 1.05 : 0.85);
        bool whole = cap < fontSize;
        if (whole)
            fontSize = cap;
        if (slot is not null)
        {
            fontSize = SlotFont;
            whole = true;
        }
        string font = whole ? Num((int)fontSize) : Num(fontSize);
        string tip = $"{token.Name} ({Cells.CellName(token.Col, token.Row)})";
        if (token.Note.Length > 0)
            tip += $": {token.Note}";
        var parts = new List<string>
        {
            $"<g opacity=\"{opacity}\"><title>{Py.HtmlEscape(tip)}</title>",
            $"<circle cx=\"{F(cx)}\" cy=\"{F(cy)}\" r=\"{F(radius)}\" fill=\"{color}\" stroke=\"#ffffff\" stroke-width=\"2.5\"/>",
            $"<circle cx=\"{F(cx)}\" cy=\"{F(cy)}\" r=\"{F(radius + 1.5)}\" fill=\"none\" stroke=\"#15181c\" stroke-width=\"1\"/>",
            Text(cx, cy + fontSize * 0.36, token.Id, font, "#ffffff", "middle", "bold"),
        };
        if (token.Down)
        {
            double d = radius * 0.7;
            parts.Insert(parts.Count - 1, $"<path d=\"M{F(cx - d)} {F(cy + d)} L{F(cx + d)} {F(cy - d)}\" "
                                          + "stroke=\"#15181c\" stroke-width=\"2.5\"/>");
        }
        parts.Add("</g>");
        return string.Concat(parts);
    }

    // ---------- the picture ----------

    /// <summary>
    /// The whole picture as one SVG document: the title and notes, the grid with its coordinates, edge captions,
    /// labels and creatures, the scale bar and compass, and, to the right, the panel that lists the creatures and
    /// the key to the symbols. Its size is worked out from the contents, in the units the PNG uses as pixels.
    /// </summary>
    /// <param name="m">A map that passed <see cref="MapChecker.Check"/>.</param>
    public static string Render(MapModel m)
    {
        int cols = m.Cols, rows = m.Rows;
        int mapW = cols * Cell, mapH = rows * Cell;
        var edges = m.Edges;
        int sideL = edges.ContainsKey("left") ? 150 : 0;
        int sideR = edges.ContainsKey("right") ? 150 : 0;

        // The map sits on the left; the legend is a panel to its right.
        int x0 = Pad + sideL + Gutter;                          // left edge of the grid
        int leftW = Math.Max(x0 + mapW + Gutter + sideR, 300);  // right edge of the map block
        int panelX = leftW + 18;
        int width = panelX + Panel + Pad;
        var @out = new List<string>();
        int y = Pad + 22;

        @out.Add(Text(Pad, y, m.Title, 20, Ink, weight: "bold"));
        if (m.Notes.Length > 0)
        {
            foreach (string line in Wrap(m.Notes, (int)((width - 2 * Pad) / 7.0)))
            {
                y += 19;
                @out.Add(Text(Pad, y, line, 13, Muted));
            }
        }
        y += 14;
        int top = y;
        if (edges.TryGetValue("top", out string? topText))
        {
            foreach (string line in Wrap(topText, Math.Max((int)(mapW / 6.5), 20)))
            {
                y += 16;
                @out.Add(Text(x0 + mapW / 2.0, y, line, 12, Muted, "middle"));
            }
            y += 6;
        }

        int y0 = y + Gutter; // top edge of the grid

        // tiles
        for (int r = 0; r < m.Grid.Length; r++)
        {
            for (int c = 0; c < m.Grid[r].Length; c++)
            {
                int symbol = m.Grid[r][c];
                if (symbol == ' ')
                    continue;
                var entry = m.Legend[symbol];
                @out.Add(Tile(entry.Terrain, symbol, x0 + c * Cell, y0 + r * Cell, Cell, entry.Color));
            }
        }

        // coordinates on all four sides
        for (int c = 0; c < cols; c++)
        {
            double cx = x0 + c * Cell + Cell / 2.0;
            @out.Add(Text(cx, y0 - 8, Cells.ColName(c), 12, Muted, "middle"));
            @out.Add(Text(cx, y0 + mapH + 17, Cells.ColName(c), 12, Muted, "middle"));
        }
        for (int r = 0; r < rows; r++)
        {
            double cy = y0 + r * Cell + Cell / 2.0 + 4;
            @out.Add(Text(x0 - 8, cy, Num(r + 1), 12, Muted, "end"));
            @out.Add(Text(x0 + mapW + 8, cy, Num(r + 1), 12, Muted, "start"));
        }

        // side captions
        foreach (var (key, anchor, tx) in new[] { ("left", "end", x0 - Gutter - 8), ("right", "start", x0 + mapW + Gutter + 8) })
        {
            if (edges.TryGetValue(key, out string? caption))
            {
                var lines = Wrap(caption, 20);
                double ty = y0 + mapH / 2.0 - (lines.Count - 1) * 8;
                foreach (string line in lines)
                {
                    @out.Add(Text(tx, ty, line, 12, Muted, anchor));
                    ty += 16;
                }
            }
        }

        // labels: text on a pale plate, centred on the cell
        foreach (var label in m.Labels)
        {
            double lx = x0 + label.Col * Cell + Cell / 2.0;
            double ly = y0 + label.Row * Cell + Cell / 2.0;
            double w = Py.Len(label.Text) * 6.4 + 10;
            @out.Add($"<rect x=\"{F(lx - w / 2)}\" y=\"{F(ly - 9)}\" width=\"{F(w)}\" height=\"18\" rx=\"4\" "
                     + "fill=\"#fffdf5\" fill-opacity=\"0.88\" stroke=\"#8a8371\" stroke-width=\"0.8\"/>");
            @out.Add(Text(lx, ly + 4, label.Text, 11, "#2b2f36", "middle"));
        }

        // creatures
        var (slots, badges) = SharedSlots(m.Tokens);
        foreach (var token in m.Tokens)
        {
            if (!slots.TryGetValue(token.Id, out var slot))
                @out.Add(DrawToken(token, x0, y0));
            else if (slot is not null) // null: too many in one cell, shown as +N
                @out.Add(DrawToken(token, x0, y0, slot: slot));
        }
        foreach (var (col, row, dx, dy, hidden) in badges)
            @out.Add(DrawBadge(col, row, dx, dy, hidden, x0, y0));

        y = y0 + mapH + Gutter;
        if (edges.TryGetValue("bottom", out string? bottomText))
        {
            foreach (string line in Wrap(bottomText, Math.Max((int)(mapW / 6.5), 20)))
            {
                y += 14;
                @out.Add(Text(x0 + mapW / 2.0, y, line, 12, Muted, "middle"));
            }
            y += 6;
        }

        // scale bar and compass, under the map
        y += 22;
        @out.Add($"<path d=\"M{Pad} {y - 9} v8 h{Cell} v-8\" fill=\"none\" stroke=\"{Ink}\" stroke-width=\"1.5\"/>");
        string scale = $"1 square = {Distances.FormatDistance(1, m.CellFt)}";
        if (m.ScaleNote.Length > 0)
            scale += $". {m.ScaleNote}";
        int sx = Pad + Cell + 10;
        var scaleLines = Wrap(scale, Math.Max((int)((leftW - sx - 60) / 7.0), 16));
        for (int i = 0; i < scaleLines.Count; i++)
            @out.Add(Text(sx, y + i * 17, scaleLines[i], 13, Ink));
        if (m.North is not null)
        {
            int nx = leftW - 18, ny = y - 4;
            @out.Add($"<g transform=\"rotate({MapRules.NorthTurn[m.North]} {nx} {ny})\">"
                     + $"<path d=\"M{nx} {ny - 15} L{nx + 7} {ny + 9} L{nx} {ny + 4} L{nx - 7} {ny + 9} Z\" "
                     + $"fill=\"{Ink}\"/></g>");
            @out.Add(Text(nx - 16, y, "N", 13, Ink, "end", "bold"));
        }
        int mapBottom = y + (scaleLines.Count - 1) * 17 + Pad;

        // legend panel: creatures first, then terrain
        int chars = (int)((Panel - 34) / 6.8);
        const int rowH = 17;
        int py = top + 14;

        void Heading(string label)
        {
            @out.Add(Text(panelX, py, label.ToUpperInvariant(), 11, Muted, weight: "bold"));
            py += 10;
        }

        void EntryText(List<string> lines)
        {
            for (int i = 0; i < lines.Count; i++)
                @out.Add(Text(panelX + 34, py + 16 + i * rowH, lines[i], 13, Ink));
            py += Math.Max(lines.Count * rowH, 24) + 7;
        }

        if (m.Tokens.Count > 0)
        {
            Heading("On the map");
            foreach (var token in m.Tokens)
            {
                string label = $"{token.Name}  ·  {Cells.CellName(token.Col, token.Row)}";
                if (token.Size > 1)
                    label += $" ({token.SizeText}×{token.SizeText})";
                if (token.Note.Length > 0)
                    label += $"  ·  {token.Note}";
                @out.Add(DrawToken(token, panelX + 12, py + 12, legendScale: 11));
                EntryText(Wrap(label, chars));
            }
            py += 16;
        }

        var shown = m.Legend.Where(p => !(p.Value.Terrain == "floor" && p.Value.Name == "floor")).ToList(); // plain floor needs no key
        if (shown.Count > 0)
        {
            Heading("Key");
            foreach (var (symbol, entry) in shown)
            {
                @out.Add(Tile(entry.Terrain, symbol, panelX, py, 24, entry.Color));
                EntryText(Wrap(entry.Name, chars));
            }
        }

        @out.Add($"<line x1=\"{leftW + 4}\" y1=\"{top}\" x2=\"{leftW + 4}\" y2=\"{Math.Max(py, mapBottom) - Pad}\" "
                 + "stroke=\"#3a4048\" stroke-width=\"1\"/>");
        int height = Math.Max(py + Pad - 7, mapBottom);
        return $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {F0(width)} {F0(height)}\" "
               + $"width=\"{F0(width)}\" height=\"{F0(height)}\">"
               + $"<rect width=\"100%\" height=\"100%\" fill=\"{PageBg}\"/>" + string.Concat(@out) + "</svg>";
    }

    /// <summary>A whole number as Python's <c>f"{value:.0f}"</c> writes it (used for the picture's size).</summary>
    static string F0(double value) => Py.F0(value);
}
