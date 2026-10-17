#!/usr/bin/env bash
# Install dmc, the toolkit's helper program, into bin/: the release pinned in tools/dmc-release.json, for this
# computer. Run it as "bash tools/get-dmc.sh". It prints nothing when bin/dmc is already that release, one line when
# it installs it, and one line on stderr (exit code 1) when it cannot.
#
# The archive comes from this repository's GitHub Release, over HTTPS only. Its SHA-256 is checked against the pin
# before anything in it is unpacked or run; on a mismatch nothing is installed.
#
# What it installed is recorded in bin/.dmc-release (the tag, and the SHA-256 of the installed program, one key per
# line). bin/dmc counts as the pinned release only when that stamp names the pinned tag, the program still has the
# stamp's hash, and it prints the pinned version. A build made on this computer (dotnet publish ... -o bin) has no
# stamp, and carries the same version number as the release it was made for, so it is replaced by the release.
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

pin="$root/tools/dmc-release.json"
pinned_tag() {
    sed -n 's/^[[:space:]]*"tag"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' "$pin" 2>/dev/null | head -n 1
}

system="$(uname -s 2>/dev/null)"

# Sets $sha256 to the SHA-256 of the file $1, in lower case (both tools print it so). The file is read from standard
# input, so its name cannot change how the hash is printed. Fails when there is no tool for it. This and is_current
# use shell built-ins where they can: starting a program costs far more than hashing on Windows.
sha256_of() {
    if [ "$system" = Darwin ] && command -v shasum >/dev/null 2>&1; then
        sha256="$(shasum -a 256 < "$1")"
    elif command -v sha256sum >/dev/null 2>&1; then
        sha256="$(sha256sum < "$1")"
    elif command -v shasum >/dev/null 2>&1; then
        sha256="$(shasum -a 256 < "$1")"
    else
        return 1
    fi
    sha256="${sha256%% *}"
    [ -n "$sha256" ]
}

stamp="$root/bin/.dmc-release"

# Reads bin/.dmc-release into $stamped_tag and $stamped_sha (the first line of each key). Fails when there is no stamp.
read_stamp() {
    stamped_tag=''
    stamped_sha=''
    [ -f "$stamp" ] || return 1
    while IFS= read -r line || [ -n "$line" ]; do
        line="${line%$'\r'}"
        case "$line" in
            tag=*) if [ -z "$stamped_tag" ]; then stamped_tag="${line#tag=}"; fi ;;
            sha256=*) if [ -z "$stamped_sha" ]; then stamped_sha="${line#sha256=}"; fi ;;
        esac
    done < "$stamp"
}

# True when the program $1 is the release $2 that get-dmc installed: the stamp names that tag, the program has the
# hash in the stamp, and it prints that version. Quiet, and fast enough to run at every launch.
is_current() {
    [ -n "$2" ] && [ -f "$1" ] || return 1
    read_stamp && [ "$stamped_tag" = "$2" ] && [ -n "$stamped_sha" ] || return 1
    sha256_of "$1" && [ "$sha256" = "$stamped_sha" ] || return 1
    ran="$("$1" --version 2>/dev/null)"
    [ "${ran//$'\r'/}" = "dmc ${2#dmc-v}" ]
}

case "$system" in
    MINGW* | MSYS* | CYGWIN*)
        # Most launches find bin/dmc.exe current: say nothing and do not start PowerShell for that. Old copies of a
        # dmc.exe that was running when it was replaced go too (one still running stays until next time). Without a
        # tool to hash with, this is not current as far as it can tell, and PowerShell has Get-FileHash.
        tag="$(pinned_tag)"
        rm -f "$root"/bin/*.old 2>/dev/null
        if is_current "$root/bin/dmc.exe" "$tag"; then
            exit 0
        fi
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

[ -f "$pin" ] || fail "tools/dmc-release.json is missing"
tag="$(pinned_tag)"
if [ -z "$tag" ]; then
    fail "no dmc release is pinned yet (tools/dmc-release.json); a developer can build one with: dotnet publish src/Dmc.Cli -r $rid -o bin"
fi
printf '%s\n' "$tag" | grep -Eq '^dmc-v[0-9]+\.[0-9]+\.[0-9]+$' || fail "tools/dmc-release.json: \"$tag\" is not a dmc release tag"
sha="$(sed -n "s/^[[:space:]]*\"$rid\"[[:space:]]*:[[:space:]]*\"\([0-9a-fA-F]\{64\}\)\".*/\1/p" "$pin" | head -n 1 | tr 'A-F' 'a-f')"
[ -n "$sha" ] || fail "tools/dmc-release.json has no SHA-256 for $rid"

version="${tag#dmc-v}"
exe="$root/bin/dmc"
if [ -x "$exe" ] && is_current "$exe" "$tag"; then
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

sha256_of "$work/$asset" || fail "sha256sum or shasum is needed to check dmc"
[ "$sha256" = "$sha" ] || fail "$asset does not match its SHA-256 in tools/dmc-release.json, so nothing was installed"

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
# The stamp comes last, and goes in whole: a run cut short before it leaves a stamp that does not match the program,
# and the next run installs again.
sha256_of "$exe" || fail "cannot read bin/dmc to record it"
printf 'tag=%s\nsha256=%s\n' "$tag" "$sha256" > "$work/stamp" && mv -f "$work/stamp" "$stamp" \
    || fail "cannot write bin/.dmc-release"
printf 'get-dmc: installed dmc %s (%s) in bin/\n' "$version" "$rid"
