# PowerShell - Subir ambiente com Docker Compose

Write-Host "[dev-up] Subindo ambiente (SQL Server + API + Frontend)..." -ForegroundColor Cyan

# Se quiser definir a senha do SA aqui:
# $env:MSSQL_SA_PASSWORD = "Your_strong_Passw0rd"

docker compose up --build
