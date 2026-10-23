using System.Security.Cryptography;

namespace Dmc.Dice;

/// <summary>A source of random bytes. The dice use the operating system's; tests use a scripted one.</summary>
public interface IRandomBytes
{
    /// <summary>Overwrites every byte of <paramref name="buffer"/> with random bytes.</summary>
    /// <param name="buffer">The span to fill.</param>
    void Fill(Span<byte> buffer);
}

/// <summary>
/// The operating system's cryptographic random source.
/// <para>
/// On Linux the bytes come from <c>/dev/urandom</c>, as <c>roll.sh</c> reads them, 64 at a time. <see cref="RandomNumberGenerator"/>
/// is OpenSSL's <c>RAND_bytes</c> there, and where libssl is missing (a bare <c>debian:12-slim</c>) the runtime
/// aborts the whole process, which no <c>catch</c> can stop (Phase 0 spike, finding 1). On Windows it is
/// <see cref="RandomNumberGenerator"/>, which is BCrypt there.
/// </para>
/// </summary>
public sealed class OsRandomBytes : IRandomBytes, IDisposable
{
    /// <summary>Bytes read from /dev/urandom at a time (a die takes 4), like roll.sh's pool: one read serves many dice.</summary>
    internal const int PoolSize = 64;

    /// <summary>True when bytes come from a stream (<c>/dev/urandom</c>, or a test's) and not from <see cref="RandomNumberGenerator"/>.</summary>
    readonly bool _fromStream = OperatingSystem.IsLinux();

    /// <summary>The bytes read from the stream and not yet handed out; only used when <see cref="_fromStream"/> is true.</summary>
    readonly byte[] _pool = new byte[PoolSize];

    /// <summary>The index in <see cref="_pool"/> of the next byte to hand out; <see cref="PoolSize"/> means the pool is used up.</summary>
    int _next = PoolSize; // the pool starts empty

    /// <summary>The stream behind the pool: opened on the first read on Linux, or passed in by a test. Null until then.</summary>
    Stream? _source;

    /// <summary>The real source: <see cref="RandomNumberGenerator"/> everywhere except Linux, where it is <c>/dev/urandom</c>.</summary>
    public OsRandomBytes()
    {
    }

    /// <summary>Draw from this stream instead of the OS, on any platform. For tests of the pooling.</summary>
    /// <param name="source">The stream to read; it is disposed with this object.</param>
    internal OsRandomBytes(Stream source)
    {
        _source = source;
        _fromStream = true;
    }

    /// <summary>Closes <c>/dev/urandom</c> if it was opened.</summary>
    public void Dispose() => _source?.Dispose();

    /// <summary>
    /// Fills <paramref name="buffer"/>. From the operating system's generator directly, or from the pool, which is
    /// refilled 64 bytes at a time when it runs out.
    /// </summary>
    /// <param name="buffer">The span to fill.</param>
    /// <exception cref="DmcException">The stream cannot be read (exit code 2).</exception>
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

    /// <summary>Reads a whole new pool from the stream, opening <c>/dev/urandom</c> the first time.</summary>
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
