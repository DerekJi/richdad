using Trading.AlertSystem.Service.Services;

namespace Trading.AlertSystem.Web.Services;

/// <summary>
/// 基于 Streaming 的价格监控后台服务
/// 使用 OANDA Streaming API 实现实时价格告警
/// </summary>
public class StreamingPriceMonitorHostedService : IHostedService
{
    private readonly IStreamingPriceMonitorService _monitorService;
    private readonly ILogger<StreamingPriceMonitorHostedService> _logger;

    public StreamingPriceMonitorHostedService(
        IStreamingPriceMonitorService monitorService,
        ILogger<StreamingPriceMonitorHostedService> logger)
    {
        _monitorService = monitorService;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🚀 启动 Streaming 价格监控后台服务");
        await _monitorService.StartAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🛑 停止 Streaming 价格监控后台服务");
        await _monitorService.StopAsync();
    }
}
