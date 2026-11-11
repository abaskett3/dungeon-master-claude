# The recall test of issue #179 (spec 009, SC-002 and SC-003; contract specs/009-retrieval-lookup-recall/contracts/
# recall-test.md): runs "dmc recall" once per question in a questions file and counts, per question style, how often the
# answer is in the output and how often it is in the first hit. It is the repeatable form of the prototype that produced
# the issue's figures (28/30 named and 22/30 indirect answers in the output).
#
# It is for the maintainer's own check on a real play folder, after the merge and after migration 004 has given the
# sequel its Predecessor line. It never runs in CI and writes nothing. The questions file quotes campaign facts, so it
# lives in the maintainer's DMC folder (suggested: recall-test/questions.tsv) and is never committed.
#
# Usage:     pwsh -File tools/recall-test.ps1 -Questions <file> [-Campaign <name>] [-Dmc <path>] [-Folder <DMC folder>]
# Inputs:    the questions file: UTF-8, tab-separated, one question per line, "id<TAB>style<TAB>question<TAB>answer",
#            where style is named or indirect and answer is a .NET regular expression matched case-insensitively;
#            blank lines and lines starting with # are skipped. -Campaign defaults to the active campaign; -Dmc to
#            artifacts/bin/dmc when it exists, else dmc on the PATH; -Folder is passed to dmc recall --folder.
# Output:    one line per question (HIT or MISS, "first" when the answer is in the first hit), then per style
#            "<style>: answer in output a/n; first hit b/n".
# Exit:      0 when every question ran; 1, with one line, when the file cannot be read, a line is malformed, or dmc
#            recall fails for a reason other than "nothing matched".

param(
    [Parameter(Mandatory = $true)][string]$Questions,
    [string]$Campaign,
    [string]$Dmc,
    [string]$Folder
)

$ErrorActionPreference = 'Stop'

if (-not $Dmc) {
    $root = Split-Path -Parent $PSScriptRoot
    $built = Join-Path $root 'artifacts/bin/dmc'
    $Dmc = if ((Test-Path $built) -or (Test-Path "$built.exe")) { $built } else { 'dmc' }
}

if (-not (Test-Path -LiteralPath $Questions -PathType Leaf)) {
    [Console]::Error.WriteLine("recall-test: cannot read $Questions")
    exit 1
}

$rows = @()
$lineNumber = 0
foreach ($line in [IO.File]::ReadAllLines((Resolve-Path -LiteralPath $Questions), [Text.UTF8Encoding]::new($false))) {
    $lineNumber++
    if ($line.Trim().Length -eq 0 -or $line.TrimStart().StartsWith('#')) { continue }
    $cells = $line.Split("`t")
    if ($cells.Count -ne 4 -or $cells[1] -notin @('named', 'indirect') -or $cells[2].Trim().Length -eq 0 -or $cells[3].Length -eq 0) {
        [Console]::Error.WriteLine("recall-test: line $lineNumber of $Questions is not id<TAB>named|indirect<TAB>question<TAB>answer")
        exit 1
    }
    $rows += [pscustomobject]@{ Id = $cells[0]; Style = $cells[1]; Question = $cells[2]; Answer = $cells[3] }
}

$totals = [ordered]@{}
foreach ($row in $rows) {
    $arguments = @('recall')
    if ($Folder) { $arguments += @('--folder', $Folder) }
    if ($Campaign) { $arguments += $Campaign }
    $arguments += $row.Question

    $output = & $Dmc @arguments 2>&1
    $code = $LASTEXITCODE
    $text = ($output | ForEach-Object { "$_" }) -join "`n"
    if ($code -ne 0) {
        [Console]::Error.WriteLine("recall-test: question $($row.Id) ($($row.Style)): $text")
        exit 1
    }

    $lines = @($text -split "`n" | Where-Object { $_ -match ' \| ' })
    $inOutput = [regex]::IsMatch($text, $row.Answer, 'IgnoreCase')
    $inFirst = $lines.Count -gt 0 -and [regex]::IsMatch($lines[0], $row.Answer, 'IgnoreCase')

    if (-not $totals.Contains($row.Style)) { $totals[$row.Style] = [pscustomobject]@{ Output = 0; First = 0; Count = 0 } }
    $total = $totals[$row.Style]
    $total.Count++
    if ($inOutput) { $total.Output++ }
    if ($inFirst) { $total.First++ }

    $mark = if ($inOutput) { 'HIT ' } else { 'MISS' }
    $first = if ($inFirst) { 'first' } else { '     ' }
    '{0,3} {1,-8} {2} {3}  {4}' -f $row.Id, $row.Style, $mark, $first, $row.Question
}

foreach ($style in $totals.Keys) {
    $t = $totals[$style]
    "${style}: answer in output $($t.Output)/$($t.Count); first hit $($t.First)/$($t.Count)"
}
exit 0
