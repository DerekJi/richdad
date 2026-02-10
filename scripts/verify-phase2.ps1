#!/usr/bin/env pwsh
# Phase 2 验证脚本
# 用途：验证四级决策模型的所有功能

Write-Host "================================" -ForegroundColor Cyan
Write-Host "Phase 2 模型验证测试" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

# 检查服务器是否运行
Write-Host "[1/4] 检查服务器状态..." -ForegroundColor Yellow
$serverRunning = $false
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5000/health" -Method GET -TimeoutSec 2 -ErrorAction SilentlyContinue
    $serverRunning = $true
    Write-Host "✓ 服务器正在运行" -ForegroundColor Green
} catch {
    Write-Host "✗ 服务器未运行，正在启动..." -ForegroundColor Red

    # 启动服务器
    Push-Location "$PSScriptRoot\..\src\Trading.Web"
    Start-Process pwsh -ArgumentList "-NoExit", "-Command", "dotnet run" -WindowStyle Minimized
    Pop-Location

    Write-Host "等待服务器启动 (10秒)..." -ForegroundColor Yellow
    Start-Sleep -Seconds 10

    # 再次检查
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:5000/health" -Method GET -TimeoutSec 2 -ErrorAction SilentlyContinue
        $serverRunning = $true
        Write-Host "✓ 服务器已启动" -ForegroundColor Green
    } catch {
        Write-Host "✗ 无法启动服务器" -ForegroundColor Red
        exit 1
    }
}

Write-Host ""

# 运行所有验证测试
Write-Host "[2/4] 运行完整验证测试..." -ForegroundColor Yellow
try {
    $allTestsResponse = Invoke-RestMethod -Uri "http://localhost:5000/api/phase2validation/all" -Method GET

    if ($allTestsResponse.success) {
        Write-Host "✓ 所有验证测试通过！" -ForegroundColor Green
    } else {
        Write-Host "✗ 部分测试失败" -ForegroundColor Red
    }

    Write-Host ""
    Write-Host "测试时间: $($allTestsResponse.timestamp)" -ForegroundColor Gray
} catch {
    Write-Host "✗ 测试失败: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""

# JSON 序列化测试
Write-Host "[3/4] 测试 JSON 序列化..." -ForegroundColor Yellow
try {
    $jsonResponse = Invoke-RestMethod -Uri "http://localhost:5000/api/phase2validation/json" -Method GET

    $models = @("DailyBias", "StructureAnalysis", "SignalDetection", "FinalDecision")
    foreach ($model in $models) {
        $result = $jsonResponse.results.$model
        if ($result.success) {
            Write-Host "  ✓ $model" -ForegroundColor Green
        } else {
            Write-Host "  ✗ $model" -ForegroundColor Red
        }
    }
} catch {
    Write-Host "✗ JSON 测试失败: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# TradingContext 级联验证测试
Write-Host "[4/4] 测试 TradingContext 级联验证..." -ForegroundColor Yellow
try {
    $contextResponse = Invoke-RestMethod -Uri "http://localhost:5000/api/phase2validation/context" -Method GET

    Write-Host "  测试场景数: $($contextResponse.totalScenarios)" -ForegroundColor Gray

    foreach ($scenario in $contextResponse.scenarios) {
        $name = $scenario.scenario
        $level = $scenario.terminatedLevel

        if ($level -eq "None") {
            Write-Host "  ✓ $name - 完整通过" -ForegroundColor Green
        } else {
            Write-Host "  ✓ $name - 在 $level 终止" -ForegroundColor Yellow
        }
    }
} catch {
    Write-Host "✗ Context 测试失败: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "================================" -ForegroundColor Cyan
Write-Host "验证完成！" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

# 显示详细测试端点
Write-Host "可用的测试端点:" -ForegroundColor Cyan
Write-Host "  - http://localhost:5000/api/phase2validation/all" -ForegroundColor Gray
Write-Host "  - http://localhost:5000/api/phase2validation/json" -ForegroundColor Gray
Write-Host "  - http://localhost:5000/api/phase2validation/context" -ForegroundColor Gray
Write-Host "  - http://localhost:5000/api/phase2validation/properties" -ForegroundColor Gray
Write-Host ""
