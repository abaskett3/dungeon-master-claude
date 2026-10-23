using Dmc.Tests.Support;

namespace Dmc.Tests.Campaign;

/// <summary>
/// The inputs of <c>dmc campaign</c> that must fail with one line (feeding the one-line error guard, <c>ErrorPathTests</c>).
/// The published binary resolves the toolkit root to the real repository, so a case here must fail on its arguments or on a
/// file in its own temporary folder, before anything is written: never under the real <c>adventures/</c> or
/// <c>exports/</c>. Every other refusal is checked in-process, against a temporary root, in the other tests of this folder.
/// </summary>
public static class CampaignCases
{
    /// <summary>A case that starts from a working folder made by <paramref name="seed"/>; there is no standard input.</summary>
    static CommandCase Case(string name, Action<string> seed, params string[] args) => new(name, _ => args, null, seed);

    /// <summary>A case whose arguments name a file of the working folder.</summary>
    static CommandCase OnFile(string name, Action<string> seed, params string[] args) =>
        new(name, work => [.. args.Select(a => a.Replace("<work>", work))], null, seed);

    /// <summary>The mistakes in the arguments: no verb, an unknown one, a missing name or file, an unknown option, a bad <c>--as</c>.</summary>
    public static IEnumerable<CommandCase> ErrorCases() =>
    [
        new("no verb"),
        new("unknown verb", "frobnicate"),
        new("an option where the verb belongs", "--bogus"),
        new("export without a name", "export"),
        new("import without a file", "import"),
        new("show without a file", "show"),
        new("export with an unknown option", "export", "x", "--bogus"),
        new("export with two names", "export", "a", "b"),
        new("export with --out and no value", "export", "x", "--out"),
        new("export with --force given a value", "export", "x", "--force=yes"),
        new("show with an option of export", "show", "x.zip", "--out", "y"),
        new("import with an option of export", "import", "x.zip", "--force"),
        new("import with a bad --as", "import", "x.zip", "--as", "Bad_Name"),
        new("import with an empty --as", "import", "x.zip", "--as="),
        new("import with --as starting with a hyphen", "import", "x.zip", "--as=-bad"),
        new("import with --as and no value", "import", "x.zip", "--as"),
        // The binary works on the real repository, so the name cannot be one that exists; the message lists whatever campaigns there are.
        new("export of a campaign that does not exist", "export", "no-such-campaign-zz9"),
        new("export of a name that is not one folder", "export", "../adventures"),
    ];

    /// <summary>
    /// Seeds the working folder with the sample campaign and an archive of it, <c>export.zip</c>, spoiled by <paramref name="mutate"/>.
    /// The archive is written by hand (<see cref="CampaignFixtures"/>), so these cases do not depend on the exporter.
    /// </summary>
    static Action<string> Archive(Action<CampaignFixtures.ArchiveParts>? mutate) => work =>
    {
        string source = CampaignFixtures.WriteCampaign(work);
        CampaignFixtures.BuildArchive(Path.Combine(work, "export.zip"), source, mutate: mutate);
    };

    /// <summary>A working folder with a text file that is not a ZIP, <c>not.zip</c>.</summary>
    static void NotAZip(string work) => File.WriteAllText(Path.Combine(work, "not.zip"), "this is not a zip file");

    /// <summary>No seed: the file the case names does not exist.</summary>
    static void Nothing(string work)
    {
    }

    /// <summary>
    /// The refusals that need a file in the working folder. Each fails before the importer creates any folder, so the real
    /// <c>adventures/</c> is never touched: a name that is taken or damaged contents are checked in-process instead.
    /// </summary>
    public static IEnumerable<CommandCase> DmcOnlyErrorCases() =>
    [
        OnFile("import of a file that does not exist", Nothing, "import", "<work>/missing.zip"),
        OnFile("import of a text file", NotAZip, "import", "<work>/not.zip"),
        OnFile("import of a zip with no manifest", Archive(p => p.OmitManifest = true), "import", "<work>/export.zip"),
        OnFile("import of a manifest that is not JSON", Archive(p => p.ManifestBytes = "{ nope"u8.ToArray()), "import", "<work>/export.zip"),
        OnFile("import of a newer format", Archive(p => p.Manifest["formatVersion"] = 2), "import", "<work>/export.zip"),
        OnFile("import of a manifest naming ../x", Archive(p => p.Manifest["campaign"] = "../x"), "import", "<work>/export.zip"),
        OnFile("import of an entry the manifest does not list", Archive(p => p.Entries.Add(("campaign/extra.md", [1]))), "import", "<work>/export.zip"),
        OnFile("import with --as and a file that does not exist", Nothing, "import", "<work>/missing.zip", "--as", "copy"),
        OnFile("show of a file that does not exist", Nothing, "show", "<work>/missing.zip"),
        OnFile("show of a text file", NotAZip, "show", "<work>/not.zip"),
        OnFile("show of a newer format", Archive(p => p.Manifest["formatVersion"] = 2), "show", "<work>/export.zip"),
        OnFile("show of an archive that disagrees with its manifest", Archive(p => p.DropEntry("game-state.md")), "show", "<work>/export.zip"),
    ];
}
