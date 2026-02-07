using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Trading.AlertSystem.Data.Models;
using Trading.AlertSystem.Data.Repositories;
using Trading.AlertSystem.Data.Services;
using Trading.Core.Strategies;
using Trading.AI.Models;
using Trading.AI.Services;
using CoreCandle = Trading.Data.Models.Candle;

namespace Trading.AlertSystem.Service.Services;

/// <summary>
/// 使用双级AI架构的PinBar监控服务
/// </summary>
public class DualTierPinBarMonitoringService : BackgroundService
{
    private readonly ILogger<DualTierPinBarMonitoringService> _logger;
    private readonly IPinBarMonitorRepository _repository;
    private readonly IMarketDataService _marketDataService;
    private readonly ITelegramService _telegramService;
    private readonly IDualTierAIService? _dualTierAI;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(15); // 每15分钟检查一次
    private readonly Dictionary<string, DateTime> _lastSignalTimes = new();

    public DualTierPinBarMonitoringService(
        ILogger<DualTierPinBarMonitoringService> logger,
        IPinBarMonitorRepository repository,
        IMarketDataService marketDataService,
        ITelegramService telegramService,
        IDualTierAIService? dualTierAI = null)
    {
        _logger = logger;
        _repository = repository;
        _marketDataService = marketDataService;
        _telegramService = telegramService;
        _dualTierAI = dualTierAI;

        if (_dualTierAI != null)
        {
            _logger.LogInformation("✅ 双级AI架构已启用 - 成本优化模式");
        }
        else
        {
            _logger.LogWarning("⚠️ 双级AI未配置，运行在传统模式");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 双级AI PinBar监控服务已启动 - 检查间隔: {Interval}分钟",
            _checkInterval.TotalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckPinBarSignalsWithDualTierAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ PinBar监控检查时发生错误");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("🛑 双级AI PinBar监控服务已停止");
    }

    private async Task CheckPinBarSignalsWithDualTierAsync(CancellationToken stoppingToken)
    {
        var config = await _repository.GetConfigAsync();
        if (config == null || !config.Enabled)
        {
            return;
        }

        _logger.LogDebug("🔍 开始检查PinBar信号 - Symbols: {Symbols}, TimeFrames: {TimeFrames}",
            string.Join(",", config.Symbols), string.Join(",", config.TimeFrames));

        foreach (var symbol in config.Symbols)
        {
            foreach (var timeFrame in config.TimeFrames)
            {
                try
                {
                    await CheckSymbolWithDualTierAsync(symbol, timeFrame, config, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ 检查PinBar信号失败: {Symbol} {TimeFrame}", symbol, timeFrame);
                }
            }
        }

        // 输出今日统计
        if (_dualTierAI != null)
        {
            var usage = _dualTierAI.GetTodayUsageCount();
            var cost = _dualTierAI.GetEstimatedMonthlyCost();
            _logger.LogInformation("📊 今日AI使用统计 - 调用次数: {Count}, 本月成本: ${Cost:F2}",
                usage, cost);
        }
    }

    private async Task CheckSymbolWithDualTierAsync(
        string symbol,
        string timeFrame,
        PinBarMonitoringConfig config,
        CancellationToken stoppingToken)
    {
        // 1. 获取市场数据
        var candles = await FetchMarketDataAsync(symbol, timeFrame, config);
        if (candles == null || candles.Count == 0)
        {
            _logger.LogWarning("⚠️ 无法获取市场数据: {Symbol} {TimeFrame}", symbol, timeFrame);
            return;
        }

        // 2. 检测PinBar信号
        var strategy = BuildPinBarStrategy(config.StrategySettings);

        if (candles.Count < 2)
        {
            _logger.LogWarning("⚠️ K线数据不足: {Symbol} {TimeFrame}", symbol, timeFrame);
            return;
        }

        var current = candles[^1];
        var previous = candles[^2];

        // 检查开多或开空信号
        bool isLongSignal = strategy.CanOpenLong(current, previous, false);
        bool isShortSignal = strategy.CanOpenShort(current, previous, false);

        if (!isLongSignal && !isShortSignal)
        {
            return;
        }

        string direction = isLongSignal ? "Long" : "Short";

        // 3. 检查信号冷却期
        if (IsInCooldownPeriod(symbol, timeFrame))
        {
            _logger.LogDebug("⏰ 信号仍在冷却期: {Symbol} {TimeFrame}", symbol, timeFrame);
            return;
        }

        // 4. 准备双级AI分析的市场数据
        var marketDataForAI = PrepareMarketDataForAI(candles, symbol, timeFrame);

        // 5. 执行双级AI分析
        if (_dualTierAI == null)
        {
            // 降级处理：无AI时直接发送信号
            await SendTraditionalSignalAsync(symbol, timeFrame, direction, previous, config);
            return;
        }

        DualTierAnalysisResult? aiResult;
        try
        {
            aiResult = await _dualTierAI.AnalyzeAsync(marketDataForAI, symbol, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 双级AI分析失败，降级为传统模式");
            await SendTraditionalSignalAsync(symbol, timeFrame, direction, previous, config);
            return;
        }

        // 6. 检查Tier1过滤结果
        if (!aiResult.PassedTier1)
        {
            _logger.LogInformation(
                "🚫 Tier1拦截信号 - {Symbol} {TimeFrame} | Score: {Score}/100 | Reason: {Reason}",
                symbol, timeFrame,
                aiResult.Tier1Result?.OpportunityScore,
                aiResult.Tier1Result?.RejectionReason);
            return;
        }

        // 7. 检查Tier2是否建议入场
        if (!aiResult.ShouldEnter || aiResult.Tier2Result == null)
        {
            _logger.LogInformation(
                "⚠️ Tier2不建议入场 - {Symbol} {TimeFrame} | Action: {Action}",
                symbol, timeFrame, aiResult.Tier2Result?.Action);
            return;
        }

        // 8. 验证风险管理
        if (!ValidateRiskManagement(aiResult.Tier2Result))
        {
            _logger.LogWarning(
                "⚠️ 风险管理验证失败 - {Symbol} {TimeFrame} | Risk: ${Risk:F2}",
                symbol, timeFrame, aiResult.Tier2Result.RiskAmountUsd);
            return;
        }

        // 9. 构建并发送消息
        var message = TradingMessageBuilder.BuildDualTierSignalMessage(
            symbol, timeFrame, direction, previous, aiResult);

        try
        {
            await _telegramService.SendMessageAsync(message);

            _logger.LogInformation(
                "✅ 双级AI验证通过，信号已发送 - {Symbol} {TimeFrame} | " +
                "Tier1Score: {T1Score} | Action: {Action} | Entry: {Entry} | Cost: ${Cost:F4}",
                symbol, timeFrame,
                aiResult.Tier1Result?.OpportunityScore,
                aiResult.Tier2Result.Action,
                aiResult.Tier2Result.EntryPrice,
                aiResult.TotalCostUsd);

            // 记录信号时间
            RecordSignalTime(symbol, timeFrame);

            // 保存到数据库
            await SaveSignalToDatabase(symbol, timeFrame, direction, previous,
                aiResult, config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 发送Telegram消息失败");
        }
    }

    private async Task<List<CoreCandle>> FetchMarketDataAsync(
        string symbol,
        string timeFrame,
        PinBarMonitoringConfig config)
    {
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
            return new List<CoreCandle>();
        }

        // 转换为CoreCandle
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

    private string PrepareMarketDataForAI(List<CoreCandle> candles, string symbol, string timeFrame)
    {
        // 压缩市场数据为CSV格式
        var csvData = "DateTime,Open,High,Low,Close,TickVolume\n";

        // 只取最近100根K线以节省Token
        var recentCandles = candles.TakeLast(100);

        foreach (var candle in recentCandles)
        {
            csvData += $"{candle.DateTime:yyyy-MM-dd HH:mm},{candle.Open:F5}," +
                      $"{candle.High:F5},{candle.Low:F5},{candle.Close:F5},{candle.TickVolume}\n";
        }

        return csvData;
    }

    private bool ValidateRiskManagement(Tier2AnalysisResult tier2Result)
    {
        // 验证单笔风险不超过$40
        if (tier2Result.RiskAmountUsd.HasValue && tier2Result.RiskAmountUsd > 40m)
        {
            return false;
        }

        // 验证风险回报比至少1.5:1
        if (tier2Result.RiskRewardRatio.HasValue && tier2Result.RiskRewardRatio < 1.5m)
        {
            return false;
        }

        // 验证必要的价格信息存在
        if (!tier2Result.EntryPrice.HasValue ||
            !tier2Result.StopLoss.HasValue ||
            !tier2Result.TakeProfit.HasValue)
        {
            return false;
        }

        return true;
    }

    private async Task SendTraditionalSignalAsync(
        string symbol,
        string timeFrame,
        string direction,
        CoreCandle pinBar,
        PinBarMonitoringConfig config)
    {
        // 降级处理：使用传统方式计算交易参数
        var isLong = direction == "Long";
        var entryPrice = isLong ? pinBar.High : pinBar.Low;
        var stopLoss = isLong ? pinBar.Low : pinBar.High;
        var rrRatio = config.StrategySettings.RiskRewardRatio;
        var riskPips = Math.Abs(entryPrice - stopLoss);
        var takeProfit = isLong
            ? entryPrice + (riskPips * rrRatio)
            : entryPrice - (riskPips * rrRatio);

        var emoji = direction == "Long" ? "🟢" : "🔴";
        var message = $@"{emoji} **PinBar {direction}信号 [传统模式]**

**品种**: {symbol}
**周期**: {timeFrame}
**信号时间**: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC

📊 **交易参数**:
• 入场价: {entryPrice:F5}
• 止损价: {stopLoss:F5}
• 止盈价: {takeProfit:F5}
• 盈亏比: {rrRatio:F2}

⚠️ AI分析未启用，请手动验证信号质量！";

        await _telegramService.SendMessageAsync(message);
        RecordSignalTime(symbol, timeFrame);
    }

    private async Task SaveSignalToDatabase(
        string symbol,
        string timeFrame,
        string direction,
        CoreCandle pinBar,
        DualTierAnalysisResult aiResult,
        PinBarMonitoringConfig config)
    {
        try
        {
            var signal = new PinBarSignalHistory
            {
                Symbol = symbol,
                TimeFrame = timeFrame,
                Direction = direction,
                SignalTime = DateTime.UtcNow,
                PinBarTime = pinBar.DateTime,
                EntryPrice = aiResult.Tier2Result?.EntryPrice ?? 0m,
                StopLoss = aiResult.Tier2Result?.StopLoss ?? 0m,
                TakeProfit = aiResult.Tier2Result?.TakeProfit ?? 0m,
                RiskRewardRatio = aiResult.Tier2Result?.RiskRewardRatio ?? 0m,
                Adx = 0m,
                IsSent = true,
                Message = null,
                AiQualityScore = aiResult.Tier1Result?.OpportunityScore,
                AiRiskLevel = null,
                AiValidated = true,
                AiRecommendation = aiResult.Tier2Result?.Reasoning
            };

            await _repository.SaveSignalAsync(signal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存信号到数据库失败");
        }
    }

    private bool IsInCooldownPeriod(string symbol, string timeFrame)
    {
        var key = $"{symbol}_{timeFrame}";
        if (!_lastSignalTimes.TryGetValue(key, out var lastTime))
        {
            return false;
        }

        var cooldownMinutes = GetTimeFrameMinutes(timeFrame) * 4; // 4个周期冷却
        return DateTime.UtcNow < lastTime.AddMinutes(cooldownMinutes);
    }

    private void RecordSignalTime(string symbol, string timeFrame)
    {
        var key = $"{symbol}_{timeFrame}";
        _lastSignalTimes[key] = DateTime.UtcNow;
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
            _ => 15
        };
    }
}
