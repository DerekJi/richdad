# K线数据持久化系统验证脚本
# 验证 Issue 6 的实现功能

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "K线数据持久化系统验证" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# 1. 检查配置
Write-Host "1. 检查配置文件..." -ForegroundColor Yellow
$appsettings = Get-Content "src/Trading.Web/appsettings.json" | ConvertFrom-Json

# 检查 Azure Table Storage 配置
$azureStorage = $appsettings.AzureTableStorage
Write-Host "   ✓ AzureTableStorage.Enabled: $($azureStorage.Enabled)" -ForegroundColor Green
Write-Host "   ✓ CandleTableName: $($azureStorage.CandleTableName)" -ForegroundColor Green
Write-Host "   ✓ CandleIndicatorTableName: $($azureStorage.CandleIndicatorTableName)" -ForegroundColor Green

if ([string]::IsNullOrEmpty($azureStorage.ConnectionString)) {
    Write-Host "   ⚠ ConnectionString 未配置（将使用开发存储模拟器）" -ForegroundColor Yellow
} else {
    Write-Host "   ✓ ConnectionString 已配置" -ForegroundColor Green
}

# 检查 CandleCache 配置
$candleCache = $appsettings.CandleCache
Write-Host "   ✓ CandleCache.EnableSmartCache: $($candleCache.EnableSmartCache)" -ForegroundColor Green
Write-Host "   ✓ MaxCacheAgeDays: $($candleCache.MaxCacheAgeDays)" -ForegroundColor Green
Write-Host "   ✓ PreloadSymbols: $($candleCache.PreloadSymbols -join ', ')" -ForegroundColor Green
Write-Host ""

# 2. 检查代码文件
Write-Host "2. 检查核心文件是否存在..." -ForegroundColor Yellow
$files = @(
    "src/Trading.Infrastructure/Models/CandleEntity.cs",
    "src/Trading.Infrastructure/Models/CandleIndicatorEntity.cs",
    "src/Trading.Infrastructure/Repositories/ICandleRepository.cs",
    "src/Trading.Infrastructure/Repositories/CandleRepository.cs",
    "src/Trading.Infrastructure/Configuration/CandleCacheSettings.cs",
    "src/Trading.Services/Services/CandleCacheService.cs",
    "src/Trading.Services/Services/CandleInitializationService.cs",
    "src/Trading.Services/Extensions/CandleExtensions.cs",
    "src/Trading.Web/Controllers/CandleController.cs",
    "src/Trading.Web/Configuration/CandleCacheConfiguration.cs"
)

$allExist = $true
foreach ($file in $files) {
    if (Test-Path $file) {
        Write-Host "   ✓ $file" -ForegroundColor Green
    } else {
        Write-Host "   ✗ $file (缺失)" -ForegroundColor Red
        $allExist = $false
    }
}
Write-Host ""

# 3. 编译检查
Write-Host "3. 编译项目..." -ForegroundColor Yellow
$buildOutput = dotnet build TradingSystem.sln --nologo --verbosity quiet 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "   ✓ 编译成功，无错误无警告" -ForegroundColor Green
} else {
    Write-Host "   ✗ 编译失败" -ForegroundColor Red
    Write-Host $buildOutput
    exit 1
}
Write-Host ""

# 4. 启动应用测试
Write-Host "4. API 端点测试准备..." -ForegroundColor Yellow
Write-Host "   提示：需要先启动应用才能测试API" -ForegroundColor Cyan
Write-Host ""
Write-Host "   启动命令：" -ForegroundColor Cyan
Write-Host "   cd src/Trading.Web" -ForegroundColor White
Write-Host "   dotnet run" -ForegroundColor White
Write-Host ""

# 5. 提供测试命令
Write-Host "5. API 测试命令（应用启动后执行）..." -ForegroundColor Yellow
Write-Host ""
Write-Host "   # 获取K线数据（智能缓存）" -ForegroundColor Cyan
Write-Host "   curl -X GET ""http://localhost:5086/api/candle/candles?symbol=XAUUSD&timeFrame=M5&count=100""" -ForegroundColor White
Write-Host ""
Write-Host "   # 刷新缓存" -ForegroundColor Cyan
Write-Host "   curl -X POST ""http://localhost:5086/api/candle/refresh?symbol=XAUUSD&timeFrame=M5""" -ForegroundColor White
Write-Host ""
Write-Host "   # 获取数据统计" -ForegroundColor Cyan
Write-Host "   curl -X GET ""http://localhost:5086/api/candle/stats?symbol=XAUUSD&timeFrame=M5""" -ForegroundColor White
Write-Host ""
Write-Host "   # 初始化历史数据" -ForegroundColor Cyan
Write-Host "   curl -X POST ""http://localhost:5086/api/candle/initialize"" -H ""Content-Type: application/json"" -d ""{\""symbol\"":\""XAUUSD\"",\""timeFrame\"":\""M5\"",\""days\"":30}""" -ForegroundColor White
Write-Host ""

# 6. Azure Storage Explorer 验证
Write-Host "6. Azure Storage Explorer 验证步骤..." -ForegroundColor Yellow
Write-Host "   a) 打开 Azure Storage Explorer" -ForegroundColor Cyan
Write-Host "   b) 连接到本地存储模拟器或 Azure 账户" -ForegroundColor Cyan
Write-Host "   c) 查找表：" -ForegroundColor Cyan
Write-Host "      - Candles (K线原始数据)" -ForegroundColor White
Write-Host "      - CandleIndicators (技术指标数据)" -ForegroundColor White
Write-Host "   d) 检查数据是否正确存储" -ForegroundColor Cyan
Write-Host ""

# 7. 文档检查
Write-Host "7. 检查文档..." -ForegroundColor Yellow
$docs = @(
    "docs/CANDLE_CACHE_GUIDE.md",
    "docs/CANDLE_INITIALIZATION.md"
)

foreach ($doc in $docs) {
    if (Test-Path $doc) {
        Write-Host "   ✓ $doc" -ForegroundColor Green
    } else {
        Write-Host "   ✗ $doc (缺失)" -ForegroundColor Red
    }
}
Write-Host ""

# 总结
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "验证完成总结" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
if ($allExist -and $LASTEXITCODE -eq 0) {
    Write-Host "✓ 所有文件已就绪，编译成功" -ForegroundColor Green
    Write-Host "✓ 可以启动应用进行实际测试" -ForegroundColor Green
    Write-Host ""
    Write-Host "下一步：" -ForegroundColor Yellow
    Write-Host "1. 配置 Azure Table Storage 连接字符串（或使用 Azurite 模拟器）" -ForegroundColor White
    Write-Host "2. cd src/Trading.Web && dotnet run" -ForegroundColor White
    Write-Host "3. 使用上述 curl 命令测试 API" -ForegroundColor White
} else {
    Write-Host "✗ 验证发现问题，请检查上述错误" -ForegroundColor Red
}
Write-Host ""
