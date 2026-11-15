using Dmc.Modules;

namespace Dmc.Commands;

/// <summary>
/// <c>dmc module check NAME [--folder PATH]</c>: what in an ingested module keeps a lookup from printing one area or one
/// entry (spec 012, contracts/dmc-module-check.md): a file at the Read cap mark, a heading over the section limit, a
/// heading over several stat blocks, a heading repeated under one parent. One line each, at most
/// <see cref="Dmc.Size.Budgets.CheckLines"/>, and nothing for a sound module.
/// </summary>
/// <remarks>
/// It never writes, moves or deletes a file. A checked module exits 0 whatever it found, as <c>dmc size</c> does,
/// because DMC acts on what it reads; a failure is one line and exit 1, a usage mistake exit 2. The work is in
/// <see cref="ModuleCheck"/>, which <c>dmc size --check</c> also uses for its <c>module:</c> line.
/// </remarks>
public sealed class ModuleCommand : ICommand
{
    /// <inheritdoc />
    public string Name => "module";

    /// <inheritdoc />
    public string Summary => "what in an ingested module keeps lookup from printing one area or entry: dmc module check NAME [--folder PATH]";

    /// <summary>The text <c>--help</c> prints: usage and what the check reports.</summary>
    // A checkout with CRLF line endings makes the literal CRLF too; the program always prints "\n".
    static readonly string Help = """
usage: dmc module check NAME [--folder PATH]

Checks an ingested module in modules/NAME/ (its index, its other top-level markdown and chapters/, never source/)
for what keeps dmc lookup from printing one area or entry, one line each, and prints nothing when all is well:
a file at the Read tool's cap mark, a heading whose own text is over the section limit, a heading over several
stat blocks, and a heading repeated under the same parent. It changes nothing; the fix is an edit to the files.

  NAME      a module folder in modules/ (modules/NAME is understood too)
  --folder  the DMC folder (default: the one dmc folder prints)
""".Replace("\r\n", "\n");

    /// <summary>Parses the arguments, finds the DMC folder and the module, checks it and prints its problems.</summary>
    /// <param name="args">The arguments after <c>module</c>.</param>
    /// <param name="io">Standard output gets the problem lines.</param>
    /// <returns>0 when the module was checked or for <c>--help</c>. A usage mistake throws <see cref="DmcException"/> with exit code 2; every other failure exits 1.</returns>
    public int Run(string[] args, Io io)
    {
        var request = ModuleArguments.Parse(args);
        if (request is null)
        {
            io.Out.WriteLine(Help);
            return 0;
        }

        string folder = DmcFolder.FromOption(request.Folder);
        string module = ModuleCheck.Resolve(folder, request.Name);
        foreach (string line in Capped(ModuleCheck.Problems(folder, module)))
            io.Out.WriteLine(line);
        return 0;
    }

    /// <summary>The lines to print: all of them up to <see cref="Dmc.Size.Budgets.CheckLines"/>; past it, one fewer and a last line saying how many more.</summary>
    /// <param name="problems">Every problem line, in order.</param>
    internal static List<string> Capped(IReadOnlyList<string> problems)
    {
        if (problems.Count <= Dmc.Size.Budgets.CheckLines)
            return [.. problems];
        int shown = Dmc.Size.Budgets.CheckLines - 1;
        return [.. problems.Take(shown), $"... and {problems.Count - shown} more"];
    }
}
