[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [Parameter(Position = 0)]
    [string]$RootPath = ".\MarcusRunge.Mopr.Workbench",

    [switch]$NoBackup
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Register Windows code-page support when the host requires it.
try {
    [System.Text.Encoding]::RegisterProvider([System.Text.CodePagesEncodingProvider]::Instance)
}
catch {
    # Windows PowerShell already provides Windows-1252 and does not need registration.
}

$windows1252 = [System.Text.Encoding]::GetEncoding(
    1252,
    [System.Text.EncoderExceptionFallback]::new(),
    [System.Text.DecoderExceptionFallback]::new())
$utf8Strict = [System.Text.UTF8Encoding]::new($false, $true)
$utf8NoBom = [System.Text.UTF8Encoding]::new($false)
$utf8Bom = [System.Text.UTF8Encoding]::new($true)

# These characters are typical remnants of UTF-8 bytes decoded as Windows-1252.
$mojibakePattern = '[\u00C2\u00C3\u00C5\u00C6\u00E2\uFFFD]'
$tokenPattern = '[^\s<>]*[\u00C2\u00C3\u00C5\u00C6\u00E2\uFFFD][^\s<>]*'

function Get-MojibakeScore {
    param([Parameter(Mandatory = $true)][string]$Text)

    return [System.Text.RegularExpressions.Regex]::Matches($Text, $mojibakePattern).Count
}

function Repair-MojibakeToken {
    param([Parameter(Mandatory = $true)][string]$Token)

    $current = $Token
    $currentScore = Get-MojibakeScore -Text $current

    # Two passes repair common double corruption such as "ÃƒÂ¤" -> "ä".
    for ($pass = 1; $pass -le 2 -and $currentScore -gt 0; $pass++) {
        try {
            $candidate = $utf8Strict.GetString($windows1252.GetBytes($current))
        }
        catch {
            break
        }

        if ($candidate.Contains([char]0xFFFD)) {
            break
        }

        $candidateScore = Get-MojibakeScore -Text $candidate
        if ($candidateScore -ge $currentScore) {
            break
        }

        $current = $candidate
        $currentScore = $candidateScore
    }

    return $current
}

function Repair-MojibakeText {
    param([Parameter(Mandatory = $true)][string]$Text)

    return [System.Text.RegularExpressions.Regex]::Replace(
        $Text,
        $tokenPattern,
        [System.Text.RegularExpressions.MatchEvaluator]{
            param([System.Text.RegularExpressions.Match]$match)
            return Repair-MojibakeToken -Token $match.Value
        })
}

$resolvedRoot = (Resolve-Path -LiteralPath $RootPath).Path
$resourceFiles = @(Get-ChildItem -LiteralPath $resolvedRoot -Recurse -Filter "Resources.de.resx" -File)

if ($resourceFiles.Count -eq 0) {
    Write-Warning "No Resources.de.resx files were found below '$resolvedRoot'."
    exit 0
}

$changedFiles = [System.Collections.Generic.List[string]]::new()
$cleanFiles = [System.Collections.Generic.List[string]]::new()
$unresolvedFiles = [System.Collections.Generic.List[string]]::new()
$backupRoot = $null

foreach ($file in $resourceFiles) {
    $bytes = [System.IO.File]::ReadAllBytes($file.FullName)
    $hasUtf8Bom = $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
    $offset = if ($hasUtf8Bom) { 3 } else { 0 }
    $content = $utf8Strict.GetString($bytes, $offset, $bytes.Length - $offset)

    if ($content -notmatch $mojibakePattern) {
        $cleanFiles.Add($file.FullName)
        continue
    }

    $repaired = Repair-MojibakeText -Text $content

    if ($repaired -eq $content) {
        $unresolvedFiles.Add($file.FullName)
        Write-Warning "Suspicious text was detected but could not be repaired safely: $($file.FullName)"
        continue
    }

    # Refuse to write malformed XML.
    try {
        $xmlDocument = [System.Xml.XmlDocument]::new()
        $xmlDocument.PreserveWhitespace = $true
        $xmlDocument.LoadXml($repaired)
    }
    catch {
        throw "Repair produced invalid XML for '$($file.FullName)': $($_.Exception.Message)"
    }

    if ($repaired -match $mojibakePattern) {
        $unresolvedFiles.Add($file.FullName)
        Write-Warning "The file still contains suspicious sequences after the safe repair: $($file.FullName)"
    }

    if (-not $PSCmdlet.ShouldProcess($file.FullName, "Repair mojibake in German resources")) {
        continue
    }

    if (-not $NoBackup) {
        if ($null -eq $backupRoot) {
            $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
            $backupRoot = Join-Path $resolvedRoot ".resource-encoding-backup-$timestamp"
            New-Item -ItemType Directory -Path $backupRoot -Force | Out-Null
        }

        $relativePath = $file.FullName.Substring($resolvedRoot.Length).TrimStart('\', '/')
        $backupPath = Join-Path $backupRoot $relativePath
        $backupDirectory = Split-Path -Parent $backupPath
        New-Item -ItemType Directory -Path $backupDirectory -Force | Out-Null
        [System.IO.File]::WriteAllBytes($backupPath, $bytes)
    }

    $outputEncoding = if ($hasUtf8Bom) { $utf8Bom } else { $utf8NoBom }
    $temporaryPath = "$($file.FullName).encoding-repair.tmp"

    try {
        [System.IO.File]::WriteAllText($temporaryPath, $repaired, $outputEncoding)
        Move-Item -LiteralPath $temporaryPath -Destination $file.FullName -Force
    }
    finally {
        if (Test-Path -LiteralPath $temporaryPath) {
            Remove-Item -LiteralPath $temporaryPath -Force
        }
    }

    $changedFiles.Add($file.FullName)
    Write-Host "Repaired: $($file.FullName)" -ForegroundColor Green
}

Write-Host ""
Write-Host "Resource encoding repair summary" -ForegroundColor Cyan
Write-Host "  Scanned:    $($resourceFiles.Count)"
Write-Host "  Repaired:   $($changedFiles.Count)"
Write-Host "  Clean:      $($cleanFiles.Count)"
Write-Host "  Unresolved: $($unresolvedFiles.Count)"

if ($null -ne $backupRoot -and (Test-Path -LiteralPath $backupRoot)) {
    Write-Host "  Backup:     $backupRoot"
}

if ($unresolvedFiles.Count -gt 0) {
    Write-Host ""
    Write-Warning "Review the unresolved files manually before committing."
    foreach ($path in $unresolvedFiles) {
        Write-Host "  $path" -ForegroundColor Yellow
    }
    exit 2
}

exit 0
