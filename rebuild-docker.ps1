# Script para reconstruir la imagen Docker con cambios
Write-Host "Deteniendo contenedores..." -ForegroundColor Yellow
docker-compose down

Write-Host "Reconstruyendo imagen Docker..." -ForegroundColor Green
docker-compose up --build -d

Write-Host "Esperando que la aplicación se inicie..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

Write-Host "Verificando estado..." -ForegroundColor Green
docker-compose ps

Write-Host ""
Write-Host "URLs de acceso:" -ForegroundColor Cyan
Write-Host "  API: http://localhost:8080" -ForegroundColor White
Write-Host "  Swagger: http://localhost:8080" -ForegroundColor White
Write-Host "  Health: http://localhost:8080/health" -ForegroundColor White
Write-Host ""
Write-Host "Para ver logs: docker-compose logs -f" -ForegroundColor Yellow 