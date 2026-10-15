# Install dmc, the toolkit's helper program, into bin/: the release pinned in tools/dmc-release.json, for this
# computer. Run it as ./tools/get-dmc.ps1 (Windows PowerShell 5.1 or PowerShell 7, on Windows, Linux or macOS). It
# prints nothing when bin/dmc is already that release, one line when it installs it, and one line on stderr (exit
# code 1) when it cannot.
#
# The archive comes from this repository's GitHub Release, over HTTPS only. Its SHA-256 is checked against the pin
# before anything in it is unpacked or run; on a mismatch nothing is installed.
#
# For the tests only: DMC_GET_TEST_ASSETS names a local folder to take the archive from instead of GitHub. The
# SHA-256 check still applies.

$ErrorActionPreference = 'Stop'
# The progress bar makes Invoke-WebRequest very slow in Windows PowerShell 5.1.
$ProgressPreference = 'SilentlyContinue'

$repo = 'abaskett3/dungeon-master-claude'
$root = Split-Path -Parent $PSScriptRoot

function Fail([string] $message) {
    [Console]::Error.WriteLine('get-dmc: ' + ($message -replace '\s*[\r\n]+\s*', ' ').Trim())
    exit 1
}

function Get-Platform {
    # $IsWindows does not exist in Windows PowerShell 5.1, which runs only on Windows.
    $onWindows = $PSVersionTable.PSEdition -ne 'Core' -or $IsWindows
    if ($onWindows) {
        # A 32-bit or emulated PowerShell names the machine in PROCESSOR_ARCHITEW6432.
        $machine = $env:PROCESSOR_ARCHITEW6432
        if (-not $machine) { $machine = $env:PROCESSOR_ARCHITECTURE }
        switch ($machine) {
            'AMD64' { return 'win-x64' }
            'ARM64' { return 'win-arm64' }
            default { Fail "dmc is not built for $machine processors on Windows: it needs x86-64 or Arm64" }
        }
    }
    $machine = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString()
    $os = if ($IsMacOS) { 'osx' } elseif ($IsLinux) { 'linux' } else { Fail 'dmc is not built for this system: it runs on Windows, Linux and macOS' }
    switch ($machine) {
        'X64' { $arch = 'x64' }
        'Arm64' { $arch = 'arm64' }
        default { Fail "dmc is not built for $machine processors: it needs x86-64 or Arm64" }
    }
    if ("$os-$arch" -eq 'osx-x64') { Fail 'dmc is not built for Intel Macs: on macOS it needs Apple Silicon' }
    return "$os-$arch"
}

function Get-Version([string] $exe) {
    try {
        return ((& $exe --version 2>&1) | Out-String).Trim()
    } catch {
        return $_.Exception.Message
    }
}

try {
    $rid = Get-Platform
    $windows = $rid.StartsWith('win-')

    $pinFile = Join-Path $root 'tools/dmc-release.json'
    if (-not (Test-Path -LiteralPath $pinFile -PathType Leaf)) { Fail 'tools/dmc-release.json is missing' }
    try {
        $pin = Get-Content -Raw -LiteralPath $pinFile | ConvertFrom-Json
    } catch {
        Fail "tools/dmc-release.json is not valid JSON: $($_.Exception.Message)"
    }
    $tag = [string] $pin.tag
    if (-not $tag) {
        Fail "no dmc release is pinned yet (tools/dmc-release.json); a developer can build one with: dotnet publish src/Dmc.Cli -r $rid -o bin"
    }
    if ($tag -notmatch '^dmc-v\d+\.\d+\.\d+$') { Fail "tools/dmc-release.json: `"$tag`" is not a dmc release tag" }
    $sha = [string] $pin.sha256.$rid
    if ($sha -notmatch '^[0-9a-fA-F]{64}$') { Fail "tools/dmc-release.json has no SHA-256 for $rid" }
    $sha = $sha.ToLowerInvariant()

    $version = $tag.Substring('dmc-v'.Length)
    $bin = Join-Path $root 'bin'
    $exeName = if ($windows) { 'dmc.exe' } else { 'dmc' }
    $exe = Join-Path $bin $exeName
    if ((Test-Path -LiteralPath $exe -PathType Leaf) -and (Get-Version $exe) -eq "dmc $version") { exit 0 }

    $asset = if ($windows) { "$tag-$rid.zip" } else { "$tag-$rid.tar.gz" }
    New-Item -ItemType Directory -Force -Path $bin | Out-Null
    # Left over from replacing a dmc.exe that was running (see below); deleted once nothing runs it.
    Get-ChildItem -LiteralPath $bin -Filter '*.old' -File -Force | Remove-Item -Force -ErrorAction SilentlyContinue
    $work = Join-Path $bin ('.get-dmc.' + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $work | Out-Null
    try {
        $file = Join-Path $work $asset
        if ($env:DMC_GET_TEST_ASSETS) {
            $source = Join-Path $env:DMC_GET_TEST_ASSETS $asset
            if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { Fail "$asset is not in $env:DMC_GET_TEST_ASSETS" }
            Copy-Item -LiteralPath $source -Destination $file
        } else {
            $url = "https://github.com/$repo/releases/download/$tag/$asset"
            # Windows PowerShell 5.1 may not offer TLS 1.2 by itself, and GitHub requires it.
            [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12
            try {
                Invoke-WebRequest -Uri $url -OutFile $file -UseBasicParsing
            } catch {
                Fail "could not download ${url}: $($_.Exception.Message)"
            }
        }

        $actual = (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($actual -ne $sha) { Fail "$asset does not match its SHA-256 in tools/dmc-release.json, so nothing was installed" }

        $unpacked = Join-Path $work 'unpacked'
        try {
            if ($windows) {
                Expand-Archive -LiteralPath $file -DestinationPath $unpacked
            } else {
                New-Item -ItemType Directory -Path $unpacked | Out-Null
                & tar -xzf $file -C $unpacked
                if ($LASTEXITCODE -ne 0) { throw "tar exited with $LASTEXITCODE" }
            }
        } catch {
            Fail "could not unpack ${asset}: $($_.Exception.Message)"
        }
        $newExe = Join-Path $unpacked $exeName
        if (-not (Test-Path -LiteralPath $newExe -PathType Leaf)) { Fail "$asset has no $exeName in it" }
        if (-not $windows) { & chmod +x $newExe }
        # Run only now that the hash has matched, and before anything replaces what is in bin/.
        $ran = Get-Version $newExe
        if ($ran -ne "dmc $version") { Fail "the dmc of $tag does not run on this computer, so nothing was installed: $ran" }

        # Only the files at the top of the archive (dmc and its three native libraries) go into bin/; dmc itself last.
        $files = @(Get-ChildItem -LiteralPath $unpacked -File | Sort-Object { $_.Name -eq $exeName })
        foreach ($new in $files) {
            $target = Join-Path $bin $new.Name
            if (Test-Path -LiteralPath $target) {
                try {
                    Remove-Item -LiteralPath $target -Force
                } catch {
                    # Windows cannot delete a dmc.exe that is running (another play session), but it can rename it.
                    Move-Item -LiteralPath $target -Destination "$target.$([guid]::NewGuid().ToString('N')).old" -Force
                }
            }
            Move-Item -LiteralPath $new.FullName -Destination $target
        }
    } finally {
        Remove-Item -LiteralPath $work -Recurse -Force -ErrorAction SilentlyContinue
    }
    [Console]::Out.WriteLine("get-dmc: installed dmc $version ($rid) in bin/")
} catch {
    Fail $_.Exception.Message
}
