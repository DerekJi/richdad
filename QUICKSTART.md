# 快速开始指南

## 当前项目状态

✅ 智能交易系统已完成主要功能：
- **实时监控**：价格监控、EMA监控、Pin Bar形态监控
- **AI智能分析**：双级AI架构（Azure OpenAI + DeepSeek）
- **风险管理**：智能仓位计算器，支持Prop Firm规则
- **数据持久化**：Azure Table Storage / Cosmos DB / 内存存储
- **告警通知**：Telegram Bot + 邮件通知
- **回测系统**：已归档至 `archived/` 目录

## 快速运行

### 1. 运行实时监控系统

```bash
cd src/Trading.Web
dotnet run
```

访问：`http://localhost:5000`

程序将：
1. 启动价格监控后台服务
2. 启动Pin Bar形态监控服务
3. 启动AI分析服务（如已配置）
4. 提供Web界面进行配置管理

### 2. 配置监控规则

通过Web界面配置：
- **价格监控**：`http://localhost:5000/`
- **Pin Bar监控**：`http://localhost:5000/pinbar-config.html`

### 3. 测试告警（可选）

创建一个测试告警，验证Telegram通知是否正常。

## 项目结构

```
TradingSystem/
├── src/
│   ├── Trading.Models/              # 核心数据模型
│   │   └── Models/                   # 数据模型（Candle、Trade等）
│   │
│   ├── Trading.Core/                # 核心交易逻辑
│   │   ├── Strategies/               # 交易策略
│   │   ├── Indicators/               # 技术指标
│   │   └── RiskManagement/           # 风险管理
│   │
│   ├── Trading.Infrastructure/      # 基础设施层
│   │   ├── AI/                       # AI分析服务（集成在基础设施层）
│   │   ├── CosmosDB/                 # Cosmos DB实现
│   │   ├── AzureTable/               # Azure Table Storage实现
│   │   ├── Telegram/                 # Telegram Bot集成
│   │   └── Email/                    # 邮件服务
│   │
│   ├── Trading.Services/            # 业务服务层
│   │   ├── Services/                 # 业务服务（监控、告警）
│   │   └── BackgroundJobs/           # 后台任务
│   │
│   └── Trading.Web/                 # Web应用
│       ├── Controllers/              # REST API
│       └── wwwroot/                  # 前端界面
│
├── archived/                        # 已归档的回测和分析工具
│   ├── TradingBacktest.sln           # 回测系统独立解决方案
│   ├── Trading.Backtest.Data/        # 回测数据基础设施
│   ├── Trading.Backtest/             # 回测引擎
│   ├── Trading.Backtest.Console/     # 回测控制台工具
│   ├── Trading.Backtest.Web/         # Web回测界面
│   ├── Trading.Backtest.ParameterOptimizer/ # 参数优化器
│   └── Trading.Strategy.Analyzer/    # 策略分析器
│
├── data/                             # CSV历史数据
├── docs/                             # 文档
│   ├── setup/                        # 配置指南
│   ├── guides/                       # 使用指南
│   ├── strategies/                   # 策略文档
│   └── maintenance/                  # 维护文档
├── TradingSystem.sln                 # 主系统解决方案
└── README.md                         # 项目说明
```

## 主要功能

### 实时监控
- **价格监控**：固定价格、EMA交叉、MA交叉
- **形态监控**：Pin Bar形态自动识别
- **多渠道告警**：Telegram Bot + 邮件通知

### AI智能分析
- **双级AI架构**：成本优化的智能决策
- **多提供商**：Azure OpenAI、DeepSeek
- **自动过滤**：筛选高质量交易信号

### 风险管理
- **智能仓位计算**：基于风险百分比自动计算
- **Prop Firm规则**：预设FTMO、Blue Guardian等规则
- **单日风险控制**：自动追踪限制单日亏损

## 配置要求

### 必需配置
- **Telegram Bot**：用于告警通知
- **OANDA API**：用于实时数据

### 可选配置
- **Azure Table Storage**：低成本持久化（推荐，$1-3/月）
- **Cosmos DB**：高性能数据库（可选）
- **Azure OpenAI**：GPT-4o智能分析
- **DeepSeek API**：低成本AI分析

## 快速配置

详细配置说明参见：
- [用户密钥配置](docs/setup/USER_SECRETS_SETUP.md)
- [告警系统快速入门](docs/ALERT_SYSTEM_QUICKSTART.md)
- [Pin Bar监控快速入门](docs/PINBAR_QUICKSTART.md)
- [双级AI快速入门](docs/DUAL_TIER_AI_QUICKSTART.md)

## 回测功能（已归档）

历史回测功能已移至 `archived/` 目录，如需使用：

```bash
cd archived
dotnet build TradingBacktest.sln
cd Trading.Backtest.Console
dotnet run
```

回测系统支持：
- CSV历史数据导入
- Pin Bar策略回测
- 参数优化
- 详细统计报告

## 更多信息

完整文档请参见：[README.md](README.md)

核心策略说明：[Pin Bar策略](docs/strategies/pin-bar.strategy.md)

## 下一步

1. **调整参数**：修改 `StrategyConfig` 中的参数，运行多次回测找到最优组合
2. **测试不同品种**：使用 `StrategyConfig.CreateXagDefault()` 测试白银策略
3. **查看详细交易**：程序输出每笔交易的开仓、平仓、盈亏详情
4. **保存结果**：将回测结果保存到Cosmos DB以便对比分析

## 常见问题

**Q: 找不到CSV文件？**
A: 确保CSV文件在 `data/` 目录下，文件名包含 `XAUUSD` 或 `XAGUSD`

**Q: 如何添加新策略？**
A: 实现 `ITradingStrategy` 接口，参考 `PinBarStrategy` 的实现

**Q: 如何修改交易时间？**
A: 修改 `StartTradingHour` 和 `EndTradingHour` (UTC时区)

**Q: 胜率太低怎么办？**
A: 尝试调整：
- 增大 `MinLongerWickPercentage` (更严格的Pin Bar)
- 减小 `MaxBodyPercentage` (更纯粹的Pin Bar)
- 调整 `RiskRewardRatio` (更保守的盈亏比)
- 修改 `NearEmaThreshold` (更精确的EMA靠近)

## 技术说明

- ✅ 指标计算：手动实现EMA和ATR (避免第三方库兼容问题)
- ✅ 回测引擎：逐根K线模拟，精确的开平仓逻辑
- ✅ 统计报告：总体/周期/交易详情/收益曲线
- ✅ 扩展性：接口设计支持多数据源、多策略

## 联系与反馈

如有问题或建议，请查看 [README.md](../README.md) 了解更多详情。
