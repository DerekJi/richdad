# 快速启动 - 四级 AI 决策系统

**版本**: 1.0
**预计时间**: 10 分钟
**前置要求**: .NET 9.0 SDK

## 第一步: 配置 API 密钥

### 1. 打开配置文件

```bash
# Windows
notepad src\Trading.Web\appsettings.json

# macOS/Linux
nano src/Trading.Web/appsettings.json
```

### 2. 配置 Azure OpenAI

找到 `AzureOpenAI` 部分，填入您的凭据：

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ApiKey": "YOUR_AZURE_OPENAI_API_KEY",
    "Deployment": "gpt-4o",
    "DeploymentMini": "gpt-4o-mini"
  }
}
```

**获取 Azure OpenAI 密钥**:
1. 访问 [Azure Portal](https://portal.azure.com)
2. 进入 Azure OpenAI 资源
3. 在"密钥和终结点"页面复制密钥

### 3. 配置 DeepSeek

找到 `DeepSeek` 部分，填入 API 密钥：

```json
{
  "DeepSeek": {
    "ApiEndpoint": "https://api.deepseek.com/v1/chat/completions",
    "ApiKey": "YOUR_DEEPSEEK_API_KEY",
    "Model": "deepseek-chat",
    "ModelV3": "deepseek-chat-v3",
    "ModelReasoner": "deepseek-reasoner"
  }
}
```

**获取 DeepSeek 密钥**:
1. 访问 [DeepSeek](https://platform.deepseek.com)
2. 注册账号
3. 在 API Keys 页面创建新密钥

### 4. (可选) 配置 Cosmos DB

如果需要使用持久化存储：

```json
{
  "CosmosDb": {
    "EndpointUrl": "https://your-cosmos.documents.azure.com:443/",
    "PrimaryKey": "YOUR_COSMOS_DB_KEY",
    "DatabaseName": "TradingSystem",
    "Containers": {
      "PriceData": "PriceData",
      "TradingContext": "TradingContext",
      "CandleData": "CandleData"
    }
  }
}
```

## 第二步: 启动服务器

### 方法 1: 使用命令行

```bash
# 进入 Web 项目目录
cd src/Trading.Web

# 启动服务器
dotnet run
```

### 方法 2: 使用 VS Code

1. 按 `F5` 或点击 "运行和调试"
2. 选择 ".NET Core Launch (web)"

### 方法 3: 使用 Visual Studio

1. 打开 `TradingSystem.sln`
2. 设置 `Trading.Web` 为启动项目
3. 按 `F5` 启动

**预期输出**:
```
info: Microsoft.Hosting.Lifetime[0]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

## 第三步: 测试第一个请求

### 测试 L1 (D1 日线分析)

**测试方式 1: 使用浏览器**

打开浏览器访问：
```
http://localhost:5000/api/phase3orchestration/l1?symbol=XAUUSD
```

**测试方式 2: 使用 curl**

```bash
curl http://localhost:5000/api/phase3orchestration/l1?symbol=XAUUSD
```

**测试方式 3: 使用 PowerShell**

```powershell
Invoke-WebRequest -Uri "http://localhost:5000/api/phase3orchestration/l1?symbol=XAUUSD" | Select-Object -ExpandProperty Content
```

**预期响应** (JSON 格式):
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

### 测试完整分析

```bash
curl http://localhost:5000/api/phase3orchestration/full?symbol=XAUUSD
```

**注意**: 完整分析可能需要 10-15 秒，因为会调用 4 个 AI 模型。

## 第四步: 理解输出

### L1 输出说明

| 字段 | 说明 | 可能值 |
|------|------|--------|
| `direction` | 交易方向 | Bullish / Bearish / Neutral |
| `confidence` | 置信度 | 0-100 |
| `trendType` | 趋势类型 | Strong / Weak / Range |
| `supportLevels` | 支撑位 | 价格数组 |
| `resistanceLevels` | 阻力位 | 价格数组 |

**判断规则**:
- ✅ **可交易**: Direction = Bullish/Bearish, Confidence >= 60
- ⛔ **不可交易**: Direction = Neutral 或 Confidence < 60

### 完整输出说明

完整分析包含 4 个级别的结果：

```json
{
  "context": {
    "l1_DailyBias": { /* L1 结果 */ },
    "l2_Structure": { /* L2 结果 */ },
    "l3_Signal": { /* L3 结果 */ },
    "l4_Decision": { /* L4 决策 */ },
    "validation": {
      "isFullyAligned": true,  // 是否所有级别都通过
      "terminatedLevel": "None"  // 如果早期终止，显示终止级别
    }
  }
}
```

**L4 决策字段**:
```json
{
  "action": "Execute",  // Execute 或 Hold
  "direction": "Buy",   // Buy 或 Sell
  "entryPrice": 2890.50,
  "stopLoss": 2885.00,
  "takeProfit": 2905.00,
  "riskRewardRatio": 2.64,
  "confidence": 85,
  "reasoning": "... 详细推理过程 ..."
}
```

## 第五步: 集成到您的系统

### 场景 1: 每日开盘前分析

```bash
# 每天 09:00 UTC 运行
curl "http://localhost:5000/api/phase3orchestration/l1?symbol=XAUUSD"
```

### 场景 2: 实时交易监控

```bash
# 每 5 分钟运行一次
while true; do
    curl "http://localhost:5000/api/phase3orchestration/full?symbol=XAUUSD"
    sleep 300
done
```

### 场景 3: Python 集成

```python
import requests
import time

def check_trading_signal(symbol="XAUUSD"):
    url = f"http://localhost:5000/api/phase3orchestration/full"
    response = requests.get(url, params={"symbol": symbol})
    data = response.json()

    if data["context"]["validation"]["isFullyAligned"]:
        decision = data["context"]["l4_Decision"]
        if decision["action"] == "Execute":
            print(f"✅ 交易信号: {decision['direction']}")
            print(f"入场: {decision['entryPrice']}")
            print(f"止损: {decision['stopLoss']}")
            print(f"止盈: {decision['takeProfit']}")
            return decision

    return None

# 每 5 分钟检查
while True:
    signal = check_trading_signal()
    if signal:
        # 执行交易
        pass
    time.sleep(300)
```

### 场景 4: Node.js 集成

```javascript
const axios = require('axios');

async function checkTradingSignal(symbol = 'XAUUSD') {
    const response = await axios.get(
        'http://localhost:5000/api/phase3orchestration/full',
        { params: { symbol } }
    );

    const { context } = response.data;

    if (context.validation.isFullyAligned) {
        const decision = context.l4_Decision;
        if (decision.action === 'Execute') {
            console.log(`✅ 交易信号: ${decision.direction}`);
            console.log(`入场: ${decision.entryPrice}`);
            console.log(`止损: ${decision.stopLoss}`);
            console.log(`止盈: ${decision.takeProfit}`);
            return decision;
        }
    }

    return null;
}

// 每 5 分钟检查
setInterval(() => {
    checkTradingSignal();
}, 300000);
```

## 常见问题

### Q1: 服务器启动失败

**错误**: `Unable to bind to http://localhost:5000`

**解决**: 端口已被占用，修改端口：

```bash
dotnet run --urls "http://localhost:5001"
```

---

### Q2: API 调用返回 401 Unauthorized

**原因**: API 密钥未配置或错误

**解决**:
1. 检查 `appsettings.json` 中的 API 密钥
2. 确认密钥未过期
3. 重启服务器

---

### Q3: L1 返回错误

**错误**: `Error calling Azure OpenAI`

**原因**: Azure OpenAI 配置错误

**解决**:
1. 检查 `Endpoint` 格式: `https://your-resource.openai.azure.com/`
2. 检查 `Deployment` 名称是否正确
3. 测试 API 密钥是否有效

---

### Q4: DeepSeek 调用失败

**错误**: `Error calling DeepSeek API`

**原因**: DeepSeek API 配置错误

**解决**:
1. 检查 API 密钥是否正确
2. 确认账户余额充足
3. 验证模型名称: `deepseek-chat`, `deepseek-chat-v3`, `deepseek-reasoner`

---

### Q5: 响应时间过长

**症状**: 完整分析超过 30 秒

**原因**:
- 网络延迟
- AI 模型响应慢

**解决**:
1. 使用 `should-analyze` 端点预检查
2. 启用缓存（L1、L2 已默认启用）
3. 考虑使用更快的 AI 模型

---

### Q6: 始终返回 Neutral

**症状**: L1 返回 Direction = "Neutral"

**原因**: 市场处于震荡或不明确趋势

**解决**:
- 这是正常现象，等待市场突破
- 尝试其他品种
- 检查 D1 K 线数据是否正确

## 成本预估

### 单次调用成本

| API | 成本 | 说明 |
|-----|------|------|
| L1 (GPT-4o) | $0.05 | 24h 缓存，每日 1 次 |
| L2 (DeepSeek-V3) | $0.01 | 1h 缓存，每小时 1 次 |
| L3 (GPT-4o-mini) | $0.001 | 无缓存，实时 |
| L4 (DeepSeek-R1) | $0.05 | 无缓存，仅信号触发 |

### 每日成本预估

假设每 5 分钟运行一次完整分析（288 次/天）：

- L1: 1 次 × $0.05 = **$0.05**
- L2: 24 次 × $0.01 = **$0.24**
- L3: 288 次 × $0.001 = **$0.29**
- L4: 5 次 × $0.05 = **$0.25** (假设 5 次触发)

**总计**: **~$0.83/天**

**节省成本**:
- 使用 `should-analyze` 预检查: **-40% → $0.50/天**
- 减少 L3 监控频率（如 10 分钟）: **-50% → $0.42/天**

## 下一步

### 推荐阅读

1. **详细使用指南**: [PHASE3_USAGE_GUIDE.md](PHASE3_USAGE_GUIDE.md)
2. **实现报告**: [PHASE3_COMPLETION_REPORT.md](PHASE3_COMPLETION_REPORT.md)
3. **Issue 文档**: [issue-08-ai-orchestration.md](issue-08-ai-orchestration.md)

### 进阶功能

- **批量分析**: 同时监控多个品种
- **自动化交易**: 集成实盘交易系统
- **告警系统**: 集成 Telegram/Email 通知
- **性能监控**: 添加日志和指标收集

### 生产部署

准备部署到生产环境？查看：
- **DEPLOYMENT_GUIDE.md** (待创建)
- **MONITORING_GUIDE.md** (待创建)

## 技术支持

- **GitHub Issues**: `docs/issues/`
- **完整文档**: `docs/`
- **示例代码**: `src/Trading.Web/Controllers/`

---

**欢迎使用四级 AI 决策系统！** 🎉

**最后更新**: 2026-02-10
**版本**: 1.0
**状态**: ✅ 生产就绪
