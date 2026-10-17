namespace Dmc.Pdf;

/// <summary>
/// Makes what <c>dmc pdf</c> writes all or nothing. The files are written into a temporary folder next to the output
/// folder (<c>&lt;out&gt;.partial-&lt;random&gt;</c>, on the same volume, so that moving a file is a rename), and
/// <see cref="Commit"/> moves them into the output folder only once every one has been written. A run that fails, or any
/// disposal without a commit, deletes the temporary folder and anything that had to be created to hold it, and touches
/// nothing that was already in the output folder. Moving is not one atomic step: a failure while the files are being moved
/// (a full disk, say) can leave some of them in place.
/// </summary>
internal sealed class OutputStaging : IDisposable
{
    /// <summary>The output folder the files end up in, as a full path without a trailing separator. It may not exist yet.</summary>
    readonly string _output;

    /// <summary>The folders that did not exist when the staging folder was made and were made to hold it, outermost first. Removed again, if still empty, when a run is abandoned.</summary>
    readonly List<string> _created = [];

    /// <summary>True once <see cref="Commit"/> has finished, so that <see cref="Dispose"/> has nothing to undo.</summary>
    bool _committed;

    /// <summary>The temporary folder the run writes into. It exists from construction until <see cref="Commit"/> or <see cref="Dispose"/>.</summary>
    public string Path { get; }

    /// <summary>Makes the temporary folder next to <paramref name="output"/>, and the folders above it that are missing.</summary>
    /// <param name="output">The output folder. It is not created here, and nothing in it is read or changed.</param>
    public OutputStaging(string output)
    {
        _output = System.IO.Path.TrimEndingDirectorySeparator(System.IO.Path.GetFullPath(output));
        // A root folder has no parent: its staging folder is made inside it, which is all that is left.
        string parent = System.IO.Path.GetDirectoryName(_output) ?? _output;
        string name = System.IO.Path.GetFileName(_output) + ".partial-" + Guid.NewGuid().ToString("N")[..8];
        Path = System.IO.Path.Combine(parent, name);

        // The folders that Directory.CreateDirectory is about to make, so that a failed run can remove them again.
        var missing = new Stack<string>();
        for (string? dir = parent; dir is not null && !Directory.Exists(dir); dir = System.IO.Path.GetDirectoryName(dir))
            missing.Push(dir);
        _created.AddRange(missing);
        Directory.CreateDirectory(Path);
    }

    /// <summary>
    /// Moves every file written to <see cref="Path"/> to the same place under the output folder, making the folders it
    /// needs and replacing a file of the same name, then deletes the temporary folder. Files already in the output folder
    /// that the run did not write are left as they are.
    /// </summary>
    /// <exception cref="IOException">A folder or file could not be made or moved (the output path is a file, say). Files moved before it stay moved.</exception>
    public void Commit()
    {
        Directory.CreateDirectory(_output);
        foreach (string file in Directory.EnumerateFiles(Path, "*", SearchOption.AllDirectories))
        {
            string target = System.IO.Path.Combine(_output, System.IO.Path.GetRelativePath(Path, file));
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(target)!);
            File.Move(file, target, overwrite: true);
        }
        _committed = true;
        Directory.Delete(Path, recursive: true);
    }

    /// <summary>
    /// After <see cref="Commit"/>, does nothing. Otherwise deletes the temporary folder, and the folders made to hold it
    /// that are still empty. It does not throw: this runs while another failure is being reported, which matters more.
    /// </summary>
    public void Dispose()
    {
        if (_committed)
            return;
        try
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
            for (int i = _created.Count - 1; i >= 0; i--)
                Directory.Delete(_created[i]); // not recursive: a folder that is no longer empty is somebody's, and stays
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
        }
    }
}
