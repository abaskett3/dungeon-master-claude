using System.Globalization;
using System.Security.Cryptography;

namespace Dmc.Campaign;

/// <summary>What <c>dmc campaign close</c> found it can do, or did.</summary>
/// <param name="Name">The campaign's folder name.</param>
/// <param name="Files">How many campaign files the closing backup holds, the mark included.</param>
/// <param name="Backup">The closing backup's file name, the same in <c>backups/</c> and <c>exports/</c>.</param>
/// <param name="Moment">The moment of the close, UTC, to the second: the mark's time, the manifest's <c>exportedAt</c> and the time in the name.</param>
internal sealed record ClosePlan(string Name, int Files, string Backup, DateTime Moment);

/// <summary>
/// <c>dmc campaign close NAME [--yes]</c> (spec 010 contracts/campaign-close.md; research R3): closes a current campaign for
/// good, in an order in which a failure leaves it open. The plan refuses a campaign that is already closed, unfinished,
/// behind or ahead, and a closing backup name already taken in <c>backups/</c> or <c>exports/</c>, and writes nothing. The
/// close then writes the mark, the closing backup (holding the mark), checks it whole, copies it to <c>exports/</c> and
/// checks the copy; a failure at any step removes what this run wrote, the copy, then the backup, then the mark.
/// </summary>
internal static class CampaignCloser
{
    /// <summary>The steps of a close, in order, as a failure names them.</summary>
    internal static readonly string[] Steps = ["mark", "backup", "check backup", "copy", "check copy"];

    /// <summary>
    /// A test's fault seam (CloseFailureTests only): called with the name of each step just before it is done. An exception it
    /// throws is a failure of that step. Null in the program.
    /// </summary>
    internal static Action<string>? BeforeStep { get; set; }

    /// <summary>Checks that a campaign can be closed now, and fixes the closing backup's name. Writes nothing.</summary>
    /// <param name="root">The DMC folder.</param>
    /// <param name="name">The campaign's folder name.</param>
    /// <param name="now">The moment of the close; the clock when null.</param>
    /// <returns>The plan.</returns>
    /// <exception cref="DmcException">
    /// In this order: no such campaign; already closed; an unfinished migration; behind; ahead; a record or migration that
    /// cannot be read; a file already by the closing backup's name in <c>backups/</c> or <c>exports/</c>; a campaign an export
    /// would refuse.
    /// </exception>
    public static ClosePlan Plan(string root, string name, DateTime? now = null)
    {
        DateTime moment = Second(now ?? DateTime.UtcNow);
        string adventures = Path.Combine(root, "adventures");
        string campaign = Path.Combine(adventures, CampaignExporter.FindCampaign(adventures, name));
        if (ClosedCampaign.IsClosed(campaign))
            throw new DmcException($"{name} is already closed: start a sequel to continue its story");

        var status = StandardRecord.Status(root, name);
        switch (status.State)
        {
            case StandardState.Unfinished:
                throw new DmcException($"{name} has an unfinished migration from {status.Marked}: finish it or restore it before closing");
            case StandardState.Behind:
                throw new DmcException(string.Create(CultureInfo.InvariantCulture,
                    $"{name} is behind (standard {status.Campaign}, kit {status.Kit}): bring it up to date before closing"));
            case StandardState.Ahead:
                throw new DmcException(string.Create(CultureInfo.InvariantCulture,
                    $"{name} is ahead (standard {status.Campaign}, kit {status.Kit}): update dmc"));
        }

        string file = BackupFolder.ClosedName(name, moment);
        foreach (var (label, folder) in new[] { (BackupFolder.FolderName, BackupFolder.Of(campaign)), ("exports", Path.Combine(root, "exports")) })
        {
            string path = Path.Combine(folder, file);
            if (File.Exists(path) || Directory.Exists(path))
                throw new DmcException($"{name} cannot be closed: {label}/{file} already exists");
        }
        // The backup will hold every file an export takes now, and the mark.
        return new ClosePlan(name, CampaignExporter.Count(root, name) + 1, file, moment);
    }

    /// <summary>Closes a campaign: plans it, then writes the mark, the closing backup and its copy, each checked.</summary>
    /// <param name="root">The DMC folder.</param>
    /// <param name="name">The campaign's folder name.</param>
    /// <param name="now">The moment of the close; the clock when null.</param>
    /// <returns>What was written.</returns>
    /// <exception cref="DmcException">A refusal of <see cref="Plan"/>, or a step failed: then nothing this run wrote is left and the campaign is still open.</exception>
    public static ClosePlan Close(string root, string name, DateTime? now = null)
    {
        var plan = Plan(root, name, now);
        string campaign = Path.Combine(root, "adventures", name);
        string backups = BackupFolder.Of(campaign);
        string exports = Path.Combine(root, "exports");
        string backup = Path.Combine(backups, plan.Backup);
        string copy = Path.Combine(exports, plan.Backup);
        bool hadBackups = Directory.Exists(backups), hadExports = Directory.Exists(exports);
        bool marked = false, backedUp = false, copied = false;
        string step = Steps[0];

        void Begin(string next)
        {
            step = next;
            BeforeStep?.Invoke(next);
        }

        try
        {
            Begin("mark");
            ClosedCampaign.Write(campaign, plan.Moment, plan.Backup);
            marked = true;

            Begin("backup");
            var export = CampaignExporter.ClosingBackup(root, name, plan.Backup, plan.Moment);
            backedUp = true;

            Begin("check backup");
            var held = BackupFolder.Open(backup, name).ReadCampaign();
            if (!held.ContainsKey(ClosedCampaign.FileName))
                throw new DmcException($"{plan.Backup} holds no {ClosedCampaign.FileName}");

            Begin("copy");
            Copy(backup, copy);
            copied = true;

            Begin("check copy");
            BackupFolder.Open(copy, name).ReadCampaign();
            if (!Hash(copy).SequenceEqual(Hash(backup)))
                throw new DmcException($"exports/{plan.Backup} differs from {BackupFolder.FolderName}/{plan.Backup}");

            return plan with { Files = export.Files };
        }
        catch (Exception e) when (e is DmcException or IOException or UnauthorizedAccessException or InvalidDataException)
        {
            var left = new List<string>();
            if (copied && !CampaignBackup.Remove(copy))
                left.Add($"exports/{plan.Backup}");
            if (backedUp && !CampaignBackup.Remove(backup))
                left.Add($"{BackupFolder.FolderName}/{plan.Backup}");
            if (!hadExports)
                RemoveIfEmpty(exports);
            if (!hadBackups)
                RemoveIfEmpty(backups);
            if (marked && !ClosedCampaign.Remove(campaign))
                left.Add($"adventures/{name}/{ClosedCampaign.FileName}");
            string reason = e.Message.TrimEnd('.', '\r', '\n', ' ');
            throw new DmcException(left.Count == 0
                ? $"could not close {name}: {step}: {reason}; it is still open"
                : $"could not close {name}: {step}: {reason}; could not remove {string.Join(", ", left)}: delete them by hand");
        }
    }

    /// <summary>Copies a file through a <c>.partial-</c> file renamed into place, never over an existing file.</summary>
    /// <param name="source">The closing backup.</param>
    /// <param name="target">The copy's path in <c>exports/</c>.</param>
    static void Copy(string source, string target)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        string partial = target + ".partial-" + Guid.NewGuid().ToString("N")[..8];
        try
        {
            File.Copy(source, partial);
            File.Move(partial, target, overwrite: false);
        }
        finally
        {
            CampaignBackup.Remove(partial);
        }
    }

    /// <summary>The SHA-256 of a file.</summary>
    /// <param name="path">The file.</param>
    static byte[] Hash(string path)
    {
        using var stream = File.OpenRead(path);
        return SHA256.HashData(stream);
    }

    /// <summary>Deletes a folder this run made, if nothing is in it.</summary>
    /// <param name="folder">The folder.</param>
    static void RemoveIfEmpty(string folder)
    {
        try
        {
            if (Directory.Exists(folder) && !Directory.EnumerateFileSystemEntries(folder).Any())
                Directory.Delete(folder);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // An empty folder left behind holds nothing; the failure being reported matters more.
        }
    }

    /// <summary>A moment cut to the whole second, as UTC: the mark and the manifest keep seconds only, so all three records agree.</summary>
    /// <param name="moment">The moment.</param>
    static DateTime Second(DateTime moment)
    {
        var utc = moment.ToUniversalTime();
        return new DateTime(utc.Ticks - (utc.Ticks % TimeSpan.TicksPerSecond), DateTimeKind.Utc);
    }
}
