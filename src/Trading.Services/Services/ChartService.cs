using Microsoft.Extensions.Logging;
using ScottPlot;
using Skender.Stock.Indicators;
using SkiaSharp;
using Trading.Infrastructure.Services;

namespace Trading.Services.Services;

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

        // 使用索引坐标系统，避免休市时段造成K线缺失
        // 手动绘制K线图
        for (int i = 0; i < recentCandles.Count; i++)
        {
            var candle = recentCandles[i];
            double x = i;
            double open = (double)candle.Open;
            double high = (double)candle.High;
            double low = (double)candle.Low;
            double close = (double)candle.Close;
            double bodyWidth = 0.4; // K线实体宽度

            // 绘制影线（细线）
            var wick = plot.Add.Line(x, low, x, high);
            wick.LineWidth = 1;
            wick.Color = Colors.Black;

            // 绘制实体
            double bodyTop = Math.Max(open, close);
            double bodyBottom = Math.Min(open, close);

            // 十字星的情况，给一个最小高度以便可见
            if (Math.Abs(bodyTop - bodyBottom) < (high - low) * 0.01)
            {
                bodyTop = (open + close) / 2 + (high - low) * 0.005;
                bodyBottom = (open + close) / 2 - (high - low) * 0.005;
            }

            // 使用多边形绘制实体（更可靠）
            double left = x - bodyWidth / 2;
            double right = x + bodyWidth / 2;

            var bodyCoords = new Coordinates[]
            {
                new(left, bodyBottom),
                new(right, bodyBottom),
                new(right, bodyTop),
                new(left, bodyTop)
            };

            var bodyPoly = plot.Add.Polygon(bodyCoords);

            // 中国习惯：阳线（涨）红色，阴线（跌）绿色
            if (close >= open)
            {
                bodyPoly.FillColor = Colors.Red;
                bodyPoly.LineColor = Colors.DarkRed;
            }
            else
            {
                bodyPoly.FillColor = Colors.Green;
                bodyPoly.LineColor = Colors.DarkGreen;
            }
            bodyPoly.LineWidth = 1;
        }

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

        // 准备EMA数据（使用索引作为X坐标）
        var emaData = new List<Coordinates>();
        for (int i = 0; i < emaResults.Count; i++)
        {
            if (emaResults[i].Ema.HasValue)
            {
                emaData.Add(new Coordinates(i, (double)emaResults[i].Ema!.Value));
            }
        }

        if (emaData.Count > 0)
        {
            var emaLine = plot.Add.ScatterLine(emaData);
            emaLine.Color = Colors.Blue;
            emaLine.LineWidth = 2;
            emaLine.LegendText = $"EMA{emaPeriod}";
        }

        // 显示图例
        plot.ShowLegend(Alignment.UpperLeft);

        // 设置自定义X轴刻度标签（只显示时间）
        var tickPositions = new List<double>();
        var tickLabels = new List<string>();

        int labelCount = Math.Min(8, recentCandles.Count); // 最多显示8个标签
        int step = Math.Max(1, recentCandles.Count / labelCount);

        for (int i = 0; i < recentCandles.Count; i += step)
        {
            tickPositions.Add(i);
            tickLabels.Add(recentCandles[i].Time.ToString("HH:mm"));
        }

        // 确保最后一根K线的时间也显示
        if (tickPositions.Count == 0 || tickPositions[^1] != recentCandles.Count - 1)
        {
            tickPositions.Add(recentCandles.Count - 1);
            tickLabels.Add(recentCandles[^1].Time.ToString("HH:mm"));
        }

        plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(
            tickPositions.ToArray(),
            tickLabels.ToArray()
        );

        // 设置X轴范围
        plot.Axes.SetLimitsX(-1, recentCandles.Count);

        // 设置Y轴范围（留出一些边距）
        var allPrices = recentCandles.SelectMany(c => new[] { c.High, c.Low }).ToList();
        if (emaData.Count > 0)
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
