using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Trading.Infras.Data.Configuration;
using Trading.Infras.Data.Models;

namespace Trading.Infras.Data.Services;

/// <summary>
/// OANDA API服务实现
/// 文档: https://developer.oanda.com/rest-live-v20/introduction/
/// </summary>
public class OandaService : IOandaService
{
    private readonly HttpClient _httpClient;
    private readonly OandaSettings _settings;
    private readonly ILogger<OandaService> _logger;

    public OandaService(
        HttpClient httpClient,
        OandaSettings settings,
        ILogger<OandaService> logger)
    {
        _httpClient = httpClient;
        _settings = settings;
        _logger = logger;
        _httpClient.BaseAddress = new Uri(_settings.ApiBaseUrl);

        // OANDA使用Bearer Token认证
        if (!string.IsNullOrEmpty(_settings.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
        }
    }

    public async Task<bool> ConnectAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(_settings.ApiKey) || string.IsNullOrEmpty(_settings.AccountId))
            {
                _logger.LogError("OANDA配置不完整，需要ApiKey和AccountId");
                return false;
            }

            // 测试连接：获取账户信息
            var response = await _httpClient.GetAsync($"/v3/accounts/{_settings.AccountId}");

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("连接OANDA失败: {StatusCode} - {Content}",
                    response.StatusCode, errorContent);
                return false;
            }

            _logger.LogInformation("成功连接到OANDA ({Environment}环境)", _settings.Environment);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "连接OANDA时发生错误");
            return false;
        }
    }

    public async Task<SymbolPrice?> GetSymbolPriceAsync(string symbol)
    {
        try
        {
            // 转换品种代码格式 (EURUSD -> EUR_USD, XAUUSD -> XAU_USD)
            var oandaSymbol = ConvertToOandaSymbol(symbol);

            var response = await _httpClient.GetAsync(
                $"/v3/accounts/{_settings.AccountId}/pricing?instruments={oandaSymbol}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("获取{Symbol}价格失败: {StatusCode}", symbol, response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("OANDA价格API返回: {Content}", content);

            var result = JsonSerializer.Deserialize<JsonElement>(content);
            var prices = result.GetProperty("prices");

            if (prices.GetArrayLength() == 0)
            {
                _logger.LogWarning("未找到{Symbol}的价格数据", symbol);
                return null;
            }

            var priceData = prices[0];
            var bids = priceData.GetProperty("bids");
            var asks = priceData.GetProperty("asks");

            if (bids.GetArrayLength() == 0 || asks.GetArrayLength() == 0)
            {
                _logger.LogWarning("{Symbol}的bid/ask数据为空", symbol);
                return null;
            }

            var bid = decimal.Parse(bids[0].GetProperty("price").GetString()!);
            var ask = decimal.Parse(asks[0].GetProperty("price").GetString()!);

            return new SymbolPrice
            {
                Symbol = symbol,
                Bid = bid,
                Ask = ask,
                LastPrice = (bid + ask) / 2,
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取{Symbol}价格时发生错误", symbol);
            return null;
        }
    }

    public async Task<List<Candle>> GetHistoricalDataAsync(string symbol, string timeFrame, int count)
    {
        try
        {
            // 转换品种代码和时间周期
            var oandaSymbol = ConvertToOandaSymbol(symbol);
            var granularity = ConvertToOandaGranularity(timeFrame);

            var url = $"/v3/instruments/{oandaSymbol}/candles?count={count}&granularity={granularity}&price=M";

            _logger.LogDebug("请求OANDA历史数据: {Url}", url);

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("获取{Symbol} {TimeFrame}历史数据失败: {StatusCode} - {Error}",
                    symbol, timeFrame, response.StatusCode, errorContent);
                return new List<Candle>();
            }

            var content = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("OANDA历史数据API返回: {Content}",
                content.Length > 500 ? content.Substring(0, 500) + "..." : content);

            var result = JsonSerializer.Deserialize<JsonElement>(content);
            var candlesData = result.GetProperty("candles");

            var candles = new List<Candle>();
            foreach (var candleData in candlesData.EnumerateArray())
            {
                // 跳过未完成的K线
                if (!candleData.GetProperty("complete").GetBoolean())
                {
                    continue;
                }

                var mid = candleData.GetProperty("mid");
                var time = DateTime.Parse(candleData.GetProperty("time").GetString()!);

                candles.Add(new Candle
                {
                    Time = time,
                    Open = decimal.Parse(mid.GetProperty("o").GetString()!),
                    High = decimal.Parse(mid.GetProperty("h").GetString()!),
                    Low = decimal.Parse(mid.GetProperty("l").GetString()!),
                    Close = decimal.Parse(mid.GetProperty("c").GetString()!),
                    Volume = candleData.GetProperty("volume").GetInt32()
                });
            }

            _logger.LogInformation("成功获取{Symbol} {TimeFrame}历史数据: {Count}根K线",
                symbol, timeFrame, candles.Count);

            return candles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取{Symbol} {TimeFrame}历史数据时发生错误", symbol, timeFrame);
            return new List<Candle>();
        }
    }

    public async Task<AccountInfo?> GetAccountInfoAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"/v3/accounts/{_settings.AccountId}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("获取账户信息失败: {StatusCode}", response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(content);
            var account = result.GetProperty("account");

            // 解析ID为long类型
            var accountIdStr = account.GetProperty("id").GetString()!;
            var accountId = long.Parse(accountIdStr.Split('-')[0]); // OANDA账户ID格式: XXX-XXX-XXXXXXXX-XXX，取第一部分

            return new AccountInfo
            {
                AccountId = accountId,
                AccountName = accountIdStr,
                Balance = decimal.Parse(account.GetProperty("balance").GetString()!),
                Currency = account.GetProperty("currency").GetString()!,
                Equity = decimal.Parse(account.GetProperty("NAV").GetString()!), // NAV = Net Asset Value
                Margin = decimal.Parse(account.GetProperty("marginUsed").GetString()!),
                FreeMargin = decimal.Parse(account.GetProperty("marginAvailable").GetString()!)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取账户信息时发生错误");
            return null;
        }
    }

    /// <summary>
    /// 转换品种代码格式
    /// EURUSD -> EUR_USD, XAUUSD -> XAU_USD
    /// </summary>
    private string ConvertToOandaSymbol(string symbol)
    {
        // 处理贵金属
        if (symbol.StartsWith("XAU"))
            return "XAU_USD";
        if (symbol.StartsWith("XAG"))
            return "XAG_USD";

        // 处理外汇对 (假设都是6位)
        if (symbol.Length == 6)
        {
            return $"{symbol.Substring(0, 3)}_{symbol.Substring(3, 3)}";
        }

        // 已经是OANDA格式
        return symbol;
    }

    /// <summary>
    /// 转换时间周期格式
    /// M5 -> M5, M15 -> M15, H1 -> H1, H4 -> H4, D1 -> D
    /// </summary>
    private string ConvertToOandaGranularity(string timeFrame)
    {
        return timeFrame switch
        {
            "M1" => "M1",
            "M5" => "M5",
            "M15" => "M15",
            "M30" => "M30",
            "H1" => "H1",
            "H4" => "H4",
            "D1" => "D",
            "W1" => "W",
            "MN" => "M",
            _ => timeFrame
        };
    }
}
