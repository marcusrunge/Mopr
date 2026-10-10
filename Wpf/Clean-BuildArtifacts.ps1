param(
    [string]$RootPath = ".",
    [int]$RetryCount = 3,
    [int]$RetryDelayMilliseconds = 300
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root = (Resolve-Path -LiteralPath $RootPath).Path
$buildDirectoryNames = @(
    "bin"
    "obj"
    ".vs"
    "TestResults"
    "artifacts"
)

Write-Host "Bereinige $root" -ForegroundColor Cyan

# Materialize all paths before deleting anything. This prevents recursive
# enumeration from changing while parent and child directories are removed.
$buildDirectories = @(
    Get-ChildItem -LiteralPath $root -Directory -Recurse -Force -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -in $buildDirectoryNames } |
    Sort-Object { $_.FullName.Length } -Descending |
    Select-Object -ExpandProperty FullName
)

foreach ($directoryPath in $buildDirectories)
{
    if (-not (Test-Path -LiteralPath $directoryPath -PathType Container))
    {
        continue
    }

    Write-Host "Lösche $directoryPath" -ForegroundColor Yellow

    for ($attempt = 1; $attempt -le $RetryCount; $attempt++)
    {
        try
        {
            Remove-Item -LiteralPath $directoryPath -Recurse -Force -ErrorAction Stop

            if (-not (Test-Path -LiteralPath $directoryPath))
            {
                break
            }

            throw "Das Verzeichnis ist nach Remove-Item weiterhin vorhanden."
        }
        catch
        {
            if ($attempt -eq $RetryCount)
            {
                Write-Warning "Konnte '$directoryPath' nach $RetryCount Versuchen nicht löschen: $($_.Exception.Message)"
                break
            }

            Start-Sleep -Milliseconds $RetryDelayMilliseconds
        }
    }
}

$remainingDirectories = @(
    Get-ChildItem -LiteralPath $root -Directory -Recurse -Force -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -in $buildDirectoryNames } |
    Sort-Object FullName |
    Select-Object -ExpandProperty FullName
)

Write-Host ""
Write-Host "Verbleibende Buildordner:" -ForegroundColor Cyan

if ($remainingDirectories.Count -eq 0)
{
    Write-Host "Keine." -ForegroundColor Green
    Write-Host ""
    Write-Host "Bereinigung erfolgreich abgeschlossen." -ForegroundColor Green
    exit 0
}

$remainingDirectories | ForEach-Object { Write-Warning $_ }

Write-Host ""
Write-Warning "Die Bereinigung ist unvollständig. Schließe Visual Studio sowie laufende Build- und Testprozesse und führe das Skript erneut aus."
exit 1