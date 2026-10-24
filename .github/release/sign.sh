#!/usr/bin/env bash
# Write and sign the checksum list of a release: dist/SHA256SUMS.txt, the SHA-256 of every archive, and
# dist/SHA256SUMS.txt.sig, its signature made with the project's private key. The release workflow runs it in the publish job
# (with the key from the secret DMC_RELEASE_SIGNING_KEY of the environment "release") and in the dry run (with a throwaway
# key made in the run), so the dry run tests exactly the steps a release takes (issue #67, research R16).
#
#   bash .github/release/sign.sh <dist folder> <private key file> [<ReleaseKeys.cs>]
#
# The signature is ECDSA P-256 over SHA-256, DER-encoded (openssl dgst -sha256 -sign), which dmc update verifies with the public
# keys built into dmc. After signing, the script checks the signature with the public half of the key, checks that a changed
# list no longer verifies, and, when <ReleaseKeys.cs> is given, that the key's public half is one of the two built into dmc
# there, so a secret that does not match the program is caught before anything is published (every update would refuse it).
#
# Inputs:    <dist folder> holding one "<hash>  <archive>" file (<archive>.sha256) for each archive; <private key file>, a
#            PEM ECDSA P-256 private key (never printed, never copied: the caller deletes it); openssl.
# Outputs:   <dist folder>/SHA256SUMS.txt (sorted by archive name, LF line ends) and <dist folder>/SHA256SUMS.txt.sig.
# Exit code: 0 when both files were written and the checks passed. 1 with one line "sign: <what went wrong>" on stderr when a
#            .sha256 file is missing or malformed, openssl fails, a check fails, or the key is not one built into dmc.
#
# It never prints the key, never writes the key anywhere, and never leaves a partial result: SHA256SUMS.txt.sig is removed
# when a check fails.

set -euo pipefail

fail() {
    printf 'sign: %s\n' "$*" >&2
    rm -f "${dist:-.}/SHA256SUMS.txt.sig"
    exit 1
}

[ $# -ge 2 ] && [ $# -le 3 ] || { printf 'usage: sign.sh <dist folder> <private key file> [<ReleaseKeys.cs>]\n' >&2; exit 1; }
dist="$1"
key="$2"
source_file="${3:-}"
[ -d "$dist" ] || fail "$dist is not a folder"
[ -f "$key" ] || fail "the private key file is missing"

list="$dist/SHA256SUMS.txt"
: > "$list"
count=0
for file in $(ls "$dist"/*.sha256 2>/dev/null | sort); do
    line="$(tr -d '\r' < "$file")"
    if ! printf '%s\n' "$line" | grep -Eq '^[0-9a-f]{64}  dmc-v[0-9]+\.[0-9]+\.[0-9]+-[a-z0-9-]+\.(zip|tar\.gz)$'; then
        fail "$(basename "$file") is not '<sha-256>  <archive>'"
    fi
    printf '%s\n' "$line" >> "$list"
    count=$((count + 1))
done
[ "$count" -gt 0 ] || fail "$dist holds no .sha256 file"

sig="$list.sig"
openssl dgst -sha256 -sign "$key" -out "$sig" "$list" || fail "openssl could not sign the checksum list"

pub="$(mktemp)"
trap 'rm -f "$pub"' EXIT
openssl ec -in "$key" -pubout -out "$pub" 2> /dev/null || fail "openssl could not read the public half of the key"
openssl dgst -sha256 -verify "$pub" -signature "$sig" "$list" > /dev/null || fail "the signature does not verify with the key's public half"
changed="$(mktemp)"
trap 'rm -f "$pub" "$changed"' EXIT
{ cat "$list"; printf 'x\n'; } > "$changed"
if openssl dgst -sha256 -verify "$pub" -signature "$sig" "$changed" > /dev/null 2>&1; then
    fail "a changed checksum list still verifies"
fi

if [ -n "$source_file" ]; then
    [ -f "$source_file" ] || fail "$source_file is not there"
    # The public key's base64 body, without its header, footer or line breaks, must appear in the source: the key is pasted
    # there as a PEM, in a raw string, so its lines are only separated by white space.
    body="$(grep -v -- '-----' "$pub" | tr -d ' \r\n\t')"
    [ -n "$body" ] || fail "the public key is empty"
    if ! tr -d ' \r\n\t' < "$source_file" | grep -qF -- "$body"; then
        fail "the signing key is not one of the keys built into dmc ($source_file): fix the secret, or the keys, before releasing"
    fi
fi
