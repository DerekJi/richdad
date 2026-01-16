using Trading.Backtest.Engine;
using Trading.Backtest.ParameterOptimizer.Models;
using Trading.Core.Strategies;
using Trading.Data.Configuration;
using Trading.Data.Models;

namespace Trading.Backtest.ParameterOptimizer.Services;

public class BacktestExecutor
{
    private readonly List<Candle> _candles;

    public BacktestExecutor(List<Candle> candles)
    {
        _candles = candles;
    }

    public OptimizationResult RunBacktest(BacktestParameters parameters)
    {
        var config = new StrategyConfig
        {
            Symbol = "XAUUSD",
            CsvFilter = "",
            ContractSize = 100,
            
            // Pin Bar参数
            MaxBodyPercentage = parameters.MaxBodyPercentage,
            MinLongerWickPercentage = parameters.MinLongerWickPercentage,
            MaxShorterWickPercentage = parameters.MaxShorterWickPercentage,
            MinLowerWickAtrRatio = 0,
            Threshold = 0.8m,
            
            // EMA参数
            BaseEma = 200,
            EmaList = new List<int> { 20, 60, 80, 100, 200 },
            NearEmaThreshold = parameters.NearEmaThreshold,
            AtrPeriod = 14,
            
            // 止损止盈参数
            StopLossAtrRatio = parameters.StopLossAtrRatio,
            RiskRewardRatio = parameters.RiskRewardRatio,
            
            // 交易时间
            NoTradingHoursLimit = true,
            StartTradingHour = 5,
            EndTradingHour = 11,
            RequirePinBarDirectionMatch = false
        };

        var accountSettings = new AccountSettings
        {
            InitialCapital = 100000,
            Leverage = 30,
            MaxLossPerTradePercent = (double)parameters.MaxLossPerTradePercent,
            MaxDailyLossPercent = 3
        };

        // 创建策略并执行回测（使用预加载的数据）
        var strategy = new PinBarStrategy(config);
        var backtestEngine = new BacktestEngine(strategy);
        var result = backtestEngine.RunBacktest(_candles, config, accountSettings);

        return new OptimizationResult
        {
            Parameters = parameters,
            TotalTrades = result.OverallMetrics.TotalTrades,
            WinRate = result.OverallMetrics.WinRate,
            TotalReturnRate = result.OverallMetrics.TotalReturnRate,
            TotalProfit = result.OverallMetrics.TotalProfit,
            MaxDrawdown = result.OverallMetrics.MaxDrawdown,
            AvgWin = 0,
            AvgLoss = 0,
            SharpeRatio = 0
        };
    }
}
