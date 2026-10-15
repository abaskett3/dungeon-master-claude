#!/usr/bin/env bash
# Write tools/dmc-release.json for a release: its tag, and the SHA-256 of each archive, read from the
# "<hash>  <archive>" files (<tag>-<rid>.zip.sha256, <tag>-<rid>.tar.gz.sha256) in a folder.
#
#   bash .github/release/write-pin.sh <tag> <folder> [<pin file>]
#
# The release workflow runs it twice: in each build job, with that job's one archive, to install it with get-dmc;
# and in the pin job, with all of them, for the pull request. It keeps the "about" line of the pin file as it is
# and writes one key per line, which tools/get-dmc.sh relies on (it reads the file without a JSON parser).
set -euo pipefail

tag="$1"
folder="$2"
pin="${3:-tools/dmc-release.json}"

if ! printf '%s\n' "$tag" | grep -Eq '^dmc-v[0-9]+\.[0-9]+\.[0-9]+$'; then
    echo "write-pin: \"$tag\" is not a dmc release tag (dmc-vX.Y.Z)" >&2
    exit 1
fi
if [ ! -f "$pin" ]; then
    echo "write-pin: $pin is missing" >&2
    exit 1
fi
# The about line is copied as it is, without the comma it may or may not have (it is written first, so it gets one).
about="$( (grep -m 1 '^[[:space:]]*"about"[[:space:]]*:' "$pin" || true) | tr -d '\r' | sed 's/[[:space:]]*$//; s/,$//')"
if [ -z "$about" ]; then
    echo "write-pin: $pin has no \"about\" line to keep" >&2
    exit 1
fi

entries=()
for rid in win-x64 win-arm64 linux-x64 linux-arm64 osx-arm64 osx-x64; do
    seen=""
    for extension in zip tar.gz; do
        file="$folder/$tag-$rid.$extension.sha256"
        [ -f "$file" ] || continue
        if [ -n "$seen" ]; then
            echo "write-pin: $folder has two archives for $rid ($seen and $(basename "$file")); a pin holds one" >&2
            exit 1
        fi
        seen="$(basename "$file")"
        hash="$(cut -d ' ' -f 1 < "$file" | tr 'A-F' 'a-f')"
        if ! printf '%s\n' "$hash" | grep -Eq '^[0-9a-f]{64}$'; then
            echo "write-pin: $file does not start with a SHA-256" >&2
            exit 1
        fi
        entries+=("    \"$rid\": \"$hash\"")
    done
done

found="$(find "$folder" -maxdepth 1 -name '*.sha256' | wc -l | tr -d ' ')"
if [ "${#entries[@]}" -eq 0 ] || [ "${#entries[@]}" -ne "$found" ]; then
    echo "write-pin: $folder has $found .sha256 files, of which ${#entries[@]} are archives of $tag" >&2
    exit 1
fi

{
    echo "{"
    echo "$about,"
    echo "  \"tag\": \"$tag\","
    echo "  \"sha256\": {"
    last=$((${#entries[@]} - 1))
    for i in "${!entries[@]}"; do
        if [ "$i" -lt "$last" ]; then echo "${entries[$i]},"; else echo "${entries[$i]}"; fi
    done
    echo "  }"
    echo "}"
} > "$pin.new"
mv "$pin.new" "$pin"
