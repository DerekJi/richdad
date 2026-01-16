#!/bin/bash
# Pin Bar策略参数优化结果分析工具

cd "$(dirname "$0")"

echo "========================================"
echo "  Pin Bar策略参数优化结果分析工具"
echo "========================================"
echo ""

# 运行分析命令
dotnet run --configuration Release -- analyze

echo ""
echo "📄 查看报告文件:"
ls -lh results/optimization_report_*.md 2>/dev/null | tail -3
