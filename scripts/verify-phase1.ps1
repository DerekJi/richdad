#!/usr/bin/env pwsh
# Phase 1 验证脚本 - 测试市场数据处理功能

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Phase 1 - 数据基础层验证测试" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

$baseUrl = "http://localhost:5000"
$symbol = "XAUUSD"
$timeFrame = "M5"
$count = 80

Write-Host "测试配置:" -ForegroundColor Yellow
Write-Host "  Base URL: $baseUrl"
Write-Host "  Symbol: $symbol"
Write-Host "  TimeFrame: $timeFrame"
Write-Host "  Count: $count"
Write-Host ""

# 检查应用是否运行
Write-Host "[1/4] 检查应用状态..." -ForegroundColor Green
try {
    $response = Invoke-WebRequest -Uri "$baseUrl/health" -Method GET -ErrorAction SilentlyContinue
    Write-Host "  ✓ 应用正在运行" -ForegroundColor Green
} catch {
    Write-Host "  ✗ 应用未运行，请先启动应用:" -ForegroundColor Red
    Write-Host "    cd src/Trading.Web" -ForegroundColor Yellow
    Write-Host "    dotnet run" -ForegroundColor Yellow
    exit 1
}

Write-Host ""

# 测试1: 完整处理流程
Write-Host "[2/4] 测试完整处理流程..." -ForegroundColor Green
$testUrl = "$baseUrl/api/marketdataprocessor/test?symbol=$symbol&timeFrame=$timeFrame&count=$count"
Write-Host "  URL: $testUrl" -ForegroundColor Gray

try {
    $response = Invoke-RestMethod -Uri $testUrl -Method GET
    Write-Host "  ✓ 处理成功" -ForegroundColor Green
    Write-Host "    - Symbol: $($response.symbol)" -ForegroundColor Gray
    Write-Host "    - TimeFrame: $($response.timeFrame)" -ForegroundColor Gray
    Write-Host "    - Candle Count: $($response.candleCount)" -ForegroundColor Gray
    Write-Host "    - Current Price: $($response.currentPrice)" -ForegroundColor Gray
    Write-Host "    - Current EMA20: $($response.currentEMA20)" -ForegroundColor Gray
    Write-Host "    - Pattern Count: $($response.patternCount)" -ForegroundColor Gray
    Write-Host "    - Context Table Length: $($response.contextTableLength) chars" -ForegroundColor Gray
    Write-Host "    - Focus Table Length: $($response.focusTableLength) chars" -ForegroundColor Gray
} catch {
    Write-Host "  ✗ 测试失败: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# 测试2: Markdown 输出
Write-Host "[3/4] 测试 Markdown 输出..." -ForegroundColor Green
$markdownUrl = "$baseUrl/api/marketdataprocessor/markdown?symbol=$symbol&timeFrame=$timeFrame&count=$count"

try {
    $markdown = Invoke-WebRequest -Uri $markdownUrl -Method GET
    $lines = ($markdown.Content -split "`n").Count
    Write-Host "  ✓ Markdown 生成成功" -ForegroundColor Green
    Write-Host "    - 总行数: $lines" -ForegroundColor Gray
    Write-Host "    - 内容长度: $($markdown.Content.Length) 字符" -ForegroundColor Gray

    # 保存到文件
    $outputFile = "phase1_test_output.md"
    $markdown.Content | Out-File -FilePath $outputFile -Encoding UTF8
    Write-Host "    - 已保存到: $outputFile" -ForegroundColor Gray
} catch {
    Write-Host "  ✗ 测试失败: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# 测试3: 性能基准测试
Write-Host "[4/4] 性能基准测试（可能需要1-2分钟）..." -ForegroundColor Green
$benchmarkUrl = "$baseUrl/api/marketdataprocessor/benchmark?symbol=$symbol&timeFrame=$timeFrame&count=$count&iterations=5"

try {
    $benchmark = Invoke-RestMethod -Uri $benchmarkUrl -Method GET
    Write-Host "  ✓ 基准测试完成" -ForegroundColor Green
    Write-Host "    - 使用缓存: $($benchmark.withCacheAvgMs) ms (平均)" -ForegroundColor Gray
    Write-Host "    - 不使用缓存: $($benchmark.withoutCacheAvgMs) ms (平均)" -ForegroundColor Gray
    Write-Host "    - 加速比: $([math]::Round($benchmark.speedup, 2))x" -ForegroundColor Gray
} catch {
    Write-Host "  ✗ 测试失败: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "验证完成!" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "查看详细报告: docs/issues/planned/PHASE1_COMPLETION_REPORT.md" -ForegroundColor Yellow
