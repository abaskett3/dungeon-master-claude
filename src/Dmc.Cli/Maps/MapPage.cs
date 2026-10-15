using System.Text;

namespace Dmc.Maps;

/// <summary>
/// The two files the player's browser tab is made of: <c>live.html</c>, the same every time, and
/// <c>live-data.js</c>, the current picture. The page is a static file opened from disk; it reloads the data
/// file every 2 seconds, because a page opened from disk cannot fetch files.
/// </summary>
public static class MapPage
{
    /// <summary>The page. It never changes, so a tab left open keeps working across runs.</summary>
    public static string Html { get; } = Shell
        .Replace("\r\n", "\n") // a checkout with CRLF line endings makes the literal below CRLF too
        .Replace("@BG@", SvgDrawing.PageBg).Replace("@INK@", SvgDrawing.Ink)
        .Replace("@MUTED@", SvgDrawing.Muted).Replace("@FONT@", SvgDrawing.Font);

    /// <summary>The data file: one <c>dmcMap({...});</c> call with the stamp, the clock time, the title and the picture.</summary>
    public static string Data(DateTime now, string title, string svg)
    {
        // now.isoformat(): the microseconds are left out when they are zero.
        long microseconds = now.Ticks % TimeSpan.TicksPerSecond / 10;
        string stamp = now.ToString("yyyy-MM-dd'T'HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture)
                       + (microseconds == 0 ? "" : "." + microseconds.ToString("000000", System.Globalization.CultureInfo.InvariantCulture));
        var payload = new PyDict
        {
            ["stamp"] = stamp,
            ["time"] = now.ToString("HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture),
            ["title"] = title,
            ["svg"] = svg,
        };
        return "dmcMap(" + PyJson.Dumps(payload) + ");\n";
    }

    /// <summary>
    /// Write a text file the way Python's text mode does: UTF-8 with no byte-order mark, and the platform's line
    /// ending for each "\n".
    /// </summary>
    public static void WriteText(string path, string text) =>
        File.WriteAllBytes(path, new UTF8Encoding(false).GetBytes(text.Replace("\n", Environment.NewLine)));

    const string Shell = """
<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<title>DMC map</title>
<style>
  html, body { margin: 0; height: 100%; background: @BG@; color: @INK@;
               font-family: @FONT@; }
  #map { height: 100%; display: flex; align-items: center; justify-content: center;
         padding: 8px; box-sizing: border-box; }
  #map svg { max-width: 100%; max-height: 100%; width: auto; height: auto; }
  #stamp { position: fixed; right: 10px; bottom: 8px; font-size: 12px; color: @MUTED@;
           padding: 2px 8px; border-radius: 10px; transition: background 1.5s; }
  #stamp.fresh { background: #2e9e5b; color: #fff; transition: none; }
</style>
</head>
<body>
<div id="map">Waiting for the first map…</div>
<div id="stamp"></div>
<script>
  // live-data.js is rewritten each time DMC redraws. Pages opened from disk
  // cannot fetch files, so it is loaded as a script that calls dmcMap().
  var shown = null;
  function dmcMap(data) {
    if (data.stamp === shown) return;
    var first = shown === null;
    shown = data.stamp;
    document.getElementById('map').innerHTML = data.svg;
    document.title = data.title + ' · DMC map';
    var stamp = document.getElementById('stamp');
    stamp.textContent = 'updated ' + data.time;
    if (!first) {
      stamp.className = 'fresh';
      setTimeout(function () { stamp.className = ''; }, 400);
    }
  }
  function poll() {
    var tag = document.createElement('script');
    tag.src = 'live-data.js?' + Date.now();
    tag.onload = tag.onerror = function () { tag.remove(); };
    document.head.appendChild(tag);
  }
  poll();
  setInterval(poll, 2000);
</script>
</body>
</html>

""";
}
