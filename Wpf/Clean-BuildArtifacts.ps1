param(
    [string]$RootPath = "."
)

$root = Resolve-Path $RootPath

Write-Host "Bereinige Build-Artefakte unter: $root" -ForegroundColor Cyan

$directoriesToDelete = @(
    "bin",
    "obj",
    ".vs",
    "TestResults"
)

foreach ($directoryName in $directoriesToDelete)
{
    Get-ChildItem -Path $root -Directory -Recurse -Force -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -eq $directoryName } |
        ForEach-Object {
            Write-Host "Lösche Verzeichnis: $($_.FullName)" -ForegroundColor Yellow
            Remove-Item -LiteralPath $_.FullName -Recurse -Force -ErrorAction SilentlyContinue
        }
}

$filesToDelete = @(
    "*.dll",
    "*.exe",
    "*.pdb",
    "*.cache",
    "*.deps.json",
    "*.runtimeconfig.json"
)

foreach ($pattern in $filesToDelete)
{
    Get-ChildItem -Path $root -File -Recurse -Force -Filter $pattern -ErrorAction SilentlyContinue |
        ForEach-Object {
            Write-Host "Lösche Datei: $($_.FullName)" -ForegroundColor DarkYellow
            Remove-Item -LiteralPath $_.FullName -Force -ErrorAction SilentlyContinue
        }
}

Write-Host "Bereinigung abgeschlossen." -ForegroundColor Green