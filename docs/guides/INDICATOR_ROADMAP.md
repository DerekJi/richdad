# 指标扩展计划

## 当前支持的指标

✅ **固定价格** - 监控价格是否达到指定值
✅ **EMA (指数移动平均线)** - 监控价格与EMA的关系
✅ **MA/SMA (简单移动平均线)** - 监控价格与MA的关系

## 计划添加的指标

### 高优先级

#### 1. ATR (Average True Range - 平均真实波幅)
**用途**: 衡量市场波动性
**告警场景**:
- ATR突破某个阈值（市场波动加剧）
- ATR低于某个阈值（市场平静）

**实现示例**:
```csharp
public enum AlertType
{
    FixedPrice,
    EMA,
    MA,
    ATR,  // 新增
    // ...
}

// 在PriceAlert模型中添加
public decimal? AtrThreshold { get; set; }
public int? AtrPeriod { get; set; } = 14;  // 默认14周期
```

#### 2. RSI (Relative Strength Index - 相对强弱指标)
**用途**: 判断超买超卖状态
**告警场景**:
- RSI > 70 (超买)
- RSI < 30 (超卖)
- RSI穿越50中轴线

**实现示例**:
```csharp
public enum AlertType
{
    // ...
    RSI,  // 新增
}

public decimal? RsiLevel { get; set; }  // RSI目标值 (30, 50, 70等)
public int? RsiPeriod { get; set; } = 14;
```

#### 3. MACD (Moving Average Convergence Divergence)
**用途**: 趋势跟踪和动量指标
**告警场景**:
- MACD线穿越信号线（金叉/死叉）
- MACD柱状图变色
- MACD线穿越零轴

**实现示例**:
```csharp
public enum AlertType
{
    // ...
    MACD,  // 新增
}

public enum MacdSignalType
{
    GoldenCross,    // 金叉 (MACD上穿信号线)
    DeathCross,     // 死叉 (MACD下穿信号线)
    ZeroCrossUp,    // 上穿零轴
    ZeroCrossDown   // 下穿零轴
}

public MacdSignalType? MacdSignal { get; set; }
public int? MacdFastPeriod { get; set; } = 12;
public int? MacdSlowPeriod { get; set; } = 26;
public int? MacdSignalPeriod { get; set; } = 9;
```

### 中优先级

#### 4. Bollinger Bands (布林带)
**用途**: 判断价格波动范围和突破
**告警场景**:
- 价格突破上轨
- 价格跌破下轨
- 价格回到中轨

#### 5. Stochastic Oscillator (随机振荡器)
**用途**: 判断超买超卖
**告警场景**:
- K线和D线金叉/死叉
- 指标进入超买/超卖区域

#### 6. CCI (Commodity Channel Index - 商品通道指数)
**用途**: 识别超买超卖和趋势
**告警场景**:
- CCI突破+100或-100

### 低优先级

#### 7. Ichimoku Cloud (一目均衡表)
#### 8. Parabolic SAR
#### 9. Volume Indicators (成交量指标)

## 实现步骤

### 1. 扩展数据模型

文件: `Trading.AlertSystem.Data/Models/PriceAlert.cs`

```csharp
public enum AlertType
{
    FixedPrice,
    EMA,
    MA,
    ATR,        // 新增
    RSI,        // 新增
    MACD,       // 新增
    Bollinger,  // 新增
    // ...后续添加
}

// 为每个指标添加相应的配置属性
public decimal? AtrThreshold { get; set; }
public int? AtrPeriod { get; set; }

public decimal? RsiLevel { get; set; }
public int? RsiPeriod { get; set; }

// ...
```

### 2. 实现指标计算

文件: `Trading.AlertSystem.Service/Services/PriceMonitorService.cs`

已使用 `Skender.Stock.Indicators` 库，支持所有常见指标：

```csharp
// ATR计算
private async Task<decimal> CalculateAtrAsync(string symbol, string timeFrame, int period)
{
    var candles = await _tradeLockerService.GetHistoricalDataAsync(symbol, timeFrame, period + 50);
    var quotes = ConvertToQuotes(candles);
    var atrResults = quotes.GetAtr(period).ToList();
    return (decimal)(atrResults.LastOrDefault()?.Atr ?? 0);
}

// RSI计算
private async Task<decimal> CalculateRsiAsync(string symbol, string timeFrame, int period)
{
    var candles = await _tradeLockerService.GetHistoricalDataAsync(symbol, timeFrame, period + 50);
    var quotes = ConvertToQuotes(candles);
    var rsiResults = quotes.GetRsi(period).ToList();
    return (decimal)(rsiResults.LastOrDefault()?.Rsi ?? 0);
}

// MACD计算
private async Task<(decimal macd, decimal signal, decimal histogram)> CalculateMacdAsync(
    string symbol, string timeFrame, int fastPeriod, int slowPeriod, int signalPeriod)
{
    var candles = await _tradeLockerService.GetHistoricalDataAsync(symbol, timeFrame, slowPeriod + 50);
    var quotes = ConvertToQuotes(candles);
    var macdResults = quotes.GetMacd(fastPeriod, slowPeriod, signalPeriod).ToList();
    var last = macdResults.LastOrDefault();
    return (
        (decimal)(last?.Macd ?? 0),
        (decimal)(last?.Signal ?? 0),
        (decimal)(last?.Histogram ?? 0)
    );
}
```

### 3. 更新Web界面

文件: `Trading.AlertSystem.Web/wwwroot/index.html`

在表单中添加新指标的输入字段：

```javascript
// 根据选择的告警类型动态显示不同的表单字段
function updateFormFields() {
    const type = parseInt(document.getElementById('alertType').value);

    // 隐藏所有指标特定的字段
    document.getElementById('targetPriceGroup').style.display = 'none';
    document.getElementById('emaPeriodGroup').style.display = 'none';
    document.getElementById('maPeriodGroup').style.display = 'none';
    document.getElementById('atrGroup').style.display = 'none';
    document.getElementById('rsiGroup').style.display = 'none';
    document.getElementById('macdGroup').style.display = 'none';

    // 根据类型显示对应字段
    switch(type) {
        case 0: // FixedPrice
            document.getElementById('targetPriceGroup').style.display = 'block';
            break;
        case 1: // EMA
            document.getElementById('emaPeriodGroup').style.display = 'block';
            break;
        case 2: // MA
            document.getElementById('maPeriodGroup').style.display = 'block';
            break;
        case 3: // ATR
            document.getElementById('atrGroup').style.display = 'block';
            break;
        case 4: // RSI
            document.getElementById('rsiGroup').style.display = 'block';
            break;
        case 5: // MACD
            document.getElementById('macdGroup').style.display = 'block';
            break;
    }
}
```

### 4. 更新API Controller

文件: `Trading.AlertSystem.Web/Controllers/AlertsController.cs`

在CreateAlertRequest和UpdateAlertRequest中添加新字段。

## 使用的库

所有技术指标计算使用 **Skender.Stock.Indicators** 库，已在项目中引用。

支持的指标包括：
- ✅ SMA, EMA, WMA等移动平均线
- ✅ RSI, Stochastic, CCI等振荡器
- ✅ MACD, ADX等趋势指标
- ✅ Bollinger Bands, Keltner Channels等通道指标
- ✅ ATR, ADX等波动性指标
- ✅ 60+ 种技术指标

文档: https://github.com/DaveSkender/Stock.Indicators

## 测试建议

1. **单元测试**: 为每个新指标添加单元测试
2. **集成测试**: 测试告警触发逻辑
3. **回测验证**: 使用历史数据验证指标准确性
4. **性能测试**: 确保新指标不影响监控性能

## 示例用例

### ATR告警
```
名称: XAUUSD波动性监控
品种: XAUUSD
类型: ATR
ATR阈值: 15.0
方向: 上穿
说明: 当ATR超过15时，说明市场波动加剧
```

### RSI告警
```
名称: XAUUSD超卖提醒
品种: XAUUSD
类型: RSI
RSI水平: 30
方向: 下穿
说明: RSI低于30进入超卖区域，可能反弹
```

### MACD告警
```
名称: XAUUSD MACD金叉
品种: XAUUSD
类型: MACD
信号: 金叉
说明: MACD线上穿信号线，看涨信号
```

## 实现优先级和时间估算

| 指标 | 优先级 | 预计工时 | 复杂度 |
|-----|-------|---------|--------|
| ATR | P0 | 2-3小时 | 低 |
| RSI | P0 | 2-3小时 | 低 |
| MACD | P0 | 3-4小时 | 中 |
| Bollinger Bands | P1 | 3-4小时 | 中 |
| Stochastic | P1 | 3-4小时 | 中 |
| CCI | P2 | 2-3小时 | 低 |

## 注意事项

1. **数据量**: 某些指标需要更多历史数据来计算
2. **性能**: 监控多个指标时注意API调用频率
3. **准确性**: 不同时间周期的指标可能给出不同信号
4. **用户体验**: 提供清晰的指标说明和配置提示

## 下一步

1. 根据用户反馈确定最需要的指标
2. 实现高优先级指标
3. 添加更多自定义条件组合
4. 支持多条件复合告警（如: RSI<30 且 MACD金叉）
