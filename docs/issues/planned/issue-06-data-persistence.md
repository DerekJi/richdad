## Issue 6: 实现数据持久化与智能缓存层

### 标题
🗄️ Implement Data Persistence Layer with Smart Caching for Market Data

### 描述
建立基于 Azure Table Storage 的低成本、高性能数据持久化层，解决 OANDA API 重复调用问题，为回测和 AI 分析提供数据基础。

### 背景
当前系统每次分析都需要从 OANDA API 获取数据，存在以下问题：
- **重复调用成本高**：相同的历史数据被反复请求
- **响应速度慢**：API 调用延迟影响实时决策
- **无法回测**：缺少历史数据存储，无法验证策略
- **数据不连续**：网络故障可能导致数据缺失

通过实现数据持久化层，系统可以：
- **智能缓存**：优先从数据库查询，仅补充缺失数据
- **快速响应**：本地查询延迟 < 10ms
- **支持回测**：存储完整历史数据
- **成本优化**：Azure Table Storage 成本极低（$1-3/月）

### 实现功能

#### ✅ 1. 数据模型设计

**表1: MarketData - 原始 OHLC 数据**

```csharp
public class MarketDataEntity : ITableEntity
{
    // PartitionKey: Symbol (如 "XAUUSD", "EURUSD")
    // RowKey: TimeFrame_DateTime (如 "M5_20260208_1015")

    public string Symbol { get; set; } = string.Empty;
    public string TimeFrame { get; set; } = string.Empty; // D1, H1, M5
    public DateTime Time { get; set; }

    // OHLC 数据
    public double Open { get; set; }
    public double High { get; set; }
    public double Low { get; set; }
    public double Close { get; set; }
    public long Volume { get; set; }

    // 是否完整（已收盘的 K 线）
    public bool IsComplete { get; set; }

    // 数据源
    public string Source { get; set; } = "OANDA";

    // Azure Table Storage 必需字段
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}
```

**表2: ProcessedData - 预处理指标数据**

```csharp
public class ProcessedDataEntity : ITableEntity
{
    // PartitionKey: Symbol_TimeFrame (如 "XAUUSD_M5")
    // RowKey: DateTime (如 "20260208_1015")

    public string Symbol { get; set; } = string.Empty;
    public string TimeFrame { get; set; } = string.Empty;
    public DateTime Time { get; set; }

    // Al Brooks 核心指标
    public double BodyPercent { get; set; }      // (Close-Low)/(High-Low)
    public double ClosePosition { get; set; }    // 同 BodyPercent，收盘位置
    public double DistanceToEMA20 { get; set; }  // Close - EMA20
    public double Range { get; set; }            // High - Low

    // 技术指标
    public double EMA20 { get; set; }
    public double ATR { get; set; }

    // 形态标签（JSON 数组字符串）
    public string Tags { get; set; } = "[]";  // ["ii", "H2", "Signal"]

    // Azure Table Storage 必需字段
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}
```

#### ✅ 2. 智能缓存服务

**新增服务：** `MarketDataCacheService`

```csharp
public class MarketDataCacheService
{
    private readonly IOandaService _oandaService;
    private readonly IMarketDataRepository _repository;
    private readonly ILogger<MarketDataCacheService> _logger;

    /// <summary>
    /// 智能获取 K 线数据：优先从数据库查询，仅补充缺失部分
    /// </summary>
    public async Task<List<Candle>> GetCandlesAsync(
        string symbol,
        string timeFrame,
        int count,
        DateTime? endTime = null)
    {
        endTime ??= DateTime.UtcNow;
        var startTime = CalculateStartTime(endTime.Value, timeFrame, count);

        // 1. 从数据库查询已有数据
        var cachedData = await _repository.GetRangeAsync(
            symbol, timeFrame, startTime, endTime.Value);

        _logger.LogInformation(
            "从缓存获取 {Count} 根 K 线 ({Symbol} {TimeFrame})",
            cachedData.Count, symbol, timeFrame);

        // 2. 检测缺失的时间段
        var missingRanges = DetectMissingRanges(
            startTime, endTime.Value, timeFrame, cachedData);

        if (missingRanges.Any())
        {
            _logger.LogInformation(
                "检测到 {Count} 个缺失时间段，从 OANDA 补充数据",
                missingRanges.Count);

            // 3. 从 OANDA API 获取缺失数据
            foreach (var range in missingRanges)
            {
                var freshData = await _oandaService.GetCandlesAsync(
                    symbol, timeFrame, range.Start, range.End);

                // 4. 保存到数据库
                await _repository.SaveBatchAsync(freshData);

                cachedData.AddRange(freshData);
            }
        }

        // 5. 按时间排序并返回
        return cachedData
            .OrderBy(c => c.Time)
            .Take(count)
            .ToList();
    }

    /// <summary>
    /// 检测缺失的时间段
    /// </summary>
    private List<TimeRange> DetectMissingRanges(
        DateTime start,
        DateTime end,
        string timeFrame,
        List<Candle> existingData)
    {
        var expectedTimes = GenerateExpectedTimes(start, end, timeFrame);
        var existingTimes = existingData.Select(c => c.Time).ToHashSet();
        var missingTimes = expectedTimes.Where(t => !existingTimes.Contains(t));

        // 将连续的缺失时间合并为时间段
        return MergeIntoRanges(missingTimes, timeFrame);
    }
}
```

#### ✅ 3. Repository 实现

**MarketDataRepository.cs:**

```csharp
public class MarketDataRepository : IMarketDataRepository
{
    private readonly TableClient _tableClient;
    private readonly ILogger<MarketDataRepository> _logger;

    public async Task<List<Candle>> GetRangeAsync(
        string symbol,
        string timeFrame,
        DateTime startTime,
        DateTime endTime)
    {
        // 构建查询过滤器
        var filter = $"PartitionKey eq '{symbol}' and " +
                     $"RowKey ge '{timeFrame}_{startTime:yyyyMMdd_HHmm}' and " +
                     $"RowKey le '{timeFrame}_{endTime:yyyyMMdd_HHmm}'";

        var results = new List<Candle>();
        await foreach (var entity in _tableClient.QueryAsync<MarketDataEntity>(filter))
        {
            results.Add(MapToCandle(entity));
        }

        return results;
    }

    public async Task SaveBatchAsync(List<Candle> candles)
    {
        // Azure Table Storage 批量操作限制：100条/批次
        var batches = candles.Chunk(100);

        foreach (var batch in batches)
        {
            var batchOperation = new List<TableTransactionAction>();

            foreach (var candle in batch)
            {
                var entity = MapToEntity(candle);
                batchOperation.Add(new TableTransactionAction(
                    TableTransactionActionType.UpsertReplace, entity));
            }

            await _tableClient.SubmitTransactionAsync(batchOperation);
        }

        _logger.LogInformation("成功保存 {Count} 根 K 线到数据库", candles.Count);
    }

    public async Task<DateTime?> GetLatestTimeAsync(string symbol, string timeFrame)
    {
        var filter = $"PartitionKey eq '{symbol}' and " +
                     $"RowKey ge '{timeFrame}_'";

        await foreach (var entity in _tableClient.QueryAsync<MarketDataEntity>(
            filter, maxPerPage: 1,
            select: new[] { "Time" }))
        {
            return entity.Time;
        }

        return null;
    }
}
```

#### ✅ 4. 查询 API

**新增控制器：** `MarketDataController`

```csharp
[ApiController]
[Route("api/[controller]")]
public class MarketDataController : ControllerBase
{
    private readonly MarketDataCacheService _cacheService;

    /// <summary>
    /// 获取 K 线数据（智能缓存）
    /// GET /api/marketdata/candles?symbol=XAUUSD&timeFrame=M5&count=200
    /// </summary>
    [HttpGet("candles")]
    public async Task<ActionResult<List<Candle>>> GetCandles(
        [Required] string symbol,
        [Required] string timeFrame,
        int count = 100,
        DateTime? endTime = null)
    {
        var candles = await _cacheService.GetCandlesAsync(
            symbol, timeFrame, count, endTime);

        return Ok(candles);
    }

    /// <summary>
    /// 获取最新数据时间
    /// GET /api/marketdata/latest?symbol=XAUUSD&timeFrame=M5
    /// </summary>
    [HttpGet("latest")]
    public async Task<ActionResult<DateTime?>> GetLatestTime(
        [Required] string symbol,
        [Required] string timeFrame)
    {
        var latestTime = await _repository.GetLatestTimeAsync(symbol, timeFrame);
        return Ok(new { symbol, timeFrame, latestTime });
    }

    /// <summary>
    /// 手动刷新缓存
    /// POST /api/marketdata/refresh
    /// </summary>
    [HttpPost("refresh")]
    public async Task<ActionResult> RefreshCache(
        [Required] string symbol,
        [Required] string timeFrame,
        DateTime? startTime = null)
    {
        startTime ??= DateTime.UtcNow.AddDays(-7);

        var candles = await _oandaService.GetCandlesAsync(
            symbol, timeFrame, startTime.Value, DateTime.UtcNow);

        await _repository.SaveBatchAsync(candles);

        return Ok(new {
            message = "缓存已刷新",
            count = candles.Count
        });
    }

    /// <summary>
    /// 获取缓存统计信息
    /// GET /api/marketdata/stats
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult> GetStats()
    {
        var stats = await _repository.GetStatisticsAsync();
        return Ok(stats);
    }
}
```

#### ✅ 5. 配置管理

**appsettings.json:**

```json
{
  "AzureTableStorage": {
    "ConnectionString": "UseDevelopmentStorage=true",
    "MarketDataTableName": "MarketData",
    "ProcessedDataTableName": "ProcessedData",
    "Enabled": true
  },
  "MarketDataCache": {
    "EnableSmartCache": true,
    "MaxCacheAgeDays": 90,
    "AutoRefreshEnabled": true,
    "RefreshIntervalMinutes": 5,
    "PreloadSymbols": ["XAUUSD", "XAGUSD", "EURUSD", "AUDUSD", "USDJPY"]
  }
}
```

### 数据填充策略

#### 初始化历史数据

```csharp
public class DataInitializationService
{
    /// <summary>
    /// 初始化历史数据（首次运行）
    /// </summary>
    public async Task InitializeHistoricalDataAsync()
    {
        var symbols = new[] { "XAUUSD", "XAGUSD", "EURUSD", "AUDUSD", "USDJPY" };
        var timeFrames = new[] { "D1", "H1", "M5" };

        foreach (var symbol in symbols)
        {
            foreach (var timeFrame in timeFrames)
            {
                var count = timeFrame switch
                {
                    "D1" => 200,  // 约 200 个交易日
                    "H1" => 1000, // 约 6 周
                    "M5" => 2000, // 约 1 周
                    _ => 100
                };

                _logger.LogInformation(
                    "正在初始化 {Symbol} {TimeFrame} 数据，共 {Count} 根...",
                    symbol, timeFrame, count);

                var candles = await _oandaService.GetCandlesAsync(
                    symbol, timeFrame, count);

                await _repository.SaveBatchAsync(candles);

                // 避免 API 速率限制
                await Task.Delay(1000);
            }
        }
    }
}
```

### 性能优化

#### 分区键设计

**优化策略：**
- **MarketData**：按 Symbol 分区（如 "XAUUSD"）
  - 优点：同品种查询效率高
  - 避免跨分区查询

- **ProcessedData**：按 Symbol_TimeFrame 分区（如 "XAUUSD_M5"）
  - 更细粒度的分区
  - 提高并发写入性能

#### 批量操作优化

```csharp
// 并行获取多个品种数据
var tasks = symbols.Select(symbol =>
    _cacheService.GetCandlesAsync(symbol, "M5", 200));

var results = await Task.WhenAll(tasks);
```

### 验收标准

**数据持久化：**
- [ ] MarketData 表成功创建并存储 OHLC 数据
- [ ] ProcessedData 表成功存储预处理指标
- [ ] 批量写入性能 > 1000 条/秒
- [ ] 查询性能 < 100ms（200 根 K 线）

**智能缓存：**
- [ ] 首次查询从 OANDA 获取数据
- [ ] 重复查询从缓存返回（命中率 > 90%）
- [ ] 自动检测并补充缺失数据
- [ ] 缓存失效机制正常工作

**API 接口：**
- [ ] GET /api/marketdata/candles 正常工作
- [ ] GET /api/marketdata/latest 返回正确时间
- [ ] POST /api/marketdata/refresh 刷新成功
- [ ] 错误处理和日志记录完善

**数据完整性：**
- [ ] 无重复数据
- [ ] 时间序列连续性检查
- [ ] 数据验证（OHLC 逻辑正确）

### 成本估算

**Azure Table Storage 成本：**

| 数据量 | 存储成本 | 操作成本 | 月总成本 |
|--------|----------|----------|----------|
| 10GB（约200万根K线） | $0.45 | $0.50 | **$0.95** |
| 50GB（约1000万根K线） | $2.25 | $1.00 | **$3.25** |

对比 Cosmos DB（$30-50/月），成本节省 **95%+**。

### 相关文件

**新增文件：**
- `Trading.Infras.Data/Models/MarketDataEntity.cs` - 数据模型
- `Trading.Infras.Data/Models/ProcessedDataEntity.cs` - 预处理数据模型
- `Trading.Infras.Data/Repositories/MarketDataRepository.cs` - 数据访问层
- `Trading.Infras.Service/Services/MarketDataCacheService.cs` - 缓存服务
- `Trading.Infras.Service/Services/DataInitializationService.cs` - 初始化服务
- `Trading.Infras.Web/Controllers/MarketDataController.cs` - API 控制器

**文档：**
- `docs/MARKET_DATA_CACHE_GUIDE.md` - 使用指南
- `docs/DATA_INITIALIZATION.md` - 数据初始化指南

### 后续扩展

**阶段 2（可选）：**
- [ ] 实现 Redis 二级缓存（热数据）
- [ ] 数据压缩和归档策略
- [ ] 多数据源支持（OANDA + TradeLocker）
- [ ] 数据质量监控和报警

### 标签
`enhancement`, `database`, `performance`, `azure`, `storage`, `caching`

---

