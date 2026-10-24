using System.Security.Cryptography;

namespace Dmc.Install;

/// <summary>
/// The project's release signing keys, built into <c>dmc</c> (research R16, FR-027, FR-028): two ECDSA P-256 public keys, the
/// active one and the backup. A release's <c>SHA256SUMS.txt.sig</c> is a signature of <c>SHA256SUMS.txt</c> made with the
/// matching private key (<c>openssl dgst -sha256 -sign</c>, a DER signature); <see cref="Verify(byte[], byte[])"/> is true
/// when either key verifies it. Nothing reads a key from the environment, a setting or a file, so nothing can change which
/// keys a release build trusts: a different set can only be given to the internal constructor, which only tests call.
/// Changing the keys is a code change shipped in a release that a key the previous release trusts has signed.
/// </summary>
public sealed class ReleaseKeys
{
    /// <summary>
    /// The active public key, PEM <c>SubjectPublicKeyInfo</c>: the key whose private half is the secret
    /// <c>DMC_RELEASE_SIGNING_KEY</c> of the GitHub environment <c>release</c>. A placeholder until the maintainer makes the
    /// keys (human step H1, <c>.docs/releases.md</c> § Release keys): while it is one, no release verifies and
    /// <c>ReleaseKeysTests</c> fails. To replace it, paste the PEM of <c>active.pub.pem</c> in place of the placeholder text,
    /// every line indented at least as far as the closing quotes (a raw string literal drops that indentation). The release
    /// workflow checks that the key in the secret is one of the keys written here, by its base64 body.
    /// </summary>
    public const string ActivePublicKey = """
        REPLACE WITH THE ACTIVE PUBLIC KEY (H1): paste all of active.pub.pem here, from -----BEGIN PUBLIC KEY----- to -----END PUBLIC KEY-----
        """;

    /// <summary>
    /// The backup public key, PEM <c>SubjectPublicKeyInfo</c>: its private half is kept offline by the maintainer and signs
    /// the next release by hand if the active key is lost or exposed. A placeholder until human step H1.
    /// </summary>
    public const string BackupPublicKey = """
        REPLACE WITH THE BACKUP PUBLIC KEY (H1): paste all of backup.pub.pem here, from -----BEGIN PUBLIC KEY----- to -----END PUBLIC KEY-----
        """;

    /// <summary>The object identifier of the curve P-256 (prime256v1, secp256r1), the only curve a release key may use.</summary>
    const string P256 = "1.2.840.10045.3.1.7";

    /// <summary>The two keys a release build trusts: <see cref="ActivePublicKey"/> and <see cref="BackupPublicKey"/>.</summary>
    public static ReleaseKeys Built { get; } = new([ActivePublicKey, BackupPublicKey]);

    /// <summary>The PEM text of each trusted key.</summary>
    readonly IReadOnlyList<string> _pems;

    /// <summary>Makes a set of keys; only <see cref="Built"/> and tests make one.</summary>
    /// <param name="pems">The PEM <c>SubjectPublicKeyInfo</c> text of each trusted public key.</param>
    internal ReleaseKeys(IReadOnlyList<string> pems) => _pems = pems;

    /// <summary>The PEM text of each key, in order (the active one first in <see cref="Built"/>).</summary>
    internal IReadOnlyList<string> Pems => _pems;

    /// <summary>
    /// Whether a signature of some data is valid under any of the keys: ECDSA P-256 with SHA-256, DER-encoded
    /// (<see cref="DSASignatureFormat.Rfc3279DerSequence"/>). A key that cannot be read, such as a placeholder, never verifies
    /// anything; a signature that is not well formed does not verify either.
    /// </summary>
    /// <param name="data">The signed bytes, <c>SHA256SUMS.txt</c> exactly as it was downloaded.</param>
    /// <param name="derSignature">The bytes of <c>SHA256SUMS.txt.sig</c>.</param>
    /// <returns>True when one of the keys verifies the signature.</returns>
    public bool Verify(byte[] data, byte[] derSignature)
    {
        foreach (string pem in _pems)
        {
            try
            {
                using var key = Parse(pem);
                if (key.VerifyData(data, derSignature, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence))
                    return true;
            }
            catch (Exception e) when (e is CryptographicException or ArgumentException or FormatException)
            {
                // an unreadable key, or a signature that is not a signature, verifies nothing
            }
        }
        return false;
    }

    /// <summary>Reads a public key from its PEM text.</summary>
    /// <param name="pem">The PEM <c>SubjectPublicKeyInfo</c> text.</param>
    /// <returns>The key, which the caller disposes.</returns>
    /// <exception cref="CryptographicException">The text is not a PEM public key, or is not an elliptic-curve key (<see cref="ArgumentException"/> when it holds no PEM at all, as for a placeholder).</exception>
    internal static ECDsa Parse(string pem)
    {
        var key = ECDsa.Create();
        try
        {
            key.ImportFromPem(pem);
            if (key.ExportParameters(false).Curve.Oid.Value != P256)
                throw new CryptographicException("not a P-256 key");
            return key;
        }
        catch
        {
            key.Dispose();
            throw;
        }
    }
}
