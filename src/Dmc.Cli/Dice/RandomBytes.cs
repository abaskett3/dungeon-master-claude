using System.Security.Cryptography;

namespace Dmc.Dice;

/// <summary>A source of random bytes. The dice use the operating system's; tests use a scripted one.</summary>
public interface IRandomBytes
{
    void Fill(Span<byte> buffer);
}

/// <summary>
/// The operating system's cryptographic random source.
/// <para>
/// On Linux the bytes come from <c>/dev/urandom</c>, as <c>roll.sh</c> reads them. <see cref="RandomNumberGenerator"/>
/// is OpenSSL's <c>RAND_bytes</c> there, and where libssl is missing (a bare <c>debian:12-slim</c>) the runtime
/// aborts the whole process, which no <c>catch</c> can stop (Phase 0 spike, finding 1). Everywhere else it is
/// <see cref="RandomNumberGenerator"/>: BCrypt on Windows, the Security framework on macOS.
/// </para>
/// </summary>
public sealed class OsRandomBytes : IRandomBytes, IDisposable
{
    FileStream? _urandom;

    public void Dispose() => _urandom?.Dispose();

    public void Fill(Span<byte> buffer)
    {
        if (!OperatingSystem.IsLinux())
        {
            RandomNumberGenerator.Fill(buffer);
            return;
        }

        try
        {
            _urandom ??= new FileStream("/dev/urandom", FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize: 0);
            _urandom.ReadExactly(buffer);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or EndOfStreamException)
        {
            // The same condition, the same words and the same exit code as roll.sh.
            throw new DmcException("cannot read /dev/urandom, no dice rolled", exitCode: 2);
        }
    }
}
