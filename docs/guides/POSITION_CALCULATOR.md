# Position Calculator (仓位计算器)

## 功能概述

这是一个基于风险管理的自动仓位计算工具，支持：

- ✅ **多种风险模式**：支持金额和百分比两种方式
- ✅ **Prop Firm规则**：预设 Blue Guardian、FTMO 等主流 Prop Firm 规则
- ✅ **多品种支持**：支持外汇、黄金、白银等多种交易品种
- ✅ **多平台配置**：不同平台的合约大小、杠杆率可独立配置
- ✅ **单日风险控制**：按服务器时区自动重置单日亏损
- ✅ **灵活配置**：支持自定义规则或使用预设规则

## 项目结构

```
Trading.Core/
  RiskManagement/
    Models/
      - InstrumentSpecification.cs    # 品种规格定义
      - PropFirmRules.cs              # Prop Firm 规则
      - RiskParameters.cs             # 风险参数
      - PositionSizeResult.cs         # 计算结果
      - CalculationDetail.cs          # 详细计算信息
    Configuration/
      - PropFirmRulesSettings.cs      # Prop Firm 规则配置
      - InstrumentSpecificationSettings.cs  # 品种规格配置
    - PositionSizer.cs                # 核心仓位计算引擎
    - RiskManager.cs                  # 风险管理器
    - TradingDayCalculator.cs         # 交易日计算工具
```

## 快速开始

### 1. 测试命令

```bash
cd src/Trading.Backtest.Console
dotnet run position-calc
```

这会运行4个测试场景：
1. Blue Guardian规则 - 正常黄金交易
2. 接近单日亏损限额的场景
3. 自定义风险规则
4. 白银交易

### 2. 配置文件

在 `appsettings.json` 中配置：

#### PropFirmRules (Prop Firm规则)

```json
{
  "PropFirmRules": {
    "BlueGuardian": {
      "Name": "BlueGuardian",
      "MaxDailyLossPercent": 3.0,      // 单日最大亏损 3%
      "MaxTotalLossPercent": 6.0,      // 总最大亏损 6%
      "ServerTimeZoneOffset": 2,       // UTC+2 时区
      "CalculationBase": "InitialBalance",
      "IsActive": true
    },
    "FTMO": {
      "Name": "FTMO",
      "MaxDailyLossPercent": 5.0,      // 单日最大亏损 5%
      "MaxTotalLossPercent": 10.0,     // 总最大亏损 10%
      "ServerTimeZoneOffset": 2,
      "CalculationBase": "InitialBalance",
      "IsActive": true
    }
  }
}
```

#### InstrumentSpecifications (品种规格)

```json
{
  "InstrumentSpecifications": {
    "ICMarkets": {
      "Instruments": {
        "XAUUSD": {
          "Symbol": "XAUUSD",
          "ContractSize": 100,         // 合约大小
          "TickSize": 0.01,            // 最小变动价位
          "TickValue": 1.0,
          "Leverage": 500,             // 杠杆
          "IsActive": true
        },
        "XAGUSD": {
          "Symbol": "XAGUSD",
          "ContractSize": 5000,
          "TickSize": 0.001,
          "Leverage": 500,
          "IsActive": true
        }
      }
    }
  }
}
```

## 使用方法

### 代码示例

```csharp
using Trading.Core.RiskManagement;
using Trading.Core.RiskManagement.Models;

// 1. 创建 RiskManager
var riskManager = new RiskManager();

// 2. 注册 Prop Firm 规则
riskManager.RegisterPropFirmRule(new PropFirmRules
{
    Name = "BlueGuardian",
    MaxDailyLossPercent = 3.0m,
    MaxTotalLossPercent = 6.0m,
    ServerTimeZoneOffset = 2,
    CalculationBase = CalculationBase.InitialBalance
});

// 3. 注册品种规格
riskManager.RegisterInstrumentSpec(new InstrumentSpecification
{
    Symbol = "XAUUSD",
    Broker = "ICMarkets",
    ContractSize = 100,
    TickSize = 0.01m,
    Leverage = 500
});

// 4. 设置风险参数
var riskParams = new RiskParameters
{
    AccountBalance = 10000m,          // 当前账户余额
    InitialBalance = 10000m,          // 初始余额
    PropFirmRule = "BlueGuardian",    // 使用 Blue Guardian 规则
    RiskPercentPerTrade = 1.0m,       // 单笔风险 1%
    TodayLoss = 0m,                   // 今日已亏损
    TotalLoss = 0m,                   // 总亏损
    LastResetDate = DateTime.UtcNow.Date.AddDays(-1)
};

// 5. 计算仓位
var result = riskManager.CalculatePosition(
    symbol: "XAUUSD",
    broker: "ICMarkets",
    entryPrice: 2650.50m,
    stopLoss: 2645.00m,
    riskParams: riskParams
);

// 6. 检查结果
if (result.CanTrade)
{
    Console.WriteLine($"Position Size: {result.PositionSize} lots");
    Console.WriteLine($"Risk Amount: ${result.RiskAmount}");
}
else
{
    Console.WriteLine($"Cannot Trade: {result.Reason}");
}
```

### 使用自定义规则

```csharp
var riskParams = new RiskParameters
{
    AccountBalance = 10000m,
    InitialBalance = 10000m,
    PropFirmRule = null,                      // 不使用预设规则
    CustomDailyLossPercent = 2.0m,            // 自定义单日限额 2%
    CustomTotalLossPercent = 4.0m,            // 自定义总限额 4%
    CustomServerTimeZoneOffset = 2,           // 自定义时区
    RiskPercentPerTrade = 1.0m,
    TodayLoss = 0m,
    TotalLoss = 0m
};
```

## 计算逻辑

### 核心计算公式

```
1. 每手风险 = (入场价 - 止损价) × 合约大小
2. 允许风险 = min(单笔限额, 单日剩余额度, 总剩余额度)
3. 仓位大小 = 允许风险 ÷ 每手风险
4. 结果向下取整到 0.01 手
```

### 风险检查顺序

1. **参数验证**：检查所有必需参数
2. **品种检查**：验证品种规格是否存在
3. **交易日检查**：根据服务器时区判断是否需要重置单日亏损
4. **单日限额检查**：今日亏损 >= 单日限额 → 禁止交易
5. **总限额检查**：总亏损 >= 总限额 → 禁止交易
6. **计算仓位**：基于剩余风险额度计算建议仓位

## 测试结果示例

```
Scenario 1: Blue Guardian - Normal XAUUSD Trade
Account: $10,000 | Entry: 2650.50 | Stop Loss: 2645.00

--- Result ---
Can Trade: True
Position Size: 0.18 lots
Risk Amount: $100.00
Reason: Within risk limits

--- Details ---
Entry: 2650.50 | Stop Loss: 2645.00
Risk in Pips: 550.00
Per Lot Risk: $550.00
Contract Size: 100 | Tick Size: 0.01

Daily Loss: $0.00 / $300.00 (Remaining: $300.00)
Total Loss: $0.00 / $600.00 (Remaining: $600.00)
Trading Day: 2026-02-06 (Server Time: 2026-02-06 05:15:01)
```

## 核心特性

### 1. 时区处理

- 支持任意时区偏移量（如 UTC+2, UTC+3）
- 自动计算交易日
- 凌晨 00:00 自动重置单日亏损

### 2. 灵活的风险限制

- 支持固定金额（如 $100）
- 支持百分比（如 1%）
- 自动选择较小值

### 3. 多品种支持

- 每个品种独立配置合约大小
- 支持不同平台的同一品种
- 自动计算每手风险

### 4. 详细的计算反馈

每次计算都返回：
- 是否允许交易
- 建议仓位大小
- 实际风险金额
- 详细的计算过程
- 剩余风险额度

## 后续扩展计划

### Phase 3 - 数据持久化
- [ ] Cosmos DB 实体定义
- [ ] 品种规格仓储实现
- [ ] 配置管理 API

### Phase 4 - 系统集成
- [ ] 集成到回测引擎
- [ ] 集成到实时交易系统
- [ ] Web API 接口

## 常见问题

### Q1: 如何添加新的 Prop Firm 规则？

在 `appsettings.json` 中添加：

```json
"MyPropFirm": {
  "Name": "MyPropFirm",
  "MaxDailyLossPercent": 4.0,
  "MaxTotalLossPercent": 8.0,
  "ServerTimeZoneOffset": 2,
  "CalculationBase": "InitialBalance",
  "IsActive": true
}
```

### Q2: 如何添加新的交易品种？

在 `appsettings.json` 的对应平台下添加：

```json
"USDJPY": {
  "Symbol": "USDJPY",
  "ContractSize": 100000,
  "TickSize": 0.001,
  "TickValue": 1.0,
  "Leverage": 500,
  "IsActive": true
}
```

### Q3: 时区设置是什么意思？

`ServerTimeZoneOffset` 是服务器时区相对于 UTC 的偏移小时数：
- UTC+2 → 设置为 `2`
- UTC+3 → 设置为 `3`
- UTC-5 → 设置为 `-5`

### Q4: InitialBalance 和 CurrentBalance 的区别？

- **InitialBalance**：基于初始余额计算（Prop Firm 常用）
  - 单日限额 = 初始余额 × 3%
- **CurrentBalance**：基于当前余额计算
  - 单日限额 = 当前余额 × 3%

## 技术栈

- .NET 9.0
- C#
- Microsoft.Extensions.Configuration

## 版本历史

- **v1.0** (2026-02-06) - 初始版本
  - 核心仓位计算功能
  - Prop Firm 规则支持
  - 多品种配置
  - Console 测试工具
