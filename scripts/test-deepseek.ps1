# DeepSeek集成测试脚本
Write-Host "================================" -ForegroundColor Cyan
Write-Host "DeepSeek集成测试" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

# 启动服务
Write-Host "▶️  启动服务..." -ForegroundColor Yellow
$job = Start-Job -ScriptBlock {
    Set-Location "d:\source\richdad-refactor\src\Trading.Infras.Web"
    dotnet run
}

Write-Host "✅ 服务已启动 (Job ID: $($job.Id))" -ForegroundColor Green
Write-Host "⏳ 等待服务启动完成 (15秒)..." -ForegroundColor Yellow
Start-Sleep -Seconds 15

try {
    Write-Host ""
    Write-Host "================================" -ForegroundColor Cyan
    Write-Host "1. 检查DeepSeek配置状态" -ForegroundColor Cyan
    Write-Host "================================" -ForegroundColor Cyan

    $status = Invoke-RestMethod -Uri "http://localhost:5000/api/deepseektest/status" -Method Get
    $status | ConvertTo-Json -Depth 10

    Write-Host ""
    Write-Host ""
    Write-Host "================================" -ForegroundColor Cyan
    Write-Host "2. 分析结果" -ForegroundColor Cyan
    Write-Host "================================" -ForegroundColor Cyan

    if ($status.DeepSeek.Enabled) {
        Write-Host "✅ DeepSeek已启用" -ForegroundColor Green
    }
    else {
        Write-Host "❌ DeepSeek未启用" -ForegroundColor Red
    }

    if ($status.DeepSeek.HasApiKey) {
        Write-Host "✅ API Key已配置" -ForegroundColor Green
    }
    else {
        Write-Host "⚠️  API Key未配置" -ForegroundColor Yellow
    }

    if ($status.DualTierAI.Provider -eq "DeepSeek") {
        Write-Host "✅ 双级AI使用DeepSeek" -ForegroundColor Green
    }
    else {
        $provider = $status.DualTierAI.Provider
        Write-Host "⚠️  双级AI未使用DeepSeek (当前: $provider)" -ForegroundColor Yellow
    }

    Write-Host ""
    Write-Host "📊 配置信息:" -ForegroundColor Cyan
    Write-Host "  端点: $($status.DeepSeek.Endpoint)" -ForegroundColor Gray
    Write-Host "  模型: $($status.DeepSeek.ModelName)" -ForegroundColor Gray
    Write-Host "  每日限制: $($status.DeepSeek.Configuration.MaxDailyRequests) 次" -ForegroundColor Gray
    Write-Host "  月度预算: `$$($status.DeepSeek.Configuration.MonthlyBudgetLimit)" -ForegroundColor Gray
    Write-Host ""
    Write-Host "💰 成本信息:" -ForegroundColor Cyan
    Write-Host "  输入Token: `$$($status.DeepSeek.Configuration.CostPer1MInputTokens)/1M" -ForegroundColor Gray
    Write-Host "  输出Token: `$$($status.DeepSeek.Configuration.CostPer1MOutputTokens)/1M" -ForegroundColor Gray

}
catch {
    Write-Host "❌ 无法连接到服务: $_" -ForegroundColor Red
}
finally {
    Write-Host ""
    Write-Host ""
    Write-Host "================================" -ForegroundColor Cyan
    Write-Host "测试完成" -ForegroundColor Cyan
    Write-Host "================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "💡 后续测试:" -ForegroundColor Yellow
    Write-Host "  1. 测试连接: Invoke-RestMethod 'http://localhost:5000/api/deepseektest/test-connection'" -ForegroundColor Gray
    Write-Host "  2. 测试双级AI: Invoke-RestMethod 'http://localhost:5000/api/deepseektest/test-dual-tier' -Method Post" -ForegroundColor Gray
    Write-Host ""
    Write-Host "🛑 关闭服务: Stop-Job $($job.Id); Remove-Job $($job.Id)" -ForegroundColor Yellow
    Write-Host ""

    # 保持服务运行，等待用户决定
    Write-Host "⏸️  服务继续运行，按任意键停止并退出..." -ForegroundColor Cyan
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

    Write-Host "🛑 正在停止服务..." -ForegroundColor Yellow
    Stop-Job $job
    Remove-Job $job
    Write-Host "✅ 服务已停止" -ForegroundColor Green
}
