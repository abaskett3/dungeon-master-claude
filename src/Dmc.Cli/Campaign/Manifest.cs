using System.Text.Json.Serialization;

namespace Dmc.Campaign;

/// <summary>
/// One file in an export: where it is and what it must hold. For a campaign file the path is relative to
/// <c>campaign/</c> in the archive; for the general player options it is relative to the archive's root.
/// </summary>
internal sealed record FileEntry
{
    /// <summary>The path, with <c>/</c> separators, as it is spelled in the archive.</summary>
    public required string Path { get; init; }

    /// <summary>The size in bytes. Extraction stops reading at this size (research R3).</summary>
    public required long Size { get; init; }

    /// <summary>The SHA-256 of the file's bytes, as 64 lower-case hexadecimal digits.</summary>
    public required string Sha256 { get; init; }
}

/// <summary>The module a campaign was running, as recorded in <c>module-progress.md</c> (research R8).</summary>
internal sealed record ModuleInfo
{
    /// <summary>The folder under <c>modules/</c>, or null when <c>module-progress.md</c> exists but names none.</summary>
    public required string? Folder { get; init; }
}

/// <summary>
/// <c>manifest.json</c>, the first entry of every export (data-model § Manifest). Every property is required, so a
/// manifest with a field missing does not deserialise; a nullable one must still be written, as <c>null</c>.
/// </summary>
internal sealed record Manifest
{
    /// <summary>The format this build of dmc writes. A reader accepts every format up to its own.</summary>
    public const int CurrentFormatVersion = 1;

    /// <summary>The format of the archive: 1 for this feature.</summary>
    public required int FormatVersion { get; init; }

    /// <summary>The campaign's folder name at export.</summary>
    public required string Campaign { get; init; }

    /// <summary>When the export was made: UTC, ISO 8601, like <c>2026-10-21T19:04:05Z</c>.</summary>
    public required string ExportedAt { get; init; }

    /// <summary>The version of the <c>dmc</c> that wrote the archive (<c>0.0.0-dev</c> for a development build).</summary>
    public required string DmcVersion { get; init; }

    /// <summary>Null for a campaign without <c>module-progress.md</c>; otherwise the module it runs.</summary>
    public required ModuleInfo? Module { get; init; }

    /// <summary>The general <c>adventures/player-options.md</c>, when the export holds it; null otherwise.</summary>
    public required FileEntry? PlayerOptions { get; init; }

    /// <summary>Every campaign file, sorted by path (ordinal).</summary>
    public required List<FileEntry> Files { get; init; }

    /// <summary>Every empty folder of the campaign, as a relative path, sorted (ordinal).</summary>
    public required List<string> Folders { get; init; }
}

/// <summary>
/// The source-generated JSON metadata for <see cref="Manifest"/>: reflection-based serialisation is not Native AOT safe
/// (research R4). camelCase names, indented so a person can read the file, and nulls respected.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true,
    RespectNullableAnnotations = true)]
[JsonSerializable(typeof(Manifest))]
internal sealed partial class ManifestJson : JsonSerializerContext;
