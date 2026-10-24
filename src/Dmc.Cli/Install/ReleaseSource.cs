using System.Formats.Tar;
using System.IO.Compression;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Dmc.Install;

/// <summary>A published release: its tag, its version and the digest GitHub reports for each of its files.</summary>
/// <param name="Tag">The release tag, <c>dmc-vX.Y.Z</c>.</param>
/// <param name="Version">The version in the tag, <c>X.Y.Z</c>.</param>
/// <param name="Assets">The release's file names, each with the SHA-256 (lower-case hex) the API reports for it, or null when it reports none.</param>
internal sealed record ReleaseInfo(string Tag, string Version, IReadOnlyDictionary<string, string?> Assets)
{
    /// <summary>The version as numbers, to compare with another: major, minor and patch.</summary>
    public (int Major, int Minor, int Patch) Numbers => ReleaseSource.ParseVersion(Version) ?? (0, 0, 0);
}

/// <summary>
/// Where <c>dmc update</c> gets a release from (contract <c>dmc-update.md</c>, research R9 and R16): the latest release of
/// <c>abaskett3/dungeon-master-claude</c> from the GitHub API, or, for the tests, a local folder named by
/// <c>DMC_UPDATE_TEST_ASSETS</c> (a <c>latest.json</c> in the shape of the API's answer, the archives, <c>SHA256SUMS.txt</c> and
/// <c>SHA256SUMS.txt.sig</c>). It downloads over HTTPS only, with limits (15 seconds to connect, 30 seconds with no data, 10
/// minutes at most); checks the signature of the checksum list with <see cref="ReleaseKeys"/>; and checks the archive against
/// the signed list and against the digest the API reports before anything is unpacked. No setting skips any check (FR-028):
/// the local folder replaces where files come from, not what is checked. Failures are <see cref="DmcException"/>s whose
/// messages are the contract's.
/// </summary>
internal sealed class ReleaseSource : IDisposable
{
    /// <summary>The GitHub repository releases come from.</summary>
    public const string Repository = "abaskett3/dungeon-master-claude";

    /// <summary>The API address of the latest release.</summary>
    public const string LatestUrl = "https://api.github.com/repos/" + Repository + "/releases/latest";

    /// <summary>The environment variable that names a local folder to take the release from instead of GitHub (tests only).</summary>
    public const string TestAssetsVariable = "DMC_UPDATE_TEST_ASSETS";

    /// <summary>The name of the checksum list on a release.</summary>
    public const string SumsName = "SHA256SUMS.txt";

    /// <summary>The name of the signature of the checksum list on a release.</summary>
    public const string SignatureName = "SHA256SUMS.txt.sig";

    /// <summary>The most a download may take in all.</summary>
    static readonly TimeSpan TotalLimit = TimeSpan.FromMinutes(10);

    /// <summary>The longest wait for a connection, an answer or the next piece of data.</summary>
    static readonly TimeSpan StallLimit = TimeSpan.FromSeconds(30);

    /// <summary>The longest wait for a connection to open.</summary>
    static readonly TimeSpan ConnectLimit = TimeSpan.FromSeconds(15);

    /// <summary>The largest checksum list, signature or API answer that is read (they are a few hundred bytes to a few thousand).</summary>
    const long SmallLimit = 1 << 20;

    /// <summary>The largest archive that is downloaded (a release is about 50 MB).</summary>
    const long ArchiveLimit = 500L << 20;

    /// <summary>A release tag, <c>dmc-vX.Y.Z</c>, which is also what builds the download addresses and file names.</summary>
    static readonly Regex TagPattern = new(@"^dmc-v(\d+)\.(\d+)\.(\d+)$", RegexOptions.CultureInvariant);

    /// <summary>The local folder of the test seam; null when releases come from GitHub.</summary>
    readonly string? _localFolder;

    /// <summary>The client for GitHub; null with the test seam.</summary>
    readonly HttpClient? _http;

    /// <summary>Makes a source: GitHub, or the folder named by <see cref="TestAssetsVariable"/>.</summary>
    /// <param name="environment">Reads an environment variable; null for one that is not set.</param>
    /// <param name="version">This program's version, which the client names itself with.</param>
    /// <param name="handler">What carries the requests; a test passes a fake. The real one limits the wait to connect.</param>
    public ReleaseSource(Func<string, string?> environment, string version, HttpMessageHandler? handler = null)
    {
        if (environment(TestAssetsVariable) is { Length: > 0 } folder)
        {
            _localFolder = folder;
            return;
        }
        _http = new HttpClient(handler ?? new SocketsHttpHandler { ConnectTimeout = ConnectLimit }) { Timeout = Timeout.InfiniteTimeSpan };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("dmc/" + version);
        _http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
    }

    /// <summary>The runtime identifier of this machine, as the release names its archives: <c>win-x64</c>, <c>win-arm64</c>, <c>linux-x64</c> or <c>linux-arm64</c>.</summary>
    /// <param name="windows">True for Windows, false for Linux.</param>
    public static string Rid(bool windows) =>
        (windows ? "win" : "linux") + "-" + (RuntimeInformation.OSArchitecture == Architecture.Arm64 ? "arm64" : "x64");

    /// <summary>The name of a release's archive for a runtime identifier: <c>dmc-vX.Y.Z-win-x64.zip</c> or <c>dmc-vX.Y.Z-linux-x64.tar.gz</c>.</summary>
    /// <param name="tag">The release tag.</param>
    /// <param name="rid">The runtime identifier.</param>
    public static string ArchiveName(string tag, string rid) =>
        $"{tag}-{rid}.{(rid.StartsWith("win-", StringComparison.Ordinal) ? "zip" : "tar.gz")}";

    /// <summary>Reads <c>X.Y.Z</c> as three numbers; anything after a <c>-</c> or <c>+</c> is ignored. Null when it is not three numbers.</summary>
    /// <param name="version">The version text.</param>
    public static (int Major, int Minor, int Patch)? ParseVersion(string version)
    {
        string[] parts = version.Split('-', '+')[0].Split('.');
        if (parts.Length == 3 && int.TryParse(parts[0], out int major) && int.TryParse(parts[1], out int minor) && int.TryParse(parts[2], out int patch))
            return (major, minor, patch);
        return null;
    }

    /// <summary>Reads the latest release: the API's answer (or the local <c>latest.json</c>), its tag and the digest of each file.</summary>
    /// <returns>The release.</returns>
    /// <exception cref="DmcException"><c>could not reach GitHub: &lt;reason&gt;</c>: no network, a refusal (the API's limit for requests without sign-in, say), an answer that is not a release, or a tag that is not <c>dmc-vX.Y.Z</c>.</exception>
    public ReleaseInfo Latest()
    {
        byte[] bytes;
        if (_localFolder is not null)
        {
            string file = Path.Combine(_localFolder, "latest.json");
            bytes = File.Exists(file) ? File.ReadAllBytes(file) : throw new DmcException($"could not reach GitHub: latest.json is not in {_localFolder}");
        }
        else
        {
            string temporary = Path.Combine(Path.GetTempPath(), "dmc-latest-" + Guid.NewGuid().ToString("N"));
            try
            {
                if (!FetchHttp(LatestUrl, temporary, SmallLimit))
                    throw new DmcException("could not reach GitHub: the repository has no release");
                bytes = File.ReadAllBytes(temporary);
            }
            finally
            {
                File.Delete(temporary);
            }
        }
        return Parse(bytes);
    }

    /// <summary>Reads the tag and the files of a release from an API answer.</summary>
    /// <param name="bytes">The JSON of <c>releases/latest</c>.</param>
    /// <exception cref="DmcException">The answer is not a release of this repository's tag form.</exception>
    internal static ReleaseInfo Parse(byte[] bytes)
    {
        try
        {
            using var document = JsonDocument.Parse(bytes);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("tag_name", out var tagElement) || tagElement.ValueKind != JsonValueKind.String)
            {
                string message = root.ValueKind == JsonValueKind.Object && root.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String ? m.GetString()! : "no release was returned";
                throw new DmcException($"could not reach GitHub: {message}");
            }
            string tag = tagElement.GetString()!;
            var match = TagPattern.Match(tag);
            if (!match.Success)
                throw new DmcException($"could not reach GitHub: the latest release's tag \"{tag}\" is not a dmc release");
            var assets = new Dictionary<string, string?>(StringComparer.Ordinal);
            if (root.TryGetProperty("assets", out var list) && list.ValueKind == JsonValueKind.Array)
            {
                foreach (var asset in list.EnumerateArray())
                {
                    if (asset.ValueKind != JsonValueKind.Object || !asset.TryGetProperty("name", out var name) || name.ValueKind != JsonValueKind.String)
                        continue;
                    string? digest = asset.TryGetProperty("digest", out var d) && d.ValueKind == JsonValueKind.String ? d.GetString() : null;
                    assets[name.GetString()!] = digest is not null && digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase) ? digest["sha256:".Length..].ToLowerInvariant() : null;
                }
            }
            return new ReleaseInfo(tag, tag["dmc-v".Length..], assets);
        }
        catch (JsonException)
        {
            throw new DmcException("could not reach GitHub: its answer could not be read");
        }
    }

    /// <summary>
    /// Downloads and checks what the update installs: the checksum list and its signature (verified with <paramref name="keys"/>),
    /// then the archive for <paramref name="rid"/>, whose SHA-256 must match the signed list and the digest the API reports.
    /// Nothing is unpacked or run here. Everything goes into <paramref name="folder"/>.
    /// </summary>
    /// <param name="release">The release (<see cref="Latest"/>).</param>
    /// <param name="rid">The runtime identifier of this machine (<see cref="Rid"/>).</param>
    /// <param name="folder">An existing folder to download into.</param>
    /// <param name="keys">The keys that may have signed the checksum list.</param>
    /// <returns>The full path of the downloaded, checked archive.</returns>
    /// <exception cref="DmcException">One of the contract's errors: no archive for this machine, no signature (or no list to sign), a signature that is invalid or by an unknown key, a hash that does not match (also when the signed list or the API's digest does not name the archive), or a download that failed.</exception>
    public string Download(ReleaseInfo release, string rid, string folder, ReleaseKeys keys)
    {
        string name = ArchiveName(release.Tag, rid);
        if (!release.Assets.TryGetValue(name, out string? digest))
            throw new DmcException($"the latest release has no archive for {rid}");

        string sums = Path.Combine(folder, SumsName), signature = Path.Combine(folder, SignatureName);
        if (!Fetch(release.Tag, SignatureName, signature, SmallLimit) || !Fetch(release.Tag, SumsName, sums, SmallLimit))
            throw new DmcException($"release {release.Version} has no signature: nothing was installed");
        byte[] list = File.ReadAllBytes(sums);
        if (!keys.Verify(list, File.ReadAllBytes(signature)))
            throw new DmcException($"release {release.Version} is not signed by the DMC release key: nothing was installed");

        string archive = Path.Combine(folder, name);
        if (!Fetch(release.Tag, name, archive, ArchiveLimit))
            throw new DmcException($"the latest release has no archive for {rid}");
        string actual = Sha256(archive);
        if (actual != SignedHash(list, name) || actual != digest)
            throw new DmcException("the download does not match the signed checksums (or GitHub's digest): nothing was installed");
        return archive;
    }

    /// <summary>The SHA-256 the checksum list gives for a file: lines of <c>&lt;hex&gt;  &lt;name&gt;</c> (or <c>&lt;hex&gt; *&lt;name&gt;</c>, as <c>sha256sum</c> writes them). Null when the list does not name it.</summary>
    /// <param name="list">The bytes of <c>SHA256SUMS.txt</c>.</param>
    /// <param name="name">The file's name.</param>
    internal static string? SignedHash(byte[] list, string name)
    {
        foreach (string line in Encoding.UTF8.GetString(list).Split('\n'))
        {
            string text = line.TrimEnd('\r');
            int space = text.IndexOf(' ');
            if (space != 64)
                continue;
            string file = text[(space + 1)..].TrimStart(' ', '*');
            if (file == name)
                return text[..64].ToLowerInvariant();
        }
        return null;
    }

    /// <summary>
    /// Unpacks an archive into a folder: a <c>.zip</c> or a <c>.tar.gz</c> (entries that would land outside the folder are refused).
    /// Call it only after <see cref="Download"/> has checked the archive's hash. On Linux the program is made executable.
    /// </summary>
    /// <param name="archive">The archive.</param>
    /// <param name="folder">The folder to unpack into; made if it does not exist.</param>
    /// <exception cref="DmcException"><c>could not unpack &lt;archive&gt;: &lt;reason&gt;</c>.</exception>
    public static void Unpack(string archive, string folder)
    {
        try
        {
            Directory.CreateDirectory(folder);
            if (archive.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                ZipFile.ExtractToDirectory(archive, folder);
            }
            else
            {
                using var file = File.OpenRead(archive);
                using var gzip = new GZipStream(file, CompressionMode.Decompress);
                TarFile.ExtractToDirectory(gzip, folder, overwriteFiles: false);
            }
        }
        catch (Exception e) when (e is IOException or InvalidDataException or UnauthorizedAccessException)
        {
            throw new DmcException($"could not unpack {Path.GetFileName(archive)}: {e.Message}");
        }
    }

    /// <summary>Gets one file of a release into a path: from the local folder, or from <c>https://github.com/&lt;repository&gt;/releases/download/&lt;tag&gt;/&lt;name&gt;</c>.</summary>
    /// <param name="tag">The release tag.</param>
    /// <param name="name">The file's name on the release.</param>
    /// <param name="destination">Where to write it.</param>
    /// <param name="limit">The most bytes to accept.</param>
    /// <returns>False when the release has no such file.</returns>
    /// <exception cref="DmcException">The download failed or stalled, or the file cannot be written.</exception>
    bool Fetch(string tag, string name, string destination, long limit)
    {
        if (_localFolder is null)
            return FetchHttp($"https://github.com/{Repository}/releases/download/{tag}/{name}", destination, limit);
        string source = Path.Combine(_localFolder, name);
        if (!File.Exists(source))
            return false;
        try
        {
            File.Copy(source, destination, overwrite: true);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            throw new DmcException($"cannot write {destination}: {e.Message}");
        }
        return true;
    }

    /// <summary>
    /// Downloads an address into a file over HTTPS: 15 seconds to connect, 30 seconds with no data, 10 minutes in all.
    /// Redirects are followed (a release file is served from another GitHub host) and never to a plain-HTTP address.
    /// </summary>
    /// <param name="url">The address; it must be <c>https</c>.</param>
    /// <param name="destination">The file to write.</param>
    /// <param name="limit">The most bytes to accept.</param>
    /// <returns>False for a 404; true when the file is written.</returns>
    /// <exception cref="DmcException"><c>could not reach GitHub: &lt;reason&gt;</c> for every other failure.</exception>
    bool FetchHttp(string url, string destination, long limit)
    {
        if (!url.StartsWith("https://", StringComparison.Ordinal))
            throw new DmcException($"could not reach GitHub: {url} is not an https address");
        var client = _http ?? throw new InvalidOperationException("no HTTP client with the local test folder");
        using var total = new CancellationTokenSource(TotalLimit);
        using var stall = CancellationTokenSource.CreateLinkedTokenSource(total.Token);
        try
        {
            stall.CancelAfter(StallLimit);
            using var response = client.SendAsync(new HttpRequestMessage(HttpMethod.Get, url), HttpCompletionOption.ResponseHeadersRead, stall.Token)
                .GetAwaiter().GetResult();
            if (response.StatusCode == HttpStatusCode.NotFound)
                return false;
            if (!response.IsSuccessStatusCode)
                throw new DmcException($"could not reach GitHub: {url} answered {(int)response.StatusCode} {response.ReasonPhrase}".TrimEnd());
            if (response.Content.Headers.ContentLength > limit)
                throw new DmcException($"could not reach GitHub: {url} is larger than expected");
            using var body = response.Content.ReadAsStreamAsync(stall.Token).GetAwaiter().GetResult();
            long written = 0;
            var buffer = new byte[81920];
            using var file = Create(destination);
            while (true)
            {
                stall.CancelAfter(StallLimit);
                int read = body.ReadAsync(buffer, stall.Token).AsTask().GetAwaiter().GetResult();
                if (read == 0)
                    break;
                written += read;
                if (written > limit)
                    throw new DmcException($"could not reach GitHub: {url} is larger than expected");
                try
                {
                    file.Write(buffer, 0, read);
                }
                catch (IOException e)
                {
                    throw new DmcException($"cannot write {destination}: {e.Message}");
                }
            }
            return true;
        }
        catch (OperationCanceledException)
        {
            throw new DmcException(total.IsCancellationRequested
                ? $"could not reach GitHub: {url} took longer than 10 minutes"
                : $"could not reach GitHub: no data from {url} for 30 seconds");
        }
        catch (Exception e) when (e is HttpRequestException or IOException or InvalidOperationException)
        {
            throw new DmcException($"could not reach GitHub: {(e.InnerException ?? e).Message}");
        }
    }

    /// <summary>Opens a file for writing, as a <see cref="DmcException"/> when that fails.</summary>
    /// <param name="path">The file.</param>
    static FileStream Create(string path)
    {
        try
        {
            return File.Create(path);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            throw new DmcException($"cannot write {path}: {e.Message}");
        }
    }

    /// <summary>The SHA-256 of a file as lower-case hex.</summary>
    /// <param name="path">The file.</param>
    internal static string Sha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexStringLower(SHA256.HashData(stream));
    }

    /// <summary>Releases the HTTP client, if there is one.</summary>
    public void Dispose() => _http?.Dispose();
}
