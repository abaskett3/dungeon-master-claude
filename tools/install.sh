#!/bin/sh
# The install line for Linux: downloads dmc, the toolkit's helper program, checks it, and hands over to "dmc install",
# which installs dmc for you and writes the DMC folder you play in (issue #67; contract specs/003-separate-player-install/
# contracts/bootstrap.md). Every release carries this file, so the README's line always fetches the latest one:
#
#     curl -fsSL https://github.com/abaskett3/dungeon-master-claude/releases/latest/download/install.sh | sh
#     curl -fsSL https://github.com/abaskett3/dungeon-master-claude/releases/latest/download/install.sh | sh -s -- --path ~/games/dmc
#
# It does only what dmc cannot do before it exists: picks the archive for this computer, downloads it and the release's
# SHA256SUMS.txt over HTTPS (10 seconds to connect, gives up when under 1 KB/s for 30 seconds, 10 minutes at most), reads the
# archive's SHA-256 "digest" from GitHub's releases API, checks the archive against both before unpacking anything, unpacks
# it into a new temporary folder, checks that the downloaded dmc runs, then runs its "dmc install" attached to your terminal
# (the folder question is answered from /dev/tty, because standard input is the pipe) and exits with its exit code. The
# temporary folder is removed on every exit. It does not check the release's signature: it comes from the same release as
# everything it would check, and a dmc it downloaded cannot vouch for itself. "dmc update" checks the signature.
#
# If the releases API refuses the request because of GitHub's limit for requests without sign-in (60 an hour per address),
# the archive is checked against SHA256SUMS.txt alone, and one line on stderr says so. Any other API failure is an error.
#
# Usage:     sh install.sh [--path FOLDER]       (the working folder does not matter)
# Inputs:    the latest release of abaskett3/dungeon-master-claude on GitHub. It needs curl (not with the test variable below),
#            tar, and sha256sum or shasum. --path names the DMC folder, so that dmc install does not ask.
# Outputs:   whatever "dmc install" prints. Nothing else is written outside the temporary folder, and nothing is installed
#            unless both hash checks pass and the downloaded dmc runs.
# Exit code: dmc install's exit code; 1 with one line "install: <what went wrong>" on stderr when an unsupported system or
#            processor, a failed or stalled download, a hash that does not match, an archive that cannot be unpacked or a dmc
#            that does not run stops it first. macOS is refused (dmc is built for Windows and Linux); on Windows use install.ps1.
#
# For the tests only: DMC_INSTALL_TEST_ASSETS names a local folder to take the release from instead of GitHub (SHA256SUMS.txt,
# the archive, and latest.json holding the API's answer; a latest.json whose message says "API rate limit exceeded" stands for
# the API's refusal); DMC_INSTALL_TEST_SYSTEM stands in for "uname -s" (Linux, Darwin) and DMC_INSTALL_TEST_MACHINE for "uname -m"
# (x86_64, aarch64). Every hash check still applies to them.
#
# It never installs an archive whose hash does not match both the checksum list and GitHub's digest (the digest only when the
# API's limit refused the request), never unpacks or runs anything from an archive before the hashes have matched, and never
# downloads over plain HTTP.

set -u

main() {
    repo="abaskett3/dungeon-master-claude"
    base="https://github.com/$repo/releases/latest/download"
    api="https://api.github.com/repos/$repo/releases/latest"
    assets="${DMC_INSTALL_TEST_ASSETS:-}"

    have_path=0
    path_arg=""
    while [ $# -gt 0 ]; do
        case "$1" in
            --path)
                [ $# -ge 2 ] || fail "--path needs a folder"
                path_arg="$2"
                have_path=1
                shift 2
                ;;
            --path=*)
                path_arg="${1#--path=}"
                have_path=1
                shift
                ;;
            *) fail "unknown option $1: the only one is --path FOLDER" ;;
        esac
    done
    [ "$have_path" = 0 ] || [ -n "$path_arg" ] || fail "--path needs a folder"

    system="${DMC_INSTALL_TEST_SYSTEM:-$(uname -s 2>/dev/null)}"
    case "$system" in
        Linux) os=linux ;;
        Darwin | macOS) fail "dmc is not supported on macOS: it runs on Windows and Linux" ;;
        MINGW* | MSYS* | CYGWIN*) fail "this is Windows: use the PowerShell install line from the README (install.ps1)" ;;
        *) fail "dmc is not built for ${system:-this system}: it runs on Windows and Linux" ;;
    esac
    machine="${DMC_INSTALL_TEST_MACHINE:-$(uname -m 2>/dev/null)}"
    case "$machine" in
        x86_64 | amd64) arch=x64 ;;
        aarch64 | arm64) arch=arm64 ;;
        *) fail "dmc is not built for ${machine:-this} processors on $system: it needs x86-64 or Arm64" ;;
    esac
    rid="$os-$arch"

    command -v tar >/dev/null 2>&1 || fail "tar is needed to unpack dmc"
    if command -v sha256sum >/dev/null 2>&1; then
        hasher="sha256sum"
    elif command -v shasum >/dev/null 2>&1; then
        hasher="shasum -a 256"
    else
        fail "sha256sum or shasum is needed to check dmc"
    fi
    if [ -z "$assets" ]; then
        command -v curl >/dev/null 2>&1 || fail "curl is needed to download dmc"
    fi

    work="$(mktemp -d "${TMPDIR:-/tmp}/dmc-install.XXXXXX")" || fail "cannot make a temporary folder"
    trap 'rm -rf "$work"' EXIT
    trap 'exit 1' HUP INT TERM

    # The checksum list: which archive this release has for this computer, and its hash.
    get SHA256SUMS.txt "$work/sums.raw" || fail "could not download $base/SHA256SUMS.txt"
    tr -d '\r' < "$work/sums.raw" > "$work/sums"
    asset="$(sed -n "s/^[0-9a-fA-F]\{64\} [ *]\(dmc-v[0-9][0-9]*\.[0-9][0-9]*\.[0-9][0-9]*-$rid\.tar\.gz\)\$/\1/p" "$work/sums" | head -n 1)"
    [ -n "$asset" ] || fail "the latest release has no archive for $rid"
    listed="$(sed -n "s/^\([0-9a-fA-F]\{64\}\) [ *]$asset\$/\1/p" "$work/sums" | head -n 1 | tr 'A-F' 'a-f')"

    # GitHub's own digest of the archive, from the releases API.
    digest=""
    if [ -n "$assets" ]; then
        [ -f "$assets/latest.json" ] || fail "latest.json is not in $assets"
        cp "$assets/latest.json" "$work/latest.json" 2>/dev/null || fail "cannot read $assets/latest.json"
        status=200
        if grep -qi 'rate limit' "$work/latest.json"; then status=403; fi
    else
        status="$(curl -sS --proto '=https' --tlsv1.2 --connect-timeout 10 --max-time 60 -H 'Accept: application/vnd.github+json' \
            -o "$work/latest.json" -w '%{http_code}' "$api" 2>"$work/curl.err")" \
            || fail "could not read the release from GitHub: $(head -n 1 "$work/curl.err")"
    fi
    case "$status" in
        200)
            digest="$(tr -d '\n\r' < "$work/latest.json" | sed 's/"name"[[:space:]]*:/\
&/g' \
                | sed -n "s/^\"name\"[[:space:]]*:[[:space:]]*\"$asset\".*\"digest\"[[:space:]]*:[[:space:]]*\"sha256:\([0-9a-fA-F]\{64\}\)\".*/\1/p" \
                | head -n 1 | tr 'A-F' 'a-f')"
            [ -n "$digest" ] || fail "GitHub reports no digest for $asset, so nothing was installed"
            ;;
        403 | 429)
            if grep -qi 'rate limit' "$work/latest.json"; then
                printf '%s\n' "install: GitHub's API limit was reached, so only the release's checksum list was checked" >&2
            else
                fail "could not read the release from GitHub: it answered $status"
            fi
            ;;
        *) fail "could not read the release from GitHub: it answered $status" ;;
    esac

    get "$asset" "$work/$asset" || fail "could not download $base/$asset"
    # The hash is read from standard input, so the file's name cannot change how it is printed.
    actual="$($hasher < "$work/$asset")"
    actual="${actual%% *}"
    if [ -z "$listed" ] || [ "$actual" != "$listed" ] || { [ -n "$digest" ] && [ "$actual" != "$digest" ]; }; then
        fail "$asset does not match the release's SHA-256 checksums (or GitHub's digest), so nothing was installed"
    fi

    mkdir "$work/unpacked" || fail "cannot make a temporary folder"
    tar -xzf "$work/$asset" -C "$work/unpacked" 2>/dev/null || fail "could not unpack $asset"
    [ -f "$work/unpacked/dmc" ] || fail "$asset has no dmc in it"
    chmod +x "$work/unpacked/dmc" || fail "cannot make dmc executable"
    # Run only now that both hashes have matched.
    ran="$("$work/unpacked/dmc" --version 2>&1)" \
        || fail "the downloaded dmc does not run on this computer, so nothing was installed: $(printf '%s\n' "$ran" | head -n 1)"
    case "$ran" in
        "dmc "*) ;;
        *) fail "the downloaded dmc does not run on this computer, so nothing was installed: $(printf '%s\n' "$ran" | head -n 1)" ;;
    esac

    # Attached to the terminal, so that dmc install can ask where the DMC folder goes.
    if [ "$have_path" = 1 ]; then
        "$work/unpacked/dmc" install --path "$path_arg"
    else
        "$work/unpacked/dmc" install
    fi
    code=$?
    exit "$code"
}

# One line on stderr, exit code 1.
fail() {
    printf 'install: %s\n' "$*" >&2
    exit 1
}

# Gets one file of the release into a path: from the local test folder, or over HTTPS from the latest release. 10 seconds to
# connect; under 1 KB/s for 30 seconds ends a stalled download; 10 minutes at most for a slow one that keeps going.
get() {
    if [ -n "$assets" ]; then
        cp "$assets/$1" "$2" 2>/dev/null
    else
        curl -fsSL --proto '=https' --tlsv1.2 --connect-timeout 10 --speed-limit 1024 --speed-time 30 --max-time 600 \
            -o "$2" "$base/$1" 2>/dev/null
    fi
}

# The whole script is read before anything runs, so a download cut short by the pipe cannot run half of it.
main "$@"
