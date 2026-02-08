using Trading.Infras.Data.Repositories;
using Trading.Infras.Service.Services;

namespace Trading.Infras.Web.Services;

/// <summary>
/// 价格监控后台服务
/// 根据数据源配置自动选择 Streaming（Oanda）或轮询方式
/// </summary>
public class StreamingPriceMonitorHostedService : IHostedService
{
    private readonly IStreamingPriceMonitorService? _streamingService;
    private readonly IPriceMonitorService _pollingService;
    private readonly IDataSourceConfigRepository _dataSourceRepo;
    private readonly ILogger<StreamingPriceMonitorHostedService> _logger;

    private bool _useStreaming = false;

    public StreamingPriceMonitorHostedService(
        IPriceMonitorService pollingService,
        IDataSourceConfigRepository dataSourceRepo,
        ILogger<StreamingPriceMonitorHostedService> logger,
        IStreamingPriceMonitorService? streamingService = null)
    {
        _pollingService = pollingService;
        _dataSourceRepo = dataSourceRepo;
        _logger = logger;
        _streamingService = streamingService;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // 检查数据源配置
        var config = await _dataSourceRepo.GetConfigAsync();
        _useStreaming = config.Provider.Equals("Oanda", StringComparison.OrdinalIgnoreCase)
                        && _streamingService != null;

        if (_useStreaming)
        {
            _logger.LogInformation("🚀 启动 Streaming 价格监控后台服务 (数据源: Oanda)");
            await _streamingService!.StartAsync();
        }
        else
        {
            _logger.LogInformation("🚀 启动轮询价格监控后台服务 (数据源: {Provider})", config.Provider);
            await _pollingService.StartAsync();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_useStreaming && _streamingService != null)
        {
            _logger.LogInformation("🛑 停止 Streaming 价格监控后台服务");
            await _streamingService.StopAsync();
        }
        else
        {
            _logger.LogInformation("🛑 停止轮询价格监控后台服务");
            await _pollingService.StopAsync();
        }
    }
}
