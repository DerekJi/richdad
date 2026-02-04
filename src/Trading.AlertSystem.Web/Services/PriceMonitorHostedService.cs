using Trading.AlertSystem.Service.Services;

namespace Trading.AlertSystem.Web.Services;

/// <summary>
/// 价格监控后台服务
/// </summary>
public class PriceMonitorHostedService : IHostedService
{
    private readonly IPriceMonitorService _monitorService;
    private readonly ILogger<PriceMonitorHostedService> _logger;

    public PriceMonitorHostedService(
        IPriceMonitorService monitorService,
        ILogger<PriceMonitorHostedService> logger)
    {
        _monitorService = monitorService;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("启动价格监控后台服务");
        await _monitorService.StartAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("停止价格监控后台服务");
        await _monitorService.StopAsync();
    }
}
