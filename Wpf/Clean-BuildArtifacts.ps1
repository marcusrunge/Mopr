param(
    [string]$RootPath = ".",
    [int]$RetryCount = 3,
    [int]$RetryDelayMilliseconds = 500
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

$blockingProcessNames = @(
    "devenv"
    "MSBuild"
    "dotnet"
    "vstest.console"
    "testhost"
)

$blockingProcesses = @(
    Get-Process -ErrorAction SilentlyContinue |
        Where-Object { $_.ProcessName -in $blockingProcessNames } |
        Sort-Object ProcessName, Id
)

if ($blockingProcesses.Count -gt 0)
{
    Write-Warning "Eine vollstaendige Bereinigung ist nicht moeglich, solange Visual Studio, Build- oder Testprozesse laufen."
    Write-Host ""
    Write-Host "Gefundene blockierende Prozesse:" -ForegroundColor Yellow

    foreach ($blockingProcess in $blockingProcesses)
    {
        Write-Host "  $($blockingProcess.ProcessName) (PID $($blockingProcess.Id))" -ForegroundColor Yellow
    }

    Write-Host ""
    Write-Warning "Schliesse Visual Studio und beende laufende Builds oder Tests. Fuehre das Skript anschliessend erneut aus."
    exit 1
}

Write-Host "Bereinige $root" -ForegroundColor Cyan

# Repeat discovery after every deletion pass because external processes may
# create build directories while the previous deletion pass is running.
for ($pass = 1; $pass -le 3; $pass++)
{
    $buildDirectories = @(
        Get-ChildItem -LiteralPath $root -Directory -Recurse -Force -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -in $buildDirectoryNames } |
            Sort-Object { $_.FullName.Length } -Descending |
            Select-Object -ExpandProperty FullName
    )

    if ($buildDirectories.Count -eq 0)
    {
        break
    }

    Write-Host ""
    Write-Host "Bereinigungsdurchlauf $pass" -ForegroundColor Cyan

    foreach ($directoryPath in $buildDirectories)
    {
        if (-not (Test-Path -LiteralPath $directoryPath -PathType Container))
        {
            continue
        }

        Write-Host "Loesche $directoryPath" -ForegroundColor Yellow

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
                    Write-Warning "Konnte '$directoryPath' nach $RetryCount Versuchen nicht loeschen: $($_.Exception.Message)"
                    break
                }

                Start-Sleep -Milliseconds $RetryDelayMilliseconds
            }
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

foreach ($remainingDirectory in $remainingDirectories)
{
    Write-Warning $remainingDirectory
}

Write-Host ""
Write-Warning "Die Bereinigung ist unvollstaendig. Ein Hintergrundprozess hat Ordner gesperrt oder erneut erstellt."
exit 1