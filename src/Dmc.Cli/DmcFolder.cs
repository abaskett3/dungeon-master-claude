using System.Text;
using System.Text.Json;
using Dmc.Kit;

namespace Dmc;

/// <summary>
/// Where <c>dmc</c> reads and writes game files: the DMC folder, an ordinary Claude Code project that <c>dmc install</c>
/// wrote (it replaces the toolkit root of the launcher days: the folder above <c>bin/</c>). A folder is a DMC folder when it has <c>.dmc/manifest.json</c>. The folders a
/// player has installed are recorded in <c>dmc.json</c> beside the program, as <c>{ "folders": [ ... ] }</c>; the file is
/// written by <c>dmc install</c> and never replaced by an update. Every method that reads the environment, the current folder
/// or the program's folder takes them as parameters, so a test can aim it anywhere.
/// </summary>
public static class DmcFolder
{
    /// <summary>The message of a command that needs a DMC folder and finds none.</summary>
    public const string NoFolder = "no DMC folder: run dmc install";

    /// <summary>The name of the record of installed folders, beside the program.</summary>
    internal const string RecordName = "dmc.json";

    /// <summary>The environment variable that Claude Code sets for its hooks to the project's folder.</summary>
    internal const string ProjectVariable = "CLAUDE_PROJECT_DIR";

    /// <summary>The folder this program runs from (<see cref="AppContext.BaseDirectory"/>, never the current folder), without a trailing separator.</summary>
    public static string ProgramFolder { get; } = Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory);

    /// <summary>The DMC folder of this run, from the real environment, current folder and program folder; null when there is none.</summary>
    /// <returns>See <see cref="Find(Func{string, string?}, string, string)"/>.</returns>
    public static string? Find() => Find(Environment.GetEnvironmentVariable, Directory.GetCurrentDirectory(), ProgramFolder);

    /// <summary>The DMC folder of this run, as <see cref="Find()"/>, or the failure of a command that cannot go on without one.</summary>
    /// <exception cref="DmcException"><c>no DMC folder: run dmc install</c> (exit code 1).</exception>
    public static string Require() => Find() ?? throw new DmcException(NoFolder);

    /// <summary>
    /// Finds the DMC folder: <c>CLAUDE_PROJECT_DIR</c> if it is a DMC folder (a hook), else the current folder if it is one,
    /// else the first recorded folder that is still a DMC folder, else null.
    /// </summary>
    /// <param name="environment">Reads an environment variable; null for one that is not set. A test passes its own.</param>
    /// <param name="currentFolder">The current folder.</param>
    /// <param name="programFolder">The folder that holds <c>dmc.json</c>.</param>
    /// <returns>The full path of the folder, or null when there is none.</returns>
    internal static string? Find(Func<string, string?> environment, string currentFolder, string programFolder)
    {
        if (environment(ProjectVariable) is { Length: > 0 } project && IsDmcFolder(project))
            return Path.GetFullPath(project);
        if (IsDmcFolder(currentFolder))
            return Path.GetFullPath(currentFolder);
        return Recorded(programFolder).FirstOrDefault(IsDmcFolder);
    }

    /// <summary>Whether a path is a DMC folder: it has <c>.dmc/manifest.json</c>.</summary>
    /// <param name="path">The folder to ask about; a path that cannot exist is simply not one.</param>
    public static bool IsDmcFolder(string path)
    {
        try
        {
            return File.Exists(Manifest.FileOf(path));
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    /// <summary>The folders recorded in <c>dmc.json</c>, in the order they were added, as full paths. Empty when the file is missing, unreadable or not a record.</summary>
    /// <param name="programFolder">The folder that holds <c>dmc.json</c>.</param>
    public static IReadOnlyList<string> Recorded(string programFolder)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(programFolder, RecordName), Encoding.UTF8));
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty("folders", out var folders) || folders.ValueKind != JsonValueKind.Array)
                return [];
            return folders.EnumerateArray().Where(f => f.ValueKind == JsonValueKind.String && !string.IsNullOrEmpty(f.GetString()))
                .Select(f => Path.GetFullPath(f.GetString()!)).ToList();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException or ArgumentException)
        {
            return [];
        }
    }

    /// <summary>
    /// Adds a folder to <c>dmc.json</c> beside the program, once (a folder already in the record keeps its place). The file
    /// is written to a temporary file and moved over the old one. Other entries are kept exactly.
    /// </summary>
    /// <param name="programFolder">The folder that holds <c>dmc.json</c>; made if it does not exist.</param>
    /// <param name="folder">The DMC folder to record.</param>
    /// <exception cref="DmcException">The record cannot be written (<c>cannot write &lt;file&gt;: ...</c>).</exception>
    public static void Record(string programFolder, string folder)
    {
        string full = Path.GetFullPath(folder);
        var folders = Recorded(programFolder).ToList();
        if (folders.Any(f => string.Equals(f, full, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)))
            return;
        folders.Add(full);
        string file = Path.Combine(programFolder, RecordName), temporary = file + ".tmp";
        try
        {
            Directory.CreateDirectory(programFolder);
            using (var stream = File.Create(temporary))
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true, NewLine = "\n" }))
            {
                writer.WriteStartObject();
                writer.WriteStartArray("folders");
                foreach (string f in folders)
                    writer.WriteStringValue(f);
                writer.WriteEndArray();
                writer.WriteEndObject();
            }
            File.Move(temporary, file, overwrite: true);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            throw new DmcException($"cannot write {file}: {e.Message}");
        }
    }
}
