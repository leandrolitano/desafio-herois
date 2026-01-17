# PowerShell - Rodar testes do backend (unit + integration)

Write-Host "[test] Rodando testes do backend (unit + integration)..." -ForegroundColor Cyan

$root = Get-Location
Set-Location backend

dotnet test src/Herois.Tests/Herois.Tests.csproj
if ($LASTEXITCODE -ne 0) {
  Set-Location $root
  exit $LASTEXITCODE
}

dotnet test src/Herois.Api.IntegrationTests/Herois.Api.IntegrationTests.csproj
if ($LASTEXITCODE -ne 0) {
  Set-Location $root
  exit $LASTEXITCODE
}

Set-Location $root
Write-Host "[test] OK" -ForegroundColor Green
