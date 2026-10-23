#!/usr/bin/env bash
# Install dmc, the toolkit's helper program, into bin/: the release pinned in tools/dmc-release.json, for this
# computer. Run it as "bash tools/get-dmc.sh". It prints nothing when bin/dmc is already that release, one line when
# it installs it, and one line on stderr (exit code 1) when it cannot.
#
# The archive comes from this repository's GitHub Release, over HTTPS only. Its SHA-256 is checked against the pin
# before anything in it is unpacked or run; on a mismatch nothing is installed.
#
# On Windows (Git Bash, MSYS, Cygwin) this hands over to tools/get-dmc.ps1. On macOS, which dmc is no longer built
# for, it stops at once with one line, before it reads the pin or touches bin/.
#
# For the tests only: DMC_GET_TEST_ASSETS names a local folder to take the archive from instead of GitHub. The
# SHA-256 check still applies.
#
# Usage:     bash tools/get-dmc.sh        (no arguments; the working folder does not matter)
# Inputs:    tools/dmc-release.json (the tag, and one SHA-256 for each platform); bin/dmc, to see which version is there.
#            It needs curl (not with DMC_GET_TEST_ASSETS), tar, and sha256sum or shasum.
# Outputs:   bin/dmc and the native libraries from the archive, beside it. Nothing is written outside bin/, and the
#            temporary folder it works in (bin/.get-dmc.*) is removed on every exit.
# Exit code: 0 when bin/dmc is the pinned release already, or has just been installed. 1 on any failure, with one line
#            "get-dmc: <what went wrong>" on stderr (an unsupported system or processor, no pin, a download that fails
#            or stalls, a hash that does not match, an archive that cannot be unpacked or whose dmc does not run).
#            On Windows the exit code is that of get-dmc.ps1.
#
# It never installs an archive whose hash does not match the pin, never unpacks or runs anything from an archive before
# the hash has matched, never downloads over plain HTTP, and never replaces a file already in bin/ until the new dmc has
# run. Before that it does create bin/ and its temporary folder, and on Windows it deletes the bin/*.old files left by
# an earlier update. It prints nothing when there is nothing to do.

set -u

repo="abaskett3/dungeon-master-claude"
root="$(cd "$(dirname "$0")/.." && pwd)" || exit 1

fail() {
    printf 'get-dmc: %s\n' "$*" >&2
    exit 1
}

pin="$root/tools/dmc-release.json"
pinned_tag() {
    sed -n 's/^[[:space:]]*"tag"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' "$pin" 2>/dev/null | head -n 1
}

system="$(uname -s 2>/dev/null)"
case "$system" in
    MINGW* | MSYS* | CYGWIN*)
        # Most launches find bin/dmc.exe current: say nothing and do not start PowerShell for that. Old copies of a
        # dmc.exe that was running when it was replaced go too (one still running stays until next time).
        tag="$(pinned_tag)"
        rm -f "$root"/bin/*.old 2>/dev/null
        if [ -n "$tag" ] && [ -f "$root/bin/dmc.exe" ] \
            && [ "$("$root/bin/dmc.exe" --version 2>/dev/null | tr -d '\r')" = "dmc ${tag#dmc-v}" ]; then
            exit 0
        fi
        exec powershell.exe -NoProfile -ExecutionPolicy Bypass -File "$(cygpath -w "$root/tools/get-dmc.ps1")"
        ;;
    Linux) os=linux ;;
    Darwin) fail "dmc is not supported on macOS: it runs on Windows and Linux" ;;
    *) fail "dmc is not built for ${system:-this system}: it runs on Windows and Linux" ;;
esac

machine="$(uname -m 2>/dev/null)"
case "$machine" in
    x86_64 | amd64) arch=x64 ;;
    aarch64 | arm64) arch=arm64 ;;
    *) fail "dmc is not built for ${machine:-this} processors on $system: it needs x86-64 or Arm64" ;;
esac
rid="$os-$arch"

[ -f "$pin" ] || fail "tools/dmc-release.json is missing"
tag="$(pinned_tag)"
if [ -z "$tag" ]; then
    fail "no dmc release is pinned yet (tools/dmc-release.json); a developer can build one with: dotnet publish src/Dmc.Cli -r $rid -o artifacts/bin"
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
    # No network, or one that drops the connection, gives up within seconds so that the launcher can start Claude Code
    # without dmc: 10 s to connect, and under 1 KB/s for 30 s ends a stalled download. A slow one that keeps going
    # may take up to 10 minutes.
    curl -fsSL --proto '=https' --tlsv1.2 --connect-timeout 10 --speed-limit 1024 --speed-time 30 --max-time 600 \
        -o "$work/$asset" "$url" 2>/dev/null || fail "could not download $url"
fi

# The hash is read from standard input, so the file's name cannot change how it is printed.
if command -v sha256sum >/dev/null 2>&1; then
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
