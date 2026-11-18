using Dmc.Size;
using Dmc.Tests.Size;
using Dmc.Tests.Support;

namespace Dmc.Tests.Toolkit;

/// <summary>
/// The git rules the kit gives a player for <c>adventures/</c> (spec 018, contract adventures-git-rules.md): what
/// <c>kit/templates/adventures.gitignore</c> keeps and leaves out, and the line endings
/// <c>kit/templates/adventures.gitattributes</c> sets. DMC searches with ripgrep, which skips git-ignored files, so a
/// rule that caught a campaign file would hide it from play without an error (FR-007). Each test runs the real git, in a
/// temporary repository, with the player's own git settings shut out.
/// </summary>
[TestClass]
public sealed class AdventuresGitRulesTests
{
    /// <summary>The campaign folder name the paths use.</summary>
    const string C = "rorns-landing";

    /// <summary>
    /// Paths relative to <c>adventures/</c> that must stay in git: every kind of campaign file, a closed campaign's closing
    /// backup, and the rules files themselves.
    /// </summary>
    static readonly string[] Kept =
    [
        "player-options.md", ".gitignore", ".gitattributes",
        $"{C}/.standard", $"{C}/.closed", $"{C}/player-options.md", $"{C}/game-state.md", $"{C}/module-progress.md",
        $"{C}/legacy.md", $"{C}/character-sheets/pc-a.md", $"{C}/character-sheets/npc-b.md",
        $"{C}/character-sheets/npc-b-history.md", $"{C}/character-sheets/minor-npcs.md",
        $"{C}/story/summary.md", $"{C}/story/canon.md", $"{C}/story/canon-archive.md", $"{C}/story/dm-notes.md",
        $"{C}/story/dm-notes-archive.md", $"{C}/story/sessions/session-001.md",
        $"{C}/maps/place.json", $"{C}/maps/place.md", $"{C}/maps/live.json",
        $"{C}/import/progress.md", $"{C}/digests/the-silver-egg/index.md",
        $"{C}/backups/{C}_closed_20261113212205.zip",
    ];

    /// <summary>
    /// Paths relative to <c>adventures/</c> that git must leave out: one computer's state, staging folders, the leftovers
    /// of a cut-off write of <c>.standard</c> or <c>.closed</c>, the map renders and a migration backup with its records.
    /// </summary>
    static readonly string[] Ignored =
    [
        ".active-campaign", ".reviewed-sessions", ".transcript-review/x.md",
        ".dmc-import-ab12/x", $".restore-{C}-ab12/x", $".replaced-{C}-ab12/x",
        $"{C}/.standard.partial-ab12cd34", $"{C}/.closed.partial-ab12cd34",
        $"{C}/maps/live.html", $"{C}/maps/live-data.js", $"{C}/maps/live.png", $"{C}/maps/live-source.txt",
        $"{C}/backups/{C}_backup_20261115113646.zip", $"{C}/backups/{C}_backup_20261115113646.zip.migrating",
        $"{C}/backups/{C}_backup_20261115113646.zip.accounting.tsv",
        $"{C}/backups/{C}_backup_20261115113646.zip.verified.tsv",
    ];

    /// <summary>Every kept path is not ignored, and every left-out path is ignored.</summary>
    [TestMethod]
    public void TheRulesKeepEveryCampaignFileAndLeaveOutTheRest()
    {
        string git = Git();
        string adventures = NewRepository(git, DmcFolders.NewTemp());
        try
        {
            foreach (string path in Kept.Concat(Ignored))
                Touch(adventures, path);

            var ignored = IgnoredOf(git, adventures, [.. Kept, .. Ignored]);
            CollectionAssert.AreEquivalent(Ignored, ignored.ToArray(), "ignored: " + string.Join(", ", ignored));
        }
        finally
        {
            DmcFolders.Delete(Path.GetDirectoryName(adventures)!);
        }
    }

    /// <summary>Text is stored and checked out with LF, and ZIPs and PNGs are binary, whatever the player's settings.</summary>
    [TestMethod]
    public void TextIsLfAndZipsAndPngsAreBinary()
    {
        string git = Git();
        string adventures = NewRepository(git, DmcFolders.NewTemp());
        try
        {
            var result = RunGit(git, adventures, "check-attr", "eol", "text", "--", "a/game-state.md", "a/backups/b.zip", "a/maps/live.png");
            Assert.AreEqual(0, result.ExitCode, result.ToString());
            string[] lines = result.StdoutLines;
            CollectionAssert.Contains(lines, "a/game-state.md: eol: lf");
            CollectionAssert.Contains(lines, "a/game-state.md: text: auto");
            CollectionAssert.Contains(lines, "a/backups/b.zip: text: unset");
            CollectionAssert.Contains(lines, "a/maps/live.png: text: unset");
        }
        finally
        {
            DmcFolders.Delete(Path.GetDirectoryName(adventures)!);
        }
    }

    /// <summary>
    /// The map page files the rules leave out are exactly the ones <c>dmc</c> skips when it looks for play since a backup
    /// or a close (<see cref="Dmc.Campaign.CampaignFiles.DerivedFiles"/>), so the two lists cannot drift apart.
    /// </summary>
    [TestMethod]
    public void TheIgnoredMapFilesAreTheDerivedFiles()
    {
        string rules = File.ReadAllText(Path.Combine(TestEnv.Root, "kit", "templates", "adventures.gitignore"));
        var ignored = rules.Split('\n').Select(l => l.Trim())
            .Where(l => l.StartsWith("/*/maps/", StringComparison.Ordinal))
            .Select(l => l["/*/".Length..])
            .Order(StringComparer.Ordinal).ToArray();
        CollectionAssert.AreEqual(Dmc.Campaign.CampaignFiles.DerivedFiles.Order(StringComparer.Ordinal).ToArray(), ignored);
    }

    /// <summary>
    /// Every file the resume reads (<see cref="ReadSet.Build"/>) of a fixture campaign is not ignored, so a new resume read
    /// cannot fall under a rule unnoticed.
    /// </summary>
    [TestMethod]
    public void NoFileTheResumeReadsIsIgnored()
    {
        string git = Git();
        string root = SizeFixtures.NewDmcFolderWithCampaign();
        try
        {
            string adventures = Path.Combine(root, "adventures");
            NewRepository(git, root);
            SizeFixtures.Write(root, "story/sessions/session-001.md", "# Session 1\n\n## Scene 1\n\nThe party arrives.\n");
            var paths = ReadSet.Build(root, SizeFixtures.Name)
                .Select(p => InAdventures(p.Path))
                .OfType<string>()
                .ToArray();
            Assert.IsFalse(paths.Any(p => p.EndsWith('/')), "every part is a file: " + string.Join(", ", paths));
            Assert.IsTrue(paths.Length >= 7, "the read set names the options, game state, summary, notes, sheets, log and canon: " + string.Join(", ", paths));
            foreach (string path in paths)
                Touch(adventures, path);

            var ignored = IgnoredOf(git, adventures, paths);
            Assert.AreEqual(0, ignored.Count, "the resume reads ignored files: " + string.Join(", ", ignored));
        }
        finally
        {
            DmcFolders.Delete(root);
        }
    }

    /// <summary>
    /// A read-set path relative to <c>adventures/</c>: the general options are named from the DMC folder, the rest from the
    /// campaign folder. Null for a file outside <c>adventures/</c> (the module files under <c>modules/</c>), which the
    /// rules do not reach.
    /// </summary>
    /// <param name="path">The part's path, as <see cref="ReadSet.Build"/> names it.</param>
    static string? InAdventures(string path)
    {
        path = path.Replace('\\', '/');
        if (path.StartsWith("adventures/", StringComparison.Ordinal))
            return path["adventures/".Length..];
        if (path.StartsWith("modules/", StringComparison.Ordinal) || path.StartsWith("../", StringComparison.Ordinal) || Path.IsPathRooted(path))
            return null;
        return $"{SizeFixtures.Name}/{path}";
    }

    /// <summary>Git, found on the PATH: a skip locally and a failure on CI when it is missing (Development 7).</summary>
    static string Git()
    {
        string? found = Dmc.Commands.PlayCommand.FindOnPath("git", Environment.GetEnvironmentVariable("PATH"), OperatingSystem.IsWindows() ? ".EXE" : null, OperatingSystem.IsWindows());
        TestEnv.Need(found is not null, "git");
        return found!;
    }

    /// <summary>Makes <c>adventures/</c> under a folder a new git repository holding the kit's two rules files.</summary>
    /// <param name="git">The git program.</param>
    /// <param name="folder">The folder to hold <c>adventures/</c>: a DMC folder, or an empty temporary one.</param>
    /// <returns>The <c>adventures/</c> folder.</returns>
    static string NewRepository(string git, string folder)
    {
        string adventures = Path.Combine(folder, "adventures");
        Directory.CreateDirectory(adventures);
        var init = RunGit(git, adventures, "init", "--quiet");
        Assert.AreEqual(0, init.ExitCode, init.ToString());
        File.Copy(Path.Combine(TestEnv.Root, "kit", "templates", "adventures.gitignore"), Path.Combine(adventures, ".gitignore"), overwrite: true);
        File.Copy(Path.Combine(TestEnv.Root, "kit", "templates", "adventures.gitattributes"), Path.Combine(adventures, ".gitattributes"), overwrite: true);
        return adventures;
    }

    /// <summary>The paths, of those given, that git ignores in a repository, by <c>git check-ignore</c>.</summary>
    /// <param name="git">The git program.</param>
    /// <param name="adventures">The repository.</param>
    /// <param name="paths">Paths relative to it, with <c>/</c>.</param>
    static List<string> IgnoredOf(string git, string adventures, string[] paths)
    {
        // Exit 0: some are ignored; 1: none is; anything else is an error.
        var result = RunGit(git, adventures, ["check-ignore", "--no-index", "--", .. paths]);
        Assert.IsTrue(result.ExitCode is 0 or 1, result.ToString());
        return [.. result.StdoutLines.Where(l => l.Length > 0)];
    }

    /// <summary>
    /// Runs git in a repository with no global or system configuration, so a player's excludes file or
    /// <c>core.autocrlf</c> on this computer cannot change the answer.
    /// </summary>
    /// <param name="git">The git program.</param>
    /// <param name="cwd">The folder to run it in.</param>
    /// <param name="args">Its arguments.</param>
    static ProcessResult RunGit(string git, string cwd, params string[] args) =>
        TestEnv.Run(git, args, cwd: cwd, environment: new Dictionary<string, string?>
        {
            ["GIT_CONFIG_NOSYSTEM"] = "1",
            ["GIT_CONFIG_GLOBAL"] = OperatingSystem.IsWindows() ? "NUL" : "/dev/null",
            ["HOME"] = cwd,
            ["XDG_CONFIG_HOME"] = cwd,
        });

    /// <summary>Creates an empty file, and its folders, at a path in a repository.</summary>
    /// <param name="adventures">The repository.</param>
    /// <param name="path">The path relative to it, with <c>/</c>.</param>
    static void Touch(string adventures, string path)
    {
        string full = Path.Combine(adventures, path.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        if (!File.Exists(full))
            File.WriteAllBytes(full, []);
    }
}
