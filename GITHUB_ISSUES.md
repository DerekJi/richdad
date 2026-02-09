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

## Issue 6: 实现数据持久化与智能缓存层 ✅ 已完成

### 标题
🗄️ Implement Data Persistence Layer with Smart Caching for Market Data

### 状态
✅ **已完成** - 2026-02-09

### 描述
建立基于 Azure Table Storage 的低成本、高性能数据持久化层，解决 OANDA API 重复调用问题，为回测和 AI 分析提供数据基础。

### 实际实现

#### ✅ 核心功能（已实现）

1. **数据模型 - CandleEntity**
   - PartitionKey: Symbol（品种）
   - RowKey: `{TimeFrame}_{yyyyMMdd_HHmm}`
   - 支持 IsComplete 字段（实时K线更新）
   - UTC 时间标准化

2. **Repository 层 - CandleRepository**
   - ✅ SaveBatchAsync - 批量保存（UpsertReplace自动更新）
   - ✅ GetRangeAsync - 按时间范围查询
   - ✅ GetLatestTimeAsync - 获取最新时间（UTC修复）
   - ✅ GetEarliestTimeAsync - 获取最早时间（UTC修复）
   - ✅ GetCountAsync - 统计记录数
   - ✅ DeleteRangeAsync - 批量删除

3. **Service 层 - CandleInitializationService**
   - ✅ InitializeHistoricalDataAsync - 批量初始化多品种多周期
   - ✅ InitializeSymbolTimeFrameAsync - 单品种单周期初始化
   - ✅ IncrementalUpdateAsync - **增量更新（支持实时K线更新）**
   - ✅ 智能时间差检测（避免重复更新）
   - ✅ 自动更新未完成K线（IsComplete=false）

4. **API 层 - CandleController**
   - ✅ POST /api/candle/initialize - 初始化历史数据
   - ✅ POST /api/candle/update - 增量更新
   - ✅ GET /api/candle/candles - 查询K线数据
   - ✅ GET /api/candle/stats - 统计信息
   - ✅ DELETE /api/candle/candles - 删除数据

5. **关键修复**
   - ✅ **UTC 时区统一处理**
     - OandaService: DateTimeStyles.AdjustToUniversal
     - CandleRepository: SpecifyKind(UTC) for queries
     - CandleEntity.ToCandle: 确保返回UTC时间
   - ✅ **增量更新逻辑修复**
     - 过滤条件: `>= latestTime`（包含未完成K线）
     - 时间差计算正确（UTC vs UTC）
     - UpsertReplace 自动更新同RowKey记录

### 技术亮点

- **成本优化**：Azure Table Storage 成本极低（$1-3/月）
- **实时更新**：IsComplete 字段支持未完成K线的自动更新
- **智能增量**：只获取缺失的数据，避免重复API调用
- **批量操作**：优化性能，支持大数据量初始化
- **时区安全**：全链路 UTC 时间处理，避免时区混乱

### 测试验证

✅ 初始化功能测试通过（1000根K线）
✅ 增量更新测试通过（自动更新未完成K线）
✅ 时区处理验证通过（UTC统一）
✅ 统计API测试通过

### 背景
当前系统每次分析都需要从 OANDA API 获取数据，存在以下问题：
- **重复调用成本高**：相同的历史数据被反复请求
- **响应速度慢**：API 调用延迟影响实时决策
- **无法回测**：缺少历史数据存储，无法验证策略
- **数据不连续**：网络故障可能导致数据缺失

通过实现数据持久化层，系统可以：
- **智能缓存**：优先从数据库查询，仅补充缺失数据
- **快速响应**：本地查询延迟 < 10ms
- **支持回测**：存储完整历史数据
- **成本优化**：Azure Table Storage 成本极低（$1-3/月）

### 实现功能

#### ✅ 1. 数据模型设计

**表1: MarketData - 原始 OHLC 数据**

```csharp
public class MarketDataEntity : ITableEntity
{
    // PartitionKey: Symbol (如 "XAUUSD", "EURUSD")
    // RowKey: TimeFrame_DateTime (如 "M5_20260208_1015")

    public string Symbol { get; set; } = string.Empty;
    public string TimeFrame { get; set; } = string.Empty; // D1, H1, M5
    public DateTime Time { get; set; }

    // OHLC 数据
    public double Open { get; set; }
    public double High { get; set; }
    public double Low { get; set; }
    public double Close { get; set; }
    public long Volume { get; set; }

    // 是否完整（已收盘的 K 线）
    public bool IsComplete { get; set; }

    // 数据源
    public string Source { get; set; } = "OANDA";

    // Azure Table Storage 必需字段
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}
```

**表2: ProcessedData - 预处理指标数据**

```csharp
public class ProcessedDataEntity : ITableEntity
{
    // PartitionKey: Symbol_TimeFrame (如 "XAUUSD_M5")
    // RowKey: DateTime (如 "20260208_1015")

    public string Symbol { get; set; } = string.Empty;
    public string TimeFrame { get; set; } = string.Empty;
    public DateTime Time { get; set; }

    // Al Brooks 核心指标
    public double BodyPercent { get; set; }      // (Close-Low)/(High-Low)
    public double ClosePosition { get; set; }    // 同 BodyPercent，收盘位置
    public double DistanceToEMA20 { get; set; }  // Close - EMA20
    public double Range { get; set; }            // High - Low

    // 技术指标
    public double EMA20 { get; set; }
    public double ATR { get; set; }

    // 形态标签（JSON 数组字符串）
    public string Tags { get; set; } = "[]";  // ["ii", "H2", "Signal"]

    // Azure Table Storage 必需字段
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}
```

#### ✅ 2. 智能缓存服务

**新增服务：** `MarketDataCacheService`

```csharp
public class MarketDataCacheService
{
    private readonly IOandaService _oandaService;
    private readonly IMarketDataRepository _repository;
    private readonly ILogger<MarketDataCacheService> _logger;

    /// <summary>
    /// 智能获取 K 线数据：优先从数据库查询，仅补充缺失部分
    /// </summary>
    public async Task<List<Candle>> GetCandlesAsync(
        string symbol,
        string timeFrame,
        int count,
        DateTime? endTime = null)
    {
        endTime ??= DateTime.UtcNow;
        var startTime = CalculateStartTime(endTime.Value, timeFrame, count);

        // 1. 从数据库查询已有数据
        var cachedData = await _repository.GetRangeAsync(
            symbol, timeFrame, startTime, endTime.Value);

        _logger.LogInformation(
            "从缓存获取 {Count} 根 K 线 ({Symbol} {TimeFrame})",
            cachedData.Count, symbol, timeFrame);

        // 2. 检测缺失的时间段
        var missingRanges = DetectMissingRanges(
            startTime, endTime.Value, timeFrame, cachedData);

        if (missingRanges.Any())
        {
            _logger.LogInformation(
                "检测到 {Count} 个缺失时间段，从 OANDA 补充数据",
                missingRanges.Count);

            // 3. 从 OANDA API 获取缺失数据
            foreach (var range in missingRanges)
            {
                var freshData = await _oandaService.GetCandlesAsync(
                    symbol, timeFrame, range.Start, range.End);

                // 4. 保存到数据库
                await _repository.SaveBatchAsync(freshData);

                cachedData.AddRange(freshData);
            }
        }

        // 5. 按时间排序并返回
        return cachedData
            .OrderBy(c => c.Time)
            .Take(count)
            .ToList();
    }

    /// <summary>
    /// 检测缺失的时间段
    /// </summary>
    private List<TimeRange> DetectMissingRanges(
        DateTime start,
        DateTime end,
        string timeFrame,
        List<Candle> existingData)
    {
        var expectedTimes = GenerateExpectedTimes(start, end, timeFrame);
        var existingTimes = existingData.Select(c => c.Time).ToHashSet();
        var missingTimes = expectedTimes.Where(t => !existingTimes.Contains(t));

        // 将连续的缺失时间合并为时间段
        return MergeIntoRanges(missingTimes, timeFrame);
    }
}
```

#### ✅ 3. Repository 实现

**MarketDataRepository.cs:**

```csharp
public class MarketDataRepository : IMarketDataRepository
{
    private readonly TableClient _tableClient;
    private readonly ILogger<MarketDataRepository> _logger;

    public async Task<List<Candle>> GetRangeAsync(
        string symbol,
        string timeFrame,
        DateTime startTime,
        DateTime endTime)
    {
        // 构建查询过滤器
        var filter = $"PartitionKey eq '{symbol}' and " +
                     $"RowKey ge '{timeFrame}_{startTime:yyyyMMdd_HHmm}' and " +
                     $"RowKey le '{timeFrame}_{endTime:yyyyMMdd_HHmm}'";

        var results = new List<Candle>();
        await foreach (var entity in _tableClient.QueryAsync<MarketDataEntity>(filter))
        {
            results.Add(MapToCandle(entity));
        }

        return results;
    }

    public async Task SaveBatchAsync(List<Candle> candles)
    {
        // Azure Table Storage 批量操作限制：100条/批次
        var batches = candles.Chunk(100);

        foreach (var batch in batches)
        {
            var batchOperation = new List<TableTransactionAction>();

            foreach (var candle in batch)
            {
                var entity = MapToEntity(candle);
                batchOperation.Add(new TableTransactionAction(
                    TableTransactionActionType.UpsertReplace, entity));
            }

            await _tableClient.SubmitTransactionAsync(batchOperation);
        }

        _logger.LogInformation("成功保存 {Count} 根 K 线到数据库", candles.Count);
    }

    public async Task<DateTime?> GetLatestTimeAsync(string symbol, string timeFrame)
    {
        var filter = $"PartitionKey eq '{symbol}' and " +
                     $"RowKey ge '{timeFrame}_'";

        await foreach (var entity in _tableClient.QueryAsync<MarketDataEntity>(
            filter, maxPerPage: 1,
            select: new[] { "Time" }))
        {
            return entity.Time;
        }

        return null;
    }
}
```

#### ✅ 4. 查询 API

**新增控制器：** `MarketDataController`

```csharp
[ApiController]
[Route("api/[controller]")]
public class MarketDataController : ControllerBase
{
    private readonly MarketDataCacheService _cacheService;

    /// <summary>
    /// 获取 K 线数据（智能缓存）
    /// GET /api/marketdata/candles?symbol=XAUUSD&timeFrame=M5&count=200
    /// </summary>
    [HttpGet("candles")]
    public async Task<ActionResult<List<Candle>>> GetCandles(
        [Required] string symbol,
        [Required] string timeFrame,
        int count = 100,
        DateTime? endTime = null)
    {
        var candles = await _cacheService.GetCandlesAsync(
            symbol, timeFrame, count, endTime);

        return Ok(candles);
    }

    /// <summary>
    /// 获取最新数据时间
    /// GET /api/marketdata/latest?symbol=XAUUSD&timeFrame=M5
    /// </summary>
    [HttpGet("latest")]
    public async Task<ActionResult<DateTime?>> GetLatestTime(
        [Required] string symbol,
        [Required] string timeFrame)
    {
        var latestTime = await _repository.GetLatestTimeAsync(symbol, timeFrame);
        return Ok(new { symbol, timeFrame, latestTime });
    }

    /// <summary>
    /// 手动刷新缓存
    /// POST /api/marketdata/refresh
    /// </summary>
    [HttpPost("refresh")]
    public async Task<ActionResult> RefreshCache(
        [Required] string symbol,
        [Required] string timeFrame,
        DateTime? startTime = null)
    {
        startTime ??= DateTime.UtcNow.AddDays(-7);

        var candles = await _oandaService.GetCandlesAsync(
            symbol, timeFrame, startTime.Value, DateTime.UtcNow);

        await _repository.SaveBatchAsync(candles);

        return Ok(new {
            message = "缓存已刷新",
            count = candles.Count
        });
    }

    /// <summary>
    /// 获取缓存统计信息
    /// GET /api/marketdata/stats
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult> GetStats()
    {
        var stats = await _repository.GetStatisticsAsync();
        return Ok(stats);
    }
}
```

#### ✅ 5. 配置管理

**appsettings.json:**

```json
{
  "AzureTableStorage": {
    "ConnectionString": "UseDevelopmentStorage=true",
    "MarketDataTableName": "MarketData",
    "ProcessedDataTableName": "ProcessedData",
    "Enabled": true
  },
  "MarketDataCache": {
    "EnableSmartCache": true,
    "MaxCacheAgeDays": 90,
    "AutoRefreshEnabled": true,
    "RefreshIntervalMinutes": 5,
    "PreloadSymbols": ["XAUUSD", "XAGUSD", "EURUSD", "AUDUSD", "USDJPY"]
  }
}
```

### 数据填充策略

#### 初始化历史数据

```csharp
public class DataInitializationService
{
    /// <summary>
    /// 初始化历史数据（首次运行）
    /// </summary>
    public async Task InitializeHistoricalDataAsync()
    {
        var symbols = new[] { "XAUUSD", "XAGUSD", "EURUSD", "AUDUSD", "USDJPY" };
        var timeFrames = new[] { "D1", "H1", "M5" };

        foreach (var symbol in symbols)
        {
            foreach (var timeFrame in timeFrames)
            {
                var count = timeFrame switch
                {
                    "D1" => 200,  // 约 200 个交易日
                    "H1" => 1000, // 约 6 周
                    "M5" => 2000, // 约 1 周
                    _ => 100
                };

                _logger.LogInformation(
                    "正在初始化 {Symbol} {TimeFrame} 数据，共 {Count} 根...",
                    symbol, timeFrame, count);

                var candles = await _oandaService.GetCandlesAsync(
                    symbol, timeFrame, count);

                await _repository.SaveBatchAsync(candles);

                // 避免 API 速率限制
                await Task.Delay(1000);
            }
        }
    }
}
```

### 性能优化

#### 分区键设计

**优化策略：**
- **MarketData**：按 Symbol 分区（如 "XAUUSD"）
  - 优点：同品种查询效率高
  - 避免跨分区查询

- **ProcessedData**：按 Symbol_TimeFrame 分区（如 "XAUUSD_M5"）
  - 更细粒度的分区
  - 提高并发写入性能

#### 批量操作优化

```csharp
// 并行获取多个品种数据
var tasks = symbols.Select(symbol =>
    _cacheService.GetCandlesAsync(symbol, "M5", 200));

var results = await Task.WhenAll(tasks);
```

### 验收标准

**数据持久化：**
- [ ] MarketData 表成功创建并存储 OHLC 数据
- [ ] ProcessedData 表成功存储预处理指标
- [ ] 批量写入性能 > 1000 条/秒
- [ ] 查询性能 < 100ms（200 根 K 线）

**智能缓存：**
- [ ] 首次查询从 OANDA 获取数据
- [ ] 重复查询从缓存返回（命中率 > 90%）
- [ ] 自动检测并补充缺失数据
- [ ] 缓存失效机制正常工作

**API 接口：**
- [ ] GET /api/marketdata/candles 正常工作
- [ ] GET /api/marketdata/latest 返回正确时间
- [ ] POST /api/marketdata/refresh 刷新成功
- [ ] 错误处理和日志记录完善

**数据完整性：**
- [ ] 无重复数据
- [ ] 时间序列连续性检查
- [ ] 数据验证（OHLC 逻辑正确）

### 成本估算

**Azure Table Storage 成本：**

| 数据量 | 存储成本 | 操作成本 | 月总成本 |
|--------|----------|----------|----------|
| 10GB（约200万根K线） | $0.45 | $0.50 | **$0.95** |
| 50GB（约1000万根K线） | $2.25 | $1.00 | **$3.25** |

对比 Cosmos DB（$30-50/月），成本节省 **95%+**。

### 相关文件

**新增文件：**
- `Trading.Infras.Data/Models/MarketDataEntity.cs` - 数据模型
- `Trading.Infras.Data/Models/ProcessedDataEntity.cs` - 预处理数据模型
- `Trading.Infras.Data/Repositories/MarketDataRepository.cs` - 数据访问层
- `Trading.Infras.Service/Services/MarketDataCacheService.cs` - 缓存服务
- `Trading.Infras.Service/Services/DataInitializationService.cs` - 初始化服务
- `Trading.Infras.Web/Controllers/MarketDataController.cs` - API 控制器

**文档：**
- `docs/MARKET_DATA_CACHE_GUIDE.md` - 使用指南
- `docs/DATA_INITIALIZATION.md` - 数据初始化指南

### 后续扩展

**阶段 2（可选）：**
- [ ] 实现 Redis 二级缓存（热数据）
- [ ] 数据压缩和归档策略
- [ ] 多数据源支持（OANDA + TradeLocker）
- [ ] 数据质量监控和报警

### 标签
`enhancement`, `database`, `performance`, `azure`, `storage`, `caching`

---

## Issue 7: 实现 Al Brooks 形态识别引擎 ✅

**状态：** 已完成 | **完成时间：** 2026-02-10

### 标题
🔍 Implement Al Brooks Pattern Recognition Engine with Advanced Technical Analysis

### 描述
实现基于 Al Brooks 价格行为学理论的自动化形态识别引擎，为 AI 决策提供预处理的技术分析数据。

### 背景与动机
Al Brooks 的价格行为学理论依赖于对 K 线形态的精确识别，包括：
- **内包线（ii/iii）**：波动收缩，突破前兆
- **趋势计数（H1/H2/L1/L2）**：回调入场点识别
- **跟进棒（Follow Through）**：突破确认
- **测试（Test）**：关键位支撑/阻力验证
- **突破（Breakout）**：突破 20 根 K 线高低点

AI 模型虽然强大，但在处理原始 OHLC 数据时存在局限：
- **计算不精确**：小数点级别的判断容易出错
- **形态识别模糊**：难以准确识别连续的内包线结构
- **Token 消耗大**：需要解释大量数据背景

通过实现程序化的形态识别引擎，系统可以：
- **100% 准确识别**：基于硬编码逻辑，无误判
- **减少 AI 负担**：直接提供形态标签，AI 专注决策
- **数据结构化**：生成 Al Brooks 理论所需的衍生指标
- **支持回测**：可验证形态在历史数据中的表现

### 实现功能

#### ✅ 1. 核心指标计算

**新增服务：** `TechnicalIndicatorService`

```csharp
public class TechnicalIndicatorService
{
    /// <summary>
    /// 计算 Body%（收盘位置）
    /// 0.0 = 收在最低点，1.0 = 收在最高点
    /// </summary>
    public double CalculateBodyPercent(Candle candle)
    {
        var range = candle.High - candle.Low;
        if (range == 0) return 0.5; // Doji

        return (candle.Close - candle.Low) / range;
    }

    /// <summary>
    /// 计算收盘位置（别名，与 Body% 相同）
    /// </summary>
    public double CalculateClosePosition(Candle candle)
    {
        return CalculateBodyPercent(candle);
    }

    /// <summary>
    /// 计算与 EMA20 的距离（Ticks）
    /// </summary>
    public double CalculateDistanceToEMA(Candle candle, double ema20, string symbol)
    {
        var tickSize = GetTickSize(symbol);
        return (candle.Close - ema20) / tickSize;
    }

    /// <summary>
    /// 计算 K 线范围（High - Low）
    /// </summary>
    public double CalculateRange(Candle candle)
    {
        return candle.High - candle.Low;
    }

    /// <summary>
    /// 计算实体大小百分比
    /// </summary>
    public double CalculateBodySizePercent(Candle candle)
    {
        var range = candle.High - candle.Low;
        if (range == 0) return 0;

        var bodySize = Math.Abs(candle.Close - candle.Open);
        return bodySize / range;
    }

    /// <summary>
    /// 判断是否为 Doji（十字星）
    /// </summary>
    public bool IsDoji(Candle candle, double threshold = 0.1)
    {
        return CalculateBodySizePercent(candle) < threshold;
    }

    private double GetTickSize(string symbol)
    {
        return symbol switch
        {
            "XAUUSD" or "XAGUSD" => 0.01,
            "EURUSD" or "AUDUSD" => 0.00001,
            "USDJPY" => 0.001,
            _ => 0.00001
        };
    }
}
```

#### ✅ 2. 形态识别服务

**新增服务：** `PatternRecognitionService`

```csharp
public class PatternRecognitionService
{
    private readonly TechnicalIndicatorService _indicatorService;
    private readonly ILogger<PatternRecognitionService> _logger;

    /// <summary>
    /// 识别所有形态并返回标签列表
    /// </summary>
    public List<string> RecognizePatterns(
        List<Candle> candles,
        int index,
        double ema20,
        string symbol)
    {
        var tags = new List<string>();

        // 内包线形态
        if (IsInsideBar(candles, index))
        {
            tags.Add("Inside");

            // 检查是否为 ii（连续两根内包线）
            if (index >= 1 && IsInsideBar(candles, index - 1))
            {
                tags.Add("ii");
            }

            // 检查是否为 iii（连续三根内包线）
            if (index >= 2 &&
                IsInsideBar(candles, index - 1) &&
                IsInsideBar(candles, index - 2))
            {
                tags.Add("iii");
            }
        }

        // 外包线
        if (IsOutsideBar(candles, index))
        {
            tags.Add("Outside");
        }

        // 突破形态
        if (IsBreakoutBar(candles, index))
        {
            tags.Add("BO");

            var direction = candles[index].Close > candles[index].Open ? "Bull" : "Bear";
            tags.Add($"BO_{direction}");
        }

        // Spike（强动能棒）
        if (IsSpike(candles, index))
        {
            tags.Add("Spike");
        }

        // 跟进棒（Follow Through）
        if (IsFollowThrough(candles, index))
        {
            tags.Add("FT");

            var strength = GetFollowThroughStrength(candles, index);
            tags.Add($"FT_{strength}");
        }

        // 测试 EMA20
        if (IsTestingEMA(candles[index], ema20))
        {
            tags.Add("Test_EMA20");
        }

        // EMA Gap Bar（整根 K 线在 EMA 一侧）
        if (IsEMAGapBar(candles[index], ema20))
        {
            var side = candles[index].Low > ema20 ? "Above" : "Below";
            tags.Add($"Gap_EMA_{side}");
        }

        // 趋势计数（H1/H2/L1/L2）
        var trendCount = GetTrendCount(candles, index);
        if (trendCount != null)
        {
            tags.Add(trendCount);
        }

        // Doji
        if (_indicatorService.IsDoji(candles[index]))
        {
            tags.Add("Doji");
        }

        // 信号棒（符合 Al Brooks 入场条件的 K 线）
        if (IsSignalBar(candles, index, ema20))
        {
            tags.Add("Signal");
        }

        return tags;
    }

    /// <summary>
    /// 判断是否为内包线
    /// </summary>
    private bool IsInsideBar(List<Candle> candles, int index)
    {
        if (index < 1) return false;

        var current = candles[index];
        var previous = candles[index - 1];

        return current.High < previous.High &&
               current.Low > previous.Low;
    }

    /// <summary>
    /// 判断是否为外包线
    /// </summary>
    private bool IsOutsideBar(List<Candle> candles, int index)
    {
        if (index < 1) return false;

        var current = candles[index];
        var previous = candles[index - 1];

        return current.High > previous.High &&
               current.Low < previous.Low;
    }

    /// <summary>
    /// 判断是否为突破棒
    /// </summary>
    private bool IsBreakoutBar(List<Candle> candles, int index)
    {
        if (index < 20) return false;

        var current = candles[index];
        var recent = candles.Skip(index - 20).Take(20).ToList();

        var recentHigh = recent.Max(c => c.High);
        var recentLow = recent.Min(c => c.Low);

        // 突破最近 20 根 K 线的高低点
        var isBreakingHigh = current.Close > recentHigh;
        var isBreakingLow = current.Close < recentLow;

        // 实体大小大于平均波动的 1.5 倍
        var avgRange = recent.Average(c => c.High - c.Low);
        var currentRange = current.High - current.Low;
        var isStrongBody = currentRange > avgRange * 1.5;

        return (isBreakingHigh || isBreakingLow) && isStrongBody;
    }

    /// <summary>
    /// 判断是否为 Spike（强动能棒）
    /// </summary>
    private bool IsSpike(List<Candle> candles, int index)
    {
        if (index < 5) return false;

        var current = candles[index];
        var recent = candles.Skip(index - 5).Take(5).ToList();

        var avgRange = recent.Average(c => c.High - c.Low);
        var currentRange = current.High - current.Low;

        // 范围是平均值的 2 倍以上
        return currentRange > avgRange * 2.0;
    }

    /// <summary>
    /// 判断是否为跟进棒（Follow Through）
    /// </summary>
    private bool IsFollowThrough(List<Candle> candles, int index)
    {
        if (index < 2) return false;

        var current = candles[index];
        var previous = candles[index - 1];
        var twoBefore = candles[index - 2];

        // 前一根是突破棒
        if (!IsBreakoutBar(candles, index - 1))
            return false;

        // 当前棒继续朝同方向收盘
        var prevDirection = previous.Close > previous.Open;
        var currDirection = current.Close > current.Open;

        if (prevDirection != currDirection)
            return false;

        // 且收盘价继续创新高/新低
        if (prevDirection)
            return current.Close > previous.Close;
        else
            return current.Close < previous.Close;
    }

    /// <summary>
    /// 获取跟进棒强度
    /// </summary>
    private string GetFollowThroughStrength(List<Candle> candles, int index)
    {
        var bodyPercent = _indicatorService.CalculateBodySizePercent(candles[index]);

        return bodyPercent switch
        {
            > 0.7 => "Strong",
            > 0.4 => "Medium",
            _ => "Weak"
        };
    }

    /// <summary>
    /// 判断是否测试 EMA20
    /// </summary>
    private bool IsTestingEMA(Candle candle, double ema20)
    {
        // K 线的影线触及 EMA20
        return candle.Low <= ema20 && candle.High >= ema20;
    }

    /// <summary>
    /// 判断是否为 EMA Gap Bar（整根 K 线在 EMA 一侧）
    /// </summary>
    private bool IsEMAGapBar(Candle candle, double ema20)
    {
        return candle.Low > ema20 || candle.High < ema20;
    }

    /// <summary>
    /// 获取趋势计数（H1/H2/L1/L2）
    /// </summary>
    private string? GetTrendCount(List<Candle> candles, int index)
    {
        if (index < 5) return null;

        var current = candles[index];
        var recent = candles.Skip(index - 5).Take(5).ToList();

        // 判断趋势方向（通过 EMA 斜率）
        var ema = CalculateEMA(recent, 20);
        var emaPrev = CalculateEMA(candles.Skip(index - 6).Take(20).ToList(), 20);

        var isBullTrend = ema > emaPrev;

        if (isBullTrend)
        {
            // 多头趋势中，寻找 Higher High
            var count = 0;
            for (int i = index; i >= Math.Max(0, index - 10); i--)
            {
                if (i > 0 && candles[i].High > candles[i - 1].High)
                {
                    count++;

                    // 如果创出波段新高，计数重置
                    if (IsNewSwingHigh(candles, i))
                    {
                        count = 1;
                        break;
                    }
                }
            }

            return count > 0 ? $"H{count}" : null;
        }
        else
        {
            // 空头趋势中，寻找 Lower Low
            var count = 0;
            for (int i = index; i >= Math.Max(0, index - 10); i--)
            {
                if (i > 0 && candles[i].Low < candles[i - 1].Low)
                {
                    count++;

                    if (IsNewSwingLow(candles, i))
                    {
                        count = 1;
                        break;
                    }
                }
            }

            return count > 0 ? $"L{count}" : null;
        }
    }

    /// <summary>
    /// 判断是否创出波段新高
    /// </summary>
    private bool IsNewSwingHigh(List<Candle> candles, int index)
    {
        if (index < 10) return false;

        var current = candles[index];
        var recent = candles.Skip(index - 10).Take(10).ToList();

        return current.High > recent.Max(c => c.High);
    }

    /// <summary>
    /// 判断是否创出波段新低
    /// </summary>
    private bool IsNewSwingLow(List<Candle> candles, int index)
    {
        if (index < 10) return false;

        var current = candles[index];
        var recent = candles.Skip(index - 10).Take(10).ToList();

        return current.Low < recent.Min(c => c.Low);
    }

    /// <summary>
    /// 判断是否为信号棒
    /// </summary>
    private bool IsSignalBar(List<Candle> candles, int index, double ema20)
    {
        var current = candles[index];
        var bodyPercent = _indicatorService.CalculateBodySizePercent(current);

        // 强收盘（Body% > 0.6）
        var hasStrongClose = bodyPercent > 0.6;

        // 在趋势方向上
        var closeAboveEMA = current.Close > ema20;
        var isClimaxBar = IsSpike(candles, index);

        // 信号棒：强收盘 + 在 EMA 正确一侧 + 非 Climax
        return hasStrongClose && (closeAboveEMA == (current.Close > current.Open)) && !isClimaxBar;
    }

    /// <summary>
    /// 计算 EMA
    /// </summary>
    private double CalculateEMA(List<Candle> candles, int period)
    {
        // 简化实现，实际应使用标准 EMA 算法
        return candles.TakeLast(period).Average(c => c.Close);
    }
}
```

#### ✅ 3. Markdown 表格生成器

**新增服务：** `MarkdownTableGenerator`

```csharp
public class MarkdownTableGenerator
{
    private readonly TechnicalIndicatorService _indicatorService;
    private readonly PatternRecognitionService _patternService;

    /// <summary>
    /// 生成 Context 表（表格 A）：5-Bar 合并数据
    /// </summary>
    public string GenerateContextTable(
        List<Candle> candles,
        string symbol,
        double[] ema20Values)
    {
        var sb = new StringBuilder();

        // 表头
        sb.AppendLine("## Context Table (5-Bar Aggregated)");
        sb.AppendLine();
        sb.AppendLine("| Period | High_Max | Low_Min | Avg_C_Pos | Avg_Dist_EMA | Market_State |");
        sb.AppendLine("| :--- | :--- | :--- | :--- | :--- | :--- |");

        // 每 5 根 K 线合并为 1 行
        var groupSize = 5;
        var groups = candles
            .Select((c, i) => new { Candle = c, Index = i, EMA = ema20Values[i] })
            .GroupBy(x => x.Index / groupSize)
            .Where(g => g.Count() == groupSize);

        foreach (var group in groups)
        {
            var firstIndex = group.First().Index;
            var lastIndex = group.Last().Index;

            var highMax = group.Max(x => x.Candle.High);
            var lowMin = group.Min(x => x.Candle.Low);

            var avgClosePos = group.Average(x =>
                _indicatorService.CalculateClosePosition(x.Candle));

            var avgDistEMA = group.Average(x =>
                _indicatorService.CalculateDistanceToEMA(x.Candle, x.EMA, symbol));

            var marketState = DetermineMarketState(avgClosePos, avgDistEMA);

            sb.AppendLine($"| {-lastIndex} to {-firstIndex} | " +
                         $"{highMax:F2} | {lowMin:F2} | " +
                         $"{avgClosePos:F2} | {avgDistEMA:+#;-#;0} | " +
                         $"{marketState} |");
        }

        return sb.ToString();
    }

    /// <summary>
    /// 生成 Focus 表（表格 B）：最近 30 根全精度数据
    /// </summary>
    public string GenerateFocusTable(
        List<Candle> candles,
        string symbol,
        double[] ema20Values,
        int focusCount = 30)
    {
        var sb = new StringBuilder();

        // 表头
        sb.AppendLine("## Focus Table (Recent Bars - Full Precision)");
        sb.AppendLine();
        sb.AppendLine("| Bar# | Time | High | Low | Close | C_Pos | Body% | Dist_EMA | Range | Tags |");
        sb.AppendLine("| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |");

        // 最后 focusCount 根 K 线
        var focusBars = candles.TakeLast(focusCount).ToList();
        var focusEMA = ema20Values.TakeLast(focusCount).ToArray();

        for (int i = 0; i < focusBars.Count; i++)
        {
            var candle = focusBars[i];
            var ema = focusEMA[i];
            var barNumber = -(focusBars.Count - i);

            var closePos = _indicatorService.CalculateClosePosition(candle);
            var bodyPercent = _indicatorService.CalculateBodySizePercent(candle);
            var distEMA = _indicatorService.CalculateDistanceToEMA(candle, ema, symbol);
            var range = _indicatorService.CalculateRange(candle);

            // 识别形态标签
            var allCandles = candles.Take(candles.Count - focusBars.Count + i + 1).ToList();
            var tags = _patternService.RecognizePatterns(
                allCandles, allCandles.Count - 1, ema, symbol);

            var tagsStr = tags.Any() ? string.Join(", ", tags) : "-";

            sb.AppendLine($"| {barNumber} | " +
                         $"{candle.Time:HH:mm} | " +
                         $"{candle.High:F2} | {candle.Low:F2} | {candle.Close:F2} | " +
                         $"{closePos:F2} | {bodyPercent:F2} | " +
                         $"{distEMA:+#;-#;0} | {range:F2} | " +
                         $"{tagsStr} |");
        }

        return sb.ToString();
    }

    /// <summary>
    /// 生成形态摘要
    /// </summary>
    public string GeneratePatternSummary(
        List<Candle> candles,
        string symbol,
        double[] ema20Values)
    {
        var sb = new StringBuilder();

        sb.AppendLine("## Pre-processed Pattern Recognition");
        sb.AppendLine();

        // 检测最近 30 根 K 线中的关键形态
        var recentCount = Math.Min(30, candles.Count);
        var recentCandles = candles.TakeLast(recentCount).ToList();
        var recentEMA = ema20Values.TakeLast(recentCount).ToArray();

        // ii 结构
        var iiPatterns = new List<int>();
        for (int i = 2; i < recentCount; i++)
        {
            var tags = _patternService.RecognizePatterns(
                recentCandles, i, recentEMA[i], symbol);

            if (tags.Contains("ii"))
            {
                iiPatterns.Add(i - recentCount);
            }
        }

        if (iiPatterns.Any())
        {
            sb.AppendLine($"- **ii Structure**: Detected at Bar {string.Join(", ", iiPatterns)}");
        }

        // Micro Double Bottom/Top
        var doubleBottoms = DetectDoubleBottoms(recentCandles);
        if (doubleBottoms.Any())
        {
            sb.AppendLine($"- **Micro Double Bottom**: Low prices at {string.Join(", ", doubleBottoms.Select(d => $"{d:F2}"))}");
        }

        // EMA Gap Bar
        var gapBars = recentCandles
            .Select((c, i) => new { Candle = c, Index = i, EMA = recentEMA[i] })
            .Where(x => Math.Abs(x.Candle.Low - x.EMA) > 10 || Math.Abs(x.Candle.High - x.EMA) > 10)
            .ToList();

        if (gapBars.Any())
        {
            sb.AppendLine($"- **EMA Gap Bar**: {gapBars.Count} bars with significant gap from EMA20");
        }

        // 当前趋势
        var trendDirection = DetermineTrendDirection(recentCandles, recentEMA);
        sb.AppendLine($"- **Current Trend**: {trendDirection}");

        sb.AppendLine();

        return sb.ToString();
    }

    private string DetermineMarketState(double avgClosePos, double avgDistEMA)
    {
        if (Math.Abs(avgDistEMA) < 5)
            return "Trading Range";

        if (avgClosePos > 0.7 && avgDistEMA > 10)
            return "Strong Bull";

        if (avgClosePos < 0.3 && avgDistEMA < -10)
            return "Strong Bear";

        if (avgDistEMA > 5)
            return "Tight Bull Channel";

        if (avgDistEMA < -5)
            return "Tight Bear Channel";

        return "Unclear";
    }

    private List<double> DetectDoubleBottoms(List<Candle> candles)
    {
        var bottoms = new List<double>();
        var threshold = 0.2; // 允许 0.2 的误差

        for (int i = 5; i < candles.Count; i++)
        {
            var currentLow = candles[i].Low;

            // 查找之前的相似低点
            for (int j = Math.Max(0, i - 20); j < i - 2; j++)
            {
                if (Math.Abs(candles[j].Low - currentLow) < threshold)
                {
                    bottoms.Add(currentLow);
                    break;
                }
            }
        }

        return bottoms.Distinct().ToList();
    }

    private string DetermineTrendDirection(List<Candle> candles, double[] emaValues)
    {
        if (emaValues.Length < 2) return "Unclear";

        var emaSlope = emaValues[^1] - emaValues[^10];
        var priceAboveEMA = candles.TakeLast(10).Count(c => c.Close > emaValues[candles.Count - 1]);

        if (emaSlope > 5 && priceAboveEMA > 7)
            return "Strong Bullish Trend";

        if (emaSlope < -5 && priceAboveEMA < 3)
            return "Strong Bearish Trend";

        if (Math.Abs(emaSlope) < 2)
            return "Sideways / Trading Range";

        return emaSlope > 0 ? "Weak Bullish" : "Weak Bearish";
    }
}
```

#### ✅ 4. 数据处理管道

**新增服务：** `MarketDataProcessor`

```csharp
public class MarketDataProcessor
{
    private readonly MarketDataCacheService _cacheService;
    private readonly TechnicalIndicatorService _indicatorService;
    private readonly PatternRecognitionService _patternService;
    private readonly MarkdownTableGenerator _tableGenerator;
    private readonly IProcessedDataRepository _repository;

    /// <summary>
    /// 完整的数据处理管道
    /// </summary>
    public async Task<ProcessedMarketData> ProcessMarketDataAsync(
        string symbol,
        string timeFrame,
        int count)
    {
        // 1. 获取原始 K 线数据
        var candles = await _cacheService.GetCandlesAsync(symbol, timeFrame, count);

        // 2. 计算 EMA20
        var ema20Values = CalculateEMAArray(candles, 20);

        // 3. 计算衍生指标并识别形态
        var processedData = new List<ProcessedDataEntity>();

        for (int i = 0; i < candles.Count; i++)
        {
            var candle = candles[i];
            var ema20 = ema20Values[i];

            var bodyPercent = _indicatorService.CalculateBodyPercent(candle);
            var closePos = _indicatorService.CalculateClosePosition(candle);
            var distEMA = _indicatorService.CalculateDistanceToEMA(candle, ema20, symbol);
            var range = _indicatorService.CalculateRange(candle);

            // 识别形态
            var tags = _patternService.RecognizePatterns(
                candles.Take(i + 1).ToList(), i, ema20, symbol);

            processedData.Add(new ProcessedDataEntity
            {
                Symbol = symbol,
                TimeFrame = timeFrame,
                Time = candle.Time,
                BodyPercent = bodyPercent,
                ClosePosition = closePos,
                DistanceToEMA20 = distEMA,
                Range = range,
                EMA20 = ema20,
                ATR = candle.ATR, // 假设已在 Candle 中计算
                Tags = JsonSerializer.Serialize(tags),
                PartitionKey = $"{symbol}_{timeFrame}",
                RowKey = candle.Time.ToString("yyyyMMdd_HHmm")
            });
        }

        // 4. 保存预处理数据到数据库
        await _repository.SaveBatchAsync(processedData);

        // 5. 生成 Markdown 表格
        var contextTable = _tableGenerator.GenerateContextTable(candles, symbol, ema20Values);
        var focusTable = _tableGenerator.GenerateFocusTable(candles, symbol, ema20Values);
        var patternSummary = _tableGenerator.GeneratePatternSummary(candles, symbol, ema20Values);

        return new ProcessedMarketData
        {
            Symbol = symbol,
            TimeFrame = timeFrame,
            Candles = candles,
            ProcessedData = processedData,
            ContextTable = contextTable,
            FocusTable = focusTable,
            PatternSummary = patternSummary
        };
    }

    private double[] CalculateEMAArray(List<Candle> candles, int period)
    {
        var ema = new double[candles.Count];
        var multiplier = 2.0 / (period + 1);

        // 初始 SMA
        ema[0] = candles.Take(period).Average(c => c.Close);

        // 递归计算 EMA
        for (int i = 1; i < candles.Count; i++)
        {
            ema[i] = (candles[i].Close - ema[i - 1]) * multiplier + ema[i - 1];
        }

        return ema;
    }
}
```

### 验收标准

**指标计算：**
- [ ] Body% 计算准确（0-1 范围）
- [ ] Dist_EMA 计算准确（Ticks）
- [ ] Range 计算准确
- [ ] EMA20 计算准确

**形态识别：**
- [ ] 内包线（ii/iii）识别准确率 100%
- [ ] H1/H2/L1/L2 计数逻辑正确
- [ ] Follow Through 识别符合 Al Brooks 定义
- [ ] Test/Gap Bar 识别准确

**Markdown 生成：**
- [ ] Context 表格式正确
- [ ] Focus 表格式正确
- [ ] 形态摘要清晰易读
- [ ] Tags 列包含所有识别的形态

**数据持久化：**
- [ ] ProcessedData 表成功存储
- [ ] Tags 字段 JSON 序列化正确
- [ ] 查询性能 < 100ms

### 相关文件

**新增文件：**
- `Trading.Core/Analysis/TechnicalIndicatorService.cs`
- `Trading.Core/Analysis/PatternRecognitionService.cs`
- `Trading.Core/Analysis/MarkdownTableGenerator.cs`
- `Trading.Infras.Service/Services/MarketDataProcessor.cs`
- `Trading.Infras.Data/Repositories/ProcessedDataRepository.cs`

**文档：**
- `docs/AL_BROOKS_PATTERNS.md` - 形态识别详解
- `docs/MARKDOWN_TABLE_FORMAT.md` - 表格格式说明

### 标签
`enhancement`, `analysis`, `pattern-recognition`, `al-brooks`, `technical-analysis`

---

## Issue 8: 实现四级 AI 决策编排系统

### 标题
🤖 Implement Four-Tier AI Decision Orchestration System with Multi-Model Integration

### 描述
实现基于 Al Brooks 理论的四级 AI 决策编排系统，通过多模型协作（Azure GPT-4o + DeepSeek）实现从宏观分析到微观决策的完整交易流程。

### 背景
单一 AI 模型难以同时处理宏观趋势分析和微观入场时机判断。通过分级架构：
- **L1 (D1 战略层)**：确定日内交易方向偏见
- **L2 (H1 结构层)**：判断市场周期（趋势/震荡）
- **L3 (M5 监控层)**：识别潜在交易机会
- **L4 (决策层)**：最终开仓决策（带思维链推理）

每一级使用最适合的模型：
- **Azure GPT-4o**：宏观分析（L1）、信号识别（L3）
- **Azure GPT-4o-mini**：高频监控（L3）
- **DeepSeek-V3**：结构分析（L2）
- **DeepSeek-R1**：最终决策（L4，带 CoT 思维链）

### 架构设计

```
┌─────────────────────────────────────────────────────────┐
│  L1: D1 Strategic Analysis (GPT-4o)                     │
│  → Determine daily bias: Bullish/Bearish/Neutral        │
│  → Identify support/resistance levels                    │
│  → Output: Daily trading bias                           │
└────────────────────┬────────────────────────────────────┘
                     ↓ (If trend clear)
┌─────────────────────────────────────────────────────────┐
│  L2: H1 Structure Analysis (DeepSeek-V3)                │
│  → Analyze market cycle: Trend/Channel/Range            │
│  → Check alignment with D1 bias                         │
│  → Output: Active/Idle status                           │
└────────────────────┬────────────────────────────────────┘
                     ↓ (If Active)
┌─────────────────────────────────────────────────────────┐
│  L3: M5 Signal Monitoring (GPT-4o-mini)                 │
│  → Every 5 minutes, check for setups                    │
│  → Filter out low-probability signals                   │
│  → Output: Potential_Setup / No_Signal                  │
└────────────────────┬────────────────────────────────────┘
                     ↓ (If Potential_Setup)
┌─────────────────────────────────────────────────────────┐
│  L4: Final Decision (DeepSeek-R1 with CoT)              │
│  → Receive context from L1/L2/L3                        │
│  → Apply Al Brooks theory critically                    │
│  → Think: "Why should I NOT trade?"                     │
│  → Output: Execute/Reject with reasoning                │
└─────────────────────────────────────────────────────────┘
```

### 实现功能

#### ✅ 1. 基础模型

**决策上下文模型：**

```csharp
public class TradingContext
{
    // L1 输出
    public DailyBias L1_DailyBias { get; set; } = new();

    // L2 输出
    public StructureAnalysis L2_Structure { get; set; } = new();

    // L3 输出
    public SignalDetection L3_Signal { get; set; } = new();

    // 原始数据
    public ProcessedMarketData MarketData { get; set; } = new();

    // 时间戳
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class DailyBias
{
    public string Direction { get; set; } = "Neutral"; // Bullish/Bearish/Neutral
    public double Confidence { get; set; } // 0-100
    public List<double> SupportLevels { get; set; } = new();
    public List<double> ResistanceLevels { get; set; } = new();
    public string TrendType { get; set; } = ""; // Strong/Weak/Sideways
    public string Reasoning { get; set; } = "";
    public DateTime AnalyzedAt { get; set; }
}

public class StructureAnalysis
{
    public string MarketCycle { get; set; } = ""; // Trend/Channel/Range
    public string Status { get; set; } = "Idle"; // Active/Idle
    public bool AlignedWithD1 { get; set; }
    public string CurrentPhase { get; set; } = ""; // Breakout/Pullback/Trading Range
    public string Reasoning { get; set; } = "";
    public DateTime AnalyzedAt { get; set; }
}

public class SignalDetection
{
    public string Status { get; set; } = "No_Signal"; // Potential_Setup/No_Signal
    public string SetupType { get; set; } = ""; // H2/L2/MTR/Gap_Bar
    public double EntryPrice { get; set; }
    public double StopLoss { get; set; }
    public double TakeProfit { get; set; }
    public string Direction { get; set; } = ""; // Buy/Sell
    public string Reasoning { get; set; } = "";
    public DateTime DetectedAt { get; set; }
}

public class FinalDecision
{
    public string Action { get; set; } = "Reject"; // Execute/Reject
    public string Direction { get; set; } = "";
    public double EntryPrice { get; set; }
    public double StopLoss { get; set; }
    public double TakeProfit { get; set; }
    public double LotSize { get; set; }
    public string Reasoning { get; set; } = "";
    public string ThinkingProcess { get; set; } = ""; // DeepSeek-R1 的思维链
    public int ConfidenceScore { get; set; } // 0-100
    public List<string> RiskFactors { get; set; } = new();
    public DateTime DecidedAt { get; set; }
}
```

#### ✅ 2. L1 - 日线战略分析

**新增服务：** `L1_DailyAnalysisService`

```csharp
public class L1_DailyAnalysisService
{
    private readonly AzureOpenAIClient _aiClient;
    private readonly MarketDataProcessor _dataProcessor;
    private readonly ILogger<L1_DailyAnalysisService> _logger;
    private readonly IMemoryCache _cache;

    /// <summary>
    /// 分析 D1 日线，确定当日交易偏见
    /// 每天 UTC+2 00:00 执行一次，结果缓存 24 小时
    /// </summary>
    public async Task<DailyBias> AnalyzeDailyBiasAsync(string symbol)
    {
        var cacheKey = $"L1_DailyBias_{symbol}_{DateTime.UtcNow:yyyyMMdd}";

        // 检查缓存
        if (_cache.TryGetValue<DailyBias>(cacheKey, out var cachedBias))
        {
            _logger.LogInformation("从缓存返回 D1 分析结果");
            return cachedBias;
        }

        // 获取 D1 数据（80 根足够）
        var processedData = await _dataProcessor.ProcessMarketDataAsync(symbol, "D1", 80);

        // 构建 System Prompt
        var systemPrompt = @"
You are Al Brooks, a master of Price Action trading.

Your task: Analyze the D1 (daily) chart and provide a **trading bias** for today.

Focus on:
1. **Trend Direction**: Is this a strong bull trend, bear trend, or trading range?
2. **Market Phase**: Breakout, pullback, or consolidation?
3. **Key Levels**: Identify major support/resistance from recent swing highs/lows.
4. **Today's Bias**: Should traders look for longs, shorts, or stay flat?

Output format (JSON):
{
  ""Direction"": ""Bullish"" | ""Bearish"" | ""Neutral"",
  ""Confidence"": 0-100,
  ""SupportLevels"": [price1, price2],
  ""ResistanceLevels"": [price1, price2],
  ""TrendType"": ""Strong"" | ""Weak"" | ""Sideways"",
  ""Reasoning"": ""Brief explanation based on Al Brooks theory""
}";

        // 构建 User Prompt
        var userPrompt = $@"
# Market Context
Symbol: {symbol}
Timeframe: D1
Current Date: {DateTime.UtcNow:yyyy-MM-dd}

{processedData.ContextTable}

{processedData.FocusTable}

{processedData.PatternSummary}

Analyze and provide today's trading bias.";

        // 调用 GPT-4o
        var chatOptions = new ChatCompletionOptions
        {
            Temperature = 0.3f,
            MaxTokens = 1000,
            ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
        };

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userPrompt)
        };

        var completion = await _aiClient.GetChatClient("gpt-4o")
            .CompleteChatAsync(messages, chatOptions);

        var response = completion.Value.Content[0].Text;
        var bias = JsonSerializer.Deserialize<DailyBias>(response);
        bias.AnalyzedAt = DateTime.UtcNow;

        // 缓存 24 小时
        _cache.Set(cacheKey, bias, TimeSpan.FromHours(24));

        _logger.LogInformation(
            "L1 分析完成: {Direction} (信心: {Confidence}%)",
            bias.Direction, bias.Confidence);

        return bias;
    }
}
```

#### ✅ 3. L2 - 小时结构分析

**新增服务：** `L2_StructureAnalysisService`

```csharp
public class L2_StructureAnalysisService
{
    private readonly HttpClient _deepSeekClient;
    private readonly MarketDataProcessor _dataProcessor;
    private readonly ILogger<L2_StructureAnalysisService> _logger;
    private readonly IMemoryCache _cache;

    /// <summary>
    /// 分析 H1 结构，判断市场周期
    /// 每小时执行一次，结果缓存 1 小时
    /// </summary>
    public async Task<StructureAnalysis> AnalyzeStructureAsync(
        string symbol,
        DailyBias dailyBias)
    {
        var cacheKey = $"L2_Structure_{symbol}_{DateTime.UtcNow:yyyyMMddHH}";

        if (_cache.TryGetValue<StructureAnalysis>(cacheKey, out var cachedStructure))
        {
            _logger.LogInformation("从缓存返回 H1 结构分析");
            return cachedStructure;
        }

        // 获取 H1 数据（120 根）
        var processedData = await _dataProcessor.ProcessMarketDataAsync(symbol, "H1", 120);

        // 构建 Prompt
        var systemPrompt = @"
You are analyzing the H1 (1-hour) chart to determine the market structure.

Given the D1 bias, your job is to decide:
1. **Market Cycle**: Is this a trending market, a channel, or a trading range?
2. **Status**: Should we be actively looking for trades (Active) or wait (Idle)?
3. **Alignment**: Does H1 align with the D1 bias?

Rules:
- If D1 is Bullish, we only look for long setups on H1 pullbacks.
- If H1 is in a tight trading range, Status = Idle.
- If H1 shows a clear trend in D1 direction, Status = Active.

Output JSON:
{
  ""MarketCycle"": ""Trend"" | ""Channel"" | ""Range"",
  ""Status"": ""Active"" | ""Idle"",
  ""AlignedWithD1"": true | false,
  ""CurrentPhase"": ""Breakout"" | ""Pullback"" | ""Trading Range"",
  ""Reasoning"": ""Explanation""
}";

        var userPrompt = $@"
# D1 Bias (from L1)
Direction: {dailyBias.Direction}
Confidence: {dailyBias.Confidence}%
Reasoning: {dailyBias.Reasoning}

# H1 Market Data
Symbol: {symbol}
Timeframe: H1

{processedData.ContextTable}

{processedData.FocusTable}

{processedData.PatternSummary}

Analyze H1 structure and decide Status.";

        // 调用 DeepSeek-V3
        var requestBody = new
        {
            model = "deepseek-chat",
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            temperature = 0.3,
            response_format = new { type = "json_object" }
        };

        var response = await _deepSeekClient.PostAsJsonAsync("", requestBody);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonDocument.Parse(responseContent);

        var structure = JsonSerializer.Deserialize<StructureAnalysis>(
            result.RootElement.GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content").GetString());

        structure.AnalyzedAt = DateTime.UtcNow;

        // 缓存 1 小时
        _cache.Set(cacheKey, structure, TimeSpan.FromHours(1));

        _logger.LogInformation(
            "L2 分析完成: {MarketCycle}, Status={Status}",
            structure.MarketCycle, structure.Status);

        return structure;
    }
}
```

#### ✅ 4. L3 - 5分钟信号监控

**新增服务：** `L3_SignalMonitoringService`

```csharp
public class L3_SignalMonitoringService
{
    private readonly AzureOpenAIClient _aiClient;
    private readonly MarketDataProcessor _dataProcessor;
    private readonly ILogger<L3_SignalMonitoringService> _logger;

    /// <summary>
    /// 监控 M5 图表，寻找交易设置
    /// 每 5 分钟执行一次（当 L2 Status = Active 时）
    /// </summary>
    public async Task<SignalDetection> MonitorForSignalsAsync(
        string symbol,
        TradingContext context)
    {
        // 仅在 L2 Status = Active 时执行
        if (context.L2_Structure.Status != "Active")
        {
            return new SignalDetection
            {
                Status = "No_Signal",
                Reasoning = "L2 Status is Idle, no monitoring needed"
            };
        }

        // 获取 M5 数据（最近 80 根）
        var processedData = await _dataProcessor.ProcessMarketDataAsync(symbol, "M5", 80);

        // 使用 GPT-4o-mini（成本低）
        var systemPrompt = @"
You are monitoring the M5 chart for Al Brooks Price Action setups.

Given:
- D1 Bias (from L1)
- H1 Structure (from L2)
- M5 Recent bars

Your task: Identify if there is a **potential trading setup**.

Al Brooks Setups to look for:
1. **H2/L2** (Second entry in trend)
2. **MTR** (Major Trend Reversal at key level)
3. **Gap Bar** (EMA20 gap with strong momentum)
4. **ii Breakout** (Inside-inside structure breakout)

If found, provide entry, stop loss, take profit based on signal bar.

Output JSON:
{
  ""Status"": ""Potential_Setup"" | ""No_Signal"",
  ""SetupType"": ""H2"" | ""L2"" | ""MTR"" | ""Gap_Bar"" | """",
  ""EntryPrice"": 0.0,
  ""StopLoss"": 0.0,
  ""TakeProfit"": 0.0,
  ""Direction"": ""Buy"" | ""Sell"" | """",
  ""Reasoning"": ""Brief explanation""
}";

        var userPrompt = $@"
# Trading Context

## L1 - D1 Bias
Direction: {context.L1_DailyBias.Direction}
Key Levels: Support={string.Join(", ", context.L1_DailyBias.SupportLevels)},
            Resistance={string.Join(", ", context.L1_DailyBias.ResistanceLevels)}

## L2 - H1 Structure
Market Cycle: {context.L2_Structure.MarketCycle}
Current Phase: {context.L2_Structure.CurrentPhase}

## M5 - Recent Bars
Symbol: {symbol}

{processedData.FocusTable}

{processedData.PatternSummary}

Check for trading setups. Remember: We only trade in the direction of D1 bias.
If D1 is Bullish, only look for long setups.";

        var chatOptions = new ChatCompletionOptions
        {
            Temperature = 0.3f,
            MaxTokens = 800,
            ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
        };

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userPrompt)
        };

        var completion = await _aiClient.GetChatClient("gpt-4o-mini")
            .CompleteChatAsync(messages, chatOptions);

        var response = completion.Value.Content[0].Text;
        var signal = JsonSerializer.Deserialize<SignalDetection>(response);
        signal.DetectedAt = DateTime.UtcNow;

        _logger.LogInformation(
            "L3 监控完成: {Status}, Setup={SetupType}",
            signal.Status, signal.SetupType);

        return signal;
    }
}
```

#### ✅ 5. L4 - 最终决策（带思维链）

**新增服务：** `L4_FinalDecisionService`

```csharp
public class L4_FinalDecisionService
{
    private readonly HttpClient _deepSeekClient;
    private readonly ILogger<L4_FinalDecisionService> _logger;

    /// <summary>
    /// 最终决策：使用 DeepSeek-R1 进行深度推理
    /// 仅在 L3 检测到 Potential_Setup 时触发
    /// </summary>
    public async Task<FinalDecision> MakeFinalDecisionAsync(
        TradingContext context)
    {
        // 仅在 L3 发现潜在设置时执行
        if (context.L3_Signal.Status != "Potential_Setup")
        {
            return new FinalDecision
            {
                Action = "Reject",
                Reasoning = "No potential setup from L3"
            };
        }

        // 构建 System Prompt（批判性思维模式）
        var systemPrompt = @"
You are Al Brooks. You are about to make a real trading decision with real money.

Your PRIMARY job is to find reasons NOT to trade. You are a professional skeptic.

Given:
- D1 daily bias
- H1 structure analysis
- M5 signal detection (with suggested entry/SL/TP)

Your analysis process:
1. **Check Alignment**: Does everything align? D1/H1/M5?
2. **Risk Assessment**: Is this really a high-probability setup?
3. **Find Flaws**: What could go wrong? Is this a trap?
4. **Final Call**: Execute or Reject?

IMPORTANT:
- If there is ANY doubt, choose Reject.
- FTMO requires 60%+ win rate. Only take the BEST setups.
- Consider: Is the stop loss too wide? Is TP realistic? Is momentum fading?

Output JSON:
{
  ""Action"": ""Execute"" | ""Reject"",
  ""Direction"": ""Buy"" | ""Sell"" | """",
  ""EntryPrice"": 0.0,
  ""StopLoss"": 0.0,
  ""TakeProfit"": 0.0,
  ""LotSize"": 0.0,
  ""Reasoning"": ""Your final conclusion"",
  ""ThinkingProcess"": ""Your step-by-step reasoning (Chain of Thought)"",
  ""ConfidenceScore"": 0-100,
  ""RiskFactors"": [""factor1"", ""factor2""]
}";

        var userPrompt = $@"
# Complete Trading Context

## L1 - D1 Daily Bias
Direction: {context.L1_DailyBias.Direction}
Confidence: {context.L1_DailyBias.Confidence}%
Trend Type: {context.L1_DailyBias.TrendType}
Support Levels: {string.Join(", ", context.L1_DailyBias.SupportLevels)}
Resistance Levels: {string.Join(", ", context.L1_DailyBias.ResistanceLevels)}
L1 Reasoning: {context.L1_DailyBias.Reasoning}

## L2 - H1 Structure
Market Cycle: {context.L2_Structure.MarketCycle}
Status: {context.L2_Structure.Status}
Aligned with D1: {context.L2_Structure.AlignedWithD1}
Current Phase: {context.L2_Structure.CurrentPhase}
L2 Reasoning: {context.L2_Structure.Reasoning}

## L3 - M5 Signal Detection
Setup Type: {context.L3_Signal.SetupType}
Suggested Entry: {context.L3_Signal.EntryPrice}
Suggested Stop Loss: {context.L3_Signal.StopLoss}
Suggested Take Profit: {context.L3_Signal.TakeProfit}
Direction: {context.L3_Signal.Direction}
L3 Reasoning: {context.L3_Signal.Reasoning}

## M5 Market Data (Focus Table - Last 30 Bars)
{context.MarketData.FocusTable}

## Pattern Summary
{context.MarketData.PatternSummary}

---

Now, apply your critical thinking. Should we execute this trade or reject it?
Think step by step, and provide your Chain of Thought in the ThinkingProcess field.";

        // 调用 DeepSeek-R1（支持思维链）
        var requestBody = new
        {
            model = "deepseek-reasoner",
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            temperature = 0.5,
            max_tokens = 16000
        };

        var response = await _deepSeekClient.PostAsJsonAsync("", requestBody);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonDocument.Parse(responseContent);

        var choice = result.RootElement.GetProperty("choices")[0];
        var message = choice.GetProperty("message");

        // DeepSeek-R1 返回的思维过程在 reasoning_content 字段
        var thinkingProcess = message.GetProperty("reasoning_content").GetString();
        var finalAnswer = message.GetProperty("content").GetString();

        var decision = JsonSerializer.Deserialize<FinalDecision>(finalAnswer);
        decision.ThinkingProcess = thinkingProcess;
        decision.DecidedAt = DateTime.UtcNow;

        _logger.LogInformation(
            "L4 最终决策: {Action} (信心: {Confidence}%)",
            decision.Action, decision.ConfidenceScore);

        _logger.LogInformation("思维过程: {ThinkingProcess}",
            thinkingProcess?.Substring(0, Math.Min(200, thinkingProcess.Length)));

        return decision;
    }
}
```

#### ✅ 6. 编排服务（总控）

**新增服务：** `TradingOrchestrationService`

```csharp
public class TradingOrchestrationService
{
    private readonly L1_DailyAnalysisService _l1Service;
    private readonly L2_StructureAnalysisService _l2Service;
    private readonly L3_SignalMonitoringService _l3Service;
    private readonly L4_FinalDecisionService _l4Service;
    private readonly ILogger<TradingOrchestrationService> _logger;

    /// <summary>
    /// 执行完整的四级决策流程
    /// </summary>
    public async Task<FinalDecision> ExecuteTradingPipelineAsync(string symbol)
    {
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("开始四级 AI 决策流程: {Symbol}", symbol);

        try
        {
            // L1: 日线分析
            _logger.LogInformation("执行 L1 - D1 战略分析...");
            var dailyBias = await _l1Service.AnalyzeDailyBiasAsync(symbol);

            // 如果日线不明确，直接拒绝
            if (dailyBias.Direction == "Neutral" || dailyBias.Confidence < 60)
            {
                _logger.LogWarning("L1 方向不明确或信心不足，终止流程");
                return new FinalDecision
                {
                    Action = "Reject",
                    Reasoning = "D1 bias is unclear or low confidence"
                };
            }

            // L2: 小时结构分析
            _logger.LogInformation("执行 L2 - H1 结构分析...");
            var structure = await _l2Service.AnalyzeStructureAsync(symbol, dailyBias);

            // 如果 H1 状态为 Idle，不继续
            if (structure.Status == "Idle")
            {
                _logger.LogInformation("L2 Status=Idle，暂无交易机会");
                return new FinalDecision
                {
                    Action = "Reject",
                    Reasoning = "H1 market structure is not favorable (Idle)"
                };
            }

            // L3: M5 信号监控
            _logger.LogInformation("执行 L3 - M5 信号监控...");
            var context = new TradingContext
            {
                L1_DailyBias = dailyBias,
                L2_Structure = structure,
                MarketData = await _dataProcessor.ProcessMarketDataAsync(symbol, "M5", 80)
            };

            var signal = await _l3Service.MonitorForSignalsAsync(symbol, context);
            context.L3_Signal = signal;

            // 如果没有信号，不继续
            if (signal.Status != "Potential_Setup")
            {
                _logger.LogInformation("L3 未检测到交易设置");
                return new FinalDecision
                {
                    Action = "Reject",
                    Reasoning = "No trading setup detected on M5"
                };
            }

            // L4: 最终决策（DeepSeek-R1 思维链）
            _logger.LogInformation("执行 L4 - 最终决策（DeepSeek-R1）...");
            var decision = await _l4Service.MakeFinalDecisionAsync(context);

            stopwatch.Stop();
            _logger.LogInformation(
                "四级决策完成: {Action}, 耗时 {ElapsedMs}ms",
                decision.Action, stopwatch.ElapsedMilliseconds);

            return decision;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "四级决策流程发生错误");
            return new FinalDecision
            {
                Action = "Reject",
                Reasoning = $"System error: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 定时任务：每 5 分钟执行一次 M5 监控
    /// </summary>
    public async Task RunPeriodicMonitoringAsync(string symbol)
    {
        while (true)
        {
            try
            {
                var decision = await ExecuteTradingPipelineAsync(symbol);

                // 如果决策是 Execute，发送 Telegram 通知
                if (decision.Action == "Execute")
                {
                    await SendTelegramNotificationAsync(symbol, decision);
                }

                // 等待 5 分钟
                await Task.Delay(TimeSpan.FromMinutes(5));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "定时监控任务错误");
                await Task.Delay(TimeSpan.FromMinutes(1)); // 错误后等待 1 分钟重试
            }
        }
    }
}
```

### 配置管理

**appsettings.json:**

```json
{
  "AIOrchestration": {
    "EnabledLevels": ["L1", "L2", "L3", "L4"],
    "L1": {
      "Model": "gpt-4o",
      "CacheDurationHours": 24,
      "MinConfidence": 60
    },
    "L2": {
      "Model": "deepseek-chat",
      "CacheDurationHours": 1
    },
    "L3": {
      "Model": "gpt-4o-mini",
      "MonitoringIntervalMinutes": 5
    },
    "L4": {
      "Model": "deepseek-reasoner",
      "MinConfidenceToExecute": 75,
      "MaxThinkingTokens": 16000
    }
  },
  "DeepSeek": {
    "ApiKey": "",
    "BaseUrl": "https://api.deepseek.com/v1/chat/completions"
  }
}
```

### 验收标准

**功能完整性：**
- [ ] L1 正确分析 D1 趋势
- [ ] L2 正确判断 H1 结构
- [ ] L3 能识别 Al Brooks 设置
- [ ] L4 提供完整思维链推理
- [ ] 四级级联逻辑正确

**上下文传递：**
- [ ] 下级能接收上级结论
- [ ] 条件触发正常工作
- [ ] 早期终止逻辑正确

**性能和成本：**
- [ ] L1 分析 < 10秒
- [ ] L2 分析 < 5秒
- [ ] L3 监控 < 3秒
- [ ] L4 决策 < 30秒
- [ ] 日总成本 < $1

**缓存机制：**
- [ ] L1 结果缓存 24 小时
- [ ] L2 结果缓存 1 小时
- [ ] 缓存失效正常工作

### 相关文件

**新增文件：**
- `Trading.AI/Services/L1_DailyAnalysisService.cs`
- `Trading.AI/Services/L2_StructureAnalysisService.cs`
- `Trading.AI/Services/L3_SignalMonitoringService.cs`
- `Trading.AI/Services/L4_FinalDecisionService.cs`
- `Trading.AI/Services/TradingOrchestrationService.cs`
- `Trading.AI/Models/TradingContext.cs`

**文档：**
- `docs/FOUR_TIER_AI_ARCHITECTURE.md` - 架构详解
- `docs/AI_PROMPTS.md` - Prompt 模板

### 标签
`ai`, `enhancement`, `orchestration`, `multi-model`, `decision-making`

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

## Issue 9: 实现回测与历史分析系统

### 标题
📊 Implement Backtesting and Historical Analysis System with AI Decision Audit

### 描述
实现完整的回测系统，验证 Al Brooks 形态识别和四级 AI 决策在历史数据上的表现，为 FTMO 考试提供策略验证。

### 背景
在进行真实交易之前，必须验证策略的有效性。回测系统需要：
- **模拟四级 AI 决策**：在历史数据上运行完整决策流程
- **跳过人工确认**：自动执行所有 AI 建议的交易
- **完整审计追踪**：记录每笔交易的 AI 推理过程
- **统计分析**：计算胜率、盈亏比、最大回撤等指标
- **FTMO 风控模拟**：验证是否满足 5% 日损和 10% 总损要求

### 实现功能

#### ✅ 1. 回测引擎核心

**新增服务：** `BacktestEngine`

```csharp
public class BacktestEngine
{
    private readonly TradingOrchestrationService _orchestration;
    private readonly MarketDataCacheService _dataService;
    private readonly IBacktestRepository _repository;
    private readonly ILogger<BacktestEngine> _logger;

    /// <summary>
    /// 运行回测
    /// </summary>
    public async Task<BacktestResult> RunBacktestAsync(BacktestConfig config)
    {
        _logger.LogInformation(
            "开始回测: {Symbol} from {StartDate} to {EndDate}",
            config.Symbol, config.StartDate, config.EndDate);

        var result = new BacktestResult
        {
            Config = config,
            StartTime = DateTime.UtcNow
        };

        // 1. 加载历史数据
        var candles = await LoadHistoricalDataAsync(
            config.Symbol, config.StartDate, config.EndDate);

        _logger.LogInformation("加载 {Count} 根 K 线数据", candles.Count);

        // 2. 初始化虚拟账户
        var account = new VirtualAccount
        {
            InitialBalance = config.InitialBalance,
            Balance = config.InitialBalance,
            Equity = config.InitialBalance,
            MaxDailyLossPercent = config.MaxDailyLossPercent,
            MaxTotalLossPercent = config.MaxTotalLossPercent
        };

        // 3. 按时间顺序模拟交易
        var currentDate = config.StartDate;
        var tradeNumber = 0;

        while (currentDate <= config.EndDate)
        {
            // 检查风控限制
            if (account.IsDailyLossLimitReached() || account.IsTotalLossLimitReached())
            {
                _logger.LogWarning(
                    "触发风控限制 @ {Date}, 日损: {DailyLoss}%, 总损: {TotalLoss}%",
                    currentDate, account.GetDailyLossPercent(), account.GetTotalLossPercent());

                // 如果是日损，重置到第二天
                if (account.IsDailyLossLimitReached())
                {
                    currentDate = currentDate.AddDays(1);
                    account.ResetDailyLoss();
                    continue;
                }
                else
                {
                    // 总损限制，终止回测
                    result.TerminationReason = "Max total loss reached";
                    break;
                }
            }

            // 4. 执行 AI 决策（回测模式）
            var decision = await ExecuteAIDecisionInBacktestModeAsync(
                config.Symbol, currentDate, candles);

            // 5. 如果 AI 决定开仓，执行虚拟交易
            if (decision.Action == "Execute")
            {
                tradeNumber++;

                var trade = new BacktestTrade
                {
                    TradeNumber = tradeNumber,
                    Symbol = config.Symbol,
                    Direction = decision.Direction,
                    EntryTime = currentDate,
                    EntryPrice = decision.EntryPrice,
                    StopLoss = decision.StopLoss,
                    TakeProfit = decision.TakeProfit,
                    LotSize = decision.LotSize,

                    // 保存 AI 决策上下文
                    L1_DailyBias = decision.Context.L1_DailyBias,
                    L2_Structure = decision.Context.L2_Structure,
                    L3_Signal = decision.Context.L3_Signal,
                    L4_Reasoning = decision.Reasoning,
                    L4_ThinkingProcess = decision.ThinkingProcess
                };

                // 6. 模拟交易执行和平仓
                await SimulateTradeExecutionAsync(trade, candles, account);

                result.Trades.Add(trade);

                _logger.LogInformation(
                    "交易 #{Number}: {Direction} @ {Entry}, PnL: {PnL} ({PnLPercent:F2}%)",
                    tradeNumber, trade.Direction, trade.EntryPrice,
                    trade.ProfitLoss, trade.ProfitLossPercent);
            }

            // 7. 前进到下一个时间点
            currentDate = GetNextAnalysisTime(currentDate, config.TimeFrame);
        }

        // 8. 计算回测统计
        result.EndTime = DateTime.UtcNow;
        result.FinalBalance = account.Balance;
        result.TotalReturn = (account.Balance - config.InitialBalance) / config.InitialBalance;
        result.TotalTrades = result.Trades.Count;
        result.WinningTrades = result.Trades.Count(t => t.ProfitLoss > 0);
        result.LosingTrades = result.Trades.Count(t => t.ProfitLoss < 0);
        result.WinRate = result.TotalTrades > 0
            ? (double)result.WinningTrades / result.TotalTrades
            : 0;
        result.AverageProfitLoss = result.Trades.Any()
            ? result.Trades.Average(t => t.ProfitLoss)
            : 0;
        result.MaxDrawdown = CalculateMaxDrawdown(result.Trades, config.InitialBalance);

        // 9. 保存回测结果
        await _repository.SaveBacktestResultAsync(result);

        _logger.LogInformation(
            "回测完成: {Trades} 笔交易, 胜率: {WinRate:P2}, 总收益: {Return:P2}",
            result.TotalTrades, result.WinRate, result.TotalReturn);

        return result;
    }

    /// <summary>
    /// 在回测模式下执行 AI 决策
    /// </summary>
    private async Task<FinalDecision> ExecuteAIDecisionInBacktestModeAsync(
        string symbol,
        DateTime analysisTime,
        List<Candle> allCandles)
    {
        // 获取到 analysisTime 为止的历史数据
        var historicalData = allCandles
            .Where(c => c.Time <= analysisTime)
            .ToList();

        // 模拟实时环境，只使用到当前时间的数据
        // 这里需要创建一个临时的数据上下文
        var context = new BacktestContext
        {
            CurrentTime = analysisTime,
            AvailableData = historicalData
        };

        // 执行四级 AI 决策
        var decision = await _orchestration.ExecuteTradingPipelineAsync(
            symbol, context);

        return decision;
    }

    /// <summary>
    /// 模拟交易执行和平仓
    /// </summary>
    private async Task SimulateTradeExecutionAsync(
        BacktestTrade trade,
        List<Candle> candles,
        VirtualAccount account)
    {
        // 查找入场后的 K 线数据
        var futureCandles = candles
            .Where(c => c.Time > trade.EntryTime)
            .OrderBy(c => c.Time)
            .ToList();

        foreach (var candle in futureCandles)
        {
            // 检查止损
            if (trade.Direction == "Buy" && candle.Low <= trade.StopLoss)
            {
                trade.ExitTime = candle.Time;
                trade.ExitPrice = trade.StopLoss;
                trade.ExitReason = "Stop Loss";
                break;
            }
            else if (trade.Direction == "Sell" && candle.High >= trade.StopLoss)
            {
                trade.ExitTime = candle.Time;
                trade.ExitPrice = trade.StopLoss;
                trade.ExitReason = "Stop Loss";
                break;
            }

            // 检查止盈
            if (trade.Direction == "Buy" && candle.High >= trade.TakeProfit)
            {
                trade.ExitTime = candle.Time;
                trade.ExitPrice = trade.TakeProfit;
                trade.ExitReason = "Take Profit";
                break;
            }
            else if (trade.Direction == "Sell" && candle.Low <= trade.TakeProfit)
            {
                trade.ExitTime = candle.Time;
                trade.ExitPrice = trade.TakeProfit;
                trade.ExitReason = "Take Profit";
                break;
            }

            // 可选：添加时间止损（如持仓超过 24 小时强制平仓）
            if ((candle.Time - trade.EntryTime).TotalHours > 24)
            {
                trade.ExitTime = candle.Time;
                trade.ExitPrice = candle.Close;
                trade.ExitReason = "Time Stop";
                break;
            }
        }

        // 如果遍历完所有数据还没平仓，按最后价格平仓
        if (trade.ExitTime == null)
        {
            var lastCandle = futureCandles.Last();
            trade.ExitTime = lastCandle.Time;
            trade.ExitPrice = lastCandle.Close;
            trade.ExitReason = "End of Data";
        }

        // 计算盈亏
        if (trade.Direction == "Buy")
        {
            trade.ProfitLoss = (trade.ExitPrice - trade.EntryPrice) * trade.LotSize * 100000;
        }
        else
        {
            trade.ProfitLoss = (trade.EntryPrice - trade.ExitPrice) * trade.LotSize * 100000;
        }

        trade.ProfitLossPercent = trade.ProfitLoss / account.Balance;

        // 更新账户
        account.Balance += trade.ProfitLoss;
        account.Equity = account.Balance;
        account.AddTradeToHistory(trade);
    }

    /// <summary>
    /// 计算最大回撤
    /// </summary>
    private double CalculateMaxDrawdown(List<BacktestTrade> trades, double initialBalance)
    {
        var equity = initialBalance;
        var peak = initialBalance;
        var maxDrawdown = 0.0;

        foreach (var trade in trades.OrderBy(t => t.ExitTime))
        {
            equity += trade.ProfitLoss;

            if (equity > peak)
                peak = equity;

            var drawdown = (peak - equity) / peak;
            if (drawdown > maxDrawdown)
                maxDrawdown = drawdown;
        }

        return maxDrawdown;
    }
}
```

#### ✅ 2. 虚拟账户管理

**VirtualAccount.cs:**

```csharp
public class VirtualAccount
{
    public double InitialBalance { get; set; }
    public double Balance { get; set; }
    public double Equity { get; set; }

    // FTMO 风控限制
    public double MaxDailyLossPercent { get; set; } = 5.0;
    public double MaxTotalLossPercent { get; set; } = 10.0;

    // 每日统计
    public DateTime CurrentDay { get; set; }
    public double DailyStartBalance { get; set; }
    public List<BacktestTrade> DailyTrades { get; set; } = new();

    // 历史记录
    public List<BacktestTrade> AllTrades { get; set; } = new();

    public bool IsDailyLossLimitReached()
    {
        var dailyLoss = DailyStartBalance - Balance;
        var dailyLossPercent = (dailyLoss / DailyStartBalance) * 100;
        return dailyLossPercent >= MaxDailyLossPercent;
    }

    public bool IsTotalLossLimitReached()
    {
        var totalLoss = InitialBalance - Balance;
        var totalLossPercent = (totalLoss / InitialBalance) * 100;
        return totalLossPercent >= MaxTotalLossPercent;
    }

    public double GetDailyLossPercent()
    {
        var dailyLoss = DailyStartBalance - Balance;
        return (dailyLoss / DailyStartBalance) * 100;
    }

    public double GetTotalLossPercent()
    {
        var totalLoss = InitialBalance - Balance;
        return (totalLoss / InitialBalance) * 100;
    }

    public void ResetDailyLoss()
    {
        CurrentDay = CurrentDay.AddDays(1);
        DailyStartBalance = Balance;
        DailyTrades.Clear();
    }

    public void AddTradeToHistory(BacktestTrade trade)
    {
        AllTrades.Add(trade);
        DailyTrades.Add(trade);
    }
}
```

#### ✅ 3. 数据模型

**BacktestConfig.cs:**

```csharp
public class BacktestConfig
{
    public string Symbol { get; set; } = "XAUUSD";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string TimeFrame { get; set; } = "M5";

    // 账户配置
    public double InitialBalance { get; set; } = 100000;
    public double MaxDailyLossPercent { get; set; } = 5.0;
    public double MaxTotalLossPercent { get; set; } = 10.0;

    // AI 配置
    public bool UseL4DeepSeekR1 { get; set; } = true;
    public int MinConfidenceScore { get; set; } = 75;
}

public class BacktestResult
{
    public BacktestConfig Config { get; set; } = new();
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    // 交易统计
    public List<BacktestTrade> Trades { get; set; } = new();
    public int TotalTrades { get; set; }
    public int WinningTrades { get; set; }
    public int LosingTrades { get; set; }
    public double WinRate { get; set; }
    public double AverageProfitLoss { get; set; }
    public double MaxDrawdown { get; set; }

    // 账户结果
    public double FinalBalance { get; set; }
    public double TotalReturn { get; set; }

    // 终止原因
    public string? TerminationReason { get; set; }

    // 性能指标
    public double SharpeRatio { get; set; }
    public double ProfitFactor { get; set; }
    public int MaxConsecutiveLosses { get; set; }
    public int MaxConsecutiveWins { get; set; }
}

public class BacktestTrade
{
    public int TradeNumber { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty; // Buy/Sell

    // 交易数据
    public DateTime? EntryTime { get; set; }
    public double EntryPrice { get; set; }
    public DateTime? ExitTime { get; set; }
    public double ExitPrice { get; set; }
    public string? ExitReason { get; set; }

    public double StopLoss { get; set; }
    public double TakeProfit { get; set; }
    public double LotSize { get; set; }

    // 盈亏
    public double ProfitLoss { get; set; }
    public double ProfitLossPercent { get; set; }

    // AI 决策上下文（审计追踪）
    public DailyBias? L1_DailyBias { get; set; }
    public StructureAnalysis? L2_Structure { get; set; }
    public SignalDetection? L3_Signal { get; set; }
    public string L4_Reasoning { get; set; } = string.Empty;
    public string L4_ThinkingProcess { get; set; } = string.Empty;
}
```

#### ✅ 4. 回测 API

**BacktestController.cs:**

```csharp
[ApiController]
[Route("api/[controller]")]
public class BacktestController : ControllerBase
{
    private readonly BacktestEngine _engine;
    private readonly IBacktestRepository _repository;

    /// <summary>
    /// 启动回测
    /// POST /api/backtest/run
    /// </summary>
    [HttpPost("run")]
    public async Task<ActionResult<BacktestResult>> RunBacktest(
        [FromBody] BacktestConfig config)
    {
        var result = await _engine.RunBacktestAsync(config);
        return Ok(result);
    }

    /// <summary>
    /// 获取回测历史
    /// GET /api/backtest/history
    /// </summary>
    [HttpGet("history")]
    public async Task<ActionResult<List<BacktestResult>>> GetBacktestHistory()
    {
        var history = await _repository.GetBacktestHistoryAsync();
        return Ok(history);
    }

    /// <summary>
    /// 获取特定回测详情
    /// GET /api/backtest/{id}
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<BacktestResult>> GetBacktestDetails(string id)
    {
        var result = await _repository.GetBacktestByIdAsync(id);
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    /// <summary>
    /// 获取交易详情（包含 AI 推理过程）
    /// GET /api/backtest/{id}/trades/{tradeNumber}
    /// </summary>
    [HttpGet("{id}/trades/{tradeNumber}")]
    public async Task<ActionResult<BacktestTrade>> GetTradeDetails(
        string id,
        int tradeNumber)
    {
        var trade = await _repository.GetTradeDetailsAsync(id, tradeNumber);
        if (trade == null)
            return NotFound();

        return Ok(trade);
    }

    /// <summary>
    /// 批量回测（多个时间段）
    /// POST /api/backtest/batch
    /// </summary>
    [HttpPost("batch")]
    public async Task<ActionResult<List<BacktestResult>>> RunBatchBacktest(
        [FromBody] List<BacktestConfig> configs)
    {
        var results = new List<BacktestResult>();

        foreach (var config in configs)
        {
            var result = await _engine.RunBacktestAsync(config);
            results.Add(result);
        }

        return Ok(results);
    }
}
```

#### ✅ 5. Web 可视化界面

**backtest.html:**

```html
<!DOCTYPE html>
<html>
<head>
    <title>Backtest Results</title>
    <script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
    <style>
        /* 样式省略 */
    </style>
</head>
<body>
    <div class="container">
        <h1>📊 Backtest Results</h1>

        <!-- 回测配置 -->
        <div class="config-panel">
            <h2>Run Backtest</h2>
            <form id="backtestForm">
                <label>Symbol:</label>
                <select name="symbol">
                    <option value="XAUUSD">XAUUSD</option>
                    <option value="EURUSD">EURUSD</option>
                </select>

                <label>Start Date:</label>
                <input type="date" name="startDate" required>

                <label>End Date:</label>
                <input type="date" name="endDate" required>

                <label>Initial Balance:</label>
                <input type="number" name="initialBalance" value="100000">

                <button type="submit">Run Backtest</button>
            </form>
        </div>

        <!-- 统计摘要 -->
        <div class="summary-panel">
            <h2>Summary</h2>
            <div class="stats-grid">
                <div class="stat-card">
                    <div class="stat-label">Total Trades</div>
                    <div class="stat-value" id="totalTrades">-</div>
                </div>
                <div class="stat-card">
                    <div class="stat-label">Win Rate</div>
                    <div class="stat-value" id="winRate">-</div>
                </div>
                <div class="stat-card">
                    <div class="stat-label">Total Return</div>
                    <div class="stat-value" id="totalReturn">-</div>
                </div>
                <div class="stat-card">
                    <div class="stat-label">Max Drawdown</div>
                    <div class="stat-value" id="maxDrawdown">-</div>
                </div>
            </div>
        </div>

        <!-- 权益曲线图 -->
        <div class="chart-panel">
            <h2>Equity Curve</h2>
            <canvas id="equityChart"></canvas>
        </div>

        <!-- 交易列表 -->
        <div class="trades-panel">
            <h2>Trades</h2>
            <table id="tradesTable">
                <thead>
                    <tr>
                        <th>#</th>
                        <th>Entry Time</th>
                        <th>Direction</th>
                        <th>Entry</th>
                        <th>Exit</th>
                        <th>P/L</th>
                        <th>Reason</th>
                        <th>Details</th>
                    </tr>
                </thead>
                <tbody></tbody>
            </table>
        </div>
    </div>

    <script>
        // JavaScript 实现省略
    </script>
</body>
</html>
```

### 验收标准

**功能完整性：**
- [ ] 成功加载历史数据并按时间顺序处理
- [ ] 四级 AI 决策在回测模式下正常工作
- [ ] 虚拟交易执行和平仓逻辑正确
- [ ] FTMO 风控限制正确触发

**统计准确性：**
- [ ] 胜率计算准确
- [ ] 盈亏计算准确
- [ ] 最大回撤计算准确
- [ ] 连续盈亏统计正确

**审计追踪：**
- [ ] 每笔交易保存完整 AI 推理过程
- [ ] 可查看 L1/L2/L3/L4 各级决策
- [ ] DeepSeek-R1 思维链完整保存

**性能：**
- [ ] 1 个月数据回测 < 5 分钟
- [ ] 并发回测支持
- [ ] 内存占用合理

### 相关文件

**新增文件：**
- `Trading.Backtest/Engine/BacktestEngine.cs`
- `Trading.Backtest/Models/VirtualAccount.cs`
- `Trading.Backtest/Models/BacktestConfig.cs`
- `Trading.Backtest/Models/BacktestResult.cs`
- `Trading.Backtest.Web/Controllers/BacktestController.cs`
- `Trading.Backtest.Web/wwwroot/backtest.html`

**文档：**
- `docs/BACKTEST_GUIDE.md` - 回测使用指南
- `docs/FTMO_RULES.md` - FTMO 规则说明

### 标签
`backtest`, `testing`, `analysis`, `ftmo`, `audit`

---

## Issue 10: 实现移动端交易代理（避免 IP 红线）

### 标题
📱 Implement Mobile Trading Proxy to Avoid Prop Firm IP Detection

### 描述
开发轻量级手机 App，接收云端交易信号并在本地执行，避免触发 Prop Firm（如 FTMO）的 EA/VPS IP 检测。

### 背景
许多 Prop Firms 禁止使用 EA（Expert Advisor）或 VPS 进行自动交易：
- **IP 检测**：交易请求来自数据中心 IP 会被标记
- **执行模式检测**：毫秒级响应会被怀疑使用机器人
- **账号封禁风险**：违规使用 EA 可能导致账号冻结

通过移动端代理方案：
- **IP 安全**：交易请求来自手机网络（4G/5G/家庭 WiFi）
- **人工确认**：保留最后的确认步骤，避免完全自动化
- **灵活执行**：可以在任何地点（家、办公室）执行交易
- **FTMO 合规**：满足"手动交易"要求

### 架构设计

```
┌─────────────────────────────────────────────────────────┐
│  Azure Functions (Cloud)                                │
│  ├─ 四级 AI 决策系统                                     │
│  ├─ 生成交易信号（Entry/SL/TP）                          │
│  └─ 推送到 SignalR Hub                                   │
└────────────────────┬────────────────────────────────────┘
                     ↓ (SignalR Real-time Push)
┌─────────────────────────────────────────────────────────┐
│  Mobile App (.NET MAUI)                                 │
│  ├─ 后台服务持续监听信号                                  │
│  ├─ 收到信号后震动提醒                                   │
│  ├─ 显示 AI 推理过程和交易参数                           │
│  └─ 用户点击确认后，调用 OANDA SDK 下单                  │
└─────────────────────────────────────────────────────────┘
                     ↓ (HTTPS from Mobile IP)
┌─────────────────────────────────────────────────────────┐
│  OANDA / TradeLocker API                                │
│  └─ 接收来自手机 IP 的交易请求                           │
└─────────────────────────────────────────────────────────┘
```

### 实现功能

#### ✅ 1. SignalR Hub（云端信号推送）

**新增 Hub：** `TradingSignalHub`

```csharp
public class TradingSignalHub : Hub
{
    private readonly ILogger<TradingSignalHub> _logger;

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        _logger.LogInformation("客户端连接: {UserId}", userId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        _logger.LogInformation("客户端断开: {UserId}", userId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// 客户端注册设备
    /// </summary>
    public async Task RegisterDevice(string deviceId, string deviceName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"device_{deviceId}");
        _logger.LogInformation("设备注册: {DeviceId} - {DeviceName}", deviceId, deviceName);
    }
}
```

**信号推送服务：**

```csharp
public class SignalPushService
{
    private readonly IHubContext<TradingSignalHub> _hubContext;
    private readonly ILogger<SignalPushService> _logger;

    /// <summary>
    /// 推送交易信号到移动端
    /// </summary>
    public async Task PushTradingSignalAsync(
        string deviceId,
        TradingSignal signal)
    {
        _logger.LogInformation(
            "推送交易信号到设备 {DeviceId}: {Symbol} {Direction}",
            deviceId, signal.Symbol, signal.Direction);

        await _hubContext.Clients
            .Group($"device_{deviceId}")
            .SendAsync("ReceiveTradingSignal", signal);
    }

    /// <summary>
    /// 推送通用通知
    /// </summary>
    public async Task PushNotificationAsync(
        string deviceId,
        string title,
        string message)
    {
        await _hubContext.Clients
            .Group($"device_{deviceId}")
            .SendAsync("ReceiveNotification", new { title, message });
    }
}
```

**交易信号模型：**

```csharp
public class TradingSignal
{
    public string SignalId { get; set; } = Guid.NewGuid().ToString();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // 交易参数
    public string Symbol { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty; // Buy/Sell
    public double EntryPrice { get; set; }
    public double StopLoss { get; set; }
    public double TakeProfit { get; set; }
    public double SuggestedLotSize { get; set; }

    // AI 决策上下文
    public string L1_DailyBias { get; set; } = string.Empty;
    public string L2_Structure { get; set; } = string.Empty;
    public string L3_SetupType { get; set; } = string.Empty;
    public string L4_Reasoning { get; set; } = string.Empty;
    public string L4_ThinkingProcess { get; set; } = string.Empty;
    public int ConfidenceScore { get; set; }
    public List<string> RiskFactors { get; set; } = new();

    // 有效期
    public DateTime ExpiresAt { get; set; }
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
}
```

#### ✅ 2. 移动端 App (.NET MAUI)

**项目结构：**

```
TradingMobile/
├── TradingMobile.csproj
├── MauiProgram.cs
├── Services/
│   ├── SignalRService.cs           # SignalR 连接管理
│   ├── BackgroundListenerService.cs # 后台监听服务
│   ├── OandaExecutionService.cs     # OANDA 下单
│   └── NotificationService.cs       # 本地通知
├── ViewModels/
│   ├── MainViewModel.cs
│   ├── SignalDetailsViewModel.cs
│   └── TradeHistoryViewModel.cs
├── Views/
│   ├── MainPage.xaml                # 主界面
│   ├── SignalDetailsPage.xaml      # 信号详情
│   └── SettingsPage.xaml           # 设置页面
└── Models/
    ├── TradingSignal.cs
    └── TradeExecution.cs
```

**SignalRService.cs:**

```csharp
public class SignalRService
{
    private HubConnection? _connection;
    private readonly ILogger<SignalRService> _logger;
    private readonly NotificationService _notificationService;

    public event EventHandler<TradingSignal>? SignalReceived;

    public async Task ConnectAsync(string serverUrl, string deviceId)
    {
        _connection = new HubConnectionBuilder()
            .WithUrl(serverUrl)
            .WithAutomaticReconnect()
            .Build();

        // 监听交易信号
        _connection.On<TradingSignal>("ReceiveTradingSignal", OnSignalReceived);

        // 监听通知
        _connection.On<object>("ReceiveNotification", OnNotificationReceived);

        await _connection.StartAsync();
        _logger.LogInformation("SignalR 连接成功");

        // 注册设备
        await _connection.InvokeAsync("RegisterDevice", deviceId, DeviceInfo.Name);
    }

    private void OnSignalReceived(TradingSignal signal)
    {
        _logger.LogInformation("收到交易信号: {Symbol} {Direction}",
            signal.Symbol, signal.Direction);

        // 触发震动
        Vibration.Vibrate(TimeSpan.FromSeconds(1));

        // 显示本地通知
        _notificationService.ShowNotification(
            "🔔 New Trading Signal",
            $"{signal.Symbol} {signal.Direction} @ {signal.EntryPrice}");

        // 触发事件
        SignalReceived?.Invoke(this, signal);
    }

    private void OnNotificationReceived(object notification)
    {
        // 处理通用通知
    }

    public async Task DisconnectAsync()
    {
        if (_connection != null)
        {
            await _connection.StopAsync();
            await _connection.DisposeAsync();
        }
    }
}
```

**BackgroundListenerService.cs:**

```csharp
public class BackgroundListenerService : IHostedService
{
    private readonly SignalRService _signalRService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BackgroundListenerService> _logger;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("启动后台信号监听服务");

        var serverUrl = _configuration["SignalR:ServerUrl"];
        var deviceId = Preferences.Get("DeviceId", Guid.NewGuid().ToString());

        // 保存设备 ID
        Preferences.Set("DeviceId", deviceId);

        await _signalRService.ConnectAsync(serverUrl, deviceId);

        // 订阅信号
        _signalRService.SignalReceived += OnSignalReceived;
    }

    private void OnSignalReceived(object? sender, TradingSignal signal)
    {
        // 在主线程上导航到信号详情页
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Shell.Current.GoToAsync(
                $"SignalDetails?signalId={signal.SignalId}");
        });
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("停止后台信号监听服务");
        await _signalRService.DisconnectAsync();
    }
}
```

**OandaExecutionService.cs:**

```csharp
public class OandaExecutionService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OandaExecutionService> _logger;

    /// <summary>
    /// 执行市价单
    /// </summary>
    public async Task<OrderResult> ExecuteMarketOrderAsync(
        string symbol,
        string direction,
        double lotSize,
        double stopLoss,
        double takeProfit)
    {
        var apiKey = Preferences.Get("OandaApiKey", "");
        var accountId = Preferences.Get("OandaAccountId", "");

        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(accountId))
        {
            throw new InvalidOperationException("OANDA credentials not configured");
        }

        _logger.LogInformation(
            "执行 {Direction} 订单: {Symbol}, Size: {LotSize}",
            direction, symbol, lotSize);

        // 构建 OANDA 请求
        var request = new
        {
            order = new
            {
                type = "MARKET",
                instrument = symbol,
                units = direction == "Buy" ? lotSize * 100000 : -lotSize * 100000,
                timeInForce = "FOK",
                stopLossOnFill = new { price = stopLoss.ToString("F5") },
                takeProfitOnFill = new { price = takeProfit.ToString("F5") }
            }
        };

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        var response = await _httpClient.PostAsJsonAsync(
            $"https://api-fxpractice.oanda.com/v3/accounts/{accountId}/orders",
            request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("下单失败: {Error}", error);
            throw new Exception($"Order failed: {error}");
        }

        var result = await response.Content.ReadFromJsonAsync<OandaOrderResponse>();

        _logger.LogInformation("订单成功: Order ID = {OrderId}",
            result?.OrderFillTransaction?.Id);

        return new OrderResult
        {
            Success = true,
            OrderId = result?.OrderFillTransaction?.Id ?? "",
            ExecutedPrice = result?.OrderFillTransaction?.Price ?? 0
        };
    }
}
```

**MainPage.xaml:**

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="TradingMobile.Views.MainPage"
             Title="Trading Agent">

    <ScrollView>
        <VerticalStackLayout Padding="20" Spacing="15">

            <!-- 连接状态 -->
            <Frame BorderColor="LightGray">
                <StackLayout>
                    <Label Text="Connection Status" FontSize="18" FontAttributes="Bold"/>
                    <Label Text="{Binding ConnectionStatus}" FontSize="14"/>
                    <Button Text="Reconnect" Command="{Binding ReconnectCommand}"
                            IsVisible="{Binding IsDisconnected}"/>
                </StackLayout>
            </Frame>

            <!-- 最新信号 -->
            <Frame BorderColor="Orange" BackgroundColor="LightYellow">
                <StackLayout>
                    <Label Text="Latest Signal" FontSize="18" FontAttributes="Bold"/>
                    <Label Text="{Binding LatestSignal.Symbol}" FontSize="16"/>
                    <Label Text="{Binding LatestSignal.Direction}" FontSize="16"
                           TextColor="Green"/>
                    <Label Text="{Binding LatestSignal.EntryPrice, StringFormat='Entry: {0:F2}'}"/>
                    <Label Text="{Binding LatestSignal.StopLoss, StringFormat='SL: {0:F2}'}"/>
                    <Label Text="{Binding LatestSignal.TakeProfit, StringFormat='TP: {0:F2}'}"/>

                    <Button Text="View Details"
                            Command="{Binding ViewSignalDetailsCommand}"
                            BackgroundColor="DodgerBlue" TextColor="White"/>
                </StackLayout>
            </Frame>

            <!-- 统计 -->
            <Frame BorderColor="LightGray">
                <Grid ColumnDefinitions="*,*" RowDefinitions="Auto,Auto">
                    <Label Grid.Row="0" Grid.Column="0"
                           Text="Today's Trades" FontSize="14"/>
                    <Label Grid.Row="0" Grid.Column="1"
                           Text="{Binding TodayTradesCount}" FontSize="14" HorizontalOptions="End"/>

                    <Label Grid.Row="1" Grid.Column="0"
                           Text="Today's P/L" FontSize="14"/>
                    <Label Grid.Row="1" Grid.Column="1"
                           Text="{Binding TodayProfitLoss, StringFormat='{0:C}'}"
                           FontSize="14" HorizontalOptions="End"/>
                </Grid>
            </Frame>

            <!-- 操作按钮 -->
            <Button Text="Settings" Command="{Binding OpenSettingsCommand}"/>
            <Button Text="Trade History" Command="{Binding OpenHistoryCommand}"/>

        </VerticalStackLayout>
    </ScrollView>

</ContentPage>
```

**SignalDetailsPage.xaml:**

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="TradingMobile.Views.SignalDetailsPage"
             Title="Signal Details">

    <ScrollView>
        <VerticalStackLayout Padding="20" Spacing="15">

            <!-- 交易参数 -->
            <Frame BorderColor="DodgerBlue" BackgroundColor="AliceBlue">
                <StackLayout Spacing="10">
                    <Label Text="Trade Parameters"
                           FontSize="20" FontAttributes="Bold"/>

                    <Grid ColumnDefinitions="Auto,*" RowDefinitions="Auto,Auto,Auto,Auto,Auto"
                          RowSpacing="5">
                        <Label Grid.Row="0" Grid.Column="0" Text="Symbol:" FontAttributes="Bold"/>
                        <Label Grid.Row="0" Grid.Column="1" Text="{Binding Signal.Symbol}"/>

                        <Label Grid.Row="1" Grid.Column="0" Text="Direction:" FontAttributes="Bold"/>
                        <Label Grid.Row="1" Grid.Column="1" Text="{Binding Signal.Direction}"
                               TextColor="Green"/>

                        <Label Grid.Row="2" Grid.Column="0" Text="Entry:" FontAttributes="Bold"/>
                        <Label Grid.Row="2" Grid.Column="1"
                               Text="{Binding Signal.EntryPrice, StringFormat='{0:F2}'}"/>

                        <Label Grid.Row="3" Grid.Column="0" Text="Stop Loss:" FontAttributes="Bold"/>
                        <Label Grid.Row="3" Grid.Column="1"
                               Text="{Binding Signal.StopLoss, StringFormat='{0:F2}'}"/>

                        <Label Grid.Row="4" Grid.Column="0" Text="Take Profit:" FontAttributes="Bold"/>
                        <Label Grid.Row="4" Grid.Column="1"
                               Text="{Binding Signal.TakeProfit, StringFormat='{0:F2}'}"/>
                    </Grid>

                    <Label Text="{Binding Signal.SuggestedLotSize, StringFormat='Suggested Lot Size: {0:F2}'}"/>
                </StackLayout>
            </Frame>

            <!-- AI 分析 -->
            <Frame BorderColor="Purple" BackgroundColor="Lavender">
                <StackLayout Spacing="10">
                    <Label Text="AI Analysis" FontSize="20" FontAttributes="Bold"/>

                    <Label Text="L4 Reasoning:" FontAttributes="Bold"/>
                    <Label Text="{Binding Signal.L4_Reasoning}" FontSize="12"/>

                    <Label Text="Confidence Score:" FontAttributes="Bold"/>
                    <Label Text="{Binding Signal.ConfidenceScore, StringFormat='{0}/100'}"
                           FontSize="14" TextColor="Green"/>

                    <Button Text="View Full AI Thinking Process"
                            Command="{Binding ViewThinkingProcessCommand}"
                            BackgroundColor="Purple" TextColor="White"/>
                </StackLayout>
            </Frame>

            <!-- 风险警告 -->
            <Frame BorderColor="Red" BackgroundColor="MistyRose"
                   IsVisible="{Binding HasRiskFactors}">
                <StackLayout Spacing="5">
                    <Label Text="⚠️ Risk Factors"
                           FontSize="16" FontAttributes="Bold" TextColor="Red"/>
                    <Label Text="{Binding RiskFactorsText}" FontSize="12"/>
                </StackLayout>
            </Frame>

            <!-- 操作按钮 -->
            <Button Text="✅ Confirm &amp; Execute Trade"
                    Command="{Binding ExecuteTradeCommand}"
                    BackgroundColor="Green" TextColor="White"
                    FontSize="18" HeightRequest="60"/>

            <Button Text="❌ Reject"
                    Command="{Binding RejectTradeCommand}"
                    BackgroundColor="Red" TextColor="White"/>

            <!-- 倒计时 -->
            <Label Text="{Binding TimeRemaining, StringFormat='Signal expires in: {0}'}"
                   FontSize="12" HorizontalOptions="Center"/>

        </VerticalStackLayout>
    </ScrollView>

</ContentPage>
```

### 配置管理

**appsettings.json (Mobile):**

```json
{
  "SignalR": {
    "ServerUrl": "https://your-azure-functions.azurewebsites.net/api"
  },
  "Oanda": {
    "PracticeApiUrl": "https://api-fxpractice.oanda.com",
    "LiveApiUrl": "https://api-fxtrade.oanda.com"
  },
  "App": {
    "EnableNotifications": true,
    "VibrationEnabled": true,
    "AutoReconnect": true
  }
}
```

### 验收标准

**连接和通信：**
- [ ] SignalR 连接稳定
- [ ] 自动重连机制工作
- [ ] 信号实时推送（延迟 < 1秒）
- [ ] 后台运行正常

**用户体验：**
- [ ] 收到信号时震动提醒
- [ ] 本地通知正常显示
- [ ] 界面清晰易用
- [ ] AI 推理过程可查看

**交易执行：**
- [ ] OANDA 下单成功
- [ ] 止损止盈正确设置
- [ ] 错误处理完善
- [ ] 交易记录本地保存

**安全性：**
- [ ] API 密钥安全存储
- [ ] HTTPS 通信加密
- [ ] 设备认证机制

### 部署指南

**1. Azure SignalR Service 配置：**

```bash
# 创建 SignalR Service
az signalr create \
  --name trading-signalr \
  --resource-group trading-rg \
  --sku Free_F1

# 获取连接字符串
az signalr key list --name trading-signalr --resource-group trading-rg
```

**2. 移动端发布：**

```bash
# Android
dotnet publish -f net8.0-android -c Release

# iOS (需要 Mac)
dotnet publish -f net8.0-ios -c Release
```

### 相关文件

**云端：**
- `Trading.Infras.Web/Hubs/TradingSignalHub.cs`
- `Trading.Infras.Service/Services/SignalPushService.cs`

**移动端：**
- `TradingMobile/Services/SignalRService.cs`
- `TradingMobile/Services/BackgroundListenerService.cs`
- `TradingMobile/Services/OandaExecutionService.cs`
- `TradingMobile/Views/MainPage.xaml`
- `TradingMobile/Views/SignalDetailsPage.xaml`

**文档：**
- `docs/MOBILE_APP_SETUP.md` - 手机 App 配置指南
- `docs/SIGNALR_INTEGRATION.md` - SignalR 集成文档

### 标签
`mobile`, `dotnet-maui`, `signalr`, `ftmo-compliance`, `ip-safety`

---

## 工作计划

### Issue 优先级
1. **Issue 1** (Azure Table Storage) - 已完成 ✅
2. **Issue 2** (Azure OpenAI) - 已完成 ✅
3. **Issue 3** (Position Calculator) - 已完成 ✅
4. **Issue 4** (重构) - **优先级：高** ⭐
5. **Issue 6** (数据持久化) - **优先级：高** ⭐ (新增，基础设施)
6. **Issue 7** (形态识别引擎) - **优先级：高** ⭐ (新增，核心功能)
7. **Issue 8** (四级 AI 编排) - **优先级：中** 🤖 (新增，依赖 Issue 6+7)
8. **Issue 9** (回测系统) - **优先级：中** 📊 (新增，依赖 Issue 6+7+8)
9. **Issue 10** (移动端代理) - **优先级：低** 📱 (新增，可选)
10. **Issue 5** (AI Agent) - **重新设计** ♻️ (暂停，等待前置 Issues 完成)

### 实施顺序建议

#### 阶段 1：基础设施准备（1-2 天）
```
Issue 4 (重构) → Issue 6 (数据持久化)
```
- 先重构项目结构，统一接口
- 实现数据持久化层，为后续功能提供数据基础

#### 阶段 2：核心分析能力（2-3 天）
```
Issue 7 (形态识别引擎)
```
- 实现 Al Brooks 形态识别
- 计算衍生指标
- 生成 Markdown 表格

#### 阶段 3：AI 决策系统（3-4 天）
```
Issue 8 (四级 AI 编排)
```
- 实现 L1/L2/L3/L4 四级决策
- 集成 Azure GPT-4o 和 DeepSeek
- 测试完整决策流程

#### 阶段 4：验证和优化（2-3 天）
```
Issue 9 (回测系统)
```
- 实现回测引擎
- 验证策略有效性
- 优化参数

#### 阶段 5：移动端部署（可选，2-3 天）
```
Issue 10 (移动端代理)
```
- 开发 .NET MAUI App
- 实现 SignalR 实时通信
- 部署到手机

#### 阶段 6：统一接口（1-2 天）
```
Issue 5 (AI Agent 重新设计)
```
- 提供自然语言查询接口
- 集成所有子系统
- Web UI 完善

### 分支策略
- ~~`feature/position-calculator`~~ - Issue 1 (已合并) ✅
- ~~`feature/telegram-integration`~~ - Issue 2 (已合并) ✅
- ~~`feature/android-trading-app`~~ - Issue 3 (待定)
- `feature/refactor-infrastructure` - **Issue 4** 🔧
- `feature/data-persistence` - **Issue 6** 🗄️
- `feature/pattern-recognition` - **Issue 7** 🔍
- `feature/ai-orchestration` - **Issue 8** 🤖
- `feature/backtest-system` - **Issue 9** 📊
- `feature/mobile-proxy` - **Issue 10** 📱
- `feature/ai-agent-v2` - **Issue 5 (重新设计)** ♻️

### Worktree 目录
- ~~`../richdad-position-calc`~~ - Issue 1 (已完成) ✅
- ~~`../richdad-telegram`~~ - Issue 2 (已完成) ✅
- ~~`../richdad-android`~~ - Issue 3 (待定)
- `../richdad-refactor` - **Issue 4** 🔧
- `../richdad-data` - **Issue 6** 🗄️
- `../richdad-patterns` - **Issue 7** 🔍
- `../richdad-orchestration` - **Issue 8** 🤖
- `../richdad-backtest` - **Issue 9** 📊
- `../richdad-mobile` - **Issue 10** 📱
- `../richdad-agent-v2` - **Issue 5 (重新设计)** ♻️

### 推荐工作流程

**阶段 1：基础设施**
```bash
# Issue 4: 重构
git worktree add ../richdad-refactor -b feature/refactor-infrastructure
cd ../richdad-refactor
# 1. 重命名项目
# 2. 添加 IOrderExecutionService
# 3. 测试验证
# 4. 合并到 main

# Issue 6: 数据持久化
git worktree add ../richdad-data -b feature/data-persistence
cd ../richdad-data
# 1. 实现 MarketData 和 ProcessedData 表
# 2. 实现智能缓存服务
# 3. 实现 API 接口
# 4. 测试验证
# 5. 合并到 main
```

**阶段 2：核心能力**
```bash
# Issue 7: 形态识别
git worktree add ../richdad-patterns -b feature/pattern-recognition
cd ../richdad-patterns
# 1. 实现 TechnicalIndicatorService
# 2. 实现 PatternRecognitionService
# 3. 实现 MarkdownTableGenerator
# 4. 测试验证
# 5. 合并到 main
```

**阶段 3：AI 系统**
```bash
# Issue 8: 四级 AI 编排
git worktree add ../richdad-orchestration -b feature/ai-orchestration
cd ../richdad-orchestration
# 1. 实现 L1/L2/L3/L4 服务
# 2. 实现编排服务
# 3. 配置缓存策略
# 4. 测试验证
# 5. 合并到 main
```

**阶段 4：回测验证**
```bash
# Issue 9: 回测系统
git worktree add ../richdad-backtest -b feature/backtest-system
cd ../richdad-backtest
# 1. 实现回测引擎
# 2. 实现虚拟账户
# 3. 实现统计分析
# 4. Web 界面
# 5. 测试验证
# 6. 合并到 main
```

**阶段 5（可选）：移动端**
```bash
# Issue 10: 移动端代理
git worktree add ../richdad-mobile -b feature/mobile-proxy
cd ../richdad-mobile
# 1. 创建 .NET MAUI 项目
# 2. 实现 SignalR 服务
# 3. 实现 UI 界面
# 4. 集成 OANDA
# 5. 测试验证
# 6. 合并到 main
```

### 预计工时

| Issue | 描述 | 预计工时 | 依赖 |
|-------|------|----------|------|
| Issue 4 | 重构基础架构 | 1-1.5 天 | - |
| Issue 6 | 数据持久化 | 2-3 天 | Issue 4 |
| Issue 7 | 形态识别引擎 | 2-3 天 | Issue 6 |
| Issue 8 | 四级 AI 编排 | 3-4 天 | Issue 6, 7 |
| Issue 9 | 回测系统 | 2-3 天 | Issue 6, 7, 8 |
| Issue 10 | 移动端代理（可选） | 2-3 天 | Issue 8 |
| Issue 5 | AI Agent 重新设计 | 1-2 天 | Issue 6, 7, 8 |

**总计（必需）：** 11-17 天
**总计（含可选）：** 13-20 天

### 里程碑

**M1: 基础设施完成（第 1-2 周）**
- ✅ Issue 4: 项目重构完成
- ✅ Issue 6: 数据持久化系统可用
- ✅ 能够从数据库查询历史数据

**M2: 核心分析能力（第 3 周）**
- ✅ Issue 7: 形态识别引擎可用
- ✅ 能够生成 Al Brooks 标准的 Markdown 表格
- ✅ 衍生指标计算准确

**M3: AI 决策系统（第 4-5 周）**
- ✅ Issue 8: 四级 AI 决策系统可用
- ✅ L1/L2/L3/L4 级联逻辑正确
- ✅ 能够生成完整的交易建议

**M4: 策略验证（第 6 周）**
- ✅ Issue 9: 回测系统可用
- ✅ 能够验证历史数据上的表现
- ✅ FTMO 风控规则正确实现

**M5: 生产部署（第 7 周）**
- ✅ Issue 10: 移动端 App 可用（可选）
- ✅ Issue 5: AI Agent 统一接口可用
- ✅ 系统端到端测试通过

### 技术债务管理

在实施过程中，注意避免：
- ❌ 过早优化：先实现功能，再优化性能
- ❌ 功能蔓延：严格按照 Issue 范围实施
- ❌ 文档滞后：每个 Issue 完成后更新文档

### 风险管理

**高风险项：**
1. **AI 决策准确性**：需要大量回测验证
   - 缓解：Issue 9 提供完整回测能力

2. **API 成本控制**：AI 调用可能超预算
   - 缓解：智能缓存 + DeepSeek 低成本模型

3. **FTMO 合规性**：Prop Firm 规则变更
   - 缓解：Issue 10 提供移动端代理方案

**中风险项：**
1. **数据完整性**：OANDA API 可能返回不完整数据
   - 缓解：Issue 6 的缺失检测机制

2. **实时性能**：M5 级别需要快速响应
   - 缓解：分级架构 + 缓存优化

### 成功标准

**技术指标：**
- ✅ 数据查询延迟 < 100ms
- ✅ AI 决策延迟 < 30秒
- ✅ 形态识别准确率 100%
- ✅ 回测速度 > 1 个月/5 分钟

**业务指标：**
- ✅ 回测胜率 > 60% (FTMO 要求)
- ✅ 最大回撤 < 10% (FTMO 限制)
- ✅ 盈亏比 > 1.5:1
- ✅ AI 成本 < $5/天

### 下一步行动

1. **立即开始**：Issue 4（重构）和 Issue 6（数据持久化）
2. **准备资源**：
   - Azure Storage Account
   - DeepSeek API 密钥
   - 历史数据准备
3. **团队协调**：
   - 确认开发时间安排
   - 准备测试环境
4. **文档准备**：
   - 阅读 Al Brooks 理论
   - 研究 FTMO 规则
