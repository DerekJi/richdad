# Phase 3 使用指南 - 四级 AI 决策编排系统

**版本**: 1.0  
**日期**: 2026-02-10  
**状态**: ✅ 可用

## 概述

本指南说明如何使用 Phase 3 实现的四级 AI 决策编排系统。该系统通过 L1→L2→L3→L4 级联分析，基于 Al Brooks 价格行为理论提供完整的交易决策支持。

## 系统架构

```
┌─────────────────────────────────────────┐
│  L1: D1 战略分析 (GPT-4o, 24h 缓存)      │
│  → 确定交易方向偏见                       │
└───────────┬─────────────────────────────┘
            ↓ 验证通过
┌─────────────────────────────────────────┐
│  L2: H1 结构分析 (DeepSeek-V3, 1h 缓存)  │
│  → 判断市场周期和状态                     │
└───────────┬─────────────────────────────┘
            ↓ 验证通过
┌─────────────────────────────────────────┐
│  L3: M5 信号监控 (GPT-4o-mini, 实时)     │
│  → 识别交易设置                          │
└───────────┬─────────────────────────────┘
            ↓ 信号触发
┌─────────────────────────────────────────┐
│  L4: 最终决策 (DeepSeek-R1, 含思维链)    │
│  → 执行或拒绝交易                        │
└─────────────────────────────────────────┘
```

## 快速开始

### 1. 启动服务器

```bash
cd src/Trading.Web
dotnet run
```

等待服务器启动完成，输出类似：
```
Now listening on: http://localhost:5000
Application started. Press Ctrl+C to shut down.
```

### 2. 测试端点

#### 完整四级分析

```bash
curl http://localhost:5000/api/phase3orchestration/full?symbol=XAUUSD
```

这将执行完整的 L1→L2→L3→L4 流程。

**预期输出**:
```json
{
  "success": true,
  "symbol": "XAUUSD",
  "elapsedMs": 12500,
  "context": {
    "l1_DailyBias": {
      "direction": "Bullish",
      "confidence": 85,
      "trendType": "Strong",
      ...
    },
    "l2_Structure": {
      "marketCycle": "Trend",
      "status": "Active",
      "alignedWithD1": true,
      ...
    },
    "l3_Signal": {
      "status": "Potential_Setup",
      "setupType": "H2",
      ...
    },
    "l4_Decision": {
      "action": "Execute",
      "direction": "Buy",
      ...
    },
    "validation": {
      "isFullyAligned": true,
      "terminatedLevel": "None"
    }
  }
}
```

## API 端点详细说明

### 1. 完整分析

**端点**: `GET /api/phase3orchestration/full`

**参数**:
- `symbol` (string, 必填): 品种代码，如 XAUUSD、XAGUSD

**功能**: 执行完整的四级分析流程

**使用场景**: 
- 定期（每 5 分钟）检查交易机会
- 手动触发完整分析

**示例**:
```bash
curl "http://localhost:5000/api/phase3orchestration/full?symbol=XAUUSD"
```

---

### 2. 仅 L1 分析

**端点**: `GET /api/phase3orchestration/l1`

**参数**:
- `symbol` (string, 必填): 品种代码

**功能**: 仅执行 D1 日线分析

**使用场景**:
- 每日开盘前确定交易方向
- 验证 L1 缓存是否生效

**示例**:
```bash
curl "http://localhost:5000/api/phase3orchestration/l1?symbol=XAUUSD"
```

**响应示例**:
```json
{
  "success": true,
  "symbol": "XAUUSD",
  "result": {
    "direction": "Bullish",
    "confidence": 85,
    "supportLevels": [2850.0, 2870.5],
    "resistanceLevels": [2920.0, 2950.0],
    "trendType": "Strong",
    "reasoning": "Strong bull trend with consecutive bull bars above EMA20"
  }
}
```

---

### 3. L1 + L2 分析

**端点**: `GET /api/phase3orchestration/l2`

**参数**:
- `symbol` (string, 必填): 品种代码

**功能**: 执行 L1 和 L2 分析

**使用场景**:
- 每小时检查市场结构
- 验证 H1 是否与 D1 对齐

**示例**:
```bash
curl "http://localhost:5000/api/phase3orchestration/l2?symbol=XAUUSD"
```

---

### 4. L1 + L2 + L3 分析

**端点**: `GET /api/phase3orchestration/l3`

**参数**:
- `symbol` (string, 必填): 品种代码

**功能**: 执行 L1、L2、L3 分析

**使用场景**:
- 每 5 分钟检查交易信号
- 验证 M5 信号检测逻辑

**示例**:
```bash
curl "http://localhost:5000/api/phase3orchestration/l3?symbol=XAUUSD"
```

---

### 5. 快速检查

**端点**: `GET /api/phase3orchestration/should-analyze`

**参数**:
- `symbol` (string, 必填): 品种代码

**功能**: 快速判断是否应该执行完整分析

**使用场景**:
- 在启动完整分析前预检查
- 节省不必要的 AI 调用成本

**示例**:
```bash
curl "http://localhost:5000/api/phase3orchestration/should-analyze?symbol=XAUUSD"
```

**响应示例**:
```json
{
  "success": true,
  "symbol": "XAUUSD",
  "shouldAnalyze": true,
  "message": "✅ 满足分析条件，可以继续"
}
```

**快速检查逻辑**:
- 仅执行 L1 分析（有缓存）
- 如果 Direction = "Neutral" 或 Confidence < 60，返回 false
- 返回 true 表示可以继续完整分析

---

### 6. 清除缓存

**端点**: `POST /api/phase3orchestration/clear-cache`

**参数**:
- `symbol` (string, 必填): 品种代码

**功能**: 清除指定品种的所有缓存

**使用场景**:
- 强制刷新分析结果
- 调试时清除缓存

**示例**:
```bash
curl -X POST "http://localhost:5000/api/phase3orchestration/clear-cache?symbol=XAUUSD"
```

**响应**:
```json
{
  "success": true,
  "symbol": "XAUUSD",
  "message": "✅ 缓存已清除"
}
```

## 工作流程示例

### 场景 1: 每日交易前准备

**时间**: 每日开盘前（09:00 UTC）

```bash
# 1. 清除昨日缓存
curl -X POST "http://localhost:5000/api/phase3orchestration/clear-cache?symbol=XAUUSD"

# 2. 获取今日 D1 偏见
curl "http://localhost:5000/api/phase3orchestration/l1?symbol=XAUUSD"

# 3. 快速检查是否值得监控
curl "http://localhost:5000/api/phase3orchestration/should-analyze?symbol=XAUUSD"
```

---

### 场景 2: 实时交易监控

**时间**: 每 5 分钟运行一次

```bash
# 执行完整分析
curl "http://localhost:5000/api/phase3orchestration/full?symbol=XAUUSD"
```

**处理响应**:
```javascript
const response = await fetch('/api/phase3orchestration/full?symbol=XAUUSD');
const data = await response.json();

// 检查是否完全对齐
if (data.context.validation.isFullyAligned) {
    // 检查 L4 决策
    if (data.context.l4_Decision?.action === "Execute") {
        // 执行交易
        console.log("✅ 交易信号:", data.context.l4_Decision.direction);
        console.log("入场:", data.context.l4_Decision.entryPrice);
        console.log("止损:", data.context.l4_Decision.stopLoss);
        console.log("止盈:", data.context.l4_Decision.takeProfit);
    } else {
        console.log("⛔ L4 拒绝:", data.context.l4_Decision?.reasoning);
    }
} else {
    // 早期终止
    console.log("⏸️ 终止于:", data.context.validation.terminatedLevel);
    console.log("原因:", data.context.validation.terminationReason);
}
```

---

### 场景 3: 调试特定层级

```bash
# 测试 L1
curl "http://localhost:5000/api/phase3orchestration/l1?symbol=XAUUSD" | jq .

# 测试 L2
curl "http://localhost:5000/api/phase3orchestration/l2?symbol=XAUUSD" | jq .

# 测试 L3
curl "http://localhost:5000/api/phase3orchestration/l3?symbol=XAUUSD" | jq .
```

## 验证逻辑说明

### L1 验证

**通过条件**:
- ✅ Direction = "Bullish" 或 "Bearish"（非 "Neutral"）
- ✅ Confidence >= 60
- ✅ TrendType 不为空

**失败后果**: 流程终止，不执行 L2/L3/L4

---

### L2 验证

**通过条件**:
- ✅ L1 必须通过
- ✅ Status = "Active"（非 "Idle"）
- ✅ MarketCycle 不为空

**失败后果**: 流程终止，不执行 L3/L4

---

### L3 验证

**通过条件**:
- ✅ L1 和 L2 必须通过
- ✅ Status = "Potential_Setup"（非 "No_Signal"）

**失败后果**: 流程终止，不执行 L4

---

### L4 决策

**仅在 L3 通过后执行**

**执行条件** (所有必须满足):
- ✅ 风险回报比 >= 2:1
- ✅ 置信度 >= 70%
- ✅ 风险因素 < 3 个
- ✅ 明确的 Al Brooks 设置

## 成本控制

### 缓存策略

| 级别 | 缓存时长 | 每日调用 | 成本/天 |
|------|---------|---------|---------|
| L1 | 24 小时 | 1 次 | $0.05 |
| L2 | 1 小时 | 24 次 | $0.24 |
| L3 | 无缓存 | 288 次 | $0.29 |
| L4 | 无缓存 | 3-5 次 | $0.15 |

**总预计成本**: **~$0.54/天** (考虑早期终止)

### 成本优化建议

1. **使用快速检查**: 在完整分析前先调用 `should-analyze`
2. **合理设置监控频率**: L3 监控不必每分钟执行
3. **避免重复清除缓存**: 仅在必要时清除
4. **监控实际使用量**: 通过日志跟踪 AI 调用次数

## 日志说明

服务运行时会输出详细日志：

```
🚀 开始四级分析 - XAUUSD
📊 [L1] 分析 D1 日线...
✅ [L1] 通过 - Bullish (85%)
🔍 [L2] 分析 H1 结构...
✅ [L2] 通过 - Trend (Active), 对齐: True
🎯 [L3] 监控 M5 信号...
🎯 [L3] 检测到信号 - H2 (Buy), RR: 2.64
🤔 [L4] 最终决策思考中...
🎉 [L4] 决定执行交易！
   品种: XAUUSD
   方向: Buy
   入场: 2890.50
   止损: 2885.00
   止盈: 2905.00
   风险: $0.55
   风险回报比: 2.64
✅ 四级分析完成 - XAUUSD, 总耗时: 12500ms
```

## 故障排除

### 问题 1: L1 返回 Neutral

**症状**: L1 始终返回 Direction = "Neutral"

**原因**: D1 可能处于交易区间或趋势不明确

**解决**: 
- 等待市场突破
- 检查其他品种

---

### 问题 2: L2 始终返回 Idle

**症状**: L2 Status 始终为 "Idle"

**原因**: H1 可能与 D1 不对齐或处于震荡

**解决**:
- 等待 H1 回调至 D1 方向
- 观察 H1 是否突破区间

---

### 问题 3: L3 始终无信号

**症状**: L3 Status 始终为 "No_Signal"

**原因**: M5 没有明确的 Al Brooks 设置

**解决**:
- 等待回调至 EMA20
- 观察是否形成 H2 设置

---

### 问题 4: 缓存未生效

**症状**: 每次请求都调用 AI（无缓存提示）

**解决**:
```bash
# 检查服务器日志中是否有 "从缓存返回" 提示
# 如果没有，检查 IMemoryCache 是否正确注入
```

---

### 问题 5: DeepSeek R1 错误

**症状**: L4 调用失败

**原因**: DeepSeek R1 模型可能需要特殊配置

**解决**:
- 检查 `appsettings.json` 中 DeepSeek 配置
- 确认 API key 正确
- 验证模型名称为 "deepseek-reasoner"

## 高级用法

### 批量分析多个品种

```bash
for symbol in XAUUSD XAGUSD EURUSD; do
    echo "分析 $symbol..."
    curl -s "http://localhost:5000/api/phase3orchestration/should-analyze?symbol=$symbol" | jq .
done
```

### 自动化交易监控脚本

```bash
#!/bin/bash
while true; do
    response=$(curl -s "http://localhost:5000/api/phase3orchestration/full?symbol=XAUUSD")
    action=$(echo $response | jq -r '.context.l4_Decision.action // "None"')
    
    if [ "$action" = "Execute" ]; then
        echo "🎉 交易信号！"
        # 发送通知或执行交易
    fi
    
    sleep 300  # 5 分钟后再次检查
done
```

## 相关文档

- **实现报告**: [PHASE3_COMPLETION_REPORT.md](PHASE3_COMPLETION_REPORT.md)
- **Issue 文档**: [issue-08-ai-orchestration.md](issue-08-ai-orchestration.md)
- **Phase 2 验证**: [PHASE2_VALIDATION_REPORT.md](PHASE2_VALIDATION_REPORT.md)

## 技术支持

如有问题或建议，请查看：
- GitHub Issues: `docs/issues/`
- 完整文档: `docs/`

---

**最后更新**: 2026-02-10  
**版本**: 1.0  
**状态**: ✅ 生产就绪
