using Microsoft.Extensions.Logging;
using Skender.Stock.Indicators;
using System.Text.Json;
using Trading.Infras.Data.Models;
using Trading.Infras.Data.Repositories;
using Trading.Infras.Data.Services;
using Trading.Infras.Service.Models;

namespace Trading.Infras.Service.Services;

/// <summary>
/// EMA监测服务实现
/// </summary>
public class EmaMonitoringService : IEmaMonitoringService
{
    private readonly IMarketDataService _marketDataService;
    private readonly ITelegramService _telegramService;
    private readonly IAlertHistoryRepository _alertHistoryRepository;
    private readonly IEmaMonitorRepository _emaMonitorRepository;
    private readonly IChartService _chartService;
    private readonly ILogger<EmaMonitoringService> _logger;

    // 内存中存储监测状态和配置
    private readonly Dictionary<string, EmaMonitoringState> _states = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _isRunning = false;
    private EmaMonitoringConfig? _currentConfig;

    public EmaMonitoringService(
        IMarketDataService marketDataService,
        ITelegramService telegramService,
        IAlertHistoryRepository alertHistoryRepository,
        IEmaMonitorRepository emaMonitorRepository,
        IChartService chartService,
        ILogger<EmaMonitoringService> logger)
    {
        _marketDataService = marketDataService;
        _telegramService = telegramService;
        _alertHistoryRepository = alertHistoryRepository;
        _emaMonitorRepository = emaMonitorRepository;
        _chartService = chartService;
        _logger = logger;
    }

    public async Task StartAsync()
    {
        _logger.LogInformation("启动EMA监测服务");
        await ReloadConfigAsync();
        _isRunning = true;
    }

    public Task StopAsync()
    {
        _logger.LogInformation("停止EMA监测服务");
        _isRunning = false;
        return Task.CompletedTask;
    }

    public async Task CheckAsync()
    {
        if (_currentConfig == null || !_currentConfig.Enabled || !_isRunning)
        {
            return;
        }

        await _semaphore.WaitAsync();
        try
        {
            _logger.LogInformation("开始EMA监测检查");

            // 确保已连接到市场数据服务
            await _marketDataService.ConnectAsync();

            // 遍历所有配置的品种、周期和EMA周期组合
            foreach (var symbol in _currentConfig.Symbols)
            {
                foreach (var timeFrame in _currentConfig.TimeFrames)
                {
                    foreach (var emaPeriod in _currentConfig.EmaPeriods)
                    {
                        try
                        {
                            await CheckEmaCrossAsync(symbol, timeFrame, emaPeriod);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "检查 {Symbol} {TimeFrame} EMA{Period} 时发生错误",
                                symbol, timeFrame, emaPeriod);
                        }
                    }
                }
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task CheckEmaCrossAsync(string symbol, string timeFrame, int emaPeriod)
    {
        if (_currentConfig == null) return;

        var stateId = $"{symbol}_{timeFrame}_EMA{emaPeriod}";

        // 获取历史数据（需要足够多的K线来计算EMA）
        var barsNeeded = emaPeriod * _currentConfig.HistoryMultiplier;
        var candles = await _marketDataService.GetHistoricalDataAsync(symbol, timeFrame, barsNeeded);

        if (!candles.Any())
        {
            _logger.LogWarning("未能获取 {Symbol} {TimeFrame} 的历史数据", symbol, timeFrame);
            return;
        }

        var candleList = candles.OrderBy(c => c.Time).ToList();
        var latestCandle = candleList[^1];

        // 检查是否应该处理这根K线（避免重复处理同一根K线）
        if (_states.TryGetValue(stateId, out var existingState))
        {
            // 如果最新K线的时间与上次检查的时间相同，说明是同一根K线，跳过
            if (existingState.LastCandleTime == latestCandle.Time)
            {
                _logger.LogDebug("跳过 {Symbol} {TimeFrame} EMA{Period} 检查（K线未更新）",
                    symbol, timeFrame, emaPeriod);
                return;
            }
        }

        // 至少需要 emaPeriod + 1 根K线
        if (candleList.Count < emaPeriod + 1)
        {
            _logger.LogWarning("{Symbol} {TimeFrame} 历史数据不足，需要至少 {Required} 根K线，实际获取 {Actual} 根",
                symbol, timeFrame, emaPeriod + 1, candleList.Count);
            return;
        }

        // 计算EMA
        var quotes = candleList.Select(c => new Quote
        {
            Date = c.Time,
            Open = c.Open,
            High = c.High,
            Low = c.Low,
            Close = c.Close,
            Volume = c.Volume
        }).ToList();

        var emaResults = quotes.GetEma(emaPeriod).ToList();

        // 获取最新两根K线的数据
        var previousCandle = candleList[^2]; // 倒数第二根（已收盘）
        var currentCandle = candleList[^1];  // 最新一根（当前）

        var previousEma = emaResults[^2].Ema;
        var currentEma = emaResults[^1].Ema;

        if (previousEma == null || currentEma == null)
        {
            _logger.LogDebug("{Symbol} {TimeFrame} EMA{Period} 尚未计算完成", symbol, timeFrame, emaPeriod);
            return;
        }

        var prevEmaValue = (decimal)previousEma.Value;
        var currEmaValue = (decimal)currentEma.Value;

        // 判断前一根K线价格相对EMA的位置
        var previousPosition = previousCandle.Close > prevEmaValue ? 1 : -1;
        var currentPosition = currentCandle.Close > currEmaValue ? 1 : -1;

        // 如果有已存储的状态，检查是否发生穿越
        if (_states.TryGetValue(stateId, out var state))
        {
            // 检测穿越：位置发生变化
            if (state.LastPosition != 0 && state.LastPosition != currentPosition)
            {
                var crossEvent = new EmaCrossEvent
                {
                    Symbol = symbol,
                    TimeFrame = timeFrame,
                    EmaPeriod = emaPeriod,
                    CurrentClose = currentCandle.Close,
                    CurrentEmaValue = currEmaValue,
                    CrossType = currentPosition > 0 ? CrossType.CrossAbove : CrossType.CrossBelow,
                    EventTime = currentCandle.Time
                };

                _logger.LogInformation("检测到EMA穿越: {Message}", crossEvent.FormatMessage());

                // 发送通知（带图片）
                var message = crossEvent.FormatMessage();

                try
                {
                    // 获取4个时间周期的K线数据用于生成图表
                    var candlesM5 = await _marketDataService.GetHistoricalDataAsync(
                        symbol, "M5", _currentConfig.HistoryMultiplier * 20);
                    var candlesM15 = await _marketDataService.GetHistoricalDataAsync(
                        symbol, "M15", _currentConfig.HistoryMultiplier * 20);
                    var candlesH1 = await _marketDataService.GetHistoricalDataAsync(
                        symbol, "H1", _currentConfig.HistoryMultiplier * 20);
                    var candlesH4 = await _marketDataService.GetHistoricalDataAsync(
                        symbol, "H4", _currentConfig.HistoryMultiplier * 20);

                    // 生成图表
                    using var chartStream = await _chartService.GenerateMultiTimeFrameChartAsync(
                        symbol,
                        candlesM5,
                        candlesM15,
                        candlesH1,
                        candlesH4,
                        20  // 固定使用EMA20
                    );

                    // 发送图片和文字说明
                    await _telegramService.SendPhotoAsync(chartStream, message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "生成或发送图表失败，仅发送文字消息");
                    await _telegramService.SendMessageAsync(message);
                }

                // 保存告警历史
                var alertHistory = new AlertHistory
                {
                    Type = AlertHistoryType.EmaCross,
                    Symbol = symbol,
                    AlertTime = crossEvent.EventTime,
                    Message = message,
                    Details = JsonSerializer.Serialize(new EmaCrossAlertDetails
                    {
                        TimeFrame = timeFrame,
                        EmaPeriod = emaPeriod,
                        ClosePrice = crossEvent.CurrentClose,
                        EmaValue = crossEvent.CurrentEmaValue,
                        CrossType = crossEvent.CrossType == CrossType.CrossAbove ? "CrossAbove" : "CrossBelow"
                    }),
                    IsSent = true
                };

                try
                {
                    await _alertHistoryRepository.AddAsync(alertHistory);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "保存EMA告警历史失败");
                    // 不影响主流程，继续执行
                }

                // 更新状态
                state.LastNotificationTime = DateTime.UtcNow;
            }
        }
        else
        {
            // 首次监测，创建状态
            state = new EmaMonitoringState
            {
                Symbol = symbol,
                TimeFrame = timeFrame,
                EmaPeriod = emaPeriod
            };
            _states[stateId] = state;
        }

        // 更新状态
        state.LastClose = currentCandle.Close;
        state.LastEmaValue = currEmaValue;
        state.LastPosition = currentPosition;
        state.LastCandleTime = currentCandle.Time;
        state.LastCheckTime = DateTime.UtcNow;

        _logger.LogDebug("{Symbol} {TimeFrame} EMA{Period}: Close={Close:F4}, EMA={Ema:F4}, Position={Position}, CandleTime={CandleTime}",
            symbol, timeFrame, emaPeriod, currentCandle.Close, currEmaValue,
            currentPosition > 0 ? "Above" : "Below", currentCandle.Time.ToString("yyyy-MM-dd HH:mm:ss"));
    }

    public Task<IEnumerable<EmaMonitoringState>> GetStatesAsync()
    {
        return Task.FromResult(_states.Values.AsEnumerable());
    }

    public async Task ReloadConfigAsync()
    {
        try
        {
            var config = await _emaMonitorRepository.GetConfigAsync();
            if (config == null)
            {
                _logger.LogWarning("未找到EMA配置，将使用禁用状态");
                _currentConfig = new EmaMonitoringConfig
                {
                    Id = "default",
                    Enabled = false,
                    Symbols = new List<string>(),
                    TimeFrames = new List<string>(),
                    EmaPeriods = new List<int>(),
                    HistoryMultiplier = 3,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = "System"
                };
            }
            else
            {
                _currentConfig = config;
                _logger.LogInformation("已加载EMA配置：Enabled={Enabled}, Symbols={SymbolCount}, TimeFrames={TimeFrameCount}, EmaPeriods={EmaCount}",
                    config.Enabled, config.Symbols.Count, config.TimeFrames.Count, config.EmaPeriods.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重新加载EMA配置失败");
            throw;
        }
    }

    public bool IsEnabled()
    {
        return _currentConfig?.Enabled ?? false;
    }
}
