# PowerShell - Resetar banco (REMOVE volumes!)

Write-Host "[reset-db] ATENCAO: isso remove volumes (dados) do SQL Server!" -ForegroundColor Yellow
Write-Host "[reset-db] Derrubando containers e apagando volumes..." -ForegroundColor Cyan

docker compose down -v

Write-Host "[reset-db] Subindo tudo novamente..." -ForegroundColor Cyan

docker compose up --build
