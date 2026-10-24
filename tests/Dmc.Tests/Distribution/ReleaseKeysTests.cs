using System.Security.Cryptography;
using System.Text;
using Dmc.Commands;
using Dmc.Install;
using Dmc.Tests.Support;

namespace Dmc.Tests.Distribution;

/// <summary>
/// The release keys built into <c>dmc</c> (research R16, FR-027): the two built-in public keys are real P-256 keys and are not a
/// test key, and the check accepts a DER ECDSA SHA-256 signature by either key of a set and nothing else, including a signature
/// made by <c>openssl dgst -sha256 -sign</c>, which is what the release workflow uses. The built-in keys are placeholders until
/// the maintainer pastes the real ones (human step H1); <see cref="BothBuiltInKeysParseAndAreDistinct"/> fails until then, which
/// is the intended gate before PR 2 merges.
/// </summary>
[TestClass]
public sealed class ReleaseKeysTests
{
    /// <summary>Signs data the way the release workflow does, with <c>openssl</c>'s format: ECDSA over SHA-256, DER.</summary>
    static byte[] Sign(ECDsa key, byte[] data) => key.SignData(data, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);

    [TestMethod]
    public void BothBuiltInKeysParseAndAreDistinct()
    {
        foreach (string pem in new[] { ReleaseKeys.ActivePublicKey, ReleaseKeys.BackupPublicKey })
        {
            using var key = ReleaseKeys.Parse(pem);
            Assert.IsNotNull(key.ExportSubjectPublicKeyInfo());
        }
        using var active = ReleaseKeys.Parse(ReleaseKeys.ActivePublicKey);
        using var backup = ReleaseKeys.Parse(ReleaseKeys.BackupPublicKey);
        CollectionAssert.AreNotEqual(active.ExportSubjectPublicKeyInfo(), backup.ExportSubjectPublicKeyInfo(), "the active and the backup key are two keys");
    }

    [TestMethod]
    public void NeitherBuiltInKeyIsTheTestKey()
    {
        using var testKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        byte[] test = testKey.ExportSubjectPublicKeyInfo();
        foreach (string pem in new[] { ReleaseKeys.ActivePublicKey, ReleaseKeys.BackupPublicKey })
        {
            using var key = ReleaseKeys.Parse(pem);
            CollectionAssert.AreNotEqual(test, key.ExportSubjectPublicKeyInfo());
        }
        // And a release signed by the test key is refused by the built-in set.
        byte[] data = "SHA256SUMS"u8.ToArray();
        Assert.IsFalse(ReleaseKeys.Built.Verify(data, Sign(testKey, data)));
    }

    [TestMethod]
    public void TheBuiltInSetHoldsTheActiveKeyThenTheBackupKey()
    {
        CollectionAssert.AreEqual(new[] { ReleaseKeys.ActivePublicKey, ReleaseKeys.BackupPublicKey }, ReleaseKeys.Built.Pems.ToArray());
    }

    [TestMethod]
    public void EitherKeyOfASetVerifiesAndAStrangerDoesNot()
    {
        using var first = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using var second = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using var stranger = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var keys = new ReleaseKeys([UpdateTests.Pem(first), UpdateTests.Pem(second)]);
        byte[] data = "the checksum list\n"u8.ToArray();
        Assert.IsTrue(keys.Verify(data, Sign(first, data)));
        Assert.IsTrue(keys.Verify(data, Sign(second, data)));
        Assert.IsFalse(keys.Verify(data, Sign(stranger, data)));
    }

    [TestMethod]
    public void ChangedDataAGarbledSignatureAndAnotherFormatAreRefused()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var keys = new ReleaseKeys([UpdateTests.Pem(key)]);
        byte[] data = "the checksum list\n"u8.ToArray(), signature = Sign(key, data);
        Assert.IsTrue(keys.Verify(data, signature));
        Assert.IsFalse(keys.Verify([.. data, (byte)'x'], signature), "the data changed");
        Assert.IsFalse(keys.Verify(data, []), "no signature");
        Assert.IsFalse(keys.Verify(data, "not a signature"u8.ToArray()), "not a signature");
        Assert.IsFalse(keys.Verify(data, signature[..^1]), "a cut signature");
        // The fixed-length form (r then s) that other tools write is not what openssl writes and is not accepted.
        byte[] fixedLength = key.SignData(data, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        Assert.IsFalse(keys.Verify(data, fixedLength));
    }

    [TestMethod]
    public void AKeyThatCannotBeReadVerifiesNothingButDoesNotStopTheOtherKey()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        byte[] data = "x"u8.ToArray();
        var keys = new ReleaseKeys(["REPLACE WITH THE ACTIVE PUBLIC KEY (H1)", UpdateTests.Pem(key)]);
        Assert.IsTrue(keys.Verify(data, Sign(key, data)));
        Assert.IsFalse(new ReleaseKeys(["REPLACE WITH THE ACTIVE PUBLIC KEY (H1)"]).Verify(data, Sign(key, data)));
        Assert.IsFalse(new ReleaseKeys([]).Verify(data, Sign(key, data)));
    }

    [TestMethod]
    public void OnlyAP256KeyIsAKey()
    {
        using var p384 = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        Assert.Throws<CryptographicException>(() => ReleaseKeys.Parse(UpdateTests.Pem(p384)));
        using var rsa = RSA.Create(2048);
        Assert.Throws<CryptographicException>(() => ReleaseKeys.Parse(rsa.ExportSubjectPublicKeyInfoPem()));
    }

    /// <summary>The program <c>openssl</c>: on the PATH, or on Windows the copy that Git for Windows carries (which a GitHub runner has); null when there is none.</summary>
    static string? Openssl() =>
        PlayCommand.FindOnPath("openssl", Environment.GetEnvironmentVariable("PATH"), ".EXE", OperatingSystem.IsWindows())
        ?? (OperatingSystem.IsWindows() && Environment.GetEnvironmentVariable("ProgramFiles") is { } programFiles
            && File.Exists(Path.Combine(programFiles, "Git", "usr", "bin", "openssl.exe")) ? Path.Combine(programFiles, "Git", "usr", "bin", "openssl.exe") : null);

    [TestMethod]
    public void AnOpensslSignatureOverAChecksumListVerifies()
    {
        string? found = Openssl();
        TestEnv.Need(found is not null, "openssl");
        string openssl = found!;
        string folder = DmcFolders.NewTemp();
        try
        {
            string key = Path.Combine(folder, "test.pem"), pub = Path.Combine(folder, "test.pub.pem"), list = Path.Combine(folder, "SHA256SUMS.txt"), sig = list + ".sig";
            File.WriteAllText(list, "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef  dmc-v1.0.0-win-x64.zip\n", new UTF8Encoding(false));
            // The commands .docs/releases.md gives for making the keys, and the one the release workflow signs with.
            foreach (string[] args in new[]
                     {
                         new[] { "ecparam", "-name", "prime256v1", "-genkey", "-noout", "-out", key },
                         ["ec", "-in", key, "-pubout", "-out", pub],
                         ["dgst", "-sha256", "-sign", key, "-out", sig, list],
                     })
            {
                var ran = TestEnv.Run(openssl, args, cwd: folder);
                Assert.AreEqual(0, ran.ExitCode, ran.ToString());
            }
            var keys = new ReleaseKeys([File.ReadAllText(pub)]);
            byte[] data = File.ReadAllBytes(list);
            Assert.IsTrue(keys.Verify(data, File.ReadAllBytes(sig)), "dmc accepts what openssl signed");
            Assert.IsFalse(keys.Verify([.. data, (byte)'\n'], File.ReadAllBytes(sig)));
        }
        finally
        {
            DmcFolders.Delete(folder);
        }
    }
}
