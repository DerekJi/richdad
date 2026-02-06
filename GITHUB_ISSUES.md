# GitHub Issues 内容

## Issue 1: 集成 Azure OpenAI 进行智能交易信号分析

### 标题
🤖 Integrate Azure OpenAI for Intelligent Trading Signal Analysis

### 描述
为Pin Bar交易信号监控系统集成Azure OpenAI，提供智能的市场分析和信号质量评估，帮助交易者做出更明智的决策。

### 背景
当前系统能够自动检测Pin Bar形态并发送Telegram通知，但缺少对市场环境、趋势强度和信号质量的智能评估。通过集成Azure OpenAI，系统可以提供：
- 多周期趋势分析
- 关键支撑/阻力位识别
- 信号质量评分和风险评估
- 交易建议和市场洞察

### 实现功能

#### ✅ 1. Trading.AI 核心服务
**新增项目：** `src/Trading.AI/`

**核心服务：**
- `AzureOpenAIService` - 封装Azure OpenAI API调用
  - 支持成本追踪（每日/每月使用量）
  - 速率限制保护（MaxDailyRequests: 500）
  - 预算控制（MonthlyBudgetUSD: $50）

- `MarketAnalysisService` - 提供3个核心分析方法
  - `AnalyzeMultiTimeFrameTrendAsync()` - 多周期趋势分析（H1/H4/D1）
  - `IdentifyKeyLevelsAsync()` - 识别关键支撑/阻力位
  - `ValidatePinBarSignalAsync()` - Pin Bar信号质量验证

**智能缓存策略：**
- 趋势分析：6小时缓存（降低成本90%）
- 关键价格位：12小时缓存
- 信号验证：实时不缓存（保证准确性）

#### ✅ 2. 信号质量评估
**增强 PinBarMonitoringService：**
- AI质量评分（0-100分）
- 风险级别评估（Low/Medium/High）
- 交易建议和推理说明
- Telegram消息包含AI评估结果：
  ```
  🤖 AI评估:
  质量评分: 85/100 🟢
  风险级别: Low
  建议: LONG

  💡 分析:
  H4趋势强劲看涨，价格在关键支撑位反弹，信号质量优秀...
  ```

#### ✅ 3. AI分析历史持久化
**新增模型：** `AIAnalysisHistory`
- 保存所有AI分析记录到Cosmos DB
- 字段包括：分析类型、品种、周期、输入数据、AI响应、tokens使用、响应时间、是否来自缓存

**Repository实现：**
- `CosmosAIAnalysisRepository` - Cosmos DB操作
- 支持按品种、类型、时间范围查询
- 统计分析（成功率、缓存命中率、平均响应时间）

#### ✅ 4. Web查询界面
**新增页面：** `ai-analysis.html`

**4个查询标签页：**
1. **最近分析** - 显示最新的AI分析记录
2. **统计信息** - 总览（总次数、成功率、缓存命中率、平均响应时间、token使用、成本估算）
3. **按品种查询** - 筛选特定交易品种的分析记录
4. **按类型查询** - 按分析类型（趋势/关键位/信号验证）筛选

**详情弹窗：**
- JSON语法高亮（深色主题）
- 显示输入数据、分析结果、原始响应
- 专业代码编辑器风格

#### ✅ 5. RESTful API
**新增控制器：** `AIAnalysisController`

**5个查询端点：**
```
GET  /api/aianalysis/recent?count=50          - 获取最近分析
GET  /api/aianalysis/{id}                     - 获取分析详情
GET  /api/aianalysis/symbol/{symbol}          - 按品种查询
GET  /api/aianalysis/type/{analysisType}      - 按类型查询
GET  /api/aianalysis/statistics               - 获取统计信息
```

**测试端点：** `AITestController`
```
GET  /api/aitest/status            - AI服务状态
GET  /api/aitest/test-connection   - 测试Azure OpenAI连接
POST /api/aitest/test-persistence  - 测试持久化功能
GET  /api/aitest/usage              - 查看使用量
```

#### ✅ 6. 配置系统
**appsettings.json 新增配置：**

```json
{
  "AzureOpenAI": {
    "Enabled": false,
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ApiKey": "",
    "DeploymentName": "gpt-4o",
    "MaxDailyRequests": 500,
    "MonthlyBudgetUSD": 50
  },
  "MarketAnalysis": {
    "TrendCacheDurationMinutes": 360,
    "KeyLevelsCacheDurationMinutes": 720,
    "MinTrendConfidence": 60
  },
  "CosmosDb": {
    "AIAnalysisHistoryContainerName": "AIAnalysisHistory"
  }
}
```

### 架构设计

**设计模式：**
- **Wrapper模式** - `MarketAnalysisServiceWithPersistence` 透明包装 `MarketAnalysisService`，自动持久化所有AI调用
- **工厂模式** - AI服务通过工厂方法注册，支持条件性启用
- **Repository模式** - 统一的数据访问接口

**依赖注入：**
```csharp
// 条件注册：仅在Enabled=true时注册
if (azureOpenAISettings.Enabled)
{
    services.AddSingleton<IAzureOpenAIService, AzureOpenAIService>();
    services.AddSingleton<MarketAnalysisService>();
    services.AddSingleton<IMarketAnalysisService>(sp => {
        var inner = sp.GetRequiredService<MarketAnalysisService>();
        var repo = sp.GetRequiredService<IAIAnalysisRepository>();
        return new MarketAnalysisServiceWithPersistence(inner, repo, logger);
    });
}
```

### 性能优化

**成本控制：**
- 智能缓存减少90%的API调用
- 每日请求限制（500次）
- 月度预算控制（$50）
- Token使用追踪

**响应速度：**
- 缓存命中：< 10ms
- 趋势分析：约2-3秒
- 信号验证：约3-4秒

**可靠性：**
- 完全可选（默认禁用，不影响核心功能）
- 优雅降级（AI失败时仍发送基础信号）
- 错误日志和重试机制

### 配置指南

**完整文档：** `docs/AZURE_OPENAI_SETUP.md`

**快速设置步骤：**
1. 在Azure AI Foundry创建OpenAI资源
2. 部署GPT-4o模型（推荐Global Standard）
3. 获取API密钥和端点
4. 配置用户密钥：
   ```bash
   dotnet user-secrets set "AzureOpenAI:ApiKey" "YOUR-KEY"
   dotnet user-secrets set "AzureOpenAI:DeploymentName" "gpt-4o"
   dotnet user-secrets set "AzureOpenAI:Enabled" "true"
   ```
5. 重启应用

### 技术栈

**新增依赖：**
- `Azure.AI.OpenAI` 2.1.0 - 官方Azure OpenAI SDK
- `Microsoft.Extensions.Caching.Memory` 9.0.0 - 内存缓存

**数据库：**
- Cosmos DB - AIAnalysisHistory容器（分区键：Symbol）

**前端：**
- 原生JavaScript + Fetch API
- JSON语法高亮（自定义实现）

### 测试验证

**单元测试建议：**
- AzureOpenAIService 成本追踪测试
- MarketAnalysisService 缓存逻辑测试
- SignalValidation 评分计算测试

**集成测试：**
- 端到端信号检测 + AI评估流程
- 持久化完整性测试
- API端点响应测试

### 部署注意事项

**环境变量：**
- 生产环境使用用户密钥或Azure Key Vault
- 不要将API密钥提交到Git

**监控指标：**
- AI调用成功率
- 平均响应时间
- Token使用量和成本
- 缓存命中率

### 后续扩展建议

1. **更多AI功能**
   - 自动生成交易计划
   - 风险评分算法优化
   - 市场情绪分析

2. **用户反馈系统**
   - 对AI建议进行评分
   - 根据反馈优化提示词

3. **多模型支持**
   - 支持不同的GPT模型
   - A/B测试不同提示词策略

4. **AI学习优化**
   - 基于历史准确率优化评分算法
   - 个性化的风险偏好设置

### 相关文件

**核心代码：**
- `src/Trading.AI/` - AI服务项目
- `src/Trading.AlertSystem.Service/Services/PinBarMonitoringService.cs` - AI集成
- `src/Trading.AlertSystem.Service/Services/MarketAnalysisServiceWithPersistence.cs` - 持久化包装器
- `src/Trading.AlertSystem.Data/Repositories/CosmosAIAnalysisRepository.cs` - 数据访问
- `src/Trading.AlertSystem.Web/Controllers/AIAnalysisController.cs` - 查询API
- `src/Trading.AlertSystem.Web/wwwroot/ai-analysis.html` - Web界面

**文档：**
- `docs/AZURE_OPENAI_SETUP.md` - 完整配置指南
- `src/Trading.AI/README.md` - AI服务说明

**提交记录：**
- Commit: `c933440` - feat: 集成Azure OpenAI进行Pin Bar信号AI验证和分析历史持久化

---

## Issue 2: 实现风险管理和仓位计算系统

### 标题
Risk Management & Position Size Calculator

### 描述
实现交易风险管理系统，根据账户资金和风险参数自动计算最佳开仓头寸。

### 功能需求

**输入参数：**
- 账户资金总额
- 单笔交易最大亏损限额（金额或百分比）
- 单日最大亏损限额（金额或百分比）
- 交易品种（如XAUUSD、XAGUSD等）
- 合约大小（contract size）
- 当前价格
- 计划止损价格
- 已有持仓信息（计算剩余可用风险额度）

**输出结果：**
- 是否允许开仓（布尔值）
- 建议开仓手数（lots）
- 风险金额
- 风险百分比
- 剩余可用风险额度
- 拒绝原因（如果不允许开仓）

**计算逻辑：**
```
止损点数 = |入场价 - 止损价| / 最小变动单位
单手风险 = 止损点数 × 合约大小 × 每点价值
最大允许手数 = min(
    单笔风险限额 / 单手风险,
    (单日限额 - 当日已亏损) / 单手风险
)
```

### 技术实现

**建议目录结构：**
```
src/Trading.RiskManagement/
  ├── Models/
  │   ├── RiskParameters.cs
  │   ├── PositionSizeResult.cs
  │   └── InstrumentSpecification.cs
  ├── Services/
  │   ├── IRiskCalculator.cs
  │   ├── RiskCalculator.cs
  │   └── PositionValidator.cs
  └── Trading.RiskManagement.csproj
```

**集成点：**
- 在 `Trading.AlertSystem.Service` 中集成
- 提供 REST API 供 Web 和移动端调用

### 验收标准
- [ ] 能正确计算不同品种的仓位大小
- [ ] 单笔风险限制生效
- [ ] 单日风险限制生效
- [ ] 考虑已有持仓的影响
- [ ] 有完整的单元测试
- [ ] 有API文档和使用示例

---

## Issue 2: Telegram 双向消息集成

### 标题
Telegram Two-Way Messaging Integration for Trade Confirmation

### 描述
实现与Telegram的双向通信，发送交易信号并等待用户确认回复后执行操作。

### 功能需求

**发送消息功能：**
- 格式化交易信号（品种、方向、入场价、止损、止盈、建议手数等）
- 发送到指定Telegram聊天/频道
- 附带确认按钮（InlineKeyboard）：✅ 确认开单 / ❌ 取消

**接收消息功能：**
- 实现 Telegram Bot Webhook 或 Long Polling
- 监听用户的按钮点击回复
- 关联回复与原始交易信号
- 设置超时机制（如5分钟无回复自动取消）

**消息格式示例：**
```
🔔 交易信号 #12345

📊 品种: XAUUSD
📈 方向: 做多 (BUY)
💵 价格: 2,650.50
🛑 止损: 2,645.00 (-5.5点)
🎯 止盈: 2,665.00 (+14.5点)
📦 建议手数: 0.15 lots
💰 风险: $82.50 (1.0%)

⏰ 有效期: 5分钟
```

### 技术实现

**方案选择：**
- 使用 Telegram Bot API
- Webhook 模式（推荐）或 Long Polling
- 状态管理：Redis 或内存缓存

**建议目录结构：**
```
src/Trading.Telegram/
  ├── Models/
  │   ├── TradeSignalMessage.cs
  │   ├── UserConfirmation.cs
  │   └── TelegramConfig.cs
  ├── Services/
  │   ├── ITelegramService.cs
  │   ├── TelegramBotService.cs
  │   ├── MessageFormatter.cs
  │   └── ConfirmationManager.cs
  └── Trading.Telegram.csproj
```

**配置参数：**
- Bot Token
- Chat ID / Channel ID
- Webhook URL（如使用webhook）
- 确认超时时间

### 验收标准
- [ ] 能成功发送格式化交易信号
- [ ] InlineKeyboard 按钮正常显示
- [ ] 能接收并解析用户点击
- [ ] 超时机制正常工作
- [ ] 消息与确认正确关联
- [ ] 有错误处理和重试机制
- [ ] 有配置文档

---

## Issue 3: Android 交易执行 App

### 标题
Android Trading Executor App for TradeLocker

### 描述
开发Android应用，接收Telegram指令并通过TradeLocker API执行交易操作。

### 功能需求

**核心功能：**
1. **账号管理**
   - 配置 TradeLocker 账号信息（服务器、账号、密码、API密钥）
   - 保存多个账号配置
   - 测试连接状态

2. **命令接收**
   - 监听指定Telegram频道/机器人消息
   - 解析交易指令（开仓、平仓、修改订单等）
   - 显示待执行命令队列

3. **交易执行**
   - 解析指令参数（品种、手数、止损、止盈）
   - 调用 TradeLocker API 下单
   - 显示执行结果和错误信息

4. **持仓管理**
   - 显示当前持仓列表
   - 显示每笔订单的详情（开仓价、盈亏、止损止盈）
   - 手动平仓功能

5. **历史记录**
   - 显示过往交易记录
   - 按日期、品种筛选
   - 统计盈亏

6. **控制选项**
   - 开启/暂停接收命令
   - 仅通知模式（不自动执行）
   - 需要确认模式（手动确认每笔交易）

### UI界面设计

**主要页面：**
1. 首页 - 账号状态、持仓概览、命令开关
2. 设置页 - TradeLocker账号配置、Telegram配置
3. 持仓页 - 当前持仓列表
4. 历史页 - 交易记录
5. 日志页 - 操作日志和错误信息

### 技术栈

**推荐方案：**
- 语言：Kotlin
- UI：Jetpack Compose 或 XML
- 网络：Retrofit + OkHttp
- 数据库：Room
- 后台服务：WorkManager + Foreground Service
- Telegram：Telegram Bot API 或 TDLib

**项目结构：**
```
TradingExecutor/
  ├── app/
  │   ├── src/main/
  │   │   ├── java/com/trading/executor/
  │   │   │   ├── ui/
  │   │   │   │   ├── MainActivity.kt
  │   │   │   │   ├── SettingsActivity.kt
  │   │   │   │   └── ...
  │   │   │   ├── data/
  │   │   │   │   ├── db/
  │   │   │   │   ├── models/
  │   │   │   │   └── repositories/
  │   │   │   ├── services/
  │   │   │   │   ├── TradeLockerService.kt
  │   │   │   │   ├── TelegramService.kt
  │   │   │   │   └── CommandExecutorService.kt
  │   │   │   └── utils/
  │   │   └── res/
  │   └── build.gradle
  └── README.md
```

### TradeLocker API 集成

需要实现的主要接口：
- 登录/认证
- 获取账户信息
- 获取持仓列表
- 下市价单/限价单
- 修改止损止盈
- 平仓

### 安全考虑
- 本地加密存储账号密码
- 使用 Android Keystore
- HTTPS 连接
- 防止误操作的二次确认

### 验收标准
- [ ] 能成功配置 TradeLocker 账号
- [ ] 能接收 Telegram 消息
- [ ] 能解析交易指令
- [ ] 能通过 TradeLocker API 下单
- [ ] 持仓显示正确
- [ ] 历史记录保存完整
- [ ] 有启动/停止控制
- [ ] 有完整的错误处理
- [ ] 界面友好易用
- [ ] 有用户手册

---

## 工作计划

### Issue 优先级
1. **Issue 1** (风险管理) - 基础，需要先完成
2. **Issue 2** (Telegram集成) - 基于Issue 1
3. **Issue 3** (Android App) - 基于Issue 1和2

### 分支策略
- `feature/position-calculator` - Issue 1
- `feature/telegram-integration` - Issue 2
- `feature/android-trading-app` - Issue 3

### Worktree 目录
- `../richdad-position-calc` - Issue 1
- `../richdad-telegram` - Issue 2
- `../richdad-android` - Issue 3
