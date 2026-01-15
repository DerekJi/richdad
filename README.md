# 交易策略回测系统

基于.NET的Pin Bar交易策略回测系统，支持黄金(XAUUSD)和白银(XAGUSD)的15分钟K线回测。

## 项目结构

```
TradingBacktest/
├── src/
│   ├── TradingBacktest.Data/        # 数据层
│   │   ├── Models/                   # 数据模型
│   │   ├── Interfaces/               # 接口定义
│   │   ├── Providers/                # 数据提供者（CSV、API）
│   │   └── Repositories/             # 数据仓储（Cosmos DB）
│   ├── TradingBacktest.Core/        # 业务层
│   │   ├── Strategies/               # 交易策略
│   │   ├── Indicators/               # 技术指标
│   │   └── Backtest/                 # 回测引擎
│   ├── TradingBacktest.Console/     # Console应用
│   └── TradingBacktest.Web/         # Web应用（待完善）
├── data/                             # CSV历史数据
└── docs/                             # 文档
```

## 功能特性

### 数据层
- ✅ CSV文件数据读取
- ✅ Cosmos DB数据持久化
- ✅ 可扩展的数据提供者接口
- 🔲 OANDA API集成（待实现）

### 业务层
- ✅ Pin Bar形态识别
- ✅ EMA、ATR等技术指标计算（基于TA-Lib）
- ✅ 多空开仓条件判断
- ✅ 动态止损止盈计算
- ✅ 回测引擎

### 应用层
- ✅ Console快速回测
- 🔲 Web参数优化界面（待实现）
- 🔲 参数自动优化（待实现）

## 回测指标

系统提供完整的回测统计：

- **总体指标**：总交易数、胜率、收益率、平均持仓时间、最大连续盈亏、最大回撤、盈亏比等
- **周期指标**：每周/每月/每年的交易次数、胜率、盈亏、平均持仓时间等
- **交易详情**：每笔交易的开仓/平仓时间、价位、止损止盈、盈亏、平仓原因
- **收益曲线**：用于绘制权益曲线的时间序列数据

## 快速开始

### 前置要求

- .NET 8.0 SDK 或更高版本
- Cosmos DB Emulator（可选，用于保存回测结果）

### 运行回测

1. 克隆仓库并进入目录

2. 将CSV历史数据放入 `data/` 目录
   - 文件格式：`XAUUSD*.csv` 或 `XAGUSD*.csv`
   - 数据格式：MetaTrader导出的TSV格式

3. 运行Console应用：
```bash
cd src/TradingBacktest.Console
dotnet run
```

### 配置策略参数

在 [Program.cs](src/TradingBacktest.Console/Program.cs#L21-L22) 中修改策略配置：

```csharp
var config = StrategyConfig.CreateXauDefault();
config.RiskRewardRatio = 1.5m;  // 调整盈亏比
config.StopLossAtrRatio = 1.0m; // 调整止损ATR倍数
// ... 更多参数
```

## Pin Bar策略说明

### 多单开仓条件
1. 前一根K线为看涨Pin Bar（下影线长，实体小，上影线短）
2. 前一根K线收盘在EMA200上方
3. 前一根K线底部靠近某个EMA（20/60/80/100/200）
4. 当前K线收盘突破前一根K线高点
5. 在交易时间内（UTC 5:00-11:00）

### 空单开仓条件
- 逻辑相反

### 止损止盈
- **止损**：Pin Bar端点 ± ATR倍数
- **止盈**：按固定盈亏比计算（如1.5:1）

## 配置参数说明

| 参数 | 默认值 | 说明 |
|------|--------|------|
| BaseEma | 200 | 基准EMA周期 |
| Threshold | 1.0 | K线最小波动阈值（美元）|
| MaxBodyPercentage | 30 | Pin Bar实体最大占比(%) |
| MinLongerWickPercentage | 60 | 长影线最小占比(%) |
| MaxShorterWickPercentage | 20 | 短影线最大占比(%) |
| EmaList | [20,60,80,100,200] | 需要计算的EMA周期 |
| NearEmaThreshold | 0.8 | 靠近EMA的阈值（美元）|
| RiskRewardRatio | 1.5 | 盈亏比 |
| StopLossAtrRatio | 1.0 | 止损ATR倍数 |
| StartTradingHour | 5 | 交易开始时间(UTC) |
| EndTradingHour | 11 | 交易结束时间(UTC) |
| AtrPeriod | 14 | ATR计算周期 |

## 数据库配置

### Cosmos DB连接字符串

本地Emulator默认连接字符串（已内置）：
```
AccountEndpoint=https://localhost:8081/;
AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==
```

数据库会自动创建：
- 数据库名：`TradingBacktest`
- 容器：`BacktestResults`（分区键：`/config/symbol`）
- 容器：`StrategyConfigs`（分区键：`/symbol`）

## 扩展开发

### 添加新策略

1. 实现 `ITradingStrategy` 接口
2. 在 `Strategies` 文件夹创建新策略类
3. 实现开仓条件和止损止盈逻辑

### 添加新数据源

1. 实现 `IMarketDataProvider` 接口
2. 在 `Providers` 文件夹创建新提供者类
3. 在应用层替换数据提供者

### 添加新指标

1. 在 `IndicatorCalculator` 类中添加新指标计算方法
2. 在 `Candle` 模型中添加指标属性
3. 在策略中使用新指标

## 后续计划

- [ ] 完善Web界面用于参数可视化调整
- [ ] 实现参数自动优化（网格搜索、遗传算法）
- [ ] 集成OANDA API实时获取数据
- [ ] 实现交易信号通知（短信、邮件、APP推送）
- [ ] 添加更多技术指标和策略
- [ ] 支持多品种组合回测
- [ ] 添加FTMO风控规则验证

## 许可证

MIT License

## 联系方式

如有问题或建议，请提issue。
