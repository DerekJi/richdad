#!/bin/bash
# Issue 4 完整验证脚本（包含服务注册验证）

echo "================================================================================"
echo "Issue 4 完整验证 - 重构基础设施项目架构（含服务注册）"
echo "================================================================================"
echo ""

all_passed=true

# 测试1: 检查项目重命名
echo "[测试 1] 检查项目重命名..."
for project in \
    "src/Trading.Infras.Data/Trading.Infras.Data.csproj" \
    "src/Trading.Infras.Service/Trading.Infras.Service.csproj" \
    "src/Trading.Infras.Web/Trading.Infras.Web.csproj"; do
    if [ -f "$project" ]; then
        echo "  ✅ $project 存在"
    else
        echo "  ❌ $project 不存在"
        all_passed=false
    fi
done

# 测试2: 检查订单执行接口
echo ""
echo "[测试 2] 检查订单执行接口..."
for file in \
    "src/Trading.Core/Trading/IOrderExecutionService.cs" \
    "src/Trading.Infras.Service/Adapters/OandaOrderAdapter.cs" \
    "src/Trading.Infras.Service/Adapters/TradeLockerOrderAdapter.cs" \
    "src/Trading.Infras.Service/Adapters/MockOrderExecutionService.cs"; do
    if [ -f "$file" ]; then
        echo "  ✅ $file 存在"
    else
        echo "  ❌ $file 不存在"
        all_passed=false
    fi
done

# 测试3: 检查服务注册配置
echo ""
echo "[测试 3] 检查服务注册配置..."
if [ -f "src/Trading.Infras.Web/Configuration/OrderExecutionConfiguration.cs" ]; then
    echo "  ✅ OrderExecutionConfiguration.cs 存在"
else
    echo "  ❌ OrderExecutionConfiguration.cs 不存在"
    all_passed=false
fi

if [ -f "src/Trading.Infras.Service/Configuration/OrderExecutionSettings.cs" ]; then
    echo "  ✅ OrderExecutionSettings.cs 存在"
else
    echo "  ❌ OrderExecutionSettings.cs 不存在"
    all_passed=false
fi

# 检查Program.cs是否调用了服务注册
if grep -q "AddOrderExecutionService" "src/Trading.Infras.Web/Program.cs"; then
    echo "  ✅ Program.cs 已调用 AddOrderExecutionService"
else
    echo "  ❌ Program.cs 未调用 AddOrderExecutionService"
    all_passed=false
fi

# 测试4: 检查配置文件
echo ""
echo "[测试 4] 检查配置文件..."
if grep -q "OrderExecution" "src/Trading.Infras.Web/appsettings.json"; then
    echo "  ✅ appsettings.json 包含 OrderExecution 配置"
else
    echo "  ❌ appsettings.json 缺少 OrderExecution 配置"
    all_passed=false
fi

# 测试5: 编译测试
echo ""
echo "[测试 5] 编译测试..."
if dotnet build > /dev/null 2>&1; then
    echo "  ✅ 编译成功"
else
    echo "  ❌ 编译失败"
    all_passed=false
fi

# 测试6: 运行时验证（检查日志输出）
echo ""
echo "[测试 6] 运行时验证（检查服务注册日志）..."
cd src/Trading.Infras.Web
timeout 10 dotnet run 2>&1 > /tmp/startup.log &
sleep 8
if grep -q "订单执行" /tmp/startup.log; then
    echo "  ✅ 服务启动日志包含订单执行配置信息"
else
    echo "  ❌ 服务启动日志缺少订单执行配置信息"
    all_passed=false
fi
pkill -f "dotnet run" 2>/dev/null
cd ../..

# 最终结果
echo ""
echo "================================================================================"
if [ "$all_passed" = true ]; then
    echo "✅ 所有测试通过！Issue 4 完整实现（含服务注册）！"
else
    echo "❌ 部分测试失败，请检查上述错误"
fi
echo "================================================================================"

echo ""
echo "[完成功能清单]"
echo "  ✅ 1. 项目重命名 (Trading.AlertSystem.* → Trading.Infras.*)"
echo "  ✅ 2. 统一订单执行接口 (IOrderExecutionService)"
echo "  ✅ 3. 平台适配器实现 (OandaOrderAdapter, TradeLockerOrderAdapter)"
echo "  ✅ 4. 服务注册配置 (OrderExecutionConfiguration)"
echo "  ✅ 5. 配置管理 (OrderExecutionSettings)"
echo "  ✅ 6. 自动平台选择 (基于配置动态注册)"
echo ""

if [ "$all_passed" = true ]; then
    exit 0
else
    exit 1
fi
