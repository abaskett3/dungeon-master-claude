# Dice roller for DMC. Usage: ./scripts/roll.ps1 1d20+5 2d6+3 2d20kh1+5 4d6kh3
#   NdS      roll N dice with S sides (N defaults to 1)
#   khK/klK  keep the highest/lowest K dice (advantage: 2d20kh1, disadvantage: 2d20kl1)
#   +M/-M    flat modifier
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$Rolls
)

if (-not $Rolls) {
    Write-Output "usage: roll.ps1 <dice> [<dice> ...]   e.g. 1d20+5 2d6+3 2d20kh1+5 4d6kh3"
    exit 1
}

$failed = $false
foreach ($expr in $Rolls) {
    $m = [regex]::Match($expr.ToLower(), '^(\d*)d(\d+)(?:(kh|kl)(\d+))?([+-]\d+)?$')
    if (-not $m.Success) {
        Write-Output "${expr}: not a dice expression (try 1d20+5, 2d20kh1, 4d6kh3)"
        $failed = $true
        continue
    }

    $count = 1
    if ($m.Groups[1].Value) { $count = [int]$m.Groups[1].Value }
    $sides = [int]$m.Groups[2].Value
    $keepMode = $m.Groups[3].Value
    $mod = 0
    if ($m.Groups[5].Value) { $mod = [int]$m.Groups[5].Value }

    if ($count -lt 1 -or $count -gt 100 -or $sides -lt 2 -or $sides -gt 1000) {
        Write-Output "${expr}: out of range (1-100 dice, 2-1000 sides)"
        $failed = $true
        continue
    }

    $dice = @(for ($i = 0; $i -lt $count; $i++) { Get-Random -Minimum 1 -Maximum ($sides + 1) })
    $kept = $dice
    $keepText = ''
    if ($keepMode) {
        $keepN = [int]$m.Groups[4].Value
        if ($keepN -lt 1 -or $keepN -gt $count) {
            Write-Output "${expr}: cannot keep $keepN of $count dice"
            $failed = $true
            continue
        }
        $sorted = @($dice | Sort-Object -Descending:($keepMode -eq 'kh'))
        $kept = @($sorted | Select-Object -First $keepN)
        $keepText = " keep [$($kept -join ', ')]"
    }

    $total = ($kept | Measure-Object -Sum).Sum + $mod
    $modText = ''
    if ($mod -gt 0) { $modText = " +$mod" }
    if ($mod -lt 0) { $modText = " $mod" }

    Write-Output "${expr}: [$($dice -join ', ')]$keepText$modText = $total"
}

if ($failed) { exit 1 }
