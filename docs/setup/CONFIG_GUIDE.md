# 配置参数详细说明

## StrategyConfig 参数完整列表

### 基础配置

#### StrategyName
- **类型**: string
- **默认值**: "PinBar"
- **说明**: 策略的名称，用于识别不同的策略版本
- **建议**: 使用有意义的命名，如 "PinBar-XAUUSD-v1", "PinBar-Aggressive-v2"

#### Symbol
- **类型**: string  
- **默认值**: "XAUUSD"
- **说明**: 交易品种代码
- **支持**: XAUUSD (黄金), XAGUSD (白银)

#### ContractSize
- **类型**: decimal
- **默认值**: 100 (XAUUSD), 1000 (XAGUSD)
- **说明**: 合约规模，用于未来计算实际盈亏金额

---

## Pin Bar 形态参数

### Threshold
- **类型**: decimal
- **默认值**: 1.0 (XAUUSD), 0.8 (XAGUSD)
- **单位**: 美元
- **说明**: K线最小波动阈值，过滤波动太小的K线
- **影响**: 数值越大，过滤越严格，信号越少但质量越高
- **调参建议**:
  - XAUUSD: 0.5 - 2.0
  - XAGUSD: 0.2 - 1.0

### MaxBodyPercentage
- **类型**: decimal
- **默认值**: 30
- **单位**: %
- **说明**: Pin Bar实体部分的最大占比
- **影响**: 数值越小，Pin Bar越纯粹（实体越小）
- **调参建议**:
  - 保守: 20-25 (更纯粹的Pin Bar)
  - 标准: 25-35
  - 激进: 35-40 (允许较大实体)

### MinLongerWickPercentage
- **类型**: decimal
- **默认值**: 60
- **单位**: %
- **说明**: 长影线的最小占比（多单为下影线，空单为上影线）
- **影响**: 数值越大，Pin Bar的"针"越明显
- **调参建议**:
  - 保守: 65-75 (非常明显的针)
  - 标准: 55-65
  - 激进: 50-55

### MaxShorterWickPercentage
- **类型**: decimal
- **默认值**: 20
- **单位**: %
- **说明**: 短影线的最大占比
- **影响**: 数值越小，Pin Bar越完美（短影线越短）
- **调参建议**:
  - 保守: 10-15
  - 标准: 15-25
  - 激进: 25-30

### MinLowerWickAtrRatio
- **类型**: decimal
- **默认值**: 1.2
- **说明**: 长影线必须至少是ATR的多少倍
- **影响**: 确保Pin Bar有足够的"力度"
- **调参建议**: 1.0 - 1.5

### RequirePinBarDirectionMatch
- **类型**: bool
- **默认值**: false
- **说明**: 是否要求Pin Bar的颜色与方向匹配
  - true: 看涨Pin Bar必须是阳线，看跌Pin Bar必须是阴线
  - false: 只看形态，不看颜色
- **建议**: 通常设为false，因为颜色不是关键

---

## EMA 参数

### BaseEma
- **类型**: int
- **默认值**: 200
- **说明**: 基准EMA周期，用于判断大趋势
- **逻辑**:
  - 多单：前一根K线收盘价必须在EMA200上方
  - 空单：前一根K线收盘价必须在EMA200下方
- **调参建议**:
  - 短周期: 100-150 (更激进，更多信号)
  - 中周期: 150-200 (平衡)
  - 长周期: 200-300 (更保守，趋势更明确)

### EmaList
- **类型**: List<int>
- **默认值**: [20, 60, 80, 100, 200]
- **说明**: 需要计算的EMA周期列表，用于判断Pin Bar是否靠近某个EMA
- **调参建议**:
  - 可以添加更多周期如 [10, 20, 50, 100, 200]
  - 或简化为关键周期 [20, 100, 200]

### NearEmaThreshold
- **类型**: decimal
- **默认值**: 0.8 (XAUUSD), 0.2 (XAGUSD)
- **单位**: 美元
- **说明**: Pin Bar底部/顶部距离EMA多近算"靠近"
- **逻辑**:
  - 实体必须在EMA上方/下方
  - 影线触及EMA或距离EMA在阈值内
- **调参建议**:
  - XAUUSD: 0.5 - 1.5
  - XAGUSD: 0.1 - 0.5

---

## 风险管理参数

### RiskRewardRatio
- **类型**: decimal
- **默认值**: 1.5
- **说明**: 盈亏比 (Reward:Risk)
- **示例**:
  - 1.5 = 止盈距离是止损距离的1.5倍
  - 2.0 = 止盈距离是止损距离的2倍
- **影响**: 
  - 越大：每单盈利越多，但止盈难达到，胜率下降
  - 越小：更容易止盈，胜率上升，但单笔盈利少
- **调参建议**:
  - 保守: 1.0 - 1.2 (追求高胜率)
  - 标准: 1.5 - 2.0 (平衡)
  - 激进: 2.0 - 3.0 (追求大盈利)

### StopLossAtrRatio
- **类型**: decimal
- **默认值**: 1.0
- **说明**: 止损距离 = Pin Bar端点 ± (ATR × 此倍数)
- **影响**:
  - 越大：止损越宽松，不易被止损，但单笔亏损大
  - 越小：止损越紧，保护资金，但容易被扫损
- **调参建议**:
  - 紧止损: 0.5 - 0.8
  - 标准: 0.8 - 1.2
  - 宽止损: 1.2 - 1.5

### StopLossStrategy
- **类型**: enum
- **默认值**: PinbarEndPlusAtr
- **说明**: 止损位计算策略
- **当前仅支持**: 
  - PinbarEndPlusAtr: Pin Bar低点/高点 + ATR倍数

---

## 交易时间参数

### StartTradingHour
- **类型**: int
- **默认值**: 5
- **单位**: UTC小时 (0-23)
- **说明**: 开始交易的时间（UTC时区）
- **常用时间段**:
  - 5-11 (UTC) = 伦敦时段 + 早盘纽约
  - 13-19 (UTC) = 晚盘纽约 + 早盘亚洲
  - 0-6 (UTC) = 亚洲时段

### EndTradingHour
- **类型**: int
- **默认值**: 11
- **单位**: UTC小时 (0-23)
- **说明**: 结束交易的时间（UTC时区）
- **策略**: 只在指定时间内寻找开仓信号，已有持仓不受影响

---

## 技术指标参数

### AtrPeriod
- **类型**: int
- **默认值**: 14
- **说明**: ATR (平均真实波幅) 的计算周期
- **影响**: 周期越长，ATR越平滑但反应越慢
- **调参建议**:
  - 快速: 7-10
  - 标准: 14-20
  - 慢速: 20-30

---

## 参数组合建议

### 保守型（追求高胜率）
```csharp
var config = new StrategyConfig
{
    MaxBodyPercentage = 25,
    MinLongerWickPercentage = 70,
    MaxShorterWickPercentage = 15,
    RiskRewardRatio = 1.2m,
    StopLossAtrRatio = 1.0m,
    NearEmaThreshold = 0.5m,
    MinLowerWickAtrRatio = 1.5m
};
```

### 标准型（平衡）
```csharp
var config = StrategyConfig.CreateXauDefault(); // 使用默认值
```

### 激进型（追求高盈利）
```csharp
var config = new StrategyConfig
{
    MaxBodyPercentage = 35,
    MinLongerWickPercentage = 55,
    MaxShorterWickPercentage = 25,
    RiskRewardRatio = 2.0m,
    StopLossAtrRatio = 0.8m,
    NearEmaThreshold = 1.2m,
    MinLowerWickAtrRatio = 1.0m
};
```

---

## 参数优化流程

1. **建立基准**: 先用默认参数运行，记录结果
2. **单参数调整**: 每次只调整一个参数，观察影响
3. **关键参数**: 
   - 优先调整: RiskRewardRatio, StopLossAtrRatio, MinLongerWickPercentage
   - 次要调整: MaxBodyPercentage, NearEmaThreshold
4. **组合测试**: 找到几组表现好的参数组合
5. **多周期验证**: 在不同时间段的数据上验证

---

## 评估指标权重

在优化参数时，关注以下指标的平衡：

- **胜率** (Win Rate): 目标 > 50%
- **盈亏比** (Profit Factor): 目标 > 1.5
- **最大回撤** (Max Drawdown): 越小越好
- **交易数量**: 太少说明参数太严，太多可能过拟合
- **最大连续亏损**: 关注心理承受能力
- **平均持仓时间**: 符合你的交易风格

---

## 注意事项

⚠️ **过拟合风险**: 
- 不要过度优化单一数据集
- 在多个时间段验证参数
- 保留一部分数据作为验证集

⚠️ **实盘差异**:
- 回测不考虑滑点和佣金
- 实盘止损可能触发在稍差的价位
- 需要考虑FTMO等风控规则

⚠️ **数据质量**:
- 确保CSV数据完整无缺失
- 检查是否有异常波动
- 注意周末和节假日数据
