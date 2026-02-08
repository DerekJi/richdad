using Trading.Infras.Data.Models;

namespace Trading.Infras.Data.Services;

/// <summary>
/// OANDA Streaming API服务接口
/// 用于实时价格推送
/// </summary>
public interface IOandaStreamingService
{
    /// <summary>
    /// 价格更新事件
    /// </summary>
    event EventHandler<PriceUpdateEventArgs>? OnPriceUpdate;

    /// <summary>
    /// 连接状态变化事件
    /// </summary>
    event EventHandler<bool>? OnConnectionStatusChanged;

    /// <summary>
    /// 开始订阅品种价格流
    /// </summary>
    /// <param name="symbols">品种列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task StartStreamingAsync(IEnumerable<string> symbols, CancellationToken cancellationToken = default);

    /// <summary>
    /// 停止订阅
    /// </summary>
    Task StopStreamingAsync();

    /// <summary>
    /// 是否正在运行
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// 更新订阅的品种列表
    /// </summary>
    Task UpdateSymbolsAsync(IEnumerable<string> symbols);
}

/// <summary>
/// 价格更新事件参数
/// </summary>
public class PriceUpdateEventArgs : EventArgs
{
    public required string Symbol { get; init; }
    public required decimal Bid { get; init; }
    public required decimal Ask { get; init; }
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// 中间价（Bid + Ask）/ 2
    /// </summary>
    public decimal MidPrice => (Bid + Ask) / 2;
}
