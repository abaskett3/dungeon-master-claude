# The install line for Windows (and PowerShell on Linux): downloads dmc, the toolkit's helper program, checks it, and hands
# over to "dmc install", which installs dmc for you and writes the DMC folder you play in (issue #67; contract
# specs/003-separate-player-install/contracts/bootstrap.md). Every release carries this file, so the README's line always
# fetches the latest one:
#
#     irm https://github.com/abaskett3/dungeon-master-claude/releases/latest/download/install.ps1 | iex
#     & ([scriptblock]::Create((irm https://github.com/abaskett3/dungeon-master-claude/releases/latest/download/install.ps1))) -Path D:\Games\dmc
#
# It does only what dmc cannot do before it exists: picks the archive for this computer, downloads it and the release's
# SHA256SUMS.txt over HTTPS (15 seconds to connect, gives up when no data arrives for 30 seconds, 10 minutes at most), reads the
# archive's SHA-256 "digest" from GitHub's releases API, checks the archive against both before unpacking anything, unpacks it
# into a new temporary folder, checks that the downloaded dmc runs, then runs its "dmc install" attached to your terminal
# (passing -Path as --path) and ends with its exit code. The temporary folder is removed on every exit. It does not check the
# release's signature: it comes from the same release as everything it would check, and a dmc it downloaded cannot vouch for
# itself. "dmc update" checks the signature. It works in Windows PowerShell 5.1 and PowerShell 7.
#
# If the releases API refuses the request because of GitHub's limit for requests without sign-in (60 an hour per address),
# the archive is checked against SHA256SUMS.txt alone, and one line on stderr says so. Any other API failure is an error.
#
# Usage:     powershell -ExecutionPolicy Bypass -File tools/install.ps1 [-Path FOLDER]      (Windows)
#            pwsh -File tools/install.ps1 [-Path FOLDER]                                    (Linux)
#            or the irm line above, which runs it without saving it. Run through iex or as a script block, it never ends
#            your PowerShell session: it sets $LASTEXITCODE instead of calling exit.
# Inputs:    the latest release of abaskett3/dungeon-master-claude on GitHub. On Linux it needs tar. -Path names the DMC
#            folder, so that dmc install does not ask.
# Outputs:   whatever "dmc install" prints. Nothing else is written outside the temporary folder, and nothing is installed
#            unless both hash checks pass and the downloaded dmc runs.
# Exit code: dmc install's exit code; 1 with one line "install: <what went wrong>" on stderr when an unsupported system or
#            processor, a failed or stalled download, a hash that does not match, an archive that cannot be unpacked or a dmc
#            that does not run stops it first. macOS is refused (dmc is built for Windows and Linux).
#
# For the tests only: DMC_INSTALL_TEST_ASSETS names a local folder to take the release from instead of GitHub (SHA256SUMS.txt,
# the archive, and latest.json holding the API's answer; a latest.json whose message says "API rate limit exceeded" stands for
# the API's refusal); DMC_INSTALL_TEST_MACHINE stands in for the Windows machine type (AMD64, ARM64); DMC_INSTALL_TEST_SYSTEM=macOS
# makes the script behave as on a Mac (PowerShell cannot be made to say it runs on one). Every hash check still applies.
#
# It never installs an archive whose hash does not match both the checksum list and GitHub's digest (the digest only when the
# API's limit refused the request), never unpacks or runs anything from an archive before the hashes have matched, and never
# downloads over plain HTTP.

param([string] $Path)

$ErrorActionPreference = 'Stop'
# The progress bar makes Invoke-WebRequest very slow in Windows PowerShell 5.1.
$ProgressPreference = 'SilentlyContinue'

# Everything is in one function, so that a failure can end the install without ending the PowerShell session the line was
# pasted into (exit would close it), and so that the whole script is read before anything runs.
function Install-Dmc([string] $FolderPath) {
    $repo = 'abaskett3/dungeon-master-claude'
    $base = "https://github.com/$repo/releases/latest/download"
    $api = "https://api.github.com/repos/$repo/releases/latest"
    $assets = $env:DMC_INSTALL_TEST_ASSETS

    # Windows PowerShell 5.1 started from PowerShell 7 (or from Git Bash running in it) inherits 7's module folders, and then
    # cannot load its own Get-FileHash or Expand-Archive. Its own folders are the machine's setting.
    if ($PSVersionTable.PSEdition -ne 'Core') {
        $env:PSModulePath = [Environment]::GetEnvironmentVariable('PSModulePath', 'Machine')
    }

    function Fail([string] $message) {
        [Console]::Error.WriteLine('install: ' + ($message -replace '\s*[\r\n]+\s*', ' ').Trim())
        throw 'install-failed'
    }

    function Get-Platform {
        # macOS is not built for: say so before anything else. $IsMacOS does not exist in Windows PowerShell 5.1.
        if ($env:DMC_INSTALL_TEST_SYSTEM -eq 'macOS' -or ($PSVersionTable.PSEdition -eq 'Core' -and $IsMacOS)) {
            Fail 'dmc is not supported on macOS: it runs on Windows and Linux'
        }
        # $IsWindows does not exist in Windows PowerShell 5.1, which runs only on Windows.
        $onWindows = $PSVersionTable.PSEdition -ne 'Core' -or $IsWindows
        if ($onWindows) {
            # The machine, not this process: an x64 program emulated on Arm64 (Git Bash, say) and everything it starts see
            # PROCESSOR_ARCHITECTURE=AMD64, but the system's own setting says ARM64.
            $machine = $env:DMC_INSTALL_TEST_MACHINE
            if (-not $machine) {
                try {
                    $machine = (Get-ItemProperty -LiteralPath 'HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Environment' -Name PROCESSOR_ARCHITECTURE).PROCESSOR_ARCHITECTURE
                } catch {
                    $machine = $null
                }
            }
            if (-not $machine) { $machine = $env:PROCESSOR_ARCHITEW6432 }
            if (-not $machine) { $machine = $env:PROCESSOR_ARCHITECTURE }
            switch ($machine) {
                'AMD64' { return 'win-x64' }
                'ARM64' { return 'win-arm64' }
                default { Fail "dmc is not built for $machine processors on Windows: it needs x86-64 or Arm64" }
            }
        }
        $machine = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString()
        $os = if ($IsLinux) { 'linux' } else { Fail 'dmc is not built for this system: it runs on Windows and Linux' }
        switch ($machine) {
            'X64' { $arch = 'x64' }
            'Arm64' { $arch = 'arm64' }
            default { Fail "dmc is not built for ${machine} processors: it needs x86-64 or Arm64" }
        }
        return "$os-$arch"
    }

    # The limits: 15 seconds to connect and 30 seconds with no data where PowerShell can say so; before PowerShell 7.4 its
    # -TimeoutSec covers a whole download, so a download there gets 10 minutes.
    function Get-Limits([bool] $download) {
        $limits = @{ TimeoutSec = 30 }
        $command = Get-Command Invoke-WebRequest
        if ($command.Parameters.ContainsKey('OperationTimeoutSeconds')) {
            $limits = @{ ConnectionTimeoutSeconds = 15; OperationTimeoutSeconds = 30 }
        } elseif ($download) {
            $limits = @{ TimeoutSec = 600 }
        }
        return $limits
    }

    # Gets one file of the release into a path: from the local test folder, or over HTTPS from the latest release.
    function Get-ReleaseFile([string] $name, [string] $destination) {
        if ($assets) {
            $source = Join-Path $assets $name
            if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { Fail "$name is not in $assets" }
            Copy-Item -LiteralPath $source -Destination $destination
            return
        }
        $url = "$base/$name"
        # Windows PowerShell 5.1 may not offer TLS 1.2 by itself, and GitHub requires it.
        [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12
        $limits = Get-Limits $true
        try {
            Invoke-WebRequest -Uri $url -OutFile $destination -UseBasicParsing @limits
        } catch {
            Fail "could not download ${url}: $($_.Exception.Message)"
        }
    }

    # The body of a failed web request, in PowerShell 7 and in Windows PowerShell 5.1.
    function Get-ErrorBody($err) {
        if ($err.ErrorDetails -and $err.ErrorDetails.Message) { return [string] $err.ErrorDetails.Message }
        try {
            $stream = $err.Exception.Response.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($stream)
            return $reader.ReadToEnd()
        } catch {
            return ''
        }
    }

    function Get-Sha256([string] $file) {
        return (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash.ToLowerInvariant()
    }

    $rid = Get-Platform
    $windows = $rid.StartsWith('win-')
    $exeName = if ($windows) { 'dmc.exe' } else { 'dmc' }

    $work = Join-Path ([System.IO.Path]::GetTempPath()) ('dmc-install-' + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $work | Out-Null
    try {
        # The checksum list: which archive this release has for this computer, and its hash.
        $sumsFile = Join-Path $work 'SHA256SUMS.txt'
        Get-ReleaseFile 'SHA256SUMS.txt' $sumsFile
        $extension = if ($windows) { 'zip' } else { 'tar.gz' }
        $listed = $null
        $asset = $null
        foreach ($line in (Get-Content -LiteralPath $sumsFile)) {
            if ($line -match "^([0-9a-fA-F]{64}) [ *](dmc-v\d+\.\d+\.\d+-$([regex]::Escape($rid))\.$([regex]::Escape($extension)))\s*$") {
                $listed = $Matches[1].ToLowerInvariant()
                $asset = $Matches[2]
                break
            }
        }
        if (-not $asset) { Fail "the latest release has no archive for $rid" }

        # GitHub's own digest of the archive, from the releases API.
        $digest = $null
        $body = $null
        $limited = $false
        if ($assets) {
            $latest = Join-Path $assets 'latest.json'
            if (-not (Test-Path -LiteralPath $latest -PathType Leaf)) { Fail "latest.json is not in $assets" }
            $body = Get-Content -Raw -LiteralPath $latest
            if ($body -match '(?i)rate limit') { $limited = $true }
        } else {
            [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12
            $limits = Get-Limits $false
            try {
                $body = (Invoke-WebRequest -Uri $api -UseBasicParsing -Headers @{ Accept = 'application/vnd.github+json' } @limits).Content
            } catch {
                $status = $null
                try { $status = [int] $_.Exception.Response.StatusCode } catch { }
                $text = Get-ErrorBody $_
                if (($status -eq 403 -or $status -eq 429) -and $text -match '(?i)rate limit') {
                    $limited = $true
                } elseif ($status) {
                    Fail "could not read the release from GitHub: it answered $status"
                } else {
                    Fail "could not read the release from GitHub: $($_.Exception.Message)"
                }
            }
        }
        if ($limited) {
            [Console]::Error.WriteLine("install: GitHub's API limit was reached, so only the release's checksum list was checked")
        } else {
            try {
                $release = $body | ConvertFrom-Json
            } catch {
                Fail 'could not read the release from GitHub: its answer is not JSON'
            }
            foreach ($candidate in @($release.assets)) {
                if ($candidate.name -eq $asset -and ([string] $candidate.digest) -match '^sha256:([0-9a-fA-F]{64})$') {
                    $digest = $Matches[1].ToLowerInvariant()
                }
            }
            if (-not $digest) { Fail "GitHub reports no digest for $asset, so nothing was installed" }
        }

        $file = Join-Path $work $asset
        Get-ReleaseFile $asset $file
        $actual = Get-Sha256 $file
        if ($actual -ne $listed -or ($digest -and $actual -ne $digest)) {
            Fail "$asset does not match the release's SHA-256 checksums (or GitHub's digest), so nothing was installed"
        }

        $unpacked = Join-Path $work 'unpacked'
        try {
            if ($windows) {
                Expand-Archive -LiteralPath $file -DestinationPath $unpacked
            } else {
                New-Item -ItemType Directory -Path $unpacked | Out-Null
                & tar -xzf $file -C $unpacked | Out-Null
                if ($LASTEXITCODE -ne 0) { throw "tar exited with $LASTEXITCODE" }
            }
        } catch {
            Fail "could not unpack ${asset}: $($_.Exception.Message)"
        }
        $exe = Join-Path $unpacked $exeName
        if (-not (Test-Path -LiteralPath $exe -PathType Leaf)) { Fail "$asset has no $exeName in it" }
        if (-not $windows) { & chmod +x $exe | Out-Null }
        # Run only now that both hashes have matched.
        $ran = $null
        try {
            $ran = ((& $exe --version 2>&1) | Out-String).Trim()
        } catch {
            $ran = $_.Exception.Message
        }
        if ($ran -notmatch '^dmc ') { Fail "the downloaded dmc does not run on this computer, so nothing was installed: $ran" }

        # Attached to the terminal, so that dmc install can ask where the DMC folder goes: its output is not captured, so this
        # function's caller must not capture this function's output either.
        if ($FolderPath) {
            & $exe install --path $FolderPath
        } else {
            & $exe install
        }
        $script:DmcInstallExit = $LASTEXITCODE
    } finally {
        Remove-Item -LiteralPath $work -Recurse -Force -ErrorAction SilentlyContinue
    }
}

$script:DmcInstallExit = 1
try {
    Install-Dmc $Path
} catch {
    if ($_.Exception.Message -ne 'install-failed') {
        [Console]::Error.WriteLine('install: ' + ($_.Exception.Message -replace '\s*[\r\n]+\s*', ' ').Trim())
    }
    $script:DmcInstallExit = 1
}
# As a file, end with the exit code. Under iex or as a script block there is no file: ending the session would close the
# window the line was pasted into, so only $LASTEXITCODE is set.
if ($PSCommandPath) {
    exit $script:DmcInstallExit
}
$global:LASTEXITCODE = $script:DmcInstallExit
