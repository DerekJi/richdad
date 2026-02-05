using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;
using Trading.AlertSystem.Data.Models;
using Trading.AlertSystem.Data.Repositories;
using Trading.AlertSystem.Data.Services;
using Trading.AlertSystem.Service.Configuration;
using Trading.AlertSystem.Service.Repositories;

namespace Trading.AlertSystem.Service.Services;

/// <summary>
/// 基于 Streaming 的实时价格监控服务
/// 使用 OANDA Streaming API 实现毫秒级价格告警
/// </summary>
public class StreamingPriceMonitorService : IStreamingPriceMonitorService
{
    private readonly IPriceAlertRepository _alertRepository;
    private readonly IAlertHistoryRepository _alertHistoryRepository;
    private readonly IOandaStreamingService _streamingService;
    private readonly ITelegramService _telegramService;
    private readonly MonitoringSettings _settings;
    private readonly ILogger<StreamingPriceMonitorService> _logger;

    // 缓存已触发的告警，避免重复触发
    private readonly ConcurrentDictionary<string, DateTime> _triggeredAlerts = new();

    // 缓存告警列表，定期刷新
    private List<PriceAlert> _cachedAlerts = new();
    private DateTime _lastAlertRefresh = DateTime.MinValue;
    private readonly TimeSpan _alertRefreshInterval = TimeSpan.FromSeconds(30);

    private bool _isRunning;

    public StreamingPriceMonitorService(
        IPriceAlertRepository alertRepository,
        IAlertHistoryRepository alertHistoryRepository,
        IOandaStreamingService streamingService,
        ITelegramService telegramService,
        MonitoringSettings settings,
        ILogger<StreamingPriceMonitorService> logger)
    {
        _alertRepository = alertRepository;
        _alertHistoryRepository = alertHistoryRepository;
        _streamingService = streamingService;
        _telegramService = telegramService;
        _settings = settings;
        _logger = logger;
    }

    public async Task StartAsync()
    {
        if (_isRunning)
        {
            _logger.LogWarning("Streaming 价格监控已在运行中");
            return;
        }

        _logger.LogInformation("启动 Streaming 价格监控服务");

        // 加载告警并订阅价格
        await RefreshAlertsAndSubscribeAsync();

        // 订阅价格更新事件
        _streamingService.OnPriceUpdate += OnPriceUpdateAsync;
        _streamingService.OnConnectionStatusChanged += OnConnectionStatusChanged;

        _isRunning = true;
    }

    public async Task StopAsync()
    {
        if (!_isRunning)
        {
            return;
        }

        _logger.LogInformation("停止 Streaming 价格监控服务");

        _streamingService.OnPriceUpdate -= OnPriceUpdateAsync;
        _streamingService.OnConnectionStatusChanged -= OnConnectionStatusChanged;

        await _streamingService.StopStreamingAsync();
        _isRunning = false;
    }

    public bool IsRunning => _isRunning;

    /// <summary>
    /// 刷新告警列表并更新订阅
    /// </summary>
    public async Task RefreshAlertsAsync()
    {
        await RefreshAlertsAndSubscribeAsync();
    }

    private async Task RefreshAlertsAndSubscribeAsync()
    {
        try
        {
            // 获取所有启用的固定价格告警
            var allAlerts = await _alertRepository.GetAllAsync();
            _cachedAlerts = allAlerts
                .Where(a => a.Enabled && !a.IsTriggered && a.Type == AlertType.FixedPrice)
                .ToList();

            _lastAlertRefresh = DateTime.UtcNow;

            if (_cachedAlerts.Count == 0)
            {
                _logger.LogInformation("没有需要监控的固定价格告警");
                await _streamingService.StopStreamingAsync();
                return;
            }

            // 获取需要订阅的品种
            var symbols = _cachedAlerts.Select(a => a.Symbol).Distinct().ToList();

            _logger.LogInformation("监控 {Count} 个固定价格告警，品种: {Symbols}",
                _cachedAlerts.Count, string.Join(", ", symbols));

            // 更新订阅
            if (_streamingService.IsRunning)
            {
                await _streamingService.UpdateSymbolsAsync(symbols);
            }
            else
            {
                await _streamingService.StartStreamingAsync(symbols);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刷新告警列表失败");
        }
    }

    private async void OnPriceUpdateAsync(object? sender, PriceUpdateEventArgs e)
    {
        try
        {
            // 定期刷新告警列表
            if (DateTime.UtcNow - _lastAlertRefresh > _alertRefreshInterval)
            {
                await RefreshAlertsAndSubscribeAsync();
            }

            // 检查该品种的所有告警
            var alertsForSymbol = _cachedAlerts
                .Where(a => a.Symbol.Equals(e.Symbol, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var alert in alertsForSymbol)
            {
                await CheckAndTriggerAlertAsync(alert, e.MidPrice, e.Timestamp);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理价格更新时发生错误: {Symbol}", e.Symbol);
        }
    }

    private async Task CheckAndTriggerAlertAsync(PriceAlert alert, decimal currentPrice, DateTime timestamp)
    {
        if (!alert.TargetPrice.HasValue)
        {
            return;
        }

        // 检查是否已触发（防止短时间内重复触发）
        if (_triggeredAlerts.TryGetValue(alert.Id, out var lastTriggered))
        {
            if (DateTime.UtcNow - lastTriggered < TimeSpan.FromMinutes(1))
            {
                return;
            }
        }

        var targetPrice = alert.TargetPrice.Value;
        var isTriggered = false;

        if (alert.Direction == PriceDirection.Above)
        {
            // 上穿：当前价格 >= 目标价格
            isTriggered = currentPrice >= targetPrice;
        }
        else
        {
            // 下穿：当前价格 <= 目标价格
            isTriggered = currentPrice <= targetPrice;
        }

        if (!isTriggered)
        {
            return;
        }

        _logger.LogInformation("🔔 触发价格告警: {Name} - {Symbol} {Direction} {Target}, 当前: {Current}",
            alert.Name, alert.Symbol,
            alert.Direction == PriceDirection.Above ? "上穿" : "下穿",
            targetPrice, currentPrice);

        // 标记为已触发
        _triggeredAlerts[alert.Id] = DateTime.UtcNow;

        // 发送通知
        var message = FormatMessage(alert, currentPrice, targetPrice);
        await _telegramService.SendFormattedMessageAsync(message, alert.TelegramChatId);

        // 保存告警历史
        await SaveAlertHistoryAsync(alert, currentPrice, targetPrice, message);

        // 更新数据库中的告警状态
        await _alertRepository.MarkAsTriggeredAsync(alert.Id);

        // 从缓存中移除
        _cachedAlerts.RemoveAll(a => a.Id == alert.Id);
    }

    private string FormatMessage(PriceAlert alert, decimal currentPrice, decimal targetPrice)
    {
        if (!string.IsNullOrEmpty(alert.MessageTemplate))
        {
            return alert.MessageTemplate
                .Replace("{Symbol}", alert.Symbol)
                .Replace("{Name}", alert.Name)
                .Replace("{Price}", currentPrice.ToString())
                .Replace("{Target}", targetPrice.ToString())
                .Replace("{Direction}", alert.Direction == PriceDirection.Above ? "上穿" : "下穿")
                .Replace("{Time}", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
        }

        return $"🔔 价格提示\n\n" +
               $"品种: {alert.Symbol}\n" +
               $"名称: {alert.Name}\n" +
               $"事件: 价格{(alert.Direction == PriceDirection.Above ? "上穿" : "下穿")} {targetPrice}\n" +
               $"当前价格: {currentPrice}\n" +
               $"时间: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";
    }

    private async Task SaveAlertHistoryAsync(PriceAlert alert, decimal currentPrice, decimal targetPrice, string message)
    {
        try
        {
            var history = new AlertHistory
            {
                Type = AlertHistoryType.PriceAlert,
                Symbol = alert.Symbol,
                AlertTime = DateTime.UtcNow,
                Message = message,
                Details = JsonSerializer.Serialize(new
                {
                    AlertId = alert.Id,
                    AlertName = alert.Name,
                    TargetPrice = targetPrice,
                    CurrentPrice = currentPrice,
                    Direction = alert.Direction.ToString(),
                    Source = "Streaming"
                }),
                IsSent = true
            };

            await _alertHistoryRepository.AddAsync(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存告警历史失败");
        }
    }

    private void OnConnectionStatusChanged(object? sender, bool connected)
    {
        if (connected)
        {
            _logger.LogInformation("✅ Streaming 价格监控连接已建立");
        }
        else
        {
            _logger.LogWarning("⚠️ Streaming 价格监控连接断开");
        }
    }
}

/// <summary>
/// Streaming 价格监控服务接口
/// </summary>
public interface IStreamingPriceMonitorService
{
    Task StartAsync();
    Task StopAsync();
    Task RefreshAlertsAsync();
    bool IsRunning { get; }
}
