# DeepSeek Integration Test Script
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  DeepSeek Integration Test" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Starting Trading.Infras.Web service..." -ForegroundColor Yellow
$job = Start-Job -ScriptBlock {
    Set-Location "d:\source\richdad-refactor\src\Trading.Infras.Web"
    dotnet run
}

Write-Host "Service started (Job ID: $($job.Id))" -ForegroundColor Green
Write-Host "Waiting for service to initialize (15 seconds)..." -ForegroundColor Yellow
Start-Sleep -Seconds 15

try {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "  Testing DeepSeek Status API" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan

    $response = Invoke-RestMethod -Uri "http://localhost:5000/api/deepseektest/status" -Method Get

    Write-Host ""
    Write-Host "DeepSeek Status:" -ForegroundColor White
    Write-Host "  Enabled: $($response.DeepSeek.Enabled)" -ForegroundColor $(if ($response.DeepSeek.Enabled) { "Green" } else { "Red" })
    Write-Host "  Has API Key: $($response.DeepSeek.HasApiKey)" -ForegroundColor $(if ($response.DeepSeek.HasApiKey) { "Green" } else { "Yellow" })
    Write-Host "  Endpoint: $($response.DeepSeek.Endpoint)" -ForegroundColor Gray
    Write-Host "  Model: $($response.DeepSeek.ModelName)" -ForegroundColor Gray

    Write-Host ""
    Write-Host "Dual-Tier AI Status:" -ForegroundColor White
    Write-Host "  Provider: $($response.DualTierAI.Provider)" -ForegroundColor $(if ($response.DualTierAI.Provider -eq "DeepSeek") { "Green" } else { "Yellow" })
    Write-Host "  Enabled: $($response.DualTierAI.Enabled)" -ForegroundColor $(if ($response.DualTierAI.Enabled) { "Green" } else { "Red" })

    Write-Host ""
    Write-Host "Configuration:" -ForegroundColor White
    Write-Host "  Max Daily Requests: $($response.DeepSeek.Configuration.MaxDailyRequests)" -ForegroundColor Gray
    Write-Host "  Monthly Budget: `$$($response.DeepSeek.Configuration.MonthlyBudgetLimit)" -ForegroundColor Gray
    Write-Host "  Input Cost: `$$($response.DeepSeek.Configuration.CostPer1MInputTokens)/1M tokens" -ForegroundColor Gray
    Write-Host "  Output Cost: `$$($response.DeepSeek.Configuration.CostPer1MOutputTokens)/1M tokens" -ForegroundColor Gray

    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "  Test Result: SUCCESS" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green

} catch {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "  Test Result: FAILED" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "Error: $_" -ForegroundColor Red
} finally {
    Write-Host ""
    Write-Host "Next Steps:" -ForegroundColor Yellow
    Write-Host "  1. Test connection: http://localhost:5000/api/deepseektest/test-connection" -ForegroundColor Gray
    Write-Host "  2. Test dual-tier: http://localhost:5000/api/deepseektest/test-dual-tier" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Press any key to stop service and exit..." -ForegroundColor Cyan
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

    Write-Host "Stopping service..." -ForegroundColor Yellow
    Stop-Job $job -ErrorAction SilentlyContinue
    Remove-Job $job -ErrorAction SilentlyContinue
    Write-Host "Service stopped." -ForegroundColor Green
}
