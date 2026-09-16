param(
    [string]$RootPath = "."
)

$root = (Resolve-Path $RootPath).Path

Write-Host "Bereinige $root" -ForegroundColor Cyan

Get-ChildItem `
    -Path $root `
    -Directory `
    -Recurse `
    -Force |
Where-Object {
    $_.Name -eq "bin" `
    -or $_.Name -eq "obj" `
    -or $_.Name -eq ".vs" `
    -or $_.Name -eq "TestResults" `
    -or $_.Name -eq "artifacts"
} |
Sort-Object FullName -Descending |
ForEach-Object {

    Write-Host "Lösche $($_.FullName)" -ForegroundColor Yellow

    try
    {
        Remove-Item `
            -LiteralPath $_.FullName `
            -Recurse `
            -Force `
            -ErrorAction Stop
    }
    catch
    {
        Write-Warning $_.Exception.Message
    }
}

Write-Host ""
Write-Host "Verbleibende Buildordner:" -ForegroundColor Cyan

Get-ChildItem `
    -Path $root `
    -Directory `
    -Recurse `
    -Force |
Where-Object {
    $_.Name -eq "bin" `
    -or $_.Name -eq "obj" `
    -or $_.Name -eq ".vs" `
    -or $_.Name -eq "TestResults" `
    -or $_.Name -eq "artifacts"
} |
Select-Object -ExpandProperty FullName

Write-Host ""
Write-Host "Fertig." -ForegroundColor Green