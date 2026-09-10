param(
    [string]$RootPath = "."
)

$root = (Resolve-Path $RootPath).Path

Write-Host "Bereinige $root" -ForegroundColor Cyan

$folders = @("bin", "obj", ".vs", "TestResults")

foreach ($folder in $folders)
{
    Get-ChildItem -Path $root -Recurse -Force -Directory -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -eq $folder } |
        ForEach-Object {
            Write-Host "Lösche $($_.FullName)" -ForegroundColor Yellow

            try
            {
                Remove-Item -LiteralPath $_.FullName -Recurse -Force -ErrorAction Stop
            }
            catch
            {
                Write-Warning "Konnte nicht löschen: $($_.FullName)"
                Write-Warning $_.Exception.Message
            }
        }
}

Write-Host ""
Write-Host "Verbleibende Build-Verzeichnisse:" -ForegroundColor Cyan

Get-ChildItem -Path $root -Recurse -Force -Directory -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -in @("bin","obj",".vs","TestResults") } |
    Select-Object -ExpandProperty FullName

Write-Host ""
Write-Host "Fertig." -ForegroundColor Green