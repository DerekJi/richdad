# 市场数据缓存系统使用指南

## 概述

市场数据缓存系统提供基于 Azure Table Storage 的低成本、高性能数据持久化层，解决 OANDA API 重复调用问题，为回测和 AI 分析提供数据基础。

## 核心特性

### 🎯 智能缓存机制
- **优先本地查询**：优先从数据库获取数据，查询延迟 < 10ms
- **自动补充缺失**：仅从 API 获取缺失的时间段
- **实时更新**：自动检测并更新最新数据

### 💰 成本优化
- **Azure Table Storage**：月成本 $1-3（vs Cosmos DB $30-50）
- **减少API调用**：缓存命中率 > 90%
- **按需加载**：只获取需要的数据

### 📊 数据完整性
- **完整历史数据**：支持回测和策略验证
- **数据连续性检测**：自动发现并填补缺失
- **完整性报告**：提供数据质量监控

## 配置

### appsettings.json

```json
{
  "AzureTableStorage": {
    "ConnectionString": "UseDevelopmentStorage=true",
    "Enabled": true,
    "MarketDataTableName": "MarketData",
    "ProcessedDataTableName": "ProcessedData"
  },
  "MarketDataCache": {
    "EnableSmartCache": true,
    "MaxCacheAgeDays": 90,
    "AutoRefreshEnabled": true,
    "RefreshIntervalMinutes": 5,
    "PreloadSymbols": ["XAUUSD", "XAGUSD", "EURUSD", "AUDUSD", "USDJPY"],
    "PreloadTimeFrames": ["M5", "M15", "H1", "H4", "D1"],
    "PreloadCandleCount": 500
  }
}
```

### 配置说明

| 配置项 | 说明 | 默认值 |
|--------|------|--------|
| `EnableSmartCache` | 是否启用智能缓存 | true |
| `MaxCacheAgeDays` | 缓存最大保留天数 | 90 |
| `AutoRefreshEnabled` | 是否启用自动刷新 | true |
| `RefreshIntervalMinutes` | 自动刷新间隔（分钟） | 5 |
| `PreloadSymbols` | 预加载的品种列表 | ["XAUUSD", ...] |
| `PreloadTimeFrames` | 预加载的时间周期 | ["M5", "M15", ...] |
| `PreloadCandleCount` | 预加载的K线数量 | 500 |

## API 使用

### 1. 获取K线数据（智能缓存）

```http
GET /api/marketdata/candles?symbol=XAUUSD&timeFrame=M5&count=200
```

**参数：**
- `symbol`: 品种代码（必需）
- `timeFrame`: 时间周期（必需）
- `count`: K线数量（可选，默认100）
- `endTime`: 结束时间（可选，默认当前时间）

**响应示例：**
```json
[
  {
    "dateTime": "2026-02-09T10:15:00Z",
    "open": 2850.50,
    "high": 2851.20,
    "low": 2849.80,
    "close": 2850.90,
    "tickVolume": 1234,
    "spread": 2
  }
]
```

### 2. 查看最新数据时间

```http
GET /api/marketdata/latest?symbol=XAUUSD&timeFrame=M5
```

**响应示例：**
```json
{
  "symbol": "XAUUSD",
  "timeFrame": "M5",
  "latestTime": "2026-02-09T10:15:00Z",
  "earliestTime": "2026-01-01T00:00:00Z",
  "hasData": true
}
```

### 3. 手动刷新缓存

```http
POST /api/marketdata/refresh?symbol=XAUUSD&timeFrame=M5
```

**可选参数：**
- `startTime`: 开始时间（默认7天前）
- `endTime`: 结束时间（默认当前时间）

### 4. 获取缓存统计

```http
GET /api/marketdata/stats
```

**响应示例：**
```json
{
  "totalRecords": 50000,
  "symbolTimeFrameCounts": {
    "XAUUSD_M5": 10000,
    "XAUUSD_H1": 2000
  },
  "oldestDate": "2025-11-01T00:00:00Z",
  "newestDate": "2026-02-09T10:15:00Z",
  "tableName": "MarketData"
}
```

### 5. 初始化历史数据

```http
POST /api/marketdata/initialize
```

**可选参数：**
- `symbols`: 品种列表（逗号分隔）
- `timeFrames`: 时间周期列表（逗号分隔）

**示例：**
```http
POST /api/marketdata/initialize?symbols=XAUUSD,XAGUSD&timeFrames=M5,H1
```

### 6. 增量更新数据

```http
POST /api/marketdata/update
```

**可选参数：**
- `symbol`: 特定品种（为空则更新所有）
- `timeFrame`: 特定周期（为空则更新所有）

### 7. 检查数据完整性

```http
GET /api/marketdata/integrity
```

**响应示例：**
```json
{
  "XAUUSD_M5": {
    "earliestTime": "2026-01-01T00:00:00Z",
    "latestTime": "2026-02-09T10:15:00Z",
    "expectedCount": 11520,
    "actualCount": 11450,
    "completeness": "99.39%"
  },
  "issues": [
    "EURUSD M15: 数据完整性 85.23%（低于90%）"
  ],
  "totalIssues": 1
}
```

### 8. 预加载数据

```http
POST /api/marketdata/preload
```

## 代码示例

### C# 中使用缓存服务

```csharp
public class MyTradingService
{
    private readonly MarketDataCacheService _cacheService;

    public MyTradingService(MarketDataCacheService cacheService)
    {
        _cacheService = cacheService;
    }

    public async Task AnalyzeMarketAsync()
    {
        // 智能获取最近200根M5 K线
        var candles = await _cacheService.GetCandlesAsync(
            symbol: "XAUUSD",
            timeFrame: "M5",
            count: 200);

        // 进行分析...
        foreach (var candle in candles)
        {
            Console.WriteLine($"{candle.DateTime}: {candle.Close}");
        }
    }
}
```

### 数据初始化

```csharp
public class StartupInitializer
{
    private readonly DataInitializationService _initService;

    public async Task InitializeAsync()
    {
        // 初始化所有配置的品种和周期
        await _initService.InitializeHistoricalDataAsync();

        // 或者初始化特定品种
        await _initService.InitializeHistoricalDataAsync(
            symbols: new List<string> { "XAUUSD", "XAGUSD" },
            timeFrames: new List<string> { "M5", "H1" }
        );
    }
}
```

## 数据存储结构

### MarketData 表

**PartitionKey**: Symbol (如 "XAUUSD")
**RowKey**: TimeFrame_DateTime (如 "M5_20260209_1015")

**字段：**
- Symbol: 品种代码
- TimeFrame: 时间周期
- Time: K线时间
- Open/High/Low/Close: OHLC价格
- Volume: 成交量
- Spread: 点差
- IsComplete: 是否完整
- Source: 数据源

### 查询性能

| 操作 | 延迟 | 说明 |
|------|------|------|
| 单品种查询 (200根) | < 100ms | 按PartitionKey查询 |
| 跨品种查询 | < 500ms | 并行查询多个分区 |
| 统计信息 | < 2s | 全表扫描 |

## 最佳实践

### 1. 初次部署

```bash
# 1. 配置 Azure Table Storage 连接字符串
# 2. 运行初始化
curl -X POST https://your-api/api/marketdata/initialize

# 3. 验证数据
curl https://your-api/api/marketdata/stats
```

### 2. 日常维护

```bash
# 增量更新（每日一次）
curl -X POST https://your-api/api/marketdata/update

# 检查完整性（每周一次）
curl https://your-api/api/marketdata/integrity
```

### 3. 性能优化

- **批量操作**：使用预加载而不是逐个请求
- **合理配置**：根据需求调整 PreloadCandleCount
- **定期清理**：删除超过 MaxCacheAgeDays 的旧数据

### 4. 成本控制

- **存储成本**：约 $0.045/GB/月
- **操作成本**：$0.0004/万次
- **预计月成本**：10GB 数据 + 100万次操作 = $0.85

## 故障排查

### 问题：缓存未生效

**检查：**
```json
"MarketDataCache": {
  "EnableSmartCache": true  // 确保为 true
}
```

### 问题：数据不完整

**解决方案：**
```bash
# 1. 检查完整性
curl https://your-api/api/marketdata/integrity

# 2. 重新初始化特定品种
curl -X POST "https://your-api/api/marketdata/initialize?symbols=XAUUSD&timeFrames=M5"
```

### 问题：查询太慢

**优化建议：**
- 减少查询的 count 数量
- 使用预加载减少实时查询
- 检查网络连接到 Azure

## 监控与告警

### 关键指标

1. **缓存命中率**：应 > 90%
2. **数据完整性**：应 > 95%
3. **查询延迟**：应 < 100ms
4. **存储容量**：监控增长趋势

### 日志查看

```bash
# 查看缓存服务日志
grep "MarketDataCacheService" logs/app.log

# 查看初始化日志
grep "DataInitializationService" logs/app.log
```

## 下一步

- 参考 [数据初始化指南](DATA_INITIALIZATION.md) 了解详细的数据填充策略
- 查看 Issue #6 了解实现细节和验收标准
- 探索 ProcessedData 表用于存储预处理的技术指标

## 支持

如有问题，请查看：
- GitHub Issues
- 系统日志文件
- Azure Table Storage 监控面板
