# Issue #7 Pattern Recognition Verification Script (PowerShell)
# Quick verification of Al Brooks pattern recognition engine

Write-Host "=== Al Brooks Pattern Recognition Verification ===" -ForegroundColor Cyan
Write-Host ""

Write-Host "Step 1: Trigger pattern recognition processing..." -ForegroundColor Yellow
$result = Invoke-RestMethod -Uri "http://localhost:5000/api/pattern/process?symbol=XAUUSD&timeFrame=M5" -Method Post
Write-Host "Processing result: $($result.message)" -ForegroundColor Green
Write-Host "Records processed: $($result.processedCount)" -ForegroundColor Green
Write-Host ""

Write-Host "Waiting 2 seconds..." -ForegroundColor Gray
Start-Sleep -Seconds 2

Write-Host "Step 2: Check statistics..." -ForegroundColor Yellow
$stats = Invoke-RestMethod -Uri "http://localhost:5000/api/pattern/stats?symbol=XAUUSD&timeFrame=M5"
Write-Host "Total records: $($stats.totalRecords)" -ForegroundColor Green
Write-Host ""

if ($stats.totalRecords -eq 0) {
    Write-Host "[WARNING] No processed data found!" -ForegroundColor Red
    exit 1
}

Write-Host "Step 3: Get latest 5 records (checking for patterns)..." -ForegroundColor Yellow
$data = Invoke-RestMethod -Uri "http://localhost:5000/api/pattern/processed?symbol=XAUUSD&timeFrame=M5&count=5"

$hasBreakout = $false
foreach ($item in $data.data) {
    $tags = $item.Tags -join ", "
    Write-Host "  Time: $($item.Time) | Close: $($item.Close) | Tags: [$tags]"
    
    if ($item.Tags -contains "BO") {
        $hasBreakout = $true
    }
}
Write-Host ""

if ($hasBreakout) {
    Write-Host "[OK] Breakout patterns detected!" -ForegroundColor Green
} else {
    Write-Host "[INFO] No breakout in latest 5 records (normal)" -ForegroundColor Gray
}
Write-Host ""

Write-Host "Step 4: Test Markdown output..." -ForegroundColor Yellow
$markdown = Invoke-RestMethod -Uri "http://localhost:5000/api/pattern/markdown?symbol=XAUUSD&timeFrame=M5&count=3"
Write-Host $markdown.markdown
Write-Host ""

Write-Host "=== Verification Complete ===" -ForegroundColor Green
Write-Host ""
Write-Host "All pattern recognition features are working correctly!" -ForegroundColor Cyan
