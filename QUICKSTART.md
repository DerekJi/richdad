# 快速开始指南

## 项目已完成

✅ 完整的三层架构回测系统已搭建完成：
- 数据层：CSV读取、Cosmos DB持久化
- 业务层：Pin Bar策略、技术指标计算、回测引擎
- 应用层：Console应用

## 快速运行

### 1. 运行回测

```bash
cd src/Trading.Backtest.Console
dotnet run
```

程序将：
1. 自动读取 `data/XAUUSD*.csv` 文件
2. 计算EMA和ATR指标
3. 执行Pin Bar策略回测
4. 显示详细的统计结果

### 2. 修改策略参数

编辑 `src/Trading.Backtest.Console/Program.cs`:

```csharp
var config = StrategyConfig.CreateXauDefault();
// 修改参数
config.RiskRewardRatio = 2.0m;          // 盈亏比 2:1
config.StopLossAtrRatio = 0.5m;         // 止损 0.5 ATR
config.MinLongerWickPercentage = 70;    // 长影线最小70%
config.StartTradingHour = 6;            // UTC 6点开始交易
```

## 项目结构

```
TradingSystem/
├── src/
│   ├── Trading.Data/                # 数据层
│   │   ├── Models/                   # Candle, Trade, BacktestResult等
│   │   ├── Providers/                # CsvDataProvider
│   │   ├── Repositories/             # CosmosBacktestRepository
│   │   └── Interfaces/               # IMarketDataProvider
│   │
│   ├── Trading.Core/                # 核心策略层（可复用于实盘）
│   │   ├── Strategies/               # PinBarStrategy
│   │   └── Indicators/               # IndicatorCalculator (EMA/ATR)
│   │
│   ├── Trading.Backtest/            # 回测引擎层
│   │   ├── Engine/                   # BacktestEngine
│   │   └── Services/                 # BacktestRunner, ResultPrinter
│   │
│   ├── Trading.Backtest.Console/    # Console应用
│   └── Trading.Backtest.Web/        # Web应用（待完善）
│
├── data/                             # CSV历史数据
│   └── XAUUSD*.csv
├── docs/                             # 文档
└── README.md                         # 详细文档
```

## 回测输出示例

```
=== 交易策略回测系统 ===

数据目录: d:\tmp\pinbar\data

正在加载 XAUUSD 的历史数据...
加载完成，共 47288 根K线
数据范围: 2024-01-15 01:00 至 2026-01-14 02:00

开始执行回测...

============================================================
总体统计
============================================================
策略名称: PinBar-XAUUSD-v1
回测周期: 2024-01-15 至 2026-01-14
总交易数: 156
盈利交易: 89
亏损交易: 67
胜率: 57.05%
总收益: 342.50 点
总收益率: 23.45R
平均持仓时间: 18:32:15
最大连续盈利: 7 单
最大连续亏损: 5 单
最大回撤: 45.80 点 (2024-08-23)
盈亏比: 1.85
平均每月开仓: 6.5 单
```

## 策略逻辑

### 多单开仓条件：
1. ✅ 前一根K线是看涨Pin Bar
   - 下影线 ≥ 60%
   - 实体 ≤ 30%
   - 上影线 ≤ 20%
2. ✅ 前一根K线收盘在EMA200上方
3. ✅ 前一根K线底部靠近EMA（20/60/80/100/200）
4. ✅ 当前K线收盘突破Pin Bar高点
5. ✅ 交易时间：UTC 5:00-11:00

### 止损止盈：
- 止损：Pin Bar低点 - 1.0 * ATR
- 止盈：按盈亏比1.5:1计算

## 关键配置参数

| 参数 | 默认值(XAUUSD) | 说明 |
|------|---------------|------|
| BaseEma | 200 | 趋势判断EMA |
| Threshold | 1.0 美元 | Pin Bar最小波动 |
| MaxBodyPercentage | 30% | 实体最大占比 |
| MinLongerWickPercentage | 60% | 长影线最小占比 |
| MaxShorterWickPercentage | 20% | 短影线最大占比 |
| NearEmaThreshold | 0.8 美元 | 靠近EMA的距离 |
| RiskRewardRatio | 1.5 | 盈亏比 |
| StopLossAtrRatio | 1.0 | 止损ATR倍数 |
| MinLowerWickAtrRatio | 1.2 | 长影线最小ATR倍数 |

## 数据库配置（可选）

如果要保存回测结果到Cosmos DB：

1. 启动Cosmos DB Emulator
2. 运行程序后选择 "y" 保存结果
3. 数据将保存到：
   - 数据库：`TradingBacktest`
   - 容器：`BacktestResults`

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
