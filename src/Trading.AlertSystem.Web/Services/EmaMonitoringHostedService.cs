using Microsoft.Extensions.Logging;
using Trading.AlertSystem.Service.Configuration;
using Trading.AlertSystem.Service.Services;

namespace Trading.AlertSystem.Web.Services;

/// <summary>
/// EMA监测后台服务
/// 按照配置的最小时间周期，在整点时间执行检查
/// </summary>
public class EmaMonitoringHostedService : IHostedService, IDisposable
{
    private readonly IEmaMonitoringService _emaMonitoringService;
    private readonly EmaMonitoringSettings _settings;
    private readonly ILogger<EmaMonitoringHostedService> _logger;
    private Timer? _timer;

    public EmaMonitoringHostedService(
        IEmaMonitoringService emaMonitoringService,
        EmaMonitoringSettings settings,
        ILogger<EmaMonitoringHostedService> logger)
    {
        _emaMonitoringService = emaMonitoringService;
        _settings = settings;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("EMA监测服务已禁用");
            return Task.CompletedTask;
        }

        _logger.LogInformation("启动EMA监测后台服务");
        _logger.LogInformation("监测配置:");
        _logger.LogInformation("  品种: {Symbols}", string.Join(", ", _settings.Symbols));
        _logger.LogInformation("  周期: {TimeFrames}", string.Join(", ", _settings.TimeFrames));
        _logger.LogInformation("  EMA: {EmaPeriods}", string.Join(", ", _settings.EmaPeriods));
        _logger.LogInformation("  检查间隔: {Interval}秒", _settings.MinCheckIntervalSeconds);

        // 启动服务
        _ = _emaMonitoringService.StartAsync();

        // 计算到下一个整点时间的延迟
        var now = DateTime.UtcNow;
        var interval = TimeSpan.FromSeconds(_settings.MinCheckIntervalSeconds);
        var nextRun = CalculateNextRunTime(now, interval);
        var initialDelay = nextRun - now;

        _logger.LogInformation("首次检查将在 {NextRun} UTC 执行（{Delay}秒后）",
            nextRun.ToString("yyyy-MM-dd HH:mm:ss"), initialDelay.TotalSeconds);

        // 设置定时器，首次延迟到整点时间，然后按间隔执行
        _timer = new Timer(
            async _ => await ExecuteCheckAsync(),
            null,
            initialDelay,
            interval);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("停止EMA监测后台服务");

        _timer?.Change(Timeout.Infinite, 0);
        await _emaMonitoringService.StopAsync();
    }

    private async Task ExecuteCheckAsync()
    {
        try
        {
            var now = DateTime.UtcNow;
            _logger.LogInformation("执行EMA监测检查 - {Time}", now.ToString("yyyy-MM-dd HH:mm:ss"));

            await _emaMonitoringService.CheckAsync();

            // 计算下一次运行时间
            var interval = TimeSpan.FromSeconds(_settings.MinCheckIntervalSeconds);
            var nextRun = CalculateNextRunTime(DateTime.UtcNow, interval);
            _logger.LogInformation("下次检查时间: {NextRun} UTC", nextRun.ToString("yyyy-MM-dd HH:mm:ss"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EMA监测检查时发生错误");
        }
    }

    /// <summary>
    /// 计算下一个整点运行时间
    /// 例如：如果间隔是5分钟，当前时间是9:03，则返回9:05
    /// </summary>
    private DateTime CalculateNextRunTime(DateTime current, TimeSpan interval)
    {
        var totalSeconds = (long)interval.TotalSeconds;
        var currentSeconds = current.Hour * 3600 + current.Minute * 60 + current.Second;

        // 计算到下一个整点的秒数
        var remainder = currentSeconds % totalSeconds;
        var secondsToNext = remainder == 0 ? totalSeconds : totalSeconds - remainder;

        // 确保返回的是未来时间
        var nextRun = current.AddSeconds(secondsToNext).AddMilliseconds(-current.Millisecond);

        // 如果计算出的时间在过去（可能由于计算误差），则加上一个完整间隔
        if (nextRun <= current)
        {
            nextRun = current.Add(interval);
        }

        return nextRun;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
