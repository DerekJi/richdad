using Microsoft.Extensions.Logging;
using ScottPlot;
using Skender.Stock.Indicators;
using SkiaSharp;
using Trading.AlertSystem.Data.Services;

namespace Trading.AlertSystem.Service.Services;

/// <summary>
/// K线图生成服务实现
/// </summary>
public class ChartService : IChartService
{
    private readonly ILogger<ChartService> _logger;

    public ChartService(ILogger<ChartService> logger)
    {
        _logger = logger;
    }

    public async Task<MemoryStream> GenerateMultiTimeFrameChartAsync(
        string symbol,
        IEnumerable<Candle> candlesM5,
        IEnumerable<Candle> candlesM15,
        IEnumerable<Candle> candlesH1,
        IEnumerable<Candle> candlesH4,
        int emaPeriod = 20)
    {
        return await Task.Run(() =>
        {
            try
            {
                // 创建2x2网格布局，每个图1200x800
                int width = 2400;
                int height = 1600;

                using var surface = SKSurface.Create(new SKImageInfo(width, height));
                using var canvas = surface.Canvas;
                canvas.Clear(SKColors.White);

                // 生成4个子图
                var chartConfigs = new[]
                {
                    new { Candles = candlesM5, TimeFrame = "M5", X = 0, Y = 0 },
                    new { Candles = candlesM15, TimeFrame = "M15", X = 1200, Y = 0 },
                    new { Candles = candlesH1, TimeFrame = "H1", X = 0, Y = 800 },
                    new { Candles = candlesH4, TimeFrame = "H4", X = 1200, Y = 800 }
                };

                foreach (var config in chartConfigs)
                {
                    var chartBitmap = GenerateSingleChart(
                        symbol,
                        config.Candles,
                        config.TimeFrame,
                        emaPeriod,
                        1200,
                        800
                    );

                    canvas.DrawBitmap(chartBitmap, config.X, config.Y);
                    chartBitmap.Dispose();
                }

                // 保存到内存流
                using var image = surface.Snapshot();
                using var data = image.Encode(SKEncodedImageFormat.Png, 90);
                var memoryStream = new MemoryStream();
                data.SaveTo(memoryStream);
                memoryStream.Position = 0;

                _logger.LogInformation("成功生成 {Symbol} 的多周期K线图", symbol);
                return memoryStream;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成K线图失败");
                throw;
            }
        });
    }

    private SKBitmap GenerateSingleChart(
        string symbol,
        IEnumerable<Candle> candles,
        string timeFrame,
        int emaPeriod,
        int width,
        int height)
    {
        // 只取最近30-50根K线
        var recentCandles = candles.TakeLast(40).ToList();
        if (recentCandles.Count == 0)
        {
            throw new InvalidOperationException($"没有 {timeFrame} 周期的K线数据");
        }

        var plot = new Plot();
        plot.Title($"{symbol} - {timeFrame} (EMA{emaPeriod})");
        plot.Axes.DateTimeTicksBottom();

        // 准备K线数据
        var ohlc = recentCandles.Select(c => new OHLC(
            (double)c.Open,
            (double)c.High,
            (double)c.Low,
            (double)c.Close,
            c.Time,
            TimeSpan.FromMinutes(GetTimeFrameMinutes(timeFrame))
        )).ToArray();

        // 添加K线图
        var candlestickPlot = plot.Add.Candlestick(ohlc);
        candlestickPlot.Axes.YAxis = plot.Axes.Left;

        // 计算EMA
        var quotes = recentCandles.Select(c => new Quote
        {
            Date = c.Time,
            Open = c.Open,
            High = c.High,
            Low = c.Low,
            Close = c.Close,
            Volume = c.Volume
        }).ToList();

        var emaResults = quotes.GetEma(emaPeriod).ToList();

        // 准备EMA数据（只包含有值的部分）
        var emaData = emaResults
            .Where(r => r.Ema.HasValue)
            .Select(r => new Coordinates(r.Date.ToOADate(), (double)r.Ema!.Value))
            .ToArray();

        if (emaData.Length > 0)
        {
            var emaLine = plot.Add.ScatterLine(emaData);
            emaLine.Color = Colors.Blue;
            emaLine.LineWidth = 2;
            emaLine.LegendText = $"EMA{emaPeriod}";
            emaLine.Axes.YAxis = plot.Axes.Left;
        }

        // 显示图例
        plot.ShowLegend(Alignment.UpperLeft);

        // 设置Y轴范围（留出一些边距）
        var allPrices = recentCandles.SelectMany(c => new[] { c.High, c.Low }).ToList();
        if (emaData.Length > 0)
        {
            var emaPrices = emaData.Select(d => (decimal)d.Y).ToList();
            allPrices.AddRange(emaPrices);
        }
        var minPrice = allPrices.Min();
        var maxPrice = allPrices.Max();
        var priceRange = maxPrice - minPrice;
        plot.Axes.SetLimitsY((double)(minPrice - priceRange * 0.1m), (double)(maxPrice + priceRange * 0.1m));

        // 渲染到SKBitmap
        var pixelWidth = width;
        var pixelHeight = height;

        var bitmap = new SKBitmap(pixelWidth, pixelHeight);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.White);
            plot.Render(canvas, pixelWidth, pixelHeight);
        }

        return bitmap;
    }

    private int GetTimeFrameMinutes(string timeFrame)
    {
        return timeFrame.ToUpper() switch
        {
            "M1" => 1,
            "M5" => 5,
            "M15" => 15,
            "M30" => 30,
            "H1" => 60,
            "H4" => 240,
            "D1" => 1440,
            _ => 5
        };
    }
}
