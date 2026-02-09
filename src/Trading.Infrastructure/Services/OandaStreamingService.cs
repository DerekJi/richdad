using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Trading.Infrastructure.Configuration;

namespace Trading.Infrastructure.Services;

/// <summary>
/// OANDA Streaming API服务实现
/// 使用长连接接收实时价格推送
/// </summary>
public class OandaStreamingService : IOandaStreamingService, IDisposable
{
    private readonly OandaSettings _settings;
    private readonly ILogger<OandaStreamingService> _logger;
    private readonly HttpClient _httpClient;

    private CancellationTokenSource? _cts;
    private Task? _streamingTask;
    private HashSet<string> _subscribedSymbols = new();
    private bool _isRunning;
    private bool _disposed;

    public event EventHandler<PriceUpdateEventArgs>? OnPriceUpdate;
    public event EventHandler<bool>? OnConnectionStatusChanged;

    public bool IsRunning => _isRunning;

    public OandaStreamingService(
        OandaSettings settings,
        ILogger<OandaStreamingService> logger)
    {
        _settings = settings;
        _logger = logger;

        // 创建专用的 HttpClient 用于 Streaming
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_settings.StreamingBaseUrl),
            Timeout = Timeout.InfiniteTimeSpan // Streaming 需要无限超时
        };

        if (!string.IsNullOrEmpty(_settings.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
        }
    }

    public async Task StartStreamingAsync(IEnumerable<string> symbols, CancellationToken cancellationToken = default)
    {
        if (_isRunning)
        {
            _logger.LogWarning("Streaming 服务已在运行中");
            return;
        }

        var symbolList = symbols.ToList();
        if (symbolList.Count == 0)
        {
            _logger.LogWarning("没有需要订阅的品种");
            return;
        }

        _subscribedSymbols = symbolList.ToHashSet();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _logger.LogInformation("启动 OANDA Streaming，订阅品种: {Symbols}", string.Join(", ", symbolList));

        _streamingTask = Task.Run(() => StreamingLoopAsync(_cts.Token), _cts.Token);
        _isRunning = true;

        await Task.CompletedTask;
    }

    public async Task StopStreamingAsync()
    {
        if (!_isRunning)
        {
            return;
        }

        _logger.LogInformation("停止 OANDA Streaming");

        _cts?.Cancel();

        if (_streamingTask != null)
        {
            try
            {
                await _streamingTask.WaitAsync(TimeSpan.FromSeconds(5));
            }
            catch (TimeoutException)
            {
                _logger.LogWarning("等待 Streaming 任务停止超时");
            }
            catch (OperationCanceledException)
            {
                // 预期的取消
            }
        }

        _isRunning = false;
        OnConnectionStatusChanged?.Invoke(this, false);
    }

    public async Task UpdateSymbolsAsync(IEnumerable<string> symbols)
    {
        var newSymbols = symbols.ToHashSet();

        if (_subscribedSymbols.SetEquals(newSymbols))
        {
            return; // 没有变化
        }

        _logger.LogInformation("更新订阅品种: {Symbols}", string.Join(", ", newSymbols));

        // 重启 Streaming 以更新订阅
        await StopStreamingAsync();
        await StartStreamingAsync(newSymbols);
    }

    private async Task StreamingLoopAsync(CancellationToken cancellationToken)
    {
        var retryCount = 0;
        const int maxRetries = 10;
        const int baseDelaySeconds = 5;

        while (!cancellationToken.IsCancellationRequested && retryCount < maxRetries)
        {
            try
            {
                await ConnectAndStreamAsync(cancellationToken);
                retryCount = 0; // 成功连接后重置重试计数
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Streaming 被取消");
                break;
            }
            catch (Exception ex)
            {
                retryCount++;
                var delay = TimeSpan.FromSeconds(baseDelaySeconds * retryCount);

                _logger.LogError(ex, "Streaming 连接失败，{RetryCount}/{MaxRetries}，{Delay}秒后重试",
                    retryCount, maxRetries, delay.TotalSeconds);

                OnConnectionStatusChanged?.Invoke(this, false);

                try
                {
                    await Task.Delay(delay, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        if (retryCount >= maxRetries)
        {
            _logger.LogError("Streaming 重试次数已达上限，停止服务");
        }

        _isRunning = false;
    }

    private async Task ConnectAndStreamAsync(CancellationToken cancellationToken)
    {
        // 转换品种格式 (EURUSD -> EUR_USD)
        var instruments = string.Join(",", _subscribedSymbols.Select(ConvertToOandaSymbol));
        var url = $"/v3/accounts/{_settings.AccountId}/pricing/stream?instruments={instruments}";

        _logger.LogInformation("连接 OANDA Streaming: {Url}", url);

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        _logger.LogInformation("OANDA Streaming 连接成功");
        OnConnectionStatusChanged?.Invoke(this, true);

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!cancellationToken.IsCancellationRequested && !reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                ProcessStreamingMessage(line);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "处理 Streaming 消息失败: {Line}",
                    line.Length > 200 ? line.Substring(0, 200) + "..." : line);
            }
        }
    }

    private void ProcessStreamingMessage(string json)
    {
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("type", out var typeElement))
        {
            return;
        }

        var type = typeElement.GetString();

        switch (type)
        {
            case "PRICE":
                ProcessPriceMessage(root);
                break;

            case "HEARTBEAT":
                _logger.LogDebug("收到心跳: {Time}", root.GetProperty("time").GetString());
                break;

            default:
                _logger.LogDebug("收到未知消息类型: {Type}", type);
                break;
        }
    }

    private void ProcessPriceMessage(JsonElement root)
    {
        var instrument = root.GetProperty("instrument").GetString()!;
        var symbol = ConvertFromOandaSymbol(instrument);

        if (!root.TryGetProperty("bids", out var bids) || bids.GetArrayLength() == 0)
        {
            return;
        }

        if (!root.TryGetProperty("asks", out var asks) || asks.GetArrayLength() == 0)
        {
            return;
        }

        var bid = decimal.Parse(bids[0].GetProperty("price").GetString()!);
        var ask = decimal.Parse(asks[0].GetProperty("price").GetString()!);
        var time = DateTime.Parse(root.GetProperty("time").GetString()!);

        var args = new PriceUpdateEventArgs
        {
            Symbol = symbol,
            Bid = bid,
            Ask = ask,
            Timestamp = time
        };

        // _logger.LogDebug("价格更新: {Symbol} Bid={Bid} Ask={Ask}", symbol, bid, ask);

        OnPriceUpdate?.Invoke(this, args);
    }

    /// <summary>
    /// 转换品种代码格式 (EURUSD -> EUR_USD, XAUUSD -> XAU_USD)
    /// </summary>
    private static string ConvertToOandaSymbol(string symbol)
    {
        symbol = symbol.ToUpper();

        // 贵金属
        if (symbol.StartsWith("XAU") || symbol.StartsWith("XAG") ||
            symbol.StartsWith("XPT") || symbol.StartsWith("XPD"))
        {
            return symbol.Insert(3, "_");
        }

        // 外汇货币对 (6个字符)
        if (symbol.Length == 6)
        {
            return symbol.Insert(3, "_");
        }

        return symbol;
    }

    /// <summary>
    /// 从OANDA格式转换回标准格式 (EUR_USD -> EURUSD)
    /// </summary>
    private static string ConvertFromOandaSymbol(string oandaSymbol)
    {
        return oandaSymbol.Replace("_", "");
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _cts?.Cancel();
        _cts?.Dispose();
        _httpClient.Dispose();
    }
}
