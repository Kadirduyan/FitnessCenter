# FitnessCenter Uygulamasını Başlatma Script'i
# Bu script port çakışması sorununu otomatik olarak çözer

Write-Host "=== FitnessCenter Uygulaması Başlatılıyor ===" -ForegroundColor Green
Write-Host ""

# Port 5253'ü kullanan process'leri bul ve durdur
Write-Host "Port 5253 kontrol ediliyor..." -ForegroundColor Yellow
$portProcess = netstat -ano | findstr :5253 | Select-String "LISTENING"

if ($portProcess) {
    $pid = ($portProcess -split '\s+')[-1]
    Write-Host "Port 5253 kullanımda (PID: $pid). Process durduruluyor..." -ForegroundColor Yellow
    taskkill /F /PID $pid 2>$null
    Start-Sleep -Seconds 2
    Write-Host "Process durduruldu." -ForegroundColor Green
} else {
    Write-Host "Port 5253 boş." -ForegroundColor Green
}

Write-Host ""
Write-Host "Uygulama başlatılıyor..." -ForegroundColor Cyan
Write-Host "URL: http://localhost:5253" -ForegroundColor Cyan
Write-Host "Durdurmak için Ctrl+C tuşlarına basın" -ForegroundColor Yellow
Write-Host ""

# Uygulamayı başlat
cd $PSScriptRoot
dotnet run --urls "http://localhost:5253"
