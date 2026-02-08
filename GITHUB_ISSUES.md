# GitHub Issues 内容

## Issue 1: 实现 Azure Table Storage 持久化存储

### 标题
💾 Implement Azure Table Storage for Cost-Efficient Data Persistence

### 描述
将系统从Cosmos DB迁移到Azure Table Storage，实现低成本、高性能的NoSQL持久化存储方案。Azure Table Storage提供了与Cosmos DB相当的性能，但成本仅为其2%。

### 背景
当前系统使用内存存储或Cosmos DB作为持久化方案，但存在以下问题：
- 内存存储：数据在应用重启后丢失，无法用于生产环境
- Cosmos DB：成本高昂（$30-50/月），对于小规模应用负担过重

通过集成Azure Table Storage，系统可以实现：
- **98%成本节省**：从$30-50/月降至$1-3/月
- 高性能NoSQL存储
- 按需付费，无最低消费
- 99.9%可用性保证

### 实现功能

#### ✅ 1. 核心基础设施层
**新增项目组件：** `Trading.AlertSystem.Data`

**配置类：**
- `AzureTableStorageSettings` - 统一配置管理
  - ConnectionString
  - 各表名配置（AlertHistory、PriceMonitor、EmaMonitor等）
  - Enabled 开关

**上下文类：**
- `AzureTableStorageContext` - Azure Table Storage 连接管理
  - 初始化所有表（自动创建不存在的表）
  - 提供 TableClient 获取接口
  - 连接状态检查

#### ✅ 2. 告警历史持久化
**新增仓储：** `AzureTableAlertHistoryRepository`

**核心功能：**
- ✅ 添加告警记录 - `AddAsync(AlertHistory)`
- ✅ 按ID查询 - `GetByIdAsync(string)`
- ✅ 分页查询 - `GetAllAsync()` 支持筛选：
  - 按类型筛选（PriceAlert、EmaAlert、PinBar等）
  - 按交易品种筛选
  - 按时间范围筛选
  - 分页和排序
- ✅ 批量添加 - `AddBatchAsync(IEnumerable<AlertHistory>)`
- ✅ 删除记录 - `DeleteAsync(string)`
- ✅ 统计查询 - `GetCountAsync()` 按类型统计

**设计亮点：**
- 使用日期作为 PartitionKey (`Alert_yyyyMMdd`) 优化查询性能
- 支持跨分区查询（按日期范围遍历）
- 批量操作优化（每批最多100条）

#### ✅ 3. 配置和依赖注入
**新增配置类：** `AzureTableStorageConfiguration`

**服务注册：**
```csharp
// 自动检测配置，按需注册
builder.Services.AddAzureTableStorageServices(builder.Configuration);
```

**初始化流程：**
```csharp
// 自动创建所有表
await app.InitializeAzureTableStorageAsync();
```

#### ✅ 4. 存储后备方案（Fallback）
**新增配置类：** `StorageConfiguration`

**智能存储选择：**
1. 优先使用 Azure Table Storage（如果已配置且启用）
2. 降级到内存存储（开发/测试环境）
3. 自动补充缺失的仓储实现

**混合模式支持：**
- Azure Table + InMemory 混合模式
- 当某些仓储未实现 Azure Table 版本时，自动使用内存版本
- 日志清晰标识使用的存储类型

#### ✅ 5. 配置管理
**appsettings.json 配置：**
```json
{
  "AzureTableStorage": {
    "ConnectionString": "DefaultEndpointsProtocol=https;...",
    "Enabled": true,
    "AlertHistoryTableName": "AlertHistory",
    "PriceMonitorTableName": "PriceMonitor",
    "EmaMonitorTableName": "EmaMonitor",
    "DataSourceConfigTableName": "DataSourceConfig",
    "EmailConfigTableName": "EmailConfig",
    "PinBarMonitorTableName": "PinBarMonitor",
    "PinBarSignalTableName": "PinBarSignal",
    "AIAnalysisHistoryTableName": "AIAnalysisHistory"
  }
}
```

**用户密钥支持（推荐生产环境）：**
```bash
dotnet user-secrets set "AzureTableStorage:ConnectionString" "your-connection-string"
dotnet user-secrets set "AzureTableStorage:Enabled" "true"
```

#### ✅ 6. 分区键设计优化
**告警历史分区策略：**
- PartitionKey: `Alert_yyyyMMdd` （按日期分区）
- RowKey: `{Guid}` （唯一ID）
- 优点：
  - 查询时间范围高效（只查询相关日期分区）
  - 避免热分区（数据均匀分布）
  - 支持高并发写入

#### ✅ 7. NuGet 包依赖
**已添加包：**
```xml
<PackageReference Include="Azure.Data.Tables" Version="12.9.1" />
```

### 测试验证

#### ✅ 功能测试
- ✅ 连接字符串配置正确性
- ✅ 表自动创建功能
- ✅ CRUD 操作完整性
- ✅ 分页查询准确性
- ✅ 筛选条件正确性
- ✅ 批量操作性能

#### ✅ 集成测试
- ✅ 与现有告警系统集成
- ✅ 存储后备方案切换
- ✅ 配置开关功能
- ✅ 错误处理和日志记录

### 性能和成本

**成本对比：**
| 指标 | Cosmos DB | Azure Table Storage | 节省 |
|------|-----------|---------------------|------|
| 存储成本 | $0.25/GB/月 | $0.045/GB/月 | **82%** |
| 写入操作 | $0.25/百万 RU | $0.05/10万次 | **80%** |
| 读取操作 | $0.25/百万 RU | $0.004/10万次 | **98%** |
| 典型月成本 | $30-50 | **$1-3** | **98%** |

**性能特点：**
- 单表操作延迟：< 10ms
- 支持每秒数千次操作
- 自动扩展，无需预配置吞吐量
- 99.9% SLA 可用性保证

### 部署指南

**1. 创建 Storage Account（Azure Portal）：**
```
性能层级: Standard
复制: LRS (本地冗余存储)
```

**2. 配置连接字符串：**
```bash
# 使用用户密钥（推荐）
cd src/Trading.AlertSystem.Web
dotnet user-secrets set "AzureTableStorage:ConnectionString" "your-connection-string"
dotnet user-secrets set "AzureTableStorage:Enabled" "true"
```

**3. 运行应用：**
```bash
dotnet run --project src/Trading.AlertSystem.Web
```

应用启动时会自动：
- 检测 Azure Table Storage 配置
- 创建所需的表
- 记录使用的存储类型

### 未来扩展

**待实现的 Repository：**
- [ ] `AzureTablePriceMonitorRepository` - 价格监控配置
- [ ] `AzureTableEmaMonitorRepository` - EMA监控配置
- [ ] `AzureTablePinBarMonitorRepository` - Pin Bar监控配置
- [ ] `AzureTableDataSourceConfigRepository` - 数据源配置
- [ ] `AzureTableEmailConfigRepository` - 邮件配置

**性能优化：**
- [ ] 实现二级缓存（Redis）
- [ ] 批量写入优化
- [ ] 分区键策略调优
- [ ] 查询性能监控

**高级功能：**
- [ ] 数据备份和恢复
- [ ] 跨区域复制
- [ ] 数据归档策略
- [ ] 监控和告警集成

### 相关文件

**核心代码：**
- [AzureTableStorageContext.cs](src/Trading.AlertSystem.Data/Infrastructure/AzureTableStorageContext.cs) - 连接管理
- [AzureTableStorageSettings.cs](src/Trading.AlertSystem.Data/Configuration/AzureTableStorageSettings.cs) - 配置模型
- [AzureTableAlertHistoryRepository.cs](src/Trading.AlertSystem.Data/Repositories/AzureTableAlertHistoryRepository.cs) - 告警历史仓储
- [AzureTableStorageConfiguration.cs](src/Trading.AlertSystem.Web/Configuration/AzureTableStorageConfiguration.cs) - 服务配置
- [StorageConfiguration.cs](src/Trading.AlertSystem.Web/Configuration/StorageConfiguration.cs) - 存储后备方案
- [Program.cs](src/Trading.AlertSystem.Web/Program.cs) - 应用启动配置

**文档：**
- [AZURE_TABLE_STORAGE_GUIDE.md](docs/AZURE_TABLE_STORAGE_GUIDE.md) - 完整配置和使用指南
- [USER_SECRETS_SETUP.md](docs/USER_SECRETS_SETUP.md) - 用户密钥配置指南

**配置文件：**
- [appsettings.json](src/Trading.AlertSystem.Web/appsettings.json) - 应用配置
- [Trading.AlertSystem.Data.csproj](src/Trading.AlertSystem.Data/Trading.AlertSystem.Data.csproj) - 项目依赖

### 标签
`enhancement`, `database`, `cost-optimization`, `azure`, `storage`

---

## Issue 2: 集成 Azure OpenAI 进行智能交易信号分析

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

---

## Issue 4: 重构基础设施项目架构

### 标题
🏗️ Refactor Infrastructure Projects and Add Unified Order Execution Interface

### 描述
重构现有代码架构，统一命名规范，添加订单执行抽象层，为AI Agent集成做准备。

### 背景
当前系统的基础设施项目命名不够清晰，且缺少统一的订单执行接口：
- 项目命名：`Trading.AlertSystem.*` 容易与业务逻辑混淆
- 订单接口：`IOandaService` 和 `ITradeLockerService` 接口不统一
- 缺少抽象：难以在不同平台间切换

通过此次重构，系统将：
- **清晰的命名**：基础设施项目统一使用 `Trading.Infras.*` 前缀
- **统一接口**：创建 `IOrderExecutionService` 抽象层
- **易于扩展**：未来添加新交易平台更简单
- **为AI Agent做准备**：提供清晰的API供Agent调用

### 实现功能

#### ✅ 1. 项目重命名

**重命名映射：**
```
Trading.AlertSystem.Data       → Trading.Infras.Data
Trading.AlertSystem.Service    → Trading.Infras.Service
Trading.AlertSystem.Web        → Trading.Infras.Web
Trading.AlertSystem.Mobile     → Trading.Infras.Mobile (如果存在)
```

**保持不变的项目：**
- `Trading.Core` - 核心领域逻辑
- `Trading.Data` - 数据模型
- `Trading.AI` - AI分析服务
- `Trading.Backtest.*` - 回测相关

**需要更新：**
- 所有项目引用（.csproj）
- 命名空间（namespace）
- 解决方案文件（.sln）
- 文档中的引用

#### ✅ 2. 添加统一订单执行接口

**新增接口：** `Trading.Core/Trading/IOrderExecutionService.cs`

```csharp
public interface IOrderExecutionService
{
    /// <summary>
    /// 获取当前使用的交易平台名称
    /// </summary>
    string PlatformName { get; }

    /// <summary>
    /// 下市价单
    /// </summary>
    Task<OrderResult> PlaceMarketOrder(
        string symbol,
        decimal lots,
        OrderDirection direction,
        decimal? stopLoss = null,
        decimal? takeProfit = null,
        string? comment = null);

    /// <summary>
    /// 下限价单
    /// </summary>
    Task<OrderResult> PlaceLimitOrder(
        string symbol,
        decimal lots,
        OrderDirection direction,
        decimal limitPrice,
        decimal? stopLoss = null,
        decimal? takeProfit = null,
        string? comment = null);

    /// <summary>
    /// 获取订单状态
    /// </summary>
    Task<OrderStatus> GetOrderStatus(string orderId);

    /// <summary>
    /// 修改止损止盈
    /// </summary>
    Task<bool> ModifyOrder(
        string orderId,
        decimal? newStopLoss = null,
        decimal? newTakeProfit = null);

    /// <summary>
    /// 平仓
    /// </summary>
    Task<bool> CloseOrder(string orderId, decimal? lots = null);

    /// <summary>
    /// 获取当前持仓
    /// </summary>
    Task<List<Position>> GetOpenPositions(string? symbol = null);
}

public class OrderResult
{
    public bool Success { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public decimal ExecutedPrice { get; set; }
    public decimal ExecutedLots { get; set; }
    public DateTime ExecutedTime { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
}

public enum OrderDirection { Buy, Sell }

public class OrderStatus
{
    public string OrderId { get; set; } = string.Empty;
    public OrderState State { get; set; }
    public decimal FilledLots { get; set; }
    public decimal RemainingLots { get; set; }
    public decimal? AveragePrice { get; set; }
}

public enum OrderState
{
    Pending,
    PartiallyFilled,
    Filled,
    Cancelled,
    Rejected
}

public class Position
{
    public string PositionId { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public OrderDirection Direction { get; set; }
    public decimal Lots { get; set; }
    public decimal OpenPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal? StopLoss { get; set; }
    public decimal? TakeProfit { get; set; }
    public decimal ProfitLoss { get; set; }
    public DateTime OpenTime { get; set; }
    public string? Comment { get; set; }
}
```

#### ✅ 3. 实现平台适配器

**新增适配器：** `Trading.Infras.Service/Adapters/`

**OandaOrderAdapter.cs:**
```csharp
public class OandaOrderAdapter : IOrderExecutionService
{
    private readonly IOandaService _oandaService;
    private readonly ILogger<OandaOrderAdapter> _logger;

    public string PlatformName => "Oanda";

    public OandaOrderAdapter(
        IOandaService oandaService,
        ILogger<OandaOrderAdapter> logger)
    {
        _oandaService = oandaService;
        _logger = logger;
    }

    public async Task<OrderResult> PlaceMarketOrder(
        string symbol,
        decimal lots,
        OrderDirection direction,
        decimal? stopLoss = null,
        decimal? takeProfit = null,
        string? comment = null)
    {
        try
        {
            // 转换参数格式
            var oandaSymbol = ConvertToOandaSymbol(symbol);
            var units = ConvertLotsToUnits(lots, symbol);

            // 调用Oanda API
            var result = await _oandaService.PlaceMarketOrder(
                oandaSymbol,
                units,
                direction == OrderDirection.Buy ? "buy" : "sell",
                stopLoss,
                takeProfit);

            // 转换返回格式
            return new OrderResult
            {
                Success = result.Success,
                OrderId = result.OrderId,
                ExecutedPrice = result.Price,
                ExecutedLots = lots,
                ExecutedTime = result.Time,
                Message = result.Message
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Oanda下单失败: {Symbol} {Lots} {Direction}",
                symbol, lots, direction);
            return new OrderResult
            {
                Success = false,
                Message = ex.Message,
                ErrorCode = "EXECUTION_ERROR"
            };
        }
    }

    // 其他方法实现...

    private string ConvertToOandaSymbol(string symbol)
    {
        // XAUUSD -> XAU_USD
        return symbol.Contains("_") ? symbol :
            symbol.Insert(symbol.Length - 3, "_");
    }

    private int ConvertLotsToUnits(decimal lots, string symbol)
    {
        // Oanda使用单位制，1手 = 不同的单位数
        if (symbol.StartsWith("XAU")) return (int)lots; // 黄金 1手=1单位
        return (int)(lots * 100000); // 外汇 1手=100000单位
    }
}
```

**TradeLockerOrderAdapter.cs:**
```csharp
public class TradeLockerOrderAdapter : IOrderExecutionService
{
    private readonly ITradeLockerService _tradeLockerService;
    private readonly ILogger<TradeLockerOrderAdapter> _logger;

    public string PlatformName => "TradeLocker";

    // 类似实现...
}
```

#### ✅ 4. 服务注册配置

**更新：** `Trading.Infras.Web/Program.cs`

```csharp
// 根据配置选择订单执行平台
var orderPlatform = builder.Configuration["OrderExecution:Platform"] ?? "Oanda";

if (orderPlatform.Equals("Oanda", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddScoped<IOrderExecutionService, OandaOrderAdapter>();
    _logger.LogInformation("✅ 使用 Oanda 作为订单执行平台");
}
else if (orderPlatform.Equals("TradeLocker", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddScoped<IOrderExecutionService, TradeLockerOrderAdapter>();
    _logger.LogInformation("✅ 使用 TradeLocker 作为订单执行平台");
}
else
{
    _logger.LogWarning("⚠️ 未知的订单执行平台: {Platform}，使用模拟模式", orderPlatform);
    builder.Services.AddScoped<IOrderExecutionService, MockOrderExecutionService>();
}
```

**新增配置：** `appsettings.json`

```json
{
  "OrderExecution": {
    "Platform": "Oanda",  // Oanda, TradeLocker, Mock
    "TimeoutSeconds": 30,
    "MaxRetries": 3,
    "EnableLogging": true
  }
}
```

### 重构步骤

#### 阶段1: 项目重命名（2-3小时）

1. **重命名项目文件夹和文件**
   ```bash
   git mv src/Trading.AlertSystem.Data src/Trading.Infras.Data
   git mv src/Trading.AlertSystem.Service src/Trading.Infras.Service
   git mv src/Trading.AlertSystem.Web src/Trading.Infras.Web
   ```

2. **更新项目文件（.csproj）**
   - RootNamespace
   - AssemblyName
   - 项目引用路径

3. **更新解决方案文件（.sln）**
   - 项目路径
   - 项目GUID

4. **全局替换命名空间**
   ```bash
   # 查找所有需要替换的文件
   grep -r "Trading.AlertSystem" src/ --include="*.cs"

   # 批量替换（需要小心测试）
   Trading.AlertSystem.Data → Trading.Infras.Data
   Trading.AlertSystem.Service → Trading.Infras.Service
   Trading.AlertSystem.Web → Trading.Infras.Web
   ```

5. **验证编译**
   ```bash
   dotnet clean
   dotnet restore
   dotnet build
   ```

#### 阶段2: 添加订单执行接口（3-4小时）

1. **创建接口定义**
   - `Trading.Core/Trading/IOrderExecutionService.cs`
   - 相关模型类

2. **实现适配器**
   - `OandaOrderAdapter.cs`
   - `TradeLockerOrderAdapter.cs`
   - `MockOrderExecutionService.cs`（用于测试）

3. **更新服务注册**
   - `Program.cs`
   - 配置文件

4. **编写单元测试**
   - 测试适配器转换逻辑
   - 测试错误处理

#### 阶段3: 文档更新（1-2小时）

1. **更新所有文档**
   - README.md
   - QUICKSTART.md
   - docs/*.md

2. **更新配置示例**
   - appsettings.json
   - appsettings.Development.json

3. **更新 GitHub Issues**
   - 已关闭的 Issues 中的引用

### 验收标准

**重命名部分：**
- [ ] 所有项目成功重命名
- [ ] 项目引用路径正确
- [ ] 命名空间全部更新
- [ ] 解决方案编译通过
- [ ] 所有测试通过
- [ ] 文档已更新

**订单执行接口：**
- [ ] `IOrderExecutionService` 接口定义完整
- [ ] Oanda适配器实现并测试通过
- [ ] TradeLocker适配器实现并测试通过
- [ ] 配置切换功能正常
- [ ] 错误处理完善
- [ ] 日志记录清晰
- [ ] 单元测试覆盖率 > 80%

### 技术债务清理

**顺便优化：**
- [ ] 移除未使用的依赖
- [ ] 统一日志格式
- [ ] 统一异常处理模式
- [ ] 优化配置验证

### 风险评估

**中等风险：**
- ⚠️ 大量文件重命名可能导致 Git 历史混乱
  - **缓解**：使用 `git mv` 保留历史
  - **缓解**：分多个小 commit

- ⚠️ 命名空间替换可能有遗漏
  - **缓解**：使用 IDE 的全局替换功能
  - **缓解**：编译后运行完整测试套件

**低风险：**
- 新增适配器不影响现有功能
- 可以先部署 Mock 实现进行测试

### 相关文件

**需要修改的主要文件：**
- 所有 `*.csproj` 文件
- `TradingSystem.sln`
- 所有 `.cs` 文件的命名空间
- `Program.cs`
- `appsettings.json`
- 所有 `docs/*.md` 文件

**新增文件：**
- `Trading.Core/Trading/IOrderExecutionService.cs`
- `Trading.Infras.Service/Adapters/OandaOrderAdapter.cs`
- `Trading.Infras.Service/Adapters/TradeLockerOrderAdapter.cs`
- `Trading.Infras.Service/Adapters/MockOrderExecutionService.cs`

### 标签
`refactoring`, `architecture`, `breaking-change`, `enhancement`

---

## Issue 5: 实现 AI Agent 无代码交易系统

### 标题
🤖 Implement AI Trading Agent with Natural Language Interface

### 描述
实现基于 OpenAI Function Calling 的 AI Trading Agent，允许用户通过自然语言 Prompt 执行复杂的交易任务，无需手动编写代码或调用API。

### 背景
当前系统虽然功能完善，但每次执行任务都需要：
- 手动调用多个API
- 编写代码组合不同服务
- 理解复杂的参数配置

通过实现 AI Agent，用户可以：
- **自然语言交互**：用一句话描述任务，AI自动执行
- **智能任务编排**：AI自动决定调用顺序和参数
- **多步骤自动化**：复杂任务一次完成
- **降低使用门槛**：不需要编程知识

### 示例场景

**简单任务：**
```
用户: "获取最新的黄金5分钟K线图120根，导入到数据库"

AI Agent 自动:
1. 调用 get_oanda_candles("XAU_USD", "M5", 120)
2. 调用 save_to_database("Candles", data)
3. 返回: "已保存120根黄金M5 K线到数据库"
```

**复杂任务：**
```
用户: "获取黄金的M5最新120根、H1最新80根、D1最新100根K线，
      格式化为Markdown（包含EMA20和Dist_EMA20），
      然后用GPT-4o按Al Brooks理论分析是否应该开仓，
      如果要开仓就按FTMO风控计算仓位并执行，
      所有结果都要保存到数据库"

AI Agent 自动:
1. 获取3个时间框架的数据
2. 计算EMA20指标
3. 格式化为Markdown表格
4. 调用GPT-4o进行Al Brooks理论分析
5. 根据分析结果决定是否开仓
6. 如果开仓，计算FTMO风控仓位
7. 执行订单
8. 保存所有中间结果和最终决策
9. 返回完整执行报告
```

### 实现功能

#### ✅ 1. 创建 AI Agent 项目

**新项目：** `Trading.AI.Agent`

```
src/Trading.AI.Agent/
├── Trading.AI.Agent.csproj
├── Services/
│   ├── TradingAgentService.cs          # 核心Agent服务
│   ├── DataFormatterService.cs         # 数据格式化
│   └── AgentToolRegistry.cs            # 工具注册管理
├── Controllers/
│   └── AgentController.cs              # API接口
├── Models/
│   ├── AgentRequest.cs                 # 请求模型
│   ├── AgentResponse.cs                # 响应模型
│   └── ExecutionStep.cs                # 执行步骤
└── Configuration/
    └── AgentSettings.cs                # Agent配置
```

**依赖项：**
```xml
<ItemGroup>
  <PackageReference Include="Azure.AI.OpenAI" Version="2.1.0" />
  <PackageReference Include="Skender.Stock.Indicators" Version="2.7.1" />

  <ProjectReference Include="..\Trading.AI\Trading.AI.csproj" />
  <ProjectReference Include="..\Trading.Infras.Data\Trading.Infras.Data.csproj" />
  <ProjectReference Include="..\Trading.Infras.Service\Trading.Infras.Service.csproj" />
  <ProjectReference Include="..\Trading.Core\Trading.Core.csproj" />
</ItemGroup>
```

#### ✅ 2. 核心服务：TradingAgentService

**功能：**
- 定义可用的工具（Function Definitions）
- 处理用户 Prompt
- 调用 GPT-4o-mini 进行任务理解和编排
- 执行工具函数
- 返回执行结果

**主要方法：**

```csharp
public class TradingAgentService
{
    private readonly AzureOpenAIClient _aiClient;
    private readonly IOandaService _oandaService;
    private readonly IMarketAnalysisService _analysisService;
    private readonly RiskManager _riskManager;
    private readonly IOrderExecutionService _orderService;
    private readonly DataFormatterService _formatter;
    private readonly IAlertHistoryRepository _historyRepo;
    private readonly IAIAnalysisRepository _aiAnalysisRepo;
    private readonly ILogger<TradingAgentService> _logger;

    // 工具定义
    private readonly ChatTool[] _tools;

    /// <summary>
    /// 执行用户Prompt
    /// </summary>
    public async Task<AgentResponse> ExecutePrompt(
        string prompt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 执行具体的工具函数
    /// </summary>
    private async Task<string> ExecuteFunction(
        string functionName,
        string argumentsJson);
}
```

#### ✅ 3. 工具定义（8个核心工具）

**工具1: get_oanda_candles**
```csharp
ChatTool.CreateFunctionTool(
    functionName: "get_oanda_candles",
    functionDescription: """
        从OANDA获取历史K线数据。
        支持的时间框架: M1, M5, M15, M30, H1, H4, D1, W1, MN1
        支持的品种: XAU_USD(黄金), XAG_USD(白银), EUR_USD, GBP_USD等
        返回JSON格式的K线数组，包含time, open, high, low, close, volume
        """,
    functionParameters: BinaryData.FromString("""
    {
        "type": "object",
        "properties": {
            "symbol": {
                "type": "string",
                "description": "交易品种，如XAU_USD(黄金), EUR_USD(欧美)",
                "enum": ["XAU_USD", "XAG_USD", "EUR_USD", "GBP_USD", "USD_JPY"]
            },
            "timeframe": {
                "type": "string",
                "description": "时间框架",
                "enum": ["M1", "M5", "M15", "M30", "H1", "H4", "D1", "W1", "MN1"]
            },
            "count": {
                "type": "integer",
                "description": "K线数量，建议50-500根",
                "minimum": 1,
                "maximum": 5000
            }
        },
        "required": ["symbol", "timeframe", "count"]
    }
    """)
)
```

**工具2: format_candles_to_markdown**
```csharp
ChatTool.CreateFunctionTool(
    functionName: "format_candles_to_markdown",
    functionDescription: """
        将K线数据格式化为Markdown表格，包含以下列：
        - Date: 日期（MMDD格式，可选包含年份）
        - Time: 时间（HHMM格式）
        - Open, High, Low, Close: OHLC价格
        - BodyRange: High - Low
        - Body%: (Close - Open) / BodyRange * 100
        - EMA20: 20周期指数移动平均线
        - Dist_EMA20: Low - EMA20
        """,
    functionParameters: BinaryData.FromString("""
    {
        "type": "object",
        "properties": {
            "candles_json": {
                "type": "string",
                "description": "K线数据的JSON字符串（get_oanda_candles返回的结果）"
            },
            "ema_period": {
                "type": "integer",
                "description": "EMA周期，默认20",
                "default": 20
            },
            "include_year": {
                "type": "boolean",
                "description": "日期是否包含年份，默认false",
                "default": false
            }
        },
        "required": ["candles_json"]
    }
    """)
)
```

**工具3: analyze_market_with_gpt4o**
```csharp
ChatTool.CreateFunctionTool(
    functionName: "analyze_market_with_gpt4o",
    functionDescription: """
        使用GPT-4o分析市场数据，基于Al Brooks价格行为理论给出交易建议。
        可以分析单个或多个时间框架的数据。
        返回分析结果包括：
        - 是否建议开仓
        - 开仓方向（buy/sell）
        - 建议入场价
        - 建议止损价
        - 建议止盈价
        - 信号质量评分（0-100）
        - 详细分析理由
        """,
    functionParameters: BinaryData.FromString("""
    {
        "type": "object",
        "properties": {
            "symbol": {
                "type": "string",
                "description": "交易品种"
            },
            "m5_data": {
                "type": "string",
                "description": "M5时间框架的Markdown数据（可选）"
            },
            "h1_data": {
                "type": "string",
                "description": "H1时间框架的Markdown数据（可选）"
            },
            "d1_data": {
                "type": "string",
                "description": "D1时间框架的Markdown数据（可选）"
            },
            "analysis_method": {
                "type": "string",
                "description": "分析方法",
                "enum": ["AlBrooks", "PriceAction", "MultiTimeFrame"],
                "default": "AlBrooks"
            }
        },
        "required": ["symbol"]
    }
    """)
)
```

**工具4: calculate_position_size**
```csharp
ChatTool.CreateFunctionTool(
    functionName: "calculate_position_size",
    functionDescription: """
        根据风控规则计算合适的仓位大小。
        支持FTMO、Blue Guardian等Prop Firm规则。
        会检查单日亏损限额和总亏损限额。
        返回是否允许开仓、建议仓位、风险金额等。
        """,
    functionParameters: BinaryData.FromString("""
    {
        "type": "object",
        "properties": {
            "symbol": {
                "type": "string",
                "description": "交易品种，如XAUUSD"
            },
            "broker": {
                "type": "string",
                "description": "经纪商名称，用于获取品种规格",
                "enum": ["ICMarkets", "OandaV20", "BlueGuardian"],
                "default": "ICMarkets"
            },
            "entry_price": {
                "type": "number",
                "description": "入场价格"
            },
            "stop_loss": {
                "type": "number",
                "description": "止损价格"
            },
            "account_balance": {
                "type": "number",
                "description": "当前账户余额"
            },
            "prop_firm_rule": {
                "type": "string",
                "description": "使用的Prop Firm规则",
                "enum": ["FTMO", "BlueGuardian", "Custom"],
                "default": "FTMO"
            },
            "risk_percent": {
                "type": "number",
                "description": "单笔风险百分比，默认1.0%",
                "default": 1.0
            }
        },
        "required": ["symbol", "entry_price", "stop_loss", "account_balance"]
    }
    """)
)
```

**工具5: place_market_order**
```csharp
ChatTool.CreateFunctionTool(
    functionName: "place_market_order",
    functionDescription: """
        在交易平台上执行市价单开仓。
        会自动使用配置的交易平台（Oanda或TradeLocker）。
        返回订单执行结果，包括订单ID、成交价格等。
        注意：这是真实交易，请谨慎使用！
        """,
    functionParameters: BinaryData.FromString("""
    {
        "type": "object",
        "properties": {
            "symbol": {
                "type": "string",
                "description": "交易品种"
            },
            "lots": {
                "type": "number",
                "description": "交易手数"
            },
            "direction": {
                "type": "string",
                "description": "交易方向",
                "enum": ["buy", "sell"]
            },
            "stop_loss": {
                "type": "number",
                "description": "止损价格（可选）"
            },
            "take_profit": {
                "type": "number",
                "description": "止盈价格（可选）"
            },
            "comment": {
                "type": "string",
                "description": "订单备注（可选）"
            }
        },
        "required": ["symbol", "lots", "direction"]
    }
    """)
)
```

**工具6: save_analysis_to_database**
```csharp
ChatTool.CreateFunctionTool(
    functionName: "save_analysis_to_database",
    functionDescription: """
        将AI分析结果保存到Azure Table Storage。
        保存到 AIAnalysisHistory 表中，便于后续查询和回溯。
        """,
    functionParameters: BinaryData.FromString("""
    {
        "type": "object",
        "properties": {
            "symbol": {
                "type": "string",
                "description": "交易品种"
            },
            "analysis_result": {
                "type": "string",
                "description": "分析结果的JSON字符串"
            },
            "timeframe": {
                "type": "string",
                "description": "分析的时间框架"
            }
        },
        "required": ["symbol", "analysis_result"]
    }
    """)
)
```

**工具7: save_trade_decision**
```csharp
ChatTool.CreateFunctionTool(
    functionName: "save_trade_decision",
    functionDescription: """
        将交易决策保存到数据库，包括：
        - AI分析ID
        - 订单ID
        - 仓位大小
        - 入场价格
        - 止损止盈
        - 风控参数
        便于后续跟踪交易表现。
        """,
    functionParameters: BinaryData.FromString("""
    {
        "type": "object",
        "properties": {
            "decision_data": {
                "type": "string",
                "description": "交易决策数据的JSON字符串"
            }
        },
        "required": ["decision_data"]
    }
    """)
)
```

**工具8: get_account_info**
```csharp
ChatTool.CreateFunctionTool(
    functionName: "get_account_info",
    functionDescription: """
        获取当前交易账户信息，包括：
        - 账户余额
        - 当日盈亏
        - 总盈亏
        - 持仓列表
        - 可用保证金
        用于风控计算和决策参考。
        """,
    functionParameters: BinaryData.FromString("""
    {
        "type": "object",
        "properties": {}
    }
    """)
)
```

#### ✅ 4. 数据格式化服务

**文件：** `DataFormatterService.cs`

```csharp
public class DataFormatterService
{
    /// <summary>
    /// 格式化K线数据为Markdown表格
    /// </summary>
    public string FormatToMarkdown(
        List<Candle> candles,
        int emaPeriod = 20,
        bool includeYear = false)
    {
        // 1. 计算EMA指标
        var emaValues = CalculateEMA(candles, emaPeriod);

        // 2. 生成Markdown表格
        var sb = new StringBuilder();
        sb.AppendLine("| Date | Time | Open | High | Low | Close | BodyRange | Body% | EMA20 | Dist_EMA20 |");
        sb.AppendLine("|------|------|------|------|-----|-------|-----------|-------|-------|------------|");

        for (int i = 0; i < candles.Count; i++)
        {
            var c = candles[i];
            var date = includeYear
                ? c.Time.ToString("yyyyMMdd")
                : c.Time.ToString("MMdd");
            var time = c.Time.ToString("HHmm");

            var bodyRange = c.High - c.Low;
            var bodyPercent = bodyRange != 0
                ? (c.Close - c.Open) / bodyRange * 100
                : 0;
            var distEma = i < emaValues.Count
                ? c.Low - emaValues[i]
                : 0;

            sb.AppendLine($"| {date} | {time} | {c.Open:F2} | {c.High:F2} | {c.Low:F2} | {c.Close:F2} | {bodyRange:F2} | {bodyPercent:F1}% | {(i < emaValues.Count ? emaValues[i] : 0):F2} | {distEma:F2} |");
        }

        return sb.ToString();
    }

    /// <summary>
    /// 使用 Skender.Stock.Indicators 计算EMA
    /// </summary>
    private List<decimal> CalculateEMA(List<Candle> candles, int period)
    {
        var quotes = candles.Select(c => new Quote
        {
            Date = c.Time,
            Open = (decimal)c.Open,
            High = (decimal)c.High,
            Low = (decimal)c.Low,
            Close = (decimal)c.Close,
            Volume = (decimal)c.Volume
        }).ToList();

        var emaResults = quotes.GetEma(period);

        return emaResults
            .Select(e => (decimal)(e.Ema ?? 0))
            .ToList();
    }
}
```

#### ✅ 5. API 控制器

**文件：** `AgentController.cs`

```csharp
[ApiController]
[Route("api/[controller]")]
public class AgentController : ControllerBase
{
    private readonly TradingAgentService _agentService;
    private readonly ILogger<AgentController> _logger;

    public AgentController(
        TradingAgentService agentService,
        ILogger<AgentController> logger)
    {
        _agentService = agentService;
        _logger = logger;
    }

    /// <summary>
    /// 执行AI Agent任务
    /// </summary>
    [HttpPost("execute")]
    [ProducesResponseType(typeof(AgentResponse), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Execute(
        [FromBody] AgentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("收到Agent请求: {Prompt}", request.Prompt);

            var result = await _agentService.ExecutePrompt(
                request.Prompt,
                cancellationToken);

            return Ok(result);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Agent任务被取消");
            return StatusCode(499, new { error = "Request cancelled" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Agent执行失败: {Message}", ex.Message);
            return BadRequest(new
            {
                success = false,
                error = ex.Message,
                type = ex.GetType().Name
            });
        }
    }

    /// <summary>
    /// 获取Agent能力列表
    /// </summary>
    [HttpGet("capabilities")]
    public IActionResult GetCapabilities()
    {
        return Ok(new
        {
            tools = new[]
            {
                "get_oanda_candles - 获取K线数据",
                "format_candles_to_markdown - 格式化为Markdown",
                "analyze_market_with_gpt4o - GPT-4o市场分析",
                "calculate_position_size - 计算仓位（FTMO风控）",
                "place_market_order - 执行市价单",
                "save_analysis_to_database - 保存分析结果",
                "save_trade_decision - 保存交易决策",
                "get_account_info - 获取账户信息"
            },
            supported_symbols = new[] { "XAU_USD", "XAG_USD", "EUR_USD", "GBP_USD", "USD_JPY" },
            supported_timeframes = new[] { "M1", "M5", "M15", "M30", "H1", "H4", "D1", "W1", "MN1" },
            risk_rules = new[] { "FTMO", "BlueGuardian", "Custom" }
        });
    }
}
```

**模型定义：**

```csharp
public class AgentRequest
{
    [Required]
    public string Prompt { get; set; } = string.Empty;

    public Dictionary<string, object>? Context { get; set; }
}

public class AgentResponse
{
    public bool Success { get; set; }
    public string Result { get; set; } = string.Empty;
    public List<ExecutionStep> Steps { get; set; } = new();
    public int TotalSteps { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public string? ErrorMessage { get; set; }
}

public class ExecutionStep
{
    public int StepNumber { get; set; }
    public string ToolName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Result { get; set; }
    public bool Success { get; set; }
    public TimeSpan Duration { get; set; }
}
```

#### ✅ 6. 配置和服务注册

**appsettings.json:**

```json
{
  "AgentSettings": {
    "Enabled": true,
    "Model": "gpt-4o-mini",
    "MaxIterations": 20,
    "TimeoutSeconds": 300,
    "EnableTracing": true,
    "SafeMode": true,  // true=需要确认才执行真实交易
    "AllowedOperations": [
      "get_data",
      "analyze",
      "calculate_position",
      "save_data"
      // "place_order" 需要明确启用
    ]
  }
}
```

**Program.cs:**

```csharp
// 注册 Agent 服务
builder.Services.Configure<AgentSettings>(
    builder.Configuration.GetSection("AgentSettings"));

builder.Services.AddSingleton<DataFormatterService>();
builder.Services.AddScoped<TradingAgentService>();

// 注册 Controller
builder.Services.AddControllers()
    .AddApplicationPart(typeof(AgentController).Assembly);
```

### 实现步骤

#### 阶段1: 基础框架（1天）

1. **创建项目**
   ```bash
   dotnet new classlib -n Trading.AI.Agent -o src/Trading.AI.Agent
   cd src/Trading.AI.Agent
   dotnet add package Azure.AI.OpenAI --version 2.1.0
   dotnet add package Skender.Stock.Indicators --version 2.7.1
   ```

2. **添加项目引用**
   ```bash
   dotnet add reference ../Trading.AI/Trading.AI.csproj
   dotnet add reference ../Trading.Infras.Data/Trading.Infras.Data.csproj
   dotnet add reference ../Trading.Infras.Service/Trading.Infras.Service.csproj
   dotnet add reference ../Trading.Core/Trading.Core.csproj
   ```

3. **创建基础文件**
   - Models (AgentRequest, AgentResponse)
   - Configuration (AgentSettings)
   - DataFormatterService 基础实现

#### 阶段2: 核心Agent实现（2-3天）

1. **实现工具定义**
   - 定义8个工具的Function Schema
   - 编写清晰的描述和参数说明

2. **实现TradingAgentService**
   - ExecutePrompt 主循环
   - ExecuteFunction 函数路由
   - 8个工具函数的具体实现

3. **实现DataFormatterService**
   - Markdown格式化
   - EMA计算
   - 错误处理

#### 阶段3: API和集成（1天）

1. **实现AgentController**
   - POST /api/agent/execute
   - GET /api/agent/capabilities
   - 错误处理和日志

2. **服务注册和配置**
   - Program.cs 配置
   - appsettings.json
   - User Secrets

#### 阶段4: 测试和文档（1-2天）

1. **编写测试**
   - 单元测试（工具函数）
   - 集成测试（完整流程）
   - 端到端测试（真实场景）

2. **编写文档**
   - API文档
   - 使用示例
   - 故障排查指南

### 测试场景

**测试1: 简单数据获取**
```bash
curl -X POST http://localhost:5000/api/agent/execute \
  -H "Content-Type: application/json" \
  -d '{
    "prompt": "获取黄金最新100根5分钟K线"
  }'
```

**测试2: 数据格式化**
```bash
curl -X POST http://localhost:5000/api/agent/execute \
  -H "Content-Type: application/json" \
  -d '{
    "prompt": "获取黄金最新50根H1 K线，格式化为Markdown表格，包含EMA20"
  }'
```

**测试3: 市场分析**
```bash
curl -X POST http://localhost:5000/api/agent/execute \
  -H "Content-Type: application/json" \
  -d '{
    "prompt": "分析黄金当前市场状态，使用M5和H1数据，给出交易建议"
  }'
```

**测试4: 完整流程（SafeMode）**
```bash
curl -X POST http://localhost:5000/api/agent/execute \
  -H "Content-Type: application/json" \
  -d '{
    "prompt": "获取黄金M5最新120根、H1最新80根K线，用GPT-4o分析是否应该开仓，如果要开仓就计算FTMO风控仓位，但不要真的下单，只返回建议"
  }'
```

### 验收标准

**功能完整性：**
- [ ] 8个工具全部实现并测试通过
- [ ] Agent能正确理解简单任务
- [ ] Agent能正确理解复杂任务
- [ ] 工具调用顺序符合逻辑
- [ ] 参数传递正确无误

**数据格式化：**
- [ ] Markdown表格格式正确
- [ ] EMA20计算准确
- [ ] Body%和Dist_EMA20计算正确
- [ ] 日期时间格式符合要求

**错误处理：**
- [ ] API错误有明确提示
- [ ] 工具执行失败能优雅降级
- [ ] 超时处理正确
- [ ] 日志记录完整

**安全性：**
- [ ] SafeMode 正常工作
- [ ] 真实交易需要明确授权
- [ ] API Key 安全存储
- [ ] 敏感信息不记录日志

**性能：**
- [ ] 简单任务 < 10秒
- [ ] 复杂任务 < 60秒
- [ ] 并发请求正常处理
- [ ] 资源占用合理

**文档：**
- [ ] API文档完整
- [ ] 使用示例清晰
- [ ] 配置说明详细
- [ ] 故障排查指南

### 安全考虑

**SafeMode 机制：**
```csharp
if (_settings.SafeMode && toolName == "place_market_order")
{
    _logger.LogWarning("⚠️ SafeMode启用，拒绝真实下单");
    return JsonSerializer.Serialize(new
    {
        success = false,
        message = "SafeMode启用，无法执行真实交易。请在配置中禁用SafeMode或使用模拟模式。",
        simulated_result = "如果执行，将会下单..."
    });
}
```

**操作权限控制：**
```csharp
var allowedOps = _settings.AllowedOperations ?? new List<string>();
if (!allowedOps.Contains(toolName))
{
    return JsonSerializer.Serialize(new
    {
        success = false,
        message = $"操作 {toolName} 未被授权"
    });
}
```

### 未来扩展

**阶段2功能（可选）：**
- [ ] 支持更多交易平台（MT5, cTrader）
- [ ] 支持更多技术指标
- [ ] 支持自定义分析策略
- [ ] 支持语音输入
- [ ] 支持多语言
- [ ] Web UI 界面
- [ ] 实时执行监控
- [ ] 历史任务回溯

### 相关Issue
- 依赖 **Issue 4** (重构)：需要 `IOrderExecutionService` 接口
- 关联 **Issue 2** (Azure OpenAI)：使用已有的 AI 服务

### 标签
`ai`, `agent`, `enhancement`, `openai`, `automation`

---

## 工作计划

### Issue 优先级
1. **Issue 1** (Azure Table Storage) - 已完成 ✅
2. **Issue 2** (Azure OpenAI) - 已完成 ✅
3. **Issue 3** (Position Calculator) - 已完成 ✅
4. **Issue 4** (重构) - **新增，优先级高** ⭐
5. **Issue 5** (AI Agent) - **新增，依赖Issue 4** 🤖

### 分支策略
- ~~`feature/position-calculator`~~ - Issue 1 (已合并)
- ~~`feature/telegram-integration`~~ - Issue 2 (已合并)
- ~~`feature/android-trading-app`~~ - Issue 3 (待定)
- `feature/refactor-infrastructure` - **Issue 4 (新)**
- `feature/ai-agent` - **Issue 5 (新)**

### Worktree 目录
- ~~`../richdad-position-calc`~~ - Issue 1 (已完成)
- ~~`../richdad-telegram`~~ - Issue 2 (已完成)
- ~~`../richdad-android`~~ - Issue 3 (待定)
- `../richdad-refactor` - **Issue 4 (新)**
- `../richdad-agent` - **Issue 5 (新)**

### 推荐工作流程

**先完成Issue 4（重构）：**
```bash
# 创建重构分支
git worktree add ../richdad-refactor -b feature/refactor-infrastructure

cd ../richdad-refactor
# 1. 重命名项目
# 2. 添加 IOrderExecutionService
# 3. 测试验证
# 4. 合并到 main
```

**然后执行Issue 5（AI Agent）：**
```bash
# 创建AI Agent分支
git worktree add ../richdad-agent -b feature/ai-agent

cd ../richdad-agent
# 1. 创建 Trading.AI.Agent 项目
# 2. 实现 TradingAgentService
# 3. 实现 DataFormatterService
# 4. 实现 AgentController
# 5. 测试验证
# 6. 合并到 main
```

### 预计工时
- **Issue 4 (重构)**: 1-1.5天
- **Issue 5 (AI Agent)**: 4-5天
- **总计**: 5-6.5天
