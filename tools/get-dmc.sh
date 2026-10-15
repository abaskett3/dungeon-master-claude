#!/usr/bin/env bash
# Install dmc, the toolkit's helper program, into bin/: the release pinned in tools/dmc-release.json, for this
# computer. Run it as "bash tools/get-dmc.sh". It prints nothing when bin/dmc is already that release, one line when
# it installs it, and one line on stderr (exit code 1) when it cannot.
#
# The archive comes from this repository's GitHub Release, over HTTPS only. Its SHA-256 is checked against the pin
# before anything in it is unpacked or run; on a mismatch nothing is installed.
#
# On Windows (Git Bash, MSYS, Cygwin) this hands over to tools/get-dmc.ps1.
#
# For the tests only: DMC_GET_TEST_ASSETS names a local folder to take the archive from instead of GitHub. The
# SHA-256 check still applies.

set -u

repo="abaskett3/dungeon-master-claude"
root="$(cd "$(dirname "$0")/.." && pwd)" || exit 1

fail() {
    printf 'get-dmc: %s\n' "$*" >&2
    exit 1
}

system="$(uname -s 2>/dev/null)"
case "$system" in
    MINGW* | MSYS* | CYGWIN*)
        exec powershell.exe -NoProfile -ExecutionPolicy Bypass -File "$(cygpath -w "$root/tools/get-dmc.ps1")"
        ;;
    Linux) os=linux ;;
    Darwin) os=osx ;;
    *) fail "dmc is not built for ${system:-this system}: it runs on Windows, Linux and macOS" ;;
esac

machine="$(uname -m 2>/dev/null)"
# A shell running under Rosetta on Apple Silicon says x86_64; the Mac itself is arm64.
if [ "$os" = osx ] && [ "$machine" = x86_64 ] && [ "$(sysctl -n sysctl.proc_translated 2>/dev/null)" = 1 ]; then
    machine=arm64
fi
case "$machine" in
    x86_64 | amd64) arch=x64 ;;
    aarch64 | arm64) arch=arm64 ;;
    *) fail "dmc is not built for ${machine:-this} processors on $system: it needs x86-64 or Arm64" ;;
esac
rid="$os-$arch"
if [ "$rid" = osx-x64 ]; then
    fail "dmc is not built for Intel Macs: on macOS it needs Apple Silicon"
fi

pin="$root/tools/dmc-release.json"
[ -f "$pin" ] || fail "tools/dmc-release.json is missing"
tag="$(sed -n 's/^[[:space:]]*"tag"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' "$pin" | head -n 1)"
if [ -z "$tag" ]; then
    fail "no dmc release is pinned yet (tools/dmc-release.json); a developer can build one with: dotnet publish src/Dmc.Cli -r $rid -o bin"
fi
printf '%s\n' "$tag" | grep -Eq '^dmc-v[0-9]+\.[0-9]+\.[0-9]+$' || fail "tools/dmc-release.json: \"$tag\" is not a dmc release tag"
sha="$(sed -n "s/^[[:space:]]*\"$rid\"[[:space:]]*:[[:space:]]*\"\([0-9a-fA-F]\{64\}\)\".*/\1/p" "$pin" | head -n 1 | tr 'A-F' 'a-f')"
[ -n "$sha" ] || fail "tools/dmc-release.json has no SHA-256 for $rid"

version="${tag#dmc-v}"
exe="$root/bin/dmc"
if [ -f "$exe" ] && [ -x "$exe" ] && [ "$("$exe" --version 2>/dev/null)" = "dmc $version" ]; then
    exit 0
fi

asset="$tag-$rid.tar.gz"
mkdir -p "$root/bin" || fail "cannot make the folder bin/"
work="$(mktemp -d "$root/bin/.get-dmc.XXXXXX")" || fail "cannot make a temporary folder in bin/"
trap 'rm -rf "$work"' EXIT

if [ -n "${DMC_GET_TEST_ASSETS:-}" ]; then
    cp "$DMC_GET_TEST_ASSETS/$asset" "$work/$asset" 2>/dev/null || fail "$asset is not in $DMC_GET_TEST_ASSETS"
else
    command -v curl >/dev/null 2>&1 || fail "curl is needed to download dmc"
    url="https://github.com/$repo/releases/download/$tag/$asset"
    curl -fsSL --proto '=https' --tlsv1.2 --retry 2 -o "$work/$asset" "$url" 2>/dev/null || fail "could not download $url"
fi

# The hash is read from standard input, so the file's name cannot change how it is printed.
if [ "$os" = osx ] && command -v shasum >/dev/null 2>&1; then
    actual="$(shasum -a 256 < "$work/$asset")"
elif command -v sha256sum >/dev/null 2>&1; then
    actual="$(sha256sum < "$work/$asset")"
elif command -v shasum >/dev/null 2>&1; then
    actual="$(shasum -a 256 < "$work/$asset")"
else
    fail "sha256sum or shasum is needed to check dmc"
fi
actual="${actual%% *}"
[ "$actual" = "$sha" ] || fail "$asset does not match its SHA-256 in tools/dmc-release.json, so nothing was installed"

mkdir "$work/unpacked" || fail "cannot make a temporary folder in bin/"
tar -xzf "$work/$asset" -C "$work/unpacked" 2>/dev/null || fail "could not unpack $asset"
[ -f "$work/unpacked/dmc" ] || fail "$asset has no dmc in it"
chmod +x "$work/unpacked/dmc" || fail "cannot make dmc executable"
# Run only now that the hash has matched, and before anything replaces what is in bin/.
ran="$("$work/unpacked/dmc" --version 2>&1)"
if [ "$ran" != "dmc $version" ]; then
    fail "the dmc of $tag does not run on this computer, so nothing was installed: $(printf '%s\n' "$ran" | head -n 1)"
fi

# Only the files at the top of the archive (dmc and its three native libraries) go into bin/; dmc itself last.
for file in "$work/unpacked"/*; do
    if [ -f "$file" ] && [ "$file" != "$work/unpacked/dmc" ]; then
        mv -f "$file" "$root/bin/" || fail "cannot write bin/$(basename "$file")"
    fi
done
mv -f "$work/unpacked/dmc" "$exe" || fail "cannot write bin/dmc"
printf 'get-dmc: installed dmc %s (%s) in bin/\n' "$version" "$rid"
