namespace Dmc.Pdf;

/// <summary>
/// Makes what <c>dmc pdf</c> writes all or nothing. The files are written into a temporary folder inside the output
/// folder (<c>&lt;out&gt;/.dmc-partial-&lt;random&gt;</c>), and <see cref="Commit"/> moves them into place only once every
/// one has been written. Because the temporary folder is inside the output folder, a run needs write access only where it
/// writes, every move is a rename on one volume (even when the output folder is a symbolic link or a junction), and an
/// output path that cannot be used (it is a file, or cannot be written to) fails when the run starts, before any page is
/// drawn. A run that fails deletes the temporary folder and every folder it had to make, and touches nothing that was
/// already in the output folder.
/// </summary>
/// <remarks>
/// What can still go wrong, and is said so: the move is many renames, not one step, so a failure in the middle of it (a
/// read-only file in the way, a full disk) can leave some files moved, though <see cref="Commit"/> first checks for what
/// it can foresee, a file where a folder must go and the reverse, and refuses before it moves anything. Cleaning up is best
/// effort, item by item; what cannot be deleted (another program holds a file in it open) or a process that is killed
/// leaves the <c>.dmc-partial-</c> folder behind, which is safe to delete, and which <see cref="Abandon"/> and
/// <see cref="Commit"/> report by returning its path.
/// </remarks>
internal sealed class OutputStaging : IDisposable
{
    /// <summary>What every temporary folder's name starts with; the rest is random.</summary>
    public const string Prefix = ".dmc-partial-";

    /// <summary>The output folder the files end up in, as a full path without a trailing separator. It exists once the constructor has finished.</summary>
    readonly string _output;

    /// <summary>The folders this run made (the output folder and any missing parents), outermost first, recorded before they were made. Removed again, deepest first, if still empty, when a run is abandoned.</summary>
    readonly List<string> _created = [];

    /// <summary>True once the run has been committed or abandoned, so that <see cref="Dispose"/> has nothing left to undo.</summary>
    bool _finished;

    /// <summary>The temporary folder the run writes into, inside the output folder. It exists from construction until <see cref="Commit"/> or <see cref="Abandon"/>.</summary>
    public string Path { get; }

    /// <summary>
    /// Makes the output folder (and the folders above it that are missing) and the temporary folder in it. What was missing
    /// is noted before anything is made, and if making any of it fails, whatever this call made is removed again.
    /// </summary>
    /// <param name="output">The output folder. Files already in it are neither read nor changed.</param>
    /// <exception cref="IOException">The output path is a file, or a folder cannot be made. Nothing this call made is left.</exception>
    /// <exception cref="UnauthorizedAccessException">A folder cannot be made for lack of permission. Nothing this call made is left.</exception>
    public OutputStaging(string output)
    {
        _output = System.IO.Path.TrimEndingDirectorySeparator(System.IO.Path.GetFullPath(output));
        Path = System.IO.Path.Combine(_output, Prefix + Guid.NewGuid().ToString("N")[..8]);

        // A path that is a file is not missing: creating the folder below it fails, and it is not ours to remove.
        var missing = new Stack<string>();
        for (string? dir = _output; dir is not null && !Directory.Exists(dir) && !File.Exists(dir); dir = System.IO.Path.GetDirectoryName(dir))
            missing.Push(dir);
        _created.AddRange(missing);
        try
        {
            Directory.CreateDirectory(Path);
        }
        catch
        {
            Abandon();
            throw;
        }
    }

    /// <summary>
    /// Moves every file written to <see cref="Path"/> to the same place under the output folder, making the folders it needs
    /// and replacing a file of the same name, then deletes the temporary folder. Files already in the output folder that the
    /// run did not write are left as they are. The list of files is taken before the first move, and what could not be moved
    /// is checked for before it starts. Once every file has been moved the run has succeeded, whatever happens to the
    /// temporary folder: failing to delete it is reported by the return value, not by an exception.
    /// </summary>
    /// <returns>Null; or the path of the temporary folder when it could not be deleted (a program holds something in it open) and is left behind, empty, to be deleted by hand.</returns>
    /// <exception cref="DmcException">A file would have to go where a folder is, or into a folder that is a file. Nothing has been moved.</exception>
    /// <exception cref="IOException">A move failed part way (a full disk, say). Files moved before it stay moved; the caller abandons the rest.</exception>
    /// <exception cref="UnauthorizedAccessException">A move failed part way for lack of permission, as above.</exception>
    public string? Commit()
    {
        string[] files = Directory.GetFiles(Path, "*", SearchOption.AllDirectories);
        string[] targets = [.. files.Select(file => System.IO.Path.Combine(_output, System.IO.Path.GetRelativePath(Path, file)))];
        foreach (string target in targets)
            RefuseIfBlocked(target);
        for (int i = 0; i < files.Length; i++)
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(targets[i])!);
            File.Move(files[i], targets[i], overwrite: true);
        }
        _finished = true;
        try
        {
            Directory.Delete(Path, recursive: true);
            return null;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return Path;
        }
    }

    /// <summary>
    /// Checks that a file can be put at <paramref name="target"/>: nothing on the way down from the output folder is a file,
    /// and the target itself is not a folder. Looks only; changes nothing.
    /// </summary>
    /// <param name="target">A full path under the output folder.</param>
    /// <exception cref="DmcException">A file is in the way of a folder, or a folder in the way of the file.</exception>
    void RefuseIfBlocked(string target)
    {
        for (string? dir = System.IO.Path.GetDirectoryName(target); dir is not null && dir.Length > _output.Length; dir = System.IO.Path.GetDirectoryName(dir))
        {
            if (File.Exists(dir))
                throw new DmcException($"{dir} is a file, so {System.IO.Path.GetFileName(target)} cannot be written into it. Move it away, then run this again. Nothing was written.");
        }
        if (Directory.Exists(target))
            throw new DmcException($"{target} is a folder, so the file of that name cannot be written there. Move it away, then run this again. Nothing was written.");
    }

    /// <summary>
    /// Undoes the run: deletes the temporary folder and then the folders this run made, if they are empty. Best effort, one
    /// item at a time, so one that cannot be deleted does not stop the others. Does nothing after <see cref="Commit"/> or an
    /// earlier call.
    /// </summary>
    /// <returns>Null; or the path of the temporary folder if it is still there, for the caller to tell the user.</returns>
    public string? Abandon()
    {
        if (_finished)
            return null;
        _finished = true;
        TryDelete(Path, recursive: true);
        for (int i = _created.Count - 1; i >= 0; i--)
            TryDelete(_created[i], recursive: false); // not recursive: a folder that is not empty is somebody's, and stays
        return Directory.Exists(Path) ? Path : null;
    }

    /// <summary>Deletes a folder if it is there, and says nothing if it cannot be (in use, no permission, not empty).</summary>
    /// <param name="folder">The folder.</param>
    /// <param name="recursive">Whether to delete what is in it too.</param>
    static void TryDelete(string folder, bool recursive)
    {
        try
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, recursive);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
        }
    }

    /// <summary>Abandons the run unless it has been committed. A leftover is not reported here; callers that want it call <see cref="Abandon"/> themselves.</summary>
    public void Dispose() => Abandon();
}
