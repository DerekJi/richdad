using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Trading.AlertSystem.Data.Models;
using Trading.AlertSystem.Data.Repositories;
using Trading.Core.Strategies;
using Trading.AlertSystem.Data.Services;
using Trading.AI.Services;
using Trading.AI.Models;
using AlertCandle = Trading.AlertSystem.Data.Services.Candle;
using CoreCandle = Trading.Data.Models.Candle;

namespace Trading.AlertSystem.Service.Services;

public class PinBarMonitoringService : BackgroundService
{
    private readonly ILogger<PinBarMonitoringService> _logger;
    private readonly IPinBarMonitorRepository _repository;
    private readonly IMarketDataService _marketDataService;
    private readonly ITelegramService _telegramService;
    private readonly IMarketAnalysisService? _aiAnalysisService;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);
    private readonly Dictionary<string, DateTime> _lastSignalTimes = new();

    public PinBarMonitoringService(
        ILogger<PinBarMonitoringService> logger,
        IPinBarMonitorRepository repository,
        IMarketDataService marketDataService,
        ITelegramService telegramService,
        IMarketAnalysisService? aiAnalysisService = null)
    {
        _logger = logger;
        _repository = repository;
        _marketDataService = marketDataService;
        _telegramService = telegramService;
        _aiAnalysisService = aiAnalysisService;

        if (_aiAnalysisService != null)
        {
            _logger.LogInformation("✅ PinBar监控服务已启用AI增强功能");
        }
        else
        {
            _logger.LogInformation("ℹ️ PinBar监控服务运行在传统模式（AI未配置）");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PinBar监控服务已启动");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckPinBarSignalsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PinBar监控检查时发生错误");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("PinBar监控服务已停止");
    }

    private async Task CheckPinBarSignalsAsync()
    {
        var config = await _repository.GetConfigAsync();
        if (config == null || !config.Enabled)
        {
            return;
        }

        _logger.LogDebug("开始检查PinBar信号 - Symbols: {Symbols}, TimeFrames: {TimeFrames}",
            string.Join(",", config.Symbols), string.Join(",", config.TimeFrames));

        foreach (var symbol in config.Symbols)
        {
            foreach (var timeFrame in config.TimeFrames)
            {
                try
                {
                    await CheckSymbolTimeFrameAsync(symbol, timeFrame, config);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "检查PinBar信号失败: {Symbol} {TimeFrame}", symbol, timeFrame);
                }
            }
        }
    }

    private async Task CheckSymbolTimeFrameAsync(string symbol, string timeFrame, PinBarMonitoringConfig config)
    {
        // 构建策略实例
        var strategy = BuildPinBarStrategy(config.StrategySettings);

        // 计算需要的历史数据数量
        var maxEma = Math.Max(config.StrategySettings.BaseEma,
            config.StrategySettings.EmaList.Any() ? config.StrategySettings.EmaList.Max() : 0);
        var requiredBars = maxEma * config.HistoryMultiplier;

        // 获取历史数据（返回 AlertCandle）
        var alertCandles = await _marketDataService.GetHistoricalDataAsync(
            symbol,
            timeFrame,
            requiredBars);

        if (alertCandles == null || alertCandles.Count < requiredBars)
        {
            _logger.LogWarning("历史数据不足: {Symbol} {TimeFrame}, 需要: {Required}, 实际: {Actual}",
                symbol, timeFrame, requiredBars, alertCandles?.Count ?? 0);
            return;
        }

        // 转换为 CoreCandle
        var coreCandles = ConvertToCoreCandlesList(alertCandles);

        if (coreCandles.Count < 2)
        {
            _logger.LogWarning("转换后数据不足: {Symbol} {TimeFrame}", symbol, timeFrame);
            return;
        }

        var current = coreCandles[^1];
        var previous = coreCandles[^2];

        // 检查开多信号
        if (strategy.CanOpenLong(current, previous, false))
        {
            await HandleSignalAsync(symbol, timeFrame, "Long", previous, current, strategy, config);
        }

        // 检查开空信号
        if (strategy.CanOpenShort(current, previous, false))
        {
            await HandleSignalAsync(symbol, timeFrame, "Short", previous, current, strategy, config);
        }
    }

    private List<CoreCandle> ConvertToCoreCandlesList(List<AlertCandle> alertCandles)
    {
        return alertCandles.Select(ac => new CoreCandle
        {
            DateTime = ac.Time,
            Open = ac.Open,
            High = ac.High,
            Low = ac.Low,
            Close = ac.Close,
            TickVolume = (long)ac.Volume
        }).ToList();
    }

    private async Task HandleSignalAsync(
        string symbol,
        string timeFrame,
        string direction,
        CoreCandle pinBarCandle,
        CoreCandle currentCandle,
        PinBarStrategy strategy,
        PinBarMonitoringConfig config)
    {
        // 防止重复发送（同一K线只发一次）
        var signalKey = $"{symbol}_{timeFrame}_{direction}_{pinBarCandle.DateTime:yyyyMMddHHmm}";
        if (_lastSignalTimes.TryGetValue(signalKey, out var lastTime))
        {
            if (DateTime.UtcNow - lastTime < TimeSpan.FromMinutes(GetTimeFrameMinutes(timeFrame)))
            {
                return; // 已发送过
            }
        }

        // 查询数据库是否已有记录
        var existingSignal = await _repository.GetLastSignalAsync(
            symbol,
            timeFrame,
            pinBarCandle.DateTime.AddMinutes(-1));

        if (existingSignal != null)
        {
            _logger.LogDebug("信号已存在: {Symbol} {TimeFrame} {Direction} @ {Time}",
                symbol, timeFrame, direction, pinBarCandle.DateTime);
            return;
        }

        // 计算交易参数（简化版本）
        decimal entryPrice, stopLoss, takeProfit, rrRatio;
        rrRatio = config.StrategySettings.RiskRewardRatio;

        if (direction == "Long")
        {
            entryPrice = currentCandle.Close;
            // 简化：止损在PinBar的低点下方
            stopLoss = pinBarCandle.Low - (pinBarCandle.High - pinBarCandle.Low) * config.StrategySettings.StopLossAtrRatio;
            var riskPerTrade = entryPrice - stopLoss;
            takeProfit = entryPrice + (riskPerTrade * rrRatio);
        }
        else // Short
        {
            entryPrice = currentCandle.Close;
            // 简化：止损在PinBar的高点上方
            stopLoss = pinBarCandle.High + (pinBarCandle.High - pinBarCandle.Low) * config.StrategySettings.StopLossAtrRatio;
            var riskPerTrade = stopLoss - entryPrice;
            takeProfit = entryPrice - (riskPerTrade * rrRatio);
        }

        // 获取ADX值（暂时使用0）
        decimal adx = 0m;

        // AI验证信号（如果启用）
        SignalValidation? aiValidation = null;
        if (_aiAnalysisService != null)
        {
            try
            {
                _logger.LogInformation("🤖 开始AI验证信号: {Symbol} {TimeFrame} {Direction}",
                    symbol, timeFrame, direction);

                var tradeDirection = direction == "Long"
                    ? Trading.Data.Models.TradeDirection.Long
                    : Trading.Data.Models.TradeDirection.Short;

                aiValidation = await _aiAnalysisService.ValidatePinBarSignalAsync(
                    symbol: symbol,
                    pinBar: pinBarCandle,
                    direction: tradeDirection
                );

                _logger.LogInformation("✅ AI验证完成: {Symbol} 质量分数={Score}/100, 有效={IsValid}, 风险={Risk}",
                    symbol, aiValidation.QualityScore, aiValidation.IsValid, aiValidation.Risk);

                // 过滤低质量信号（质量分数低于60分）
                if (!aiValidation.IsValid || aiValidation.QualityScore < 60)
                {
                    _logger.LogWarning("⚠️ AI验证未通过，跳过信号: {Symbol} 分数={Score} 原因={Reason}",
                        symbol, aiValidation.QualityScore, aiValidation.Reason);
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ AI验证失败，继续发送信号（降级处理）: {Symbol}", symbol);
                // AI失败不影响信号发送，继续执行
            }
        }

        // 构建消息（包含AI分析）
        var message = BuildSignalMessage(symbol, timeFrame, direction, pinBarCandle,
            entryPrice, stopLoss, takeProfit, rrRatio, adx, aiValidation);

        // 发送Telegram消息
        try
        {
            await _telegramService.SendMessageAsync(message);
            _logger.LogInformation("✅ PinBar信号已发送: {Symbol} {TimeFrame} {Direction}",
                symbol, timeFrame, direction);

            // 记录到数据库
            var signal = new PinBarSignalHistory
            {
                Symbol = symbol,
                TimeFrame = timeFrame,
                SignalTime = DateTime.UtcNow,
                Direction = direction,
                PinBarTime = pinBarCandle.DateTime,
                EntryPrice = entryPrice,
                StopLoss = stopLoss,
                TakeProfit = takeProfit,
                RiskRewardRatio = rrRatio,
                Adx = adx,
                IsSent = true,
                Message = message,
                // AI评分信息
                AiQualityScore = aiValidation?.QualityScore,
                AiRiskLevel = aiValidation?.Risk.ToString(),
                AiValidated = aiValidation?.IsValid,
                AiRecommendation = aiValidation?.Recommendation ?? null
            };

            await _repository.SaveSignalAsync(signal);

            // 更新内存缓存
            _lastSignalTimes[signalKey] = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送PinBar信号失败: {Symbol} {TimeFrame} {Direction}",
                symbol, timeFrame, direction);
        }
    }

    private PinBarStrategy BuildPinBarStrategy(PinBarStrategySettings settings)
    {
        var config = new Trading.Data.Models.StrategyConfig
        {
            StrategyName = settings.StrategyName,
            BaseEma = settings.BaseEma,
            EmaList = settings.EmaList,
            NearEmaThreshold = settings.NearEmaThreshold,
            Threshold = settings.Threshold,
            MinLowerWickAtrRatio = settings.MinLowerWickAtrRatio,
            MaxBodyPercentage = settings.MaxBodyPercentage,
            MinLongerWickPercentage = settings.MinLongerWickPercentage,
            MaxShorterWickPercentage = settings.MaxShorterWickPercentage,
            RequirePinBarDirectionMatch = settings.RequirePinBarDirectionMatch,
            MinAdx = settings.MinAdx,
            LowAdxRiskRewardRatio = settings.LowAdxRiskRewardRatio,
            RiskRewardRatio = settings.RiskRewardRatio,
            NoTradingHoursLimit = settings.NoTradingHoursLimit,
            StartTradingHour = settings.StartTradingHour,
            EndTradingHour = settings.EndTradingHour,
            NoTradeHours = settings.NoTradeHours,
            StopLossStrategy = settings.StopLossStrategy == "PinbarEndPlusAtr"
                ? Trading.Data.Models.StopLossStrategy.PinbarEndPlusAtr
                : Trading.Data.Models.StopLossStrategy.PinbarEndPlusAtr,
            StopLossAtrRatio = settings.StopLossAtrRatio
        };

        return new PinBarStrategy(config);
    }

    private string BuildSignalMessage(
        string symbol,
        string timeFrame,
        string direction,
        CoreCandle pinBarCandle,
        decimal entryPrice,
        decimal stopLoss,
        decimal takeProfit,
        decimal rrRatio,
        decimal adx,
        SignalValidation? aiValidation = null)
    {
        var emoji = direction == "Long" ? "🟢" : "🔴";
        var directionCn = direction == "Long" ? "做多" : "做空";

        var message = $@"{emoji} **PinBar {directionCn}信号**

**品种**: {symbol}
**周期**: {timeFrame}
**信号时间**: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC

📊 **交易参数**:
• 入场价: {entryPrice:F5}
• 止损价: {stopLoss:F5}
• 止盈价: {takeProfit:F5}
• 盈亏比: {rrRatio:F2}
• ADX: {adx:F2}

📍 **PinBar K线**:
• 时间: {pinBarCandle.DateTime:yyyy-MM-dd HH:mm}
• 开盘: {pinBarCandle.Open:F5}
• 最高: {pinBarCandle.High:F5}
• 最低: {pinBarCandle.Low:F5}
• 收盘: {pinBarCandle.Close:F5}";

        // 添加AI分析结果
        if (aiValidation != null)
        {
            var riskEmoji = aiValidation.Risk switch
            {
                Trading.AI.Models.RiskLevel.Low => "🟢",
                Trading.AI.Models.RiskLevel.Medium => "🟡",
                Trading.AI.Models.RiskLevel.High => "🔴",
                _ => "⚪"
            };

            message += $@"

🤖 **AI质量评估**:
• 质量分数: {aiValidation.QualityScore}/100
• 风险等级: {riskEmoji} {aiValidation.Risk}
• AI建议: {aiValidation.Recommendation}
• 分析理由: {aiValidation.Reason}";

            if (!string.IsNullOrEmpty(aiValidation.Details))
            {
                message += $"\n• 详细信息: {aiValidation.Details}";
            }
        }

        message += "\n\n⚠️ 请结合实际市场情况进行判断！";

        return message;
    }

    private int GetTimeFrameMinutes(string timeFrame)
    {
        return timeFrame switch
        {
            "M1" => 1,
            "M5" => 5,
            "M15" => 15,
            "M30" => 30,
            "H1" => 60,
            "H4" => 240,
            "D1" => 1440,
            _ => 5
        };
    }
}
