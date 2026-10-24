namespace Dmc.Kit;

/// <summary>
/// One file that <c>dmc</c> writes into a DMC folder: where it goes and what it holds. The list of them is
/// <see cref="GeneratedFiles.All"/>, built from <c>kit/</c> in the repository, which is embedded in the program.
/// </summary>
/// <param name="Path">The path in the DMC folder, relative to it, with <c>/</c> as the separator (<c>.claude/skills/dmc-draw-map/SKILL.md</c>).</param>
/// <param name="Bytes">The file's content, exactly as it is written.</param>
public sealed record GeneratedFile(string Path, byte[] Bytes)
{
    /// <summary>The SHA-256 of <see cref="Bytes"/> as lowercase hex, which is what the manifest records.</summary>
    public string Sha256 => Manifest.Hash(Bytes);
}
