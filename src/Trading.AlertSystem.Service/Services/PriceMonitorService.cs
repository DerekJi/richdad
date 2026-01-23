using Microsoft.Extensions.Logging;
using Skender.Stock.Indicators;
using Trading.AlertSystem.Data.Models;
using Trading.AlertSystem.Data.Services;
using Trading.AlertSystem.Service.Configuration;
using Trading.AlertSystem.Service.Repositories;

namespace Trading.AlertSystem.Service.Services;

/// <summary>
/// 价格监控服务实现
/// </summary>
public class PriceMonitorService : IPriceMonitorService
{
    private readonly IPriceAlertRepository _alertRepository;
    private readonly ITradeLockerService _tradeLockerService;
    private readonly ITelegramService _telegramService;
    private readonly MonitoringSettings _settings;
    private readonly ILogger<PriceMonitorService> _logger;
    private Timer? _timer;
    private bool _isRunning;

    public PriceMonitorService(
        IPriceAlertRepository alertRepository,
        ITradeLockerService tradeLockerService,
        ITelegramService telegramService,
        MonitoringSettings settings,
        ILogger<PriceMonitorService> logger)
    {
        _alertRepository = alertRepository;
        _tradeLockerService = tradeLockerService;
        _telegramService = telegramService;
        _settings = settings;
        _logger = logger;
    }

    public async Task StartAsync()
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("价格监控已禁用");
            return;
        }

        if (_isRunning)
        {
            _logger.LogWarning("价格监控已经在运行中");
            return;
        }

        _logger.LogInformation("启动价格监控服务，间隔: {Interval}秒", _settings.IntervalSeconds);

        _isRunning = true;

        // 如果配置为启动时执行，立即执行一次
        if (_settings.RunOnStartup)
        {
            _ = Task.Run(ExecuteCheckAsync);
        }

        // 启动定时器
        _timer = new Timer(
            async _ => await ExecuteCheckAsync(),
            null,
            TimeSpan.FromSeconds(_settings.IntervalSeconds),
            TimeSpan.FromSeconds(_settings.IntervalSeconds)
        );

        await Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        _logger.LogInformation("停止价格监控服务");
        _isRunning = false;
        _timer?.Dispose();
        await Task.CompletedTask;
    }

    public async Task ExecuteCheckAsync()
    {
        if (!_isRunning)
            return;

        try
        {
            _logger.LogDebug("开始执行价格监控检查");

            var alerts = await _alertRepository.GetEnabledAlertsAsync();
            var alertList = alerts.ToList();

            if (!alertList.Any())
            {
                _logger.LogDebug("没有启用的告警");
                return;
            }

            _logger.LogInformation("检查 {Count} 个告警", alertList.Count);

            // 按品种分组，批量获取价格
            var symbols = alertList.Select(a => a.Symbol).Distinct().ToList();

            // 并行检查告警（限制并发数）
            var semaphore = new SemaphoreSlim(_settings.MaxConcurrency);
            var tasks = alertList.Select(async alert =>
            {
                await semaphore.WaitAsync();
                try
                {
                    await CheckAlertAsync(alert);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);

            _logger.LogDebug("价格监控检查完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行价格监控检查时发生错误");
        }
    }

    public async Task<bool> CheckAlertAsync(PriceAlert alert)
    {
        try
        {
            _logger.LogDebug("检查告警: {AlertName} ({Symbol})", alert.Name, alert.Symbol);

            // 获取当前价格
            var currentPrice = await _tradeLockerService.GetSymbolPriceAsync(alert.Symbol);
            if (currentPrice == null)
            {
                _logger.LogWarning("无法获取 {Symbol} 的价格", alert.Symbol);
                return false;
            }

            decimal targetValue;
            string targetDescription;

            // 根据告警类型计算目标值
            switch (alert.Type)
            {
                case AlertType.FixedPrice:
                    if (!alert.TargetPrice.HasValue)
                    {
                        _logger.LogWarning("告警 {AlertId} 未设置目标价格", alert.Id);
                        return false;
                    }
                    targetValue = alert.TargetPrice.Value;
                    targetDescription = $"目标价格 {targetValue}";
                    break;

                case AlertType.EMA:
                    if (!alert.EmaPeriod.HasValue)
                    {
                        _logger.LogWarning("告警 {AlertId} 未设置EMA周期", alert.Id);
                        return false;
                    }
                    targetValue = await CalculateEmaAsync(alert.Symbol, alert.TimeFrame, alert.EmaPeriod.Value);
                    targetDescription = $"EMA({alert.EmaPeriod}) {targetValue}";
                    break;

                case AlertType.MA:
                    if (!alert.MaPeriod.HasValue)
                    {
                        _logger.LogWarning("告警 {AlertId} 未设置MA周期", alert.Id);
                        return false;
                    }
                    targetValue = await CalculateMaAsync(alert.Symbol, alert.TimeFrame, alert.MaPeriod.Value);
                    targetDescription = $"MA({alert.MaPeriod}) {targetValue}";
                    break;

                default:
                    _logger.LogWarning("不支持的告警类型: {Type}", alert.Type);
                    return false;
            }

            // 检查是否触发条件
            bool isTriggered = alert.Direction switch
            {
                PriceDirection.Above => currentPrice.LastPrice >= targetValue,
                PriceDirection.Below => currentPrice.LastPrice <= targetValue,
                _ => false
            };

            if (isTriggered)
            {
                _logger.LogInformation("告警触发: {AlertName} - 当前价格 {Price} {Direction} {Target}",
                    alert.Name, currentPrice.LastPrice,
                    alert.Direction == PriceDirection.Above ? "上穿" : "下穿",
                    targetDescription);

                // 发送通知
                var message = FormatMessage(alert, currentPrice.LastPrice, targetValue, targetDescription);
                await _telegramService.SendFormattedMessageAsync(message, alert.TelegramChatId);

                // 标记为已触发
                await _alertRepository.MarkAsTriggeredAsync(alert.Id);

                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查告警 {AlertId} 时发生错误", alert.Id);
            return false;
        }
    }

    private async Task<decimal> CalculateEmaAsync(string symbol, string timeFrame, int period)
    {
        try
        {
            var candles = await _tradeLockerService.GetHistoricalDataAsync(symbol, timeFrame, period + 50);
            if (!candles.Any())
                return 0;

            var quotes = candles.Select(c => new Quote
            {
                Date = c.Time,
                Open = c.Open,
                High = c.High,
                Low = c.Low,
                Close = c.Close,
                Volume = c.Volume
            });

            var emaResults = quotes.GetEma(period).ToList();
            return (decimal)(emaResults.LastOrDefault()?.Ema ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "计算EMA时发生错误");
            return 0;
        }
    }

    private async Task<decimal> CalculateMaAsync(string symbol, string timeFrame, int period)
    {
        try
        {
            var candles = await _tradeLockerService.GetHistoricalDataAsync(symbol, timeFrame, period + 10);
            if (!candles.Any())
                return 0;

            var quotes = candles.Select(c => new Quote
            {
                Date = c.Time,
                Open = c.Open,
                High = c.High,
                Low = c.Low,
                Close = c.Close,
                Volume = c.Volume
            });

            var smaResults = quotes.GetSma(period).ToList();
            return (decimal)(smaResults.LastOrDefault()?.Sma ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "计算MA时发生错误");
            return 0;
        }
    }

    private string FormatMessage(PriceAlert alert, decimal currentPrice, decimal targetValue, string targetDescription)
    {
        var directionText = alert.Direction == PriceDirection.Above ? "上穿" : "下穿";

        // 如果有自定义模板，使用模板
        if (!string.IsNullOrEmpty(alert.MessageTemplate))
        {
            return alert.MessageTemplate
                .Replace("{Symbol}", alert.Symbol)
                .Replace("{Name}", alert.Name)
                .Replace("{Price}", currentPrice.ToString("F5"))
                .Replace("{Target}", targetDescription)
                .Replace("{Direction}", directionText)
                .Replace("{Time}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        }

        // 默认消息格式
        return $@"🔔 **价格告警触发**

📊 **品种**: {alert.Symbol}
📝 **名称**: {alert.Name}
💰 **当前价格**: {currentPrice:F5}
🎯 **{directionText}**: {targetDescription}
⏰ **时间**: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
    }
}
