using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Trading.AlertSystem.Data.Configuration;
using Trading.AlertSystem.Data.Models;

namespace Trading.AlertSystem.Data.Services;

/// <summary>
/// TradeLocker API服务实现
/// </summary>
public class TradeLockerService : ITradeLockerService
{
    private readonly HttpClient _httpClient;
    private readonly TradeLockerSettings _settings;
    private readonly ILogger<TradeLockerService> _logger;
    private string? _accessToken;

    public TradeLockerService(
        HttpClient httpClient,
        TradeLockerSettings settings,
        ILogger<TradeLockerService> logger)
    {
        _httpClient = httpClient;
        _settings = settings;
        _logger = logger;
        _httpClient.BaseAddress = new Uri(_settings.ApiBaseUrl);
    }

    public async Task<bool> ConnectAsync()
    {
        try
        {
            // 如果已经配置了AccessToken，直接使用
            if (!string.IsNullOrEmpty(_settings.AccessToken))
            {
                _accessToken = _settings.AccessToken;
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Bearer", _accessToken);
                _logger.LogInformation("使用配置的AccessToken连接TradeLocker");
                return true;
            }

            // 否则使用用户名密码登录
            if (string.IsNullOrEmpty(_settings.Username) || 
                string.IsNullOrEmpty(_settings.Password) ||
                string.IsNullOrEmpty(_settings.Server))
            {
                _logger.LogError("TradeLocker配置不完整，需要AccessToken或用户名/密码/服务器");
                return false;
            }

            var loginRequest = new
            {
                email = _settings.Username,
                password = _settings.Password,
                server = _settings.Server
            };

            var content = new StringContent(
                JsonSerializer.Serialize(loginRequest),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync("/auth/jwt/token", content);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("TradeLocker登录失败: {StatusCode}", response.StatusCode);
                return false;
            }

            var result = await response.Content.ReadAsStringAsync();
            var tokenData = JsonSerializer.Deserialize<JsonElement>(result);
            
            _accessToken = tokenData.GetProperty("accessToken").GetString();
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", _accessToken);

            _logger.LogInformation("成功连接到TradeLocker");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "连接TradeLocker时发生错误");
            return false;
        }
    }

    public async Task<SymbolPrice?> GetSymbolPriceAsync(string symbol)
    {
        try
        {
            if (string.IsNullOrEmpty(_accessToken))
            {
                await ConnectAsync();
            }

            // TradeLocker API端点可能需要根据实际文档调整
            var response = await _httpClient.GetAsync($"/trade/accounts/{_settings.AccountId}/quotes?symbol={symbol}");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("获取{Symbol}价格失败: {StatusCode}", symbol, response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(content);

            // 根据实际API响应格式解析
            return new SymbolPrice
            {
                Symbol = symbol,
                Bid = data.GetProperty("bid").GetDecimal(),
                Ask = data.GetProperty("ask").GetDecimal(),
                LastPrice = data.GetProperty("last").GetDecimal(),
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取{Symbol}价格时发生错误", symbol);
            return null;
        }
    }

    public async Task<IEnumerable<SymbolPrice>> GetSymbolPricesAsync(IEnumerable<string> symbols)
    {
        var tasks = symbols.Select(s => GetSymbolPriceAsync(s));
        var results = await Task.WhenAll(tasks);
        return results.Where(r => r != null).Cast<SymbolPrice>();
    }

    public async Task<IEnumerable<Candle>> GetHistoricalDataAsync(string symbol, string timeFrame, int bars)
    {
        try
        {
            if (string.IsNullOrEmpty(_accessToken))
            {
                await ConnectAsync();
            }

            // 根据TradeLocker实际API调整端点
            var response = await _httpClient.GetAsync(
                $"/trade/accounts/{_settings.AccountId}/history?symbol={symbol}&timeframe={timeFrame}&bars={bars}");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("获取{Symbol}历史数据失败: {StatusCode}", symbol, response.StatusCode);
                return Array.Empty<Candle>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(content);

            // 根据实际API响应格式解析
            var candles = new List<Candle>();
            if (data.TryGetProperty("bars", out var bars_data))
            {
                foreach (var bar in bars_data.EnumerateArray())
                {
                    candles.Add(new Candle
                    {
                        Time = DateTime.Parse(bar.GetProperty("time").GetString()!),
                        Open = bar.GetProperty("open").GetDecimal(),
                        High = bar.GetProperty("high").GetDecimal(),
                        Low = bar.GetProperty("low").GetDecimal(),
                        Close = bar.GetProperty("close").GetDecimal(),
                        Volume = bar.GetProperty("volume").GetDecimal()
                    });
                }
            }

            return candles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取{Symbol}历史数据时发生错误", symbol);
            return Array.Empty<Candle>();
        }
    }
}
