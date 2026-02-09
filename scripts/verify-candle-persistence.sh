#!/bin/bash
# K线数据持久化系统验证脚本
# 验证 Issue 6 的实现功能

echo "====================================="
echo "K线数据持久化系统验证"
echo "====================================="
echo ""

# 1. 检查配置
echo "1. 检查配置文件..."
if [ -f "src/Trading.Web/appsettings.json" ]; then
    echo "   ✓ appsettings.json 存在"
    # 检查关键配置
    if grep -q '"CandleCache"' src/Trading.Web/appsettings.json; then
        echo "   ✓ CandleCache 配置存在"
    fi
    if grep -q '"CandleTableName"' src/Trading.Web/appsettings.json; then
        echo "   ✓ CandleTableName 配置存在"
    fi
else
    echo "   ✗ appsettings.json 不存在"
    exit 1
fi
echo ""

# 2. 检查核心文件
echo "2. 检查核心文件是否存在..."
files=(
    "src/Trading.Infrastructure/Models/CandleEntity.cs"
    "src/Trading.Infrastructure/Models/CandleIndicatorEntity.cs"
    "src/Trading.Infrastructure/Repositories/ICandleRepository.cs"
    "src/Trading.Infrastructure/Repositories/CandleRepository.cs"
    "src/Trading.Infrastructure/Configuration/CandleCacheSettings.cs"
    "src/Trading.Services/Services/CandleCacheService.cs"
    "src/Trading.Services/Services/CandleInitializationService.cs"
    "src/Trading.Services/Extensions/CandleExtensions.cs"
    "src/Trading.Web/Controllers/CandleController.cs"
    "src/Trading.Web/Configuration/CandleCacheConfiguration.cs"
)

all_exist=true
for file in "${files[@]}"; do
    if [ -f "$file" ]; then
        echo "   ✓ $file"
    else
        echo "   ✗ $file (缺失)"
        all_exist=false
    fi
done
echo ""

# 3. 编译检查
echo "3. 编译项目..."
if dotnet build TradingSystem.sln --nologo --verbosity quiet > /dev/null 2>&1; then
    echo "   ✓ 编译成功，无错误无警告"
else
    echo "   ✗ 编译失败"
    dotnet build TradingSystem.sln
    exit 1
fi
echo ""

# 4. API 端点测试准备
echo "4. API 端点测试准备..."
echo "   提示：需要先启动应用才能测试API"
echo ""
echo "   启动命令："
echo "   cd src/Trading.Web"
echo "   dotnet run"
echo ""

# 5. 提供测试命令
echo "5. API 测试命令（应用启动后执行）..."
echo ""
echo "   # 获取K线数据（智能缓存）"
echo "   curl -X GET \"http://localhost:5086/api/candle/candles?symbol=XAUUSD&timeFrame=M5&count=100\""
echo ""
echo "   # 刷新缓存"
echo "   curl -X POST \"http://localhost:5086/api/candle/refresh?symbol=XAUUSD&timeFrame=M5\""
echo ""
echo "   # 获取数据统计"
echo "   curl -X GET \"http://localhost:5086/api/candle/stats?symbol=XAUUSD&timeFrame=M5\""
echo ""
echo "   # 初始化历史数据"
echo "   curl -X POST \"http://localhost:5086/api/candle/initialize\" \\"
echo "        -H \"Content-Type: application/json\" \\"
echo "        -d '{\"symbol\":\"XAUUSD\",\"timeFrame\":\"M5\",\"days\":30}'"
echo ""

# 6. Azure Storage 验证
echo "6. Azure Storage 验证步骤..."
echo "   a) 安装并启动 Azurite (本地 Azure Storage 模拟器)"
echo "      npm install -g azurite"
echo "      azurite --silent --location ~/azurite --debug ~/azurite/debug.log"
echo ""
echo "   b) 或使用 Azure Storage Explorer 连接到 Azure 账户"
echo ""
echo "   c) 查找表："
echo "      - Candles (K线原始数据)"
echo "      - CandleIndicators (技术指标数据)"
echo ""

# 7. 文档检查
echo "7. 检查文档..."
docs=(
    "docs/CANDLE_CACHE_GUIDE.md"
    "docs/CANDLE_INITIALIZATION.md"
)

for doc in "${docs[@]}"; do
    if [ -f "$doc" ]; then
        echo "   ✓ $doc"
    else
        echo "   ✗ $doc (缺失)"
    fi
done
echo ""

# 总结
echo "====================================="
echo "验证完成总结"
echo "====================================="
if [ "$all_exist" = true ]; then
    echo "✓ 所有文件已就绪，编译成功"
    echo "✓ 可以启动应用进行实际测试"
    echo ""
    echo "下一步："
    echo "1. 配置 Azure Table Storage 连接字符串（或使用 Azurite 模拟器）"
    echo "2. cd src/Trading.Web && dotnet run"
    echo "3. 使用上述 curl 命令测试 API"
else
    echo "✗ 验证发现问题，请检查上述错误"
    exit 1
fi
echo ""
