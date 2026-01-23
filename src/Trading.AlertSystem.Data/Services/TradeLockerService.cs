using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Trading.AlertSystem.Data.Configuration;
using Trading.AlertSystem.Data.Models;

namespace Trading.AlertSystem.Data.Services;

/// <summary>
/// TradeLocker API服务实现
/// 基于官方文档: https://public-api.tradelocker.com/docs/getting-started
/// </summary>
public class TradeLockerService : ITradeLockerService
{
    private readonly HttpClient _httpClient;
    private readonly TradeLockerSettings _settings;
    private readonly ILogger<TradeLockerService> _logger;
    private string? _accessToken;
    private string? _refreshToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public TradeLockerService(
        HttpClient httpClient,
        TradeLockerSettings settings,
        ILogger<TradeLockerService> logger)
    {
        _httpClient = httpClient;
        _settings = settings;
        _logger = logger;
        _httpClient.BaseAddress = new Uri(_settings.ApiBaseUrl);

        // 如果配置了开发者API密钥，添加到请求头
        if (!string.IsNullOrEmpty(_settings.DeveloperApiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("tl-developer-api-key", _settings.DeveloperApiKey);
        }
    }

    public async Task<bool> ConnectAsync()
    {
        try
        {
            // 检查Token是否仍然有效
            if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry)
            {
                _logger.LogDebug("使用现有的AccessToken");
                return true;
            }

            // 检查配置
            if (string.IsNullOrEmpty(_settings.Email) ||
                string.IsNullOrEmpty(_settings.Password) ||
                string.IsNullOrEmpty(_settings.Server))
            {
                _logger.LogError("TradeLocker配置不完整，需要Email、Password和Server");
                return false;
            }

            // 构建JWT Token请求
            var loginRequest = new
            {
                email = _settings.Email,
                password = _settings.Password,
                server = _settings.Server
            };

            var content = new StringContent(
                JsonSerializer.Serialize(loginRequest),
                Encoding.UTF8,
                "application/json");

            // 使用正确的认证端点：backend-api
            var response = await _httpClient.PostAsync("/backend-api/auth/jwt/token", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("TradeLocker登录失败: {StatusCode}, {Error}", response.StatusCode, errorContent);
                return false;
            }

            var result = await response.Content.ReadAsStringAsync();

            // 记录响应内容用于调试
            _logger.LogDebug("TradeLocker响应: {Response}", result.Length > 200 ? result.Substring(0, 200) : result);

            var tokenData = JsonSerializer.Deserialize<JsonElement>(result);

            _accessToken = tokenData.GetProperty("accessToken").GetString();
            _refreshToken = tokenData.GetProperty("refreshToken").GetString();

            // 设置Token过期时间（通常是1小时，这里设置为55分钟以提前刷新）
            if (tokenData.TryGetProperty("accessTokenExpiresAt", out var expiryElement))
            {
                _tokenExpiry = DateTime.Parse(expiryElement.GetString()!);
            }
            else
            {
                _tokenExpiry = DateTime.UtcNow.AddMinutes(55);
            }

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _accessToken);

            _logger.LogInformation("成功连接到TradeLocker ({Environment}环境)", _settings.Environment);
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

            // 需要先获取tradableInstrumentId和routeId
            var instrumentInfo = await GetInstrumentInfoAsync(symbol);
            if (instrumentInfo == null)
            {
                _logger.LogWarning("无法获取{Symbol}的交易品种信息", symbol);
                return null;
            }

            // 添加accNum到请求头
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"/backend-api/trade/quotes?routeId={instrumentInfo.InfoRouteId}&tradableInstrumentId={instrumentInfo.TradableInstrumentId}");
            request.Headers.Add("accNum", _settings.AccountNumber.ToString());

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("获取{Symbol}价格失败: {StatusCode}", symbol, response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("价格API返回: {Content}", content);

            var result = JsonSerializer.Deserialize<JsonElement>(content);

            if (!result.TryGetProperty("s", out var status) || status.GetString() != "ok")
            {
                _logger.LogWarning("获取{Symbol}价格返回状态异常", symbol);
                return null;
            }

            var quotes = result.GetProperty("d");
            // bp = bid price, ap = ask price
            var bid = quotes.GetProperty("bp").GetDecimal();
            var ask = quotes.GetProperty("ap").GetDecimal();

            return new SymbolPrice
            {
                Symbol = symbol,
                Bid = bid,
                Ask = ask,
                LastPrice = (bid + ask) / 2, // 使用中间价
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取{Symbol}价格时发生错误", symbol);
            return null;
        }
    }

    /// <summary>
    /// 获取交易品种信息（包含tradableInstrumentId和routeId）
    /// </summary>
    private async Task<InstrumentInfo?> GetInstrumentInfoAsync(string symbol)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"/backend-api/trade/accounts/{_settings.AccountId}/instruments");
            request.Headers.Add("accNum", _settings.AccountNumber.ToString());

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("品种列表API返回: {Content}", content.Length > 1000 ? content.Substring(0, 1000) + "..." : content);

            var result = JsonSerializer.Deserialize<JsonElement>(content);

            if (!result.TryGetProperty("d", out var data))
            {
                return null;
            }

            // TradeLocker API 返回格式: {"s":"ok","d":{"instruments":[...]}}
            if (!data.TryGetProperty("instruments", out var instruments))
            {
                _logger.LogWarning("API响应中未找到instruments字段");
                return null;
            }

            // 记录所有可用的品种名称
            var availableSymbols = new List<string>();

            // 查找匹配的交易品种
            foreach (var instrument in instruments.EnumerateArray())
            {
                if (instrument.TryGetProperty("name", out var nameElement))
                {
                    var instrumentName = nameElement.GetString();
                    availableSymbols.Add(instrumentName ?? "");

                    if (instrumentName == symbol)
                    {
                        var tradableInstrumentId = instrument.GetProperty("tradableInstrumentId").GetInt64();
                        var routes = instrument.GetProperty("routes").EnumerateArray().ToList();

                        // routes 结构: [{"id": 795894, "type": "TRADE"}, {"id": 791554, "type": "INFO"}]
                        var infoRoute = routes.FirstOrDefault(r => r.GetProperty("type").GetString() == "INFO");
                        var tradeRoute = routes.FirstOrDefault(r => r.GetProperty("type").GetString() == "TRADE");

                        if (infoRoute.ValueKind == JsonValueKind.Undefined || tradeRoute.ValueKind == JsonValueKind.Undefined)
                        {
                            _logger.LogWarning("品种 {Symbol} 缺少必要的路由信息", symbol);
                            continue;
                        }

                        return new InstrumentInfo
                        {
                            Symbol = symbol,
                            TradableInstrumentId = tradableInstrumentId,
                            InfoRouteId = infoRoute.GetProperty("id").GetInt32(),
                            TradeRouteId = tradeRoute.GetProperty("id").GetInt32()
                        };
                    }
                }
            }

            _logger.LogWarning("未找到品种 {Symbol}，可用品种数量: {Count}，品种列表: {AvailableSymbols}",
                symbol, availableSymbols.Count, string.Join(", ", availableSymbols));

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取{Symbol}交易品种信息时发生错误", symbol);
            return null;
        }
    }

    private class InstrumentInfo
    {
        public string Symbol { get; set; } = string.Empty;
        public long TradableInstrumentId { get; set; }
        public int InfoRouteId { get; set; }
        public int TradeRouteId { get; set; }
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

            // 获取交易品种信息
            var instrumentInfo = await GetInstrumentInfoAsync(symbol);
            if (instrumentInfo == null)
            {
                _logger.LogWarning("无法获取{Symbol}的交易品种信息", symbol);
                return Array.Empty<Candle>();
            }

            // 转换时间周期格式 (M5 -> 5, H1 -> 60, D1 -> 1440)
            var resolution = ConvertTimeFrameToResolution(timeFrame);
            var endTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var startTimestamp = endTimestamp - (bars * resolution * 60); // resolution是分钟数

            // 构建历史数据请求
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"/backend-api/trade/history?routeId={instrumentInfo.InfoRouteId}" +
                $"&tradableInstrumentId={instrumentInfo.TradableInstrumentId}" +
                $"&resolution={resolution}" +
                $"&startTimestamp={startTimestamp}" +
                $"&endTimestamp={endTimestamp}" +
                $"&barType=BID"); // 使用BID价格

            request.Headers.Add("accNum", _settings.AccountNumber.ToString());

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("获取{Symbol}历史数据失败: {StatusCode}", symbol, response.StatusCode);
                return Array.Empty<Candle>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(content);

            if (!result.TryGetProperty("s", out var status) || status.GetString() != "ok")
            {
                return Array.Empty<Candle>();
            }

            var data = result.GetProperty("d");
            var times = data.GetProperty("t").EnumerateArray().Select(t => t.GetInt64()).ToList();
            var opens = data.GetProperty("o").EnumerateArray().Select(o => o.GetDecimal()).ToList();
            var highs = data.GetProperty("h").EnumerateArray().Select(h => h.GetDecimal()).ToList();
            var lows = data.GetProperty("l").EnumerateArray().Select(l => l.GetDecimal()).ToList();
            var closes = data.GetProperty("c").EnumerateArray().Select(c => c.GetDecimal()).ToList();
            var volumes = data.GetProperty("v").EnumerateArray().Select(v => v.GetDecimal()).ToList();

            var candles = new List<Candle>();
            for (int i = 0; i < times.Count; i++)
            {
                candles.Add(new Candle
                {
                    Time = DateTimeOffset.FromUnixTimeSeconds(times[i]).DateTime,
                    Open = opens[i],
                    High = highs[i],
                    Low = lows[i],
                    Close = closes[i],
                    Volume = volumes[i]
                });
            }

            return candles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取{Symbol}历史数据时发生错误", symbol);
            return Array.Empty<Candle>();
        }
    }

    public async Task<AccountInfo?> GetAccountInfoAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(_accessToken))
            {
                await ConnectAsync();
            }

            // 获取账户列表
            var request = new HttpRequestMessage(HttpMethod.Get, $"/backend-api/trade/accounts");
            request.Headers.Add("accNum", _settings.AccountNumber.ToString());

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("获取账户信息失败: {StatusCode}", response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("账户API返回的原始数据: {Content}", content);

            var result = JsonSerializer.Deserialize<JsonElement>(content);

            // API返回格式: {"s":"ok","d":[...]}
            if (!result.TryGetProperty("d", out var accountsData) ||
                accountsData.ValueKind != JsonValueKind.Array ||
                accountsData.GetArrayLength() == 0)
            {
                _logger.LogWarning("账户信息为空");
                return null;
            }

            _logger.LogDebug("找到 {Count} 个账户，正在查找账户ID: {AccountId}", accountsData.GetArrayLength(), _settings.AccountId);

            // 找到匹配的账户
            var accountElement = accountsData.EnumerateArray()
                .FirstOrDefault(a => a.GetProperty("id").GetString() == _settings.AccountId.ToString());

            if (accountElement.ValueKind == JsonValueKind.Undefined)
            {
                _logger.LogWarning("未找到账户ID: {AccountId}", _settings.AccountId);
                return null;
            }

            var accountId = accountElement.GetProperty("id").GetString() ?? "0";
            var accountName = accountElement.TryGetProperty("name", out var name) ? name.GetString() ?? "" : "";
            var currency = accountElement.TryGetProperty("currency", out var curr) ? curr.GetString() ?? "USD" : "USD";

            // 获取账户状态（余额、净值等信息）
            var stateRequest = new HttpRequestMessage(HttpMethod.Get, $"/backend-api/trade/accounts/{accountId}/state");
            stateRequest.Headers.Add("accNum", _settings.AccountNumber.ToString());

            var stateResponse = await _httpClient.SendAsync(stateRequest);

            decimal balance = 0, equity = 0, margin = 0, freeMargin = 0;

            if (stateResponse.IsSuccessStatusCode)
            {
                var stateContent = await stateResponse.Content.ReadAsStringAsync();
                _logger.LogDebug("账户状态API返回: {Content}", stateContent);

                var stateResult = JsonSerializer.Deserialize<JsonElement>(stateContent);
                if (stateResult.TryGetProperty("d", out var stateData) &&
                    stateData.TryGetProperty("accountDetailsData", out var detailsArray) &&
                    detailsArray.ValueKind == JsonValueKind.Array &&
                    detailsArray.GetArrayLength() >= 5)
                {
                    // accountDetailsData数组格式: [balance, equity, equity, ?, freeMargin, ?, usedMargin, ...]
                    // 根据日志: [5041.03, 5041.03, 5041.03, 0, 5041.03, 0, 5041.03, 0, 0, ...]
                    balance = detailsArray[0].GetDecimal();
                    equity = detailsArray[1].GetDecimal();
                    freeMargin = detailsArray[4].GetDecimal();
                    margin = detailsArray[6].GetDecimal();
                }
            }
            else
            {
                _logger.LogWarning("获取账户状态失败: {StatusCode}", stateResponse.StatusCode);
            }

            var accountInfo = new AccountInfo
            {
                AccountId = long.Parse(accountId),
                AccountName = accountName,
                Balance = balance,
                Equity = equity,
                Margin = margin,
                FreeMargin = freeMargin,
                Currency = currency
            };

            return accountInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取账户信息时发生错误");
            return null;
        }
    }

    /// <summary>
    /// 转换时间周期格式为分钟数
    /// </summary>
    private int ConvertTimeFrameToResolution(string timeFrame)
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
            "W1" => 10080,
            _ => 5 // 默认5分钟
        };
    }
}
