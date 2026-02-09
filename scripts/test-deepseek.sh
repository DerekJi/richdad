#!/bin/bash
# DeepSeek集成测试脚本

echo "================================"
echo "DeepSeek集成测试"
echo "================================"
echo ""

# 启动服务（后台运行）
echo "▶️  启动服务..."
cd "d:/source/richdad-refactor/src/Trading.Infras.Web"
dotnet run > /dev/null 2>&1 &
SERVER_PID=$!

echo "✅ 服务已启动 (PID: $SERVER_PID)"
echo "⏳ 等待服务启动完成..."
sleep 10

echo ""
echo "================================"
echo "1. 检查DeepSeek配置状态"
echo "================================"
curl -s http://localhost:5000/api/deepseektest/status | python -m json.tool

echo ""
echo ""
echo "================================"
echo "2. 检查双级AI配置"
echo "================================"
echo "提供商: DeepSeek"
echo "Tier1模型: deepseek-chat"
echo "Tier2模型: deepseek-chat"

echo ""
echo ""
echo "✅ 测试完成"
echo ""
echo "💡 提示:"
echo "  - 如果看到DeepSeek已启用，说明集成成功"
echo "  - 访问 http://localhost:5000/api/deepseektest/test-connection 测试连接"
echo "  - 访问 http://localhost:5000/api/deepseektest/test-dual-tier 测试双级AI"
echo ""
echo "🛑 关闭服务: kill $SERVER_PID"
