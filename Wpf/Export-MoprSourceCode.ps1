param(
    [string]$RootPath = $PSScriptRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $RootPath -PathType Container)) {
    throw "Das Wurzelverzeichnis wurde nicht gefunden: $RootPath"
}

$rootPathFull = (Resolve-Path -LiteralPath $RootPath).Path.TrimEnd([char[]]@('\', '/'))
$outputPath = Join-Path -Path $rootPathFull -ChildPath "MOPR-Backend-Source.txt"

$excludedDirectoryNames = @(
    ".git", ".idea", ".vs", ".vscode", "artifacts", "BenchmarkDotNet.Artifacts",
    "bin", "coverage", "coverage-report", "node_modules", "obj", "packages", "TestResults"
)

$includedExtensions = @(
    ".cs", ".xaml", ".csproj", ".sln", ".slnx", ".props", ".targets",
    ".resx", ".json", ".xml", ".config", ".md", ".txt", ".editorconfig",
    ".ruleset", ".runsettings", ".ps1", ".psm1", ".cmd", ".bat", ".yml", ".yaml"
)

$includedFileNames = @(
    ".gitignore", ".gitattributes", "global.json", "nuget.config"
)

function Get-RelativeExportPath {
    param([Parameter(Mandatory)][string]$FullPath)

    return $FullPath.Substring($rootPathFull.Length).TrimStart([char[]]@('\', '/'))
}

function Test-IsExcludedPath {
    param([Parameter(Mandatory)][string]$RelativePath)

    foreach ($pathPart in ($RelativePath -split "[\\/]")) {
        if ($excludedDirectoryNames -icontains $pathPart) {
            return $true
        }
    }

    return $false
}

Write-Host "MOPR-Quellcodeexport" -ForegroundColor Cyan
Write-Host "Wurzelverzeichnis: $rootPathFull"
Write-Host "Ausgabedatei:      $outputPath"

# Die Ausgabedatei wird vor der Suche angelegt, damit Ausgabeort und Schreibzugriff
# sofort eindeutig geprüft sind.
"MOPR SOURCE EXPORT`r`n" | Set-Content -LiteralPath $outputPath -Encoding utf8 -Force

if (-not (Test-Path -LiteralPath $outputPath -PathType Leaf)) {
    throw "Die Ausgabedatei konnte nicht angelegt werden: $outputPath"
}

$files = @(
    Get-ChildItem -LiteralPath $rootPathFull -Recurse -File -Force -ErrorAction Stop |
        Where-Object {
            $relativePath = Get-RelativeExportPath -FullPath $_.FullName
            $isOutputFile = $_.FullName -ieq $outputPath
            $isExcludedPath = Test-IsExcludedPath -RelativePath $relativePath
            $isIncludedType = ($includedExtensions -icontains $_.Extension) -or ($includedFileNames -icontains $_.Name)

            -not $isOutputFile -and -not $isExcludedPath -and $isIncludedType
        } |
        Sort-Object { Get-RelativeExportPath -FullPath $_.FullName }
)

$createdAt = Get-Date -Format "yyyy-MM-dd HH:mm:ss zzz"
$header = @"
MOPR SOURCE EXPORT
==================
Root path: $rootPathFull
Created: $createdAt
Discovered files: $($files.Count)
XAML included: Yes
XAML code-behind included: Yes

"@

$header | Set-Content -LiteralPath $outputPath -Encoding utf8 -Force

$processedCount = 0
$successfulCount = 0
$xamlCount = 0
$failedFiles = New-Object "System.Collections.Generic.List[string]"

foreach ($file in $files) {
    $processedCount++
    $relativePath = Get-RelativeExportPath -FullPath $file.FullName
    $percentComplete = if ($files.Count -eq 0) { 100 } else { $processedCount / $files.Count * 100 }

    Write-Progress -Activity "MOPR-Quellcodeexport" -Status "$processedCount von $($files.Count): $relativePath" -PercentComplete $percentComplete

    if ($file.Extension -ieq ".xaml" -or $file.Name -ilike "*.xaml.cs") {
        $xamlCount++
    }

    $separator = "=" * 120
    $fileHeader = @"
$separator
BEGIN FILE: $relativePath
SIZE: $($file.Length) bytes
LAST WRITE UTC: $($file.LastWriteTimeUtc.ToString("yyyy-MM-dd HH:mm:ss"))
$separator

"@

    $fileHeader | Add-Content -LiteralPath $outputPath -Encoding utf8

    try {
        # Jede Projektdatei wird ausschließlich gelesen. Lesefehler werden sichtbar
        # protokolliert, damit die Backend-Abschlussprüfung keine unbemerkte Lücke hat.
        $content = Get-Content -LiteralPath $file.FullName -Raw -ErrorAction Stop

        if ($null -ne $content) {
            $content | Add-Content -LiteralPath $outputPath -Encoding utf8
        }

        $successfulCount++
    }
    catch {
        $failure = "$relativePath | $($_.Exception.Message)"
        $failedFiles.Add($failure)
        "[FILE COULD NOT BE READ]`r`n$($_.Exception.Message)" | Add-Content -LiteralPath $outputPath -Encoding utf8
    }

    $fileFooter = @"

$separator
END FILE: $relativePath
$separator

"@

    $fileFooter | Add-Content -LiteralPath $outputPath -Encoding utf8
}

Write-Progress -Activity "MOPR-Quellcodeexport" -Completed

$summary = @"

EXPORT SUMMARY
==============
Discovered files:      $($files.Count)
Successfully exported: $successfulCount
Failed files:          $($failedFiles.Count)
XAML files:            $xamlCount
Output path:           $outputPath
"@

$summary | Add-Content -LiteralPath $outputPath -Encoding utf8

if ($failedFiles.Count -gt 0) {
    "`r`nFAILED FILES`r`n------------" | Add-Content -LiteralPath $outputPath -Encoding utf8

    foreach ($failedFile in $failedFiles) {
        $failedFile | Add-Content -LiteralPath $outputPath -Encoding utf8
    }
}

$outputFile = Get-Item -LiteralPath $outputPath

Write-Host "Export abgeschlossen." -ForegroundColor Green
Write-Host "Ausgabedatei:        $($outputFile.FullName)"
Write-Host "Dateigroesse:        $($outputFile.Length) Bytes"
Write-Host "Gefundene Dateien:   $($files.Count)"
Write-Host "Erfolgreich gelesen: $successfulCount"
Write-Host "Nicht lesbar:        $($failedFiles.Count)"
Write-Host "XAML-Dateien:        $xamlCount"
