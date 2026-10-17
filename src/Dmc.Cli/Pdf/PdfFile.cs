using System.Runtime.InteropServices;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Exceptions;

namespace Dmc.Pdf;

/// <summary>Opens a PDF for <c>dmc pdf</c> and turns everything that can go wrong into one line.</summary>
public static class PdfFile
{
    /// <summary>
    /// Open the PDF with PdfPig. A PDF with a user password is refused with the script's message; one with only an
    /// owner password (print or copy restrictions, as commercial modules often have) opens, as it does in PyMuPDF.
    /// </summary>
    /// <param name="path">The PDF file.</param>
    /// <param name="libsslAvailable">
    /// Whether OpenSSL can be loaded, for a test to say so on any OS. Null: on Linux ask the system, elsewhere never
    /// check (Windows and macOS do not use OpenSSL).
    /// </param>
    /// <exception cref="DmcException">The PDF needs a password, is encrypted on a machine without libssl, or cannot be read.</exception>
    public static PdfDocument Open(string path, Func<bool>? libsslAvailable = null)
    {
        // PdfPig decrypts with System.Security.Cryptography, which on Linux is OpenSSL. Where libssl is missing the
        // runtime does not throw: it aborts the whole process (exit 134), and no catch can stop that. So look first.
        if ((libsslAvailable is not null || OperatingSystem.IsLinux()) && !(libsslAvailable ?? HasLibssl)() && LooksEncrypted(path))
            throw new DmcException("This PDF is encrypted, and reading it needs OpenSSL (libssl), which is not installed.");
        try
        {
            return PdfDocument.Open(path);
        }
        catch (PdfDocumentEncryptedException)
        {
            throw new DmcException("This PDF is password-protected. Ask the player for an unlocked copy.");
        }
        catch (Exception e) when (e is not DmcException)
        {
            throw new DmcException($"not a readable PDF: {e.Message.ReplaceLineEndings(" ")}");
        }
    }

    /// <summary>True when one of the OpenSSL libraries (<c>libssl.so.3</c>, <c>libssl.so.1.1</c>, <c>libssl.so</c>) can be loaded. Only meaningful on Linux.</summary>
    public static bool HasLibssl()
    {
        foreach (string name in new[] { "libssl.so.3", "libssl.so.1.1", "libssl.so" })
        {
            if (NativeLibrary.TryLoad(name, out nint handle))
            {
                NativeLibrary.Free(handle);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// An <c>/Encrypt</c> entry in the first or last MiB, where the trailer, or the first cross-reference stream of a
    /// linearized file, sits; or just after the offset the file's <c>startxref</c> gives, where the dictionary of the last
    /// cross-reference stream sits, however much compressed data follows it. A guess in the safe direction: a false "yes"
    /// only matters when libssl is missing.
    /// </summary>
    /// <param name="path">The PDF file.</param>
    /// <returns>True when the marker is found in those places.</returns>
    public static bool LooksEncrypted(string path)
    {
        using var file = File.OpenRead(path);
        const int Window = 1 << 20;
        ReadOnlySpan<byte> marker = "/Encrypt"u8;
        var buffer = new byte[(int)Math.Min(Window, file.Length)];
        file.ReadExactly(buffer);
        if (buffer.AsSpan().IndexOf(marker) >= 0)
            return true;
        if (file.Length <= Window)
            return false;
        file.Seek(-buffer.Length, SeekOrigin.End);
        file.ReadExactly(buffer);
        if (buffer.AsSpan().IndexOf(marker) >= 0)
            return true;
        if (StartXref(buffer) is not { } offset || offset >= file.Length)
            return false;
        // A cross-reference dictionary is small; 64 KiB after its start is plenty.
        file.Seek(offset, SeekOrigin.Begin);
        var section = new byte[(int)Math.Min(1 << 16, file.Length - offset)];
        file.ReadExactly(section);
        return section.AsSpan().IndexOf(marker) >= 0;
    }

    /// <summary>The number after the last <c>startxref</c> in <paramref name="tail"/>, the end of the file.</summary>
    static long? StartXref(ReadOnlySpan<byte> tail)
    {
        int at = tail.LastIndexOf("startxref"u8);
        if (at < 0)
            return null;
        var rest = tail[(at + "startxref".Length)..];
        int i = 0;
        while (i < rest.Length && rest[i] is (byte)' ' or (byte)'\r' or (byte)'\n' or (byte)'\t')
            i++;
        long value = 0;
        int digits = 0;
        for (; i < rest.Length && rest[i] is >= (byte)'0' and <= (byte)'9' && digits < 18; i++, digits++)
            value = value * 10 + (rest[i] - '0');
        return digits > 0 ? value : null;
    }
}
