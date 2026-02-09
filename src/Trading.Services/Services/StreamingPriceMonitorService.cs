using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;
using Trading.Infrastructure.Models;
using Trading.Infrastructure.Repositories;
using Trading.Infrastructure.Services;
using Trading.Services.Configuration;
using Trading.Services.Repositories;

namespace Trading.Services.Services;

/// <summary>
/// 基于 Streaming 的实时价格监控服务
/// 使用 OANDA Streaming API 实现毫秒级价格告警
/// </summary>
public class StreamingPriceMonitorService : IStreamingPriceMonitorService
{
    private readonly IPriceMonitorRepository _repository;
    private readonly IAlertHistoryRepository _alertHistoryRepository;
    private readonly IOandaStreamingService _streamingService;
    private readonly ITelegramService _telegramService;
    private readonly MonitoringSettings _settings;
    private readonly ILogger<StreamingPriceMonitorService> _logger;

    // 缓存已触发的告警，避免重复触发
    private readonly ConcurrentDictionary<string, DateTime> _triggeredAlerts = new();

    // 缓存监控规则列表，定期刷新
    private List<PriceMonitorRule> _cachedRules = new();
    private DateTime _lastRefresh = DateTime.MinValue;
    private readonly TimeSpan _refreshInterval = TimeSpan.FromSeconds(30);

    private bool _isRunning;

    public StreamingPriceMonitorService(
        IPriceMonitorRepository repository,
        IAlertHistoryRepository alertHistoryRepository,
        IOandaStreamingService streamingService,
        ITelegramService telegramService,
        MonitoringSettings settings,
        ILogger<StreamingPriceMonitorService> logger)
    {
        _repository = repository;
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
            // 获取所有启用的固定价格监控规则
            var allRules = await _repository.GetAllAsync();
            _cachedRules = allRules
                .Where(r => r.Enabled && !r.IsTriggered && r.Type == AlertType.FixedPrice)
                .ToList();

            _lastRefresh = DateTime.UtcNow;

            if (_cachedRules.Count == 0)
            {
                _logger.LogInformation("没有需要监控的固定价格规则");
                await _streamingService.StopStreamingAsync();
                return;
            }

            // 获取需要订阅的品种
            var symbols = _cachedRules.Select(r => r.Symbol).Distinct().ToList();

            _logger.LogInformation("监控 {Count} 个固定价格规则，品种: {Symbols}",
                _cachedRules.Count, string.Join(", ", symbols));

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
            // 定期刷新监控规则列表
            if (DateTime.UtcNow - _lastRefresh > _refreshInterval)
            {
                await RefreshAlertsAndSubscribeAsync();
            }

            // 检查该品种的所有监控规则
            var rulesForSymbol = _cachedRules
                .Where(r => r.Symbol.Equals(e.Symbol, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var rule in rulesForSymbol)
            {
                await CheckAndTriggerRuleAsync(rule, e.MidPrice, e.Timestamp);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理价格更新时发生错误: {Symbol}", e.Symbol);
        }
    }

    private async Task CheckAndTriggerRuleAsync(PriceMonitorRule rule, decimal currentPrice, DateTime timestamp)
    {
        if (!rule.TargetPrice.HasValue)
        {
            return;
        }

        // 检查是否已触发（防止短时间内重复触发）
        if (_triggeredAlerts.TryGetValue(rule.Id, out var lastTriggered))
        {
            if (DateTime.UtcNow - lastTriggered < TimeSpan.FromMinutes(1))
            {
                return;
            }
        }

        var targetPrice = rule.TargetPrice.Value;
        var isTriggered = false;

        if (rule.Direction == PriceDirection.Above)
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

        _logger.LogInformation("🔔 触发价格监控: {Name} - {Symbol} {Direction} {Target}, 当前: {Current}",
            rule.Name, rule.Symbol,
            rule.Direction == PriceDirection.Above ? "上穿" : "下穿",
            targetPrice, currentPrice);

        // 标记为已触发
        _triggeredAlerts[rule.Id] = DateTime.UtcNow;

        // 发送通知
        var message = FormatMessage(rule, currentPrice, targetPrice);
        await _telegramService.SendFormattedMessageAsync(message, rule.TelegramChatId);

        // 保存告警历史
        await SaveAlertHistoryAsync(rule, currentPrice, targetPrice, message);

        // 更新数据库中的监控规则状态
        await _repository.MarkAsTriggeredAsync(rule.Id);

        // 从缓存中移除
        _cachedRules.RemoveAll(r => r.Id == rule.Id);
    }

    private string FormatMessage(PriceMonitorRule rule, decimal currentPrice, decimal targetPrice)
    {
        if (!string.IsNullOrEmpty(rule.MessageTemplate))
        {
            return rule.MessageTemplate
                .Replace("{Symbol}", rule.Symbol)
                .Replace("{Name}", rule.Name)
                .Replace("{Price}", currentPrice.ToString())
                .Replace("{Target}", targetPrice.ToString())
                .Replace("{Direction}", rule.Direction == PriceDirection.Above ? "上穿" : "下穿")
                .Replace("{Time}", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
        }

        return $"🔔 价格提示\n\n" +
               $"品种: {rule.Symbol}\n" +
               $"名称: {rule.Name}\n" +
               $"事件: 价格{(rule.Direction == PriceDirection.Above ? "上穿" : "下穿")} {targetPrice}\n" +
               $"当前价格: {currentPrice}\n" +
               $"时间: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";
    }

    private async Task SaveAlertHistoryAsync(PriceMonitorRule rule, decimal currentPrice, decimal targetPrice, string message)
    {
        try
        {
            var history = new AlertHistory
            {
                Type = AlertHistoryType.PriceAlert,
                Symbol = rule.Symbol,
                AlertTime = DateTime.UtcNow,
                Message = message,
                Details = JsonSerializer.Serialize(new
                {
                    RuleId = rule.Id,
                    RuleName = rule.Name,
                    TargetPrice = targetPrice,
                    CurrentPrice = currentPrice,
                    Direction = rule.Direction.ToString(),
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
