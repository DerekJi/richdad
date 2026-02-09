using Microsoft.Extensions.Logging;
using Skender.Stock.Indicators;
using System.Text.Json;
using Trading.Infrastructure.Models;
using Trading.Infrastructure.Repositories;
using Trading.Infrastructure.Services;
using Trading.Services.Configuration;
using Trading.Services.Repositories;

namespace Trading.Services.Services;

/// <summary>
/// 价格监控服务实现
/// </summary>
public class PriceMonitorService : IPriceMonitorService
{
    private readonly IPriceMonitorRepository _repository;
    private readonly IAlertHistoryRepository _alertHistoryRepository;
    private readonly IMarketDataService _marketDataService;
    private readonly ITelegramService _telegramService;
    private readonly MonitoringSettings _settings;
    private readonly ILogger<PriceMonitorService> _logger;
    private Timer? _timer;
    private bool _isRunning;

    public PriceMonitorService(
        IPriceMonitorRepository repository,
        IAlertHistoryRepository alertHistoryRepository,
        IMarketDataService marketDataService,
        ITelegramService telegramService,
        MonitoringSettings settings,
        ILogger<PriceMonitorService> logger)
    {
        _repository = repository;
        _alertHistoryRepository = alertHistoryRepository;
        _marketDataService = marketDataService;
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

            var rules = await _repository.GetEnabledRulesAsync();
            var ruleList = rules.ToList();

            if (!ruleList.Any())
            {
                _logger.LogDebug("没有启用的监控规则");
                return;
            }

            _logger.LogInformation("检查 {Count} 个监控规则", ruleList.Count);

            // 按品种分组，批量获取价格
            var symbols = ruleList.Select(r => r.Symbol).Distinct().ToList();

            // 并行检查监控规则（限制并发数）
            var semaphore = new SemaphoreSlim(_settings.MaxConcurrency);
            var tasks = ruleList.Select(async rule =>
            {
                await semaphore.WaitAsync();
                try
                {
                    await CheckRuleAsync(rule);
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

    public async Task<bool> CheckRuleAsync(PriceMonitorRule rule)
    {
        try
        {
            _logger.LogDebug("检查监控规则: {RuleName} ({Symbol})", rule.Name, rule.Symbol);

            // 获取当前价格
            var currentPrice = await _marketDataService.GetSymbolPriceAsync(rule.Symbol);
            if (currentPrice == null)
            {
                _logger.LogWarning("无法获取 {Symbol} 的价格", rule.Symbol);
                return false;
            }

            decimal targetValue;
            string targetDescription;

            // 根据监控类型计算目标值
            switch (rule.Type)
            {
                case AlertType.FixedPrice:
                    if (!rule.TargetPrice.HasValue)
                    {
                        _logger.LogWarning("监控规则 {RuleId} 未设置目标价格", rule.Id);
                        return false;
                    }
                    targetValue = rule.TargetPrice.Value;
                    targetDescription = $"目标价格 {targetValue}";
                    break;

                case AlertType.EMA:
                    if (!rule.EmaPeriod.HasValue)
                    {
                        _logger.LogWarning("监控规则 {RuleId} 未设置EMA周期", rule.Id);
                        return false;
                    }
                    targetValue = await CalculateEmaAsync(rule.Symbol, rule.TimeFrame, rule.EmaPeriod.Value);
                    targetDescription = $"EMA({rule.EmaPeriod}) {targetValue}";
                    break;

                case AlertType.MA:
                    if (!rule.MaPeriod.HasValue)
                    {
                        _logger.LogWarning("监控规则 {RuleId} 未设置MA周期", rule.Id);
                        return false;
                    }
                    targetValue = await CalculateMaAsync(rule.Symbol, rule.TimeFrame, rule.MaPeriod.Value);
                    targetDescription = $"MA({rule.MaPeriod}) {targetValue}";
                    break;

                default:
                    _logger.LogWarning("不支持的监控类型: {Type}", rule.Type);
                    return false;
            }

            // 检查是否触发条件
            bool isTriggered = rule.Direction switch
            {
                PriceDirection.Above => currentPrice.LastPrice >= targetValue,
                PriceDirection.Below => currentPrice.LastPrice <= targetValue,
                _ => false
            };

            if (isTriggered)
            {
                _logger.LogInformation("监控规则触发: {RuleName} - 当前价格 {Price} {Direction} {Target}",
                    rule.Name, currentPrice.LastPrice,
                    rule.Direction == PriceDirection.Above ? "上穿" : "下穿",
                    targetDescription);

                // 发送通知
                var message = FormatMessage(rule, currentPrice.LastPrice, targetValue, targetDescription);
                await _telegramService.SendFormattedMessageAsync(message, rule.TelegramChatId);

                // 保存告警历史
                var alertHistory = new AlertHistory
                {
                    Type = AlertHistoryType.PriceAlert,
                    Symbol = rule.Symbol,
                    AlertTime = DateTime.UtcNow,
                    Message = message,
                    Details = JsonSerializer.Serialize(new PriceAlertDetails
                    {
                        TargetPrice = targetValue,
                        CurrentPrice = currentPrice.LastPrice,
                        Direction = rule.Direction == PriceDirection.Above ? "Above" : "Below"
                    }),
                    IsSent = true,
                    SendTarget = rule.TelegramChatId?.ToString()
                };

                try
                {
                    await _alertHistoryRepository.AddAsync(alertHistory);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "保存告警历史失败");
                    // 不影响主流程，继续执行
                }

                //  message = FormatMessage(rule, currentPrice.LastPrice, targetValue, targetDescription);
                await _telegramService.SendFormattedMessageAsync(message, rule.TelegramChatId);

                // 标记为已触发
                await _repository.MarkAsTriggeredAsync(rule.Id);

                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查监控规则 {RuleId} 时发生错误", rule.Id);
            return false;
        }
    }

    private async Task<decimal> CalculateEmaAsync(string symbol, string timeFrame, int period)
    {
        try
        {
            var candles = await _marketDataService.GetHistoricalDataAsync(symbol, timeFrame, period + 50);
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
            var candles = await _marketDataService.GetHistoricalDataAsync(symbol, timeFrame, period + 10);
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

    private string FormatMessage(PriceMonitorRule rule, decimal currentPrice, decimal targetValue, string targetDescription)
    {
        var directionText = rule.Direction == PriceDirection.Above ? "上穿" : "下穿";

        // 如果有自定义模板，使用模板
        if (!string.IsNullOrEmpty(rule.MessageTemplate))
        {
            return rule.MessageTemplate
                .Replace("{Symbol}", rule.Symbol)
                .Replace("{Name}", rule.Name)
                .Replace("{Price}", currentPrice.ToString("F5"))
                .Replace("{Target}", targetDescription)
                .Replace("{Direction}", directionText)
                .Replace("{Time}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        }

        // 默认消息格式
        return $@"🔔 **价格监控触发**

📊 **品种**: {rule.Symbol}
📝 **名称**: {rule.Name}
💰 **当前价格**: {currentPrice:F5}
🎯 **{directionText}**: {targetDescription}
⏰ **时间**: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
    }
}
