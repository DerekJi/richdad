using Trading.Infras.Data.Services;

namespace Trading.Infras.Service.Services;

/// <summary>
/// K线图生成服务接口
/// </summary>
public interface IChartService
{
    /// <summary>
    /// 生成包含4个时间周期的K线图（M5, M15, H1, H4）
    /// </summary>
    /// <param name="symbol">交易品种</param>
    /// <param name="candlesM5">M5周期K线数据</param>
    /// <param name="candlesM15">M15周期K线数据</param>
    /// <param name="candlesH1">H1周期K线数据</param>
    /// <param name="candlesH4">H4周期K线数据</param>
    /// <param name="emaPeriod">EMA周期（默认20）</param>
    /// <returns>图片的内存流</returns>
    Task<MemoryStream> GenerateMultiTimeFrameChartAsync(
        string symbol,
        IEnumerable<Candle> candlesM5,
        IEnumerable<Candle> candlesM15,
        IEnumerable<Candle> candlesH1,
        IEnumerable<Candle> candlesH4,
        int emaPeriod = 20);
}
