using Microsoft.Extensions.Logging;
using Skender.Stock.Indicators;
using System.Text.Json;
using Trading.AlertSystem.Data.Models;
using Trading.AlertSystem.Data.Repositories;
using Trading.AlertSystem.Data.Services;
using Trading.AlertSystem.Service.Configuration;
using Trading.AlertSystem.Service.Models;

namespace Trading.AlertSystem.Service.Services;

/// <summary>
/// EMA监测服务实现
/// </summary>
public class EmaMonitoringService : IEmaMonitoringService
{
    private readonly ITradeLockerService _tradeLockerService;
    private readonly ITelegramService _telegramService;
    private readonly IAlertHistoryRepository _alertHistoryRepository;
    private readonly EmaMonitoringSettings _settings;
    private readonly ILogger<EmaMonitoringService> _logger;

    // 内存中存储监测状态
    private readonly Dictionary<string, EmaMonitoringState> _states = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _isRunning = false;

    public EmaMonitoringService(
        ITradeLockerService tradeLockerService,
        ITelegramService telegramService,
        IAlertHistoryRepository alertHistoryRepository,
        EmaMonitoringSettings settings,
        ILogger<EmaMonitoringService> logger)
    {
        _tradeLockerService = tradeLockerService;
        _telegramService = telegramService;
        _alertHistoryRepository = alertHistoryRepository;
        _settings = settings;
        _logger = logger;
    }

    public Task StartAsync()
    {
        _logger.LogInformation("启动EMA监测服务");
        _isRunning = true;
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        _logger.LogInformation("停止EMA监测服务");
        _isRunning = false;
        return Task.CompletedTask;
    }

    public async Task CheckAsync()
    {
        if (!_settings.Enabled || !_isRunning)
        {
            return;
        }

        await _semaphore.WaitAsync();
        try
        {
            _logger.LogInformation("开始EMA监测检查");

            // 确保已连接到TradeLocker
            await _tradeLockerService.ConnectAsync();

            // 遍历所有配置的品种、周期和EMA周期组合
            foreach (var symbol in _settings.Symbols)
            {
                foreach (var timeFrame in _settings.TimeFrames)
                {
                    foreach (var emaPeriod in _settings.EmaPeriods)
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

            _logger.LogInformation("EMA监测检查完成");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task CheckEmaCrossAsync(string symbol, string timeFrame, int emaPeriod)
    {
        var stateId = $"{symbol}_{timeFrame}_EMA{emaPeriod}";

        // 获取历史数据（需要足够多的K线来计算EMA）
        var barsNeeded = emaPeriod * _settings.HistoryMultiplier;
        var candles = await _tradeLockerService.GetHistoricalDataAsync(symbol, timeFrame, barsNeeded);

        if (!candles.Any())
        {
            _logger.LogWarning("未能获取 {Symbol} {TimeFrame} 的历史数据", symbol, timeFrame);
            return;
        }

        var candleList = candles.OrderBy(c => c.Time).ToList();

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

                // 发送通知
                var message = crossEvent.FormatMessage();
                await _telegramService.SendMessageAsync(message);

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
        state.LastCheckTime = DateTime.UtcNow;

        _logger.LogDebug("{Symbol} {TimeFrame} EMA{Period}: Close={Close:F4}, EMA={Ema:F4}, Position={Position}",
            symbol, timeFrame, emaPeriod, currentCandle.Close, currEmaValue,
            currentPosition > 0 ? "Above" : "Below");
    }

    public Task<IEnumerable<EmaMonitoringState>> GetStatesAsync()
    {
        return Task.FromResult(_states.Values.AsEnumerable());
    }
}
