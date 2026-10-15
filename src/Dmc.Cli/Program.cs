using System.Text;

namespace Dmc;

public static class Program
{
    public static int Main(string[] args)
    {
        // No byte-order mark, and "\n" on every OS: what dmc prints lands in DMC's context, so it stays plain.
        // A BOM on stdin is not skipped: Python reads it as a character and rejects the JSON, and so must dmc.
        using var stdin = new StreamReader(Console.OpenStandardInput(), Io.Utf8, detectEncodingFromByteOrderMarks: false);
        // A character that cannot be encoded prints as "?", as it does from Python with errors="replace".
        using var stdout = new StreamWriter(Console.OpenStandardOutput(), Io.Utf8Replacing) { NewLine = "\n" };
        using var stderr = new StreamWriter(Console.OpenStandardError(), Io.Utf8) { NewLine = "\n", AutoFlush = true };
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
