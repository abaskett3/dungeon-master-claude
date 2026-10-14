using System.Text;

namespace Dmc;

public static class Program
{
    public static int Main(string[] args)
    {
        // No byte-order mark, and "\n" on every OS: what dmc prints lands in DMC's context, so it stays plain.
        var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        using var stdin = new StreamReader(Console.OpenStandardInput(), utf8);
        using var stdout = new StreamWriter(Console.OpenStandardOutput(), utf8) { NewLine = "\n" };
        using var stderr = new StreamWriter(Console.OpenStandardError(), utf8) { NewLine = "\n", AutoFlush = true };
        try
        {
            return CommandLine.Run(args, new Io(stdin, stdout, stderr), CommandTable.All);
        }
        finally
        {
            try { stdout.Flush(); }
            catch (IOException) { } // the reader went away (a closed pipe): nothing left to tell it
        }
    }
}
