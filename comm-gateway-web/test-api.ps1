# API Test Script

Write-Host "====== API Connection Test ======" -ForegroundColor Cyan

# Test endpoints to try
$endpoints = @(
    "/",
    "/api",
    "/api/Device",
    "/swagger",
    "/swagger/index.html"
)

# Test direct access to WebAPI
Write-Host "`nTesting direct access to WebAPI at port 5000..." -ForegroundColor Yellow
foreach ($endpoint in $endpoints) {
    $url = "http://localhost:5000$endpoint"
    Write-Host "Testing: $url" -ForegroundColor Gray
    try {
        $response = Invoke-WebRequest -Uri $url -UseBasicParsing -ErrorAction Stop
        Write-Host "√ Success, status code: $($response.StatusCode)" -ForegroundColor Green
    } catch {
        Write-Host "× Failed: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Test access to WebAPI via Nginx proxy
Write-Host "`nTesting access to WebAPI via Nginx proxy at port 8080..." -ForegroundColor Yellow
foreach ($endpoint in $endpoints) {
    $url = "http://localhost:8080$endpoint"
    Write-Host "Testing: $url" -ForegroundColor Gray
    try {
        $response = Invoke-WebRequest -Uri $url -UseBasicParsing -ErrorAction Stop
        Write-Host "√ Success, status code: $($response.StatusCode)" -ForegroundColor Green
    } catch {
        Write-Host "× Failed: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "`n====== Test Complete ======" -ForegroundColor Cyan 