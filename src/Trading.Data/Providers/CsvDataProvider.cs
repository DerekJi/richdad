using System.Globalization;
using Trading.Data.Interfaces;
using Trading.Data.Models;

namespace Trading.Data.Providers;

/// <summary>
/// CSV文件数据提供者
/// </summary>
public class CsvDataProvider : IMarketDataProvider
{
    private readonly string _dataDirectory;

    public CsvDataProvider(string dataDirectory)
    {
        _dataDirectory = dataDirectory;
    }

    /// <summary>
    /// 从CSV文件读取K线数据
    /// </summary>
    public async Task<List<Candle>> GetCandlesAsync(string symbol, string? csvFilter = null, DateTime? startTime = null, DateTime? endTime = null)
    {
        // 查找匹配的CSV文件
        var csvFile = FindCsvFile(symbol, csvFilter);
        if (string.IsNullOrEmpty(csvFile))
        {
            throw new FileNotFoundException($"找不到 {symbol} 的CSV文件");
        }

        var candles = new List<Candle>();
        
        using var reader = new StreamReader(csvFile);
        
        // 跳过标题行
        await reader.ReadLineAsync();
        
        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;
            
            var candle = ParseCsvLine(line);
            
            // 应用时间过滤
            if (startTime.HasValue && candle.DateTime < startTime.Value) continue;
            if (endTime.HasValue && candle.DateTime > endTime.Value) continue;
            
            candles.Add(candle);
        }

        return candles;
    }

    /// <summary>
    /// 查找CSV文件
    /// </summary>
    private string FindCsvFile(string symbol, string? csvFilter = null)
    {
        // 查找包含symbol的文件，如 XAUUSD.a_M15_*.csv
        var files = Directory.GetFiles(_dataDirectory, $"{symbol}*.csv");
        return files.FirstOrDefault(x => string.IsNullOrEmpty(csvFilter) ||Path.GetFileName(x).Contains(csvFilter, StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
    }

    /// <summary>
    /// 解析CSV行
    /// 格式: <DATE>	<TIME>	<OPEN>	<HIGH>	<LOW>	<CLOSE>	<TICKVOL>	<VOL>	<SPREAD>
    /// </summary>
    private Candle ParseCsvLine(string line)
    {
        var parts = line.Split('\t');
        
        if (parts.Length < 9)
        {
            throw new FormatException($"CSV格式不正确: {line}");
        }

        var date = DateTime.ParseExact(parts[0], "yyyy.MM.dd", CultureInfo.InvariantCulture);
        var time = TimeSpan.Parse(parts[1]);
        var dateTime = date.Add(time);

        return new Candle
        {
            DateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc),
            Open = decimal.Parse(parts[2], CultureInfo.InvariantCulture),
            High = decimal.Parse(parts[3], CultureInfo.InvariantCulture),
            Low = decimal.Parse(parts[4], CultureInfo.InvariantCulture),
            Close = decimal.Parse(parts[5], CultureInfo.InvariantCulture),
            TickVolume = long.Parse(parts[6]),
            Spread = int.Parse(parts[8])
        };
    }
}
