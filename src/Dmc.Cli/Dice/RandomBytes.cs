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
/// On Linux the bytes come from <c>/dev/urandom</c>, as <c>roll.sh</c> reads them, 64 at a time. <see cref="RandomNumberGenerator"/>
/// is OpenSSL's <c>RAND_bytes</c> there, and where libssl is missing (a bare <c>debian:12-slim</c>) the runtime
/// aborts the whole process, which no <c>catch</c> can stop (Phase 0 spike, finding 1). Everywhere else it is
/// <see cref="RandomNumberGenerator"/>: BCrypt on Windows, the Security framework on macOS.
/// </para>
/// </summary>
public sealed class OsRandomBytes : IRandomBytes, IDisposable
{
    /// <summary>Bytes read from /dev/urandom at a time (a die takes 4), like roll.sh's pool: one read serves many dice.</summary>
    internal const int PoolSize = 64;

    readonly bool _fromStream = OperatingSystem.IsLinux();
    readonly byte[] _pool = new byte[PoolSize];
    int _next = PoolSize; // the pool starts empty
    Stream? _source;

    public OsRandomBytes()
    {
    }

    /// <summary>Draw from this stream instead of the OS, on any platform. For tests of the pooling.</summary>
    internal OsRandomBytes(Stream source)
    {
        _source = source;
        _fromStream = true;
    }

    public void Dispose() => _source?.Dispose();

    public void Fill(Span<byte> buffer)
    {
        if (!_fromStream)
        {
            RandomNumberGenerator.Fill(buffer);
            return;
        }

        while (buffer.Length > 0)
        {
            if (_next == PoolSize)
                Refill();
            int n = Math.Min(buffer.Length, PoolSize - _next);
            _pool.AsSpan(_next, n).CopyTo(buffer);
            _next += n;
            buffer = buffer[n..];
        }
    }

    void Refill()
    {
        try
        {
            _source ??= new FileStream("/dev/urandom", FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize: 0);
            _source.ReadExactly(_pool);
            _next = 0;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // The same condition, the same words and the same exit code as roll.sh. (EndOfStreamException is an IOException.)
            throw new DmcException("cannot read /dev/urandom, no dice rolled", exitCode: 2);
        }
    }
}
