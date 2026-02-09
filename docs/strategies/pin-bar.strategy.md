在考虑回测这样一个XAUUSD的交易策略（以多单为例）：

1、周期：15分钟（可配置）；

2、K线收盘EMA200（可配置）以下时不开多单；

3、PinBar定义：
- 下影线长度 ≥ 实体长度 × 2
- 下影线占整根 K 线 ≥ 60%
- 上影线占整根 K 线 ≤ 20%
- Close > Open（最好，但不是必须）

4、如果出现Pin Bar，而且收盘时满足下面条件，进入下一步
  - 当前无多单；
  - 底部靠近（或穿过指定的EMA），而且实体在ema上方
  
5、有两个具体开多单的想法，一是直接开，二是等待下一根K线为看涨阳线，而且收盘高于Pin bar的高点。具体我还没想好，可能要回测了才知道

6、止损点设在Pin bar下方加0.5倍（可配置）ATR的位置

7、按固定盈亏比，比如1.5（可配置），设置止盈点

8、所有可配置选项可能需要回测中优化调整

9、需要满足FTMO的风控规则。

```csharp
enum StopLossStrategy 
{
    PinbarEndPlusAtr,
}

Config DefaultConfig = new()
{
    baseEma = 200;
    threshold = 1; // dollars
    maxBodyPercentage = 30;
    minLongerWickPercentage = 60;
    maxShorterWickPercentage = 20;
    
    emaList = [20, 60, 80, 100, 200];
    nearEmaThreshold = 0.8; // dollars

    rrRatio = 1.5; // Risk-Reward Ratio

    stopLossAtrRatio = 1; // Low - n * ATR or (High + n * ART)

    stopLossStrategy = StopLossStrategy.PinbarEndPlusAtr,

    StartTradingHour = 5; // UTC
    EndTradingHour = 11; // UTC

    ContractSize = 100;
};

Config XauConfig = new()
{
    {...DefaultConfig}，
    ContractSize = 100;
};

Config XagConfig = new()
{
    {...DefaultConfig}，
    threshold = 0.8; // dollars
    nearEmaThreshold = 0.2; // dollars
    ContractSize = 1000;
};

public class PinbarStrategy
{
    public Config _cfg { get; set; }

    public bool isPinBar(Candle c, bool bullish) {
        var total = abs(c.High - c.Low);
        if (total < _cfg.threshold) return false;
        
        var LongerWick = bullish ? min(c.Open, c.Close) - Low : c.High - max(c.Open, c.Close);
        if (LongerWick < c.ATR * 1.2) return false;

        var body = abs(c.Open - c.Close);
        var ShorterWick = bullish ? c.High - max(c.Open, c.Close) : min(c.Open, Close) - Low;
        
        return
            100 * body/total <= _cfg.maxBodyPercentage &&
            100 * LongerWick/total >= _cfg.minLongerWickPercentage &&
            100 * ShorterWick/total <= _cfg.maxShorterWickPercentage;  
    }

    public bool NearAnyEma(Candle c, bool bullish)
    {
        return _cfg.emaList.Any(ema => 
            bullish
                ? min(c.Open, c.Close) > ema && (abs(c.Low - ema) <= _cfg.nearEmaThreshold || c.Low < ema)
                : max(c.Open, c.Close) < ema && (abs(c.High - ema) <= _cfg.nearEmaThreshold || c.High > ema));
    }

    public decimal StopLossPosition(Candle pinbar, bool bullish)
    {
        switch (_cfg.stopLossStrategy)
        {
            case StopLossStrategy.PinbarEndPlusAtr:
                var plus = _cfg.stopLossAtrRatio * pinbar.ATR;
                return bullish
                    ? pinbar.Low - plus
                    : pinbar.High + plus;
            default:
                return 0m;
        }        
    }

    public bool ExceedsMaxDrawdown

    public bool IsValidTime(Hour utcHour) {
        return (utcHour >= _cfg.StartTradingHour && time.Hour <= _cfg.EndTradingHour);
    }

    public bool CanOpenLong(Candle current, Candle previous)
    {
        var bullish = true;
        return
            IsValidTime(current.UtcHour) &&
            previous.Close > previous.EMA200 &&
            current.Close > previous.High &&
            isPinBar(previous, bullish: true) &&
            NearAnyEma(previous, bullish: true);
    }

    public bool CanOpenShort(Candle current, Candle previous)
    {
        var bullish = false;
        return
            IsValidTime(current.UtcHour) &&
            previous.Close < previous.EMA200 &&
            current.Close < previous.Low &&
            isPinBar(previous, bullish) &&
            NearAnyEma(previous, bullish: true);
    }
}
```