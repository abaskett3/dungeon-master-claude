using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace Dmc.Campaign;

/// <summary>What happened to the general <c>adventures/player-options.md</c> in an import.</summary>
internal enum OptionsOutcome
{
    /// <summary>The export did not hold one.</summary>
    None,

    /// <summary>The export held one and there was none, so it was written.</summary>
    Written,

    /// <summary>The export held one but one already exists, so the existing file was left as it was.</summary>
    Kept,
}

/// <summary>What an import did.</summary>
/// <param name="Name">The campaign's folder name under <c>adventures/</c>.</param>
/// <param name="Folder">The full path of the campaign that was created.</param>
/// <param name="Files">How many files it holds.</param>
/// <param name="Options">What happened to the general player options.</param>
/// <param name="Module">The module the campaign needs, and whether this toolkit has it.</param>
internal sealed record ImportResult(string Name, string Folder, int Files, OptionsOutcome Options, ModuleState Module);

/// <summary>
/// <c>dmc campaign import</c>: creates <c>adventures/&lt;name&gt;</c> from an export, all or nothing (research R5). The whole
/// archive is checked first (<see cref="ExportSummary.Open"/>), then extracted into a staging folder inside
/// <c>adventures/</c> while every file's size and SHA-256 are checked as it is written, and only then committed with renames
/// on one volume: the general player options if none exists, then the campaign folder. Nothing that exists is ever
/// replaced, and <c>adventures/.active-campaign</c> is never written. The library takes the toolkit root as a parameter, so
/// a test can aim it at a temporary folder.
/// </summary>
internal static class CampaignImporter
{
    /// <summary>What every staging folder's name starts with; the rest is random. The leading dot keeps it out of the names campaigns have.</summary>
    internal const string StagingPrefix = ".dmc-import-";

    /// <summary>Imports an export file as a new campaign.</summary>
    /// <param name="root">The toolkit root: the campaign is made in <c>adventures/</c> here, and <c>modules/</c> is looked in for its module.</param>
    /// <param name="request">What was asked.</param>
    /// <param name="err">Where to say that clean-up failed and left a staging folder behind (the message names it); nowhere when null.</param>
    /// <returns>What was made.</returns>
    /// <exception cref="DmcException">The file is not an export that can be imported, the name is taken, or a write failed. Nothing is left under the target name, and the staging folder is deleted.</exception>
    public static ImportResult Import(string root, ImportRequest request, TextWriter? err = null)
    {
        if (request.As is not null && !CampaignArguments.IsCampaignName(request.As))
            throw Argparse.Usage(CampaignArguments.AsMessage);

        // Checks 1 to 4: nothing is created for an archive that fails them.
        var summary = ExportSummary.Open(request.File);
        string name = request.As ?? summary.Manifest.Campaign;
        if (request.As is null)
            CheckName(summary.Path, name);

        string adventures = Path.Combine(root, "adventures");
        string target = Path.Combine(adventures, name);
        if (Directory.Exists(target) || File.Exists(target))
            throw new DmcException($"adventures/{name} already exists: import under another name with --as NAME");

        string staging = Path.Combine(adventures, StagingPrefix + Guid.NewGuid().ToString("N")[..8]);
        try
        {
            Directory.CreateDirectory(staging);
            Extract(summary, staging);
            var options = Commit(summary, adventures, staging, target);
            if (DeleteStaging(staging) is { } stuck)
                err?.WriteLine($"dmc campaign: done, but could not delete {stuck}; it is safe to delete.");
            return new ImportResult(name, Path.GetFullPath(target), summary.Manifest.Files.Count, options, summary.Module(root));
        }
        catch (Exception e)
        {
            if (DeleteStaging(staging) is { } stuck)
                err?.WriteLine($"dmc campaign: could not delete {stuck}; it is safe to delete.");
            if (e is IOException or UnauthorizedAccessException)
                throw new DmcException($"could not import: {e.Message.TrimEnd('.', '\r', '\n', ' ')}");
            throw;
        }
    }

    /// <summary>Refuses a campaign name taken from the manifest that is not one plain folder name.</summary>
    /// <param name="archive">The export file, for the message.</param>
    /// <param name="name">The name in the manifest.</param>
    /// <exception cref="DmcException">The name is empty, has a separator, is a special name or starts with a dot (the toolkit's own folders do).</exception>
    static void CheckName(string archive, string name)
    {
        string? why = ArchivePaths.Refusal(name) ?? (name.Contains('/') ? "contains /" : name[0] == '.' ? "starts with a dot" : null);
        if (why is not null)
            throw new DmcException($"{archive} holds an entry that cannot be imported: campaign name \"{name}\" ({why})");
    }

    /// <summary>
    /// Extracts the campaign into <c>&lt;staging&gt;/campaign</c> and the general player options into
    /// <c>&lt;staging&gt;/player-options.md</c>, verifying each file as it is written (check 5). Every file is given the export's time as its modification time; attributes are not set.
    /// </summary>
    /// <param name="summary">The export, already checked.</param>
    /// <param name="staging">The staging folder, which exists.</param>
    /// <exception cref="DmcException">A file's size or checksum differs from the manifest.</exception>
    static void Extract(ExportSummary summary, string staging)
    {
        string campaign = Path.Combine(staging, "campaign");
        // Every file gets the export's time, the same one: the archive keeps no per-file times, and files written in archive order would otherwise
        // look newer or older than each other, which the map redraw (live.json against live-data.js) reads as a change.
        ExportSummary.TryParseTimestamp(summary.Manifest.ExportedAt, out var stamp);
        Directory.CreateDirectory(campaign);
        try
        {
            using var zip = ZipFile.Open(summary.Path, ZipArchiveMode.Read, Encoding.UTF8);
            var entries = zip.Entries.ToDictionary(e => e.FullName, StringComparer.Ordinal);
            ZipArchiveEntry Find(string entry) =>
                entries.GetValueOrDefault(entry) ?? throw new DmcException($"{summary.Path} is damaged: {entry} is missing");

            if (summary.Manifest.PlayerOptions is { } options)
                ExtractFile(summary.Path, Find(ExportSummary.PlayerOptionsEntry), options, ArchivePaths.Combine(staging, ExportSummary.PlayerOptionsEntry), stamp);
            foreach (var file in summary.Manifest.Files)
                ExtractFile(summary.Path, Find(ExportSummary.CampaignPrefix + file.Path), file, ArchivePaths.Combine(campaign, file.Path), stamp);
            foreach (string folder in summary.Manifest.Folders)
                Directory.CreateDirectory(ArchivePaths.Combine(campaign, folder));
        }
        catch (InvalidDataException)
        {
            throw new DmcException($"{summary.Path} is not a dmc campaign export");
        }
    }

    /// <summary>
    /// Writes one entry to a file, counting and hashing the bytes as they go. Reading stops as soon as the entry is longer
    /// than the manifest says, so a crafted archive cannot make this write more than it declared (research R3).
    /// </summary>
    /// <param name="archive">The export file, for the messages.</param>
    /// <param name="entry">The archive entry.</param>
    /// <param name="record">What the manifest says the file is.</param>
    /// <param name="destination">Where to write it; its folder is made.</param>
    /// <param name="stamp">The modification time to give the file once it is written (UTC).</param>
    /// <exception cref="DmcException">The entry is larger or smaller than declared, or its SHA-256 differs.</exception>
    static void ExtractFile(string archive, ZipArchiveEntry entry, FileEntry record, string destination, DateTime stamp)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        long total = 0;
        try
        {
            using var input = entry.Open();
            using var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            var buffer = new byte[81920];
            int read;
            while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                total += read;
                if (total > record.Size)
                    throw new DmcException($"{archive} is damaged: {entry.FullName} is larger than the size in the manifest");
                output.Write(buffer, 0, read);
                hash.AppendData(buffer, 0, read);
            }
        }
        catch (InvalidDataException e)
        {
            throw new DmcException($"{archive} is damaged: {entry.FullName} cannot be read ({e.Message.TrimEnd('.')})");
        }
        if (total < record.Size)
            throw new DmcException($"{archive} is damaged: {entry.FullName} is smaller than the size in the manifest");
        if (Convert.ToHexStringLower(hash.GetHashAndReset()) != record.Sha256)
            throw new DmcException($"{archive} is damaged: {entry.FullName} does not match its checksum in the manifest");
        File.SetLastWriteTimeUtc(destination, stamp);
    }

    /// <summary>
    /// Moves the checked files into place: the general player options if there are none (a move that never overwrites), then
    /// the campaign folder. If the folder cannot be moved, the options file this run moved is moved back, or deleted if that
    /// fails, so a failed import leaves <c>adventures/</c> as it found it.
    /// </summary>
    /// <param name="summary">The export.</param>
    /// <param name="adventures">The <c>adventures/</c> folder.</param>
    /// <param name="staging">The staging folder holding the extracted files.</param>
    /// <param name="target">The campaign folder to make.</param>
    /// <returns>What happened to the general player options.</returns>
    /// <exception cref="IOException">The name appeared since it was checked, or a move failed.</exception>
    static OptionsOutcome Commit(ExportSummary summary, string adventures, string staging, string target)
    {
        var outcome = OptionsOutcome.None;
        bool moved = false;
        string staged = Path.Combine(staging, ExportSummary.PlayerOptionsEntry);
        string general = Path.Combine(adventures, ExportSummary.PlayerOptionsEntry);
        if (summary.Manifest.PlayerOptions is not null)
        {
            outcome = OptionsOutcome.Kept;
            if (!File.Exists(general) && !Directory.Exists(general))
            {
                try
                {
                    File.Move(staged, general, overwrite: false);
                    moved = true;
                    outcome = OptionsOutcome.Written;
                }
                catch (IOException) when (File.Exists(general))
                {
                    // It appeared meanwhile: keep it, as if it had been there all along.
                }
            }
        }

        try
        {
            // Moving a folder onto an empty one succeeds on Linux, so look again immediately before.
            if (Directory.Exists(target) || File.Exists(target))
                throw new IOException($"adventures/{Path.GetFileName(target)} appeared while the import was running");
            Directory.Move(Path.Combine(staging, "campaign"), target);
        }
        catch
        {
            if (moved)
            {
                try
                {
                    File.Move(general, staged, overwrite: false);
                }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException)
                {
                    try
                    {
                        File.Delete(general);
                    }
                    catch (Exception inner) when (inner is IOException or UnauthorizedAccessException)
                    {
                        // Nothing more can be done; the original failure is the one to report.
                    }
                }
            }
            throw;
        }
        return outcome;
    }

    /// <summary>Deletes the staging folder with everything in it, if it is there.</summary>
    /// <param name="staging">The staging folder.</param>
    /// <returns>Null; or the folder's path when it could not be deleted (another program holds something in it open) and is left behind, safe to delete.</returns>
    static string? DeleteStaging(string staging)
    {
        try
        {
            if (Directory.Exists(staging))
                Directory.Delete(staging, recursive: true);
            return null;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return Directory.Exists(staging) ? staging : null;
        }
    }
}
