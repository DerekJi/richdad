# K线数据持久化功能说明

## 概述

基于 Azure Table Storage 实现的 K 线数据持久化层，支持历史数据存储和实时增量更新。

## 核心功能

### 1. 数据初始化

批量获取并存储历史 K 线数据。

**API:**
```bash
POST /api/candle/initialize
Content-Type: application/json

{
  "symbols": ["XAUUSD", "XAGUSD"],
  "timeFrames": ["M5", "H1"],
  "count": 1000
}
```

**特点:**
- 支持多品种、多周期批量初始化
- 自动处理 UTC 时间
- 保留 IsComplete 字段（标识K线是否完成）

### 2. 增量更新

智能更新最新数据，自动处理未完成的 K 线。

**API:**
```bash
POST /api/candle/update?symbol=XAUUSD&timeFrame=M5
```

**工作原理:**
1. 查询数据库最新时间
2. 计算与当前时间的差值
3. 仅获取缺失的数据（`>= latestTime`）
4. 自动更新未完成的 K 线（IsComplete=false）

**智能优化:**
- 如果数据已是最新（时间差 < 一个周期），则跳过更新
- UpsertReplace 机制自动更新同一时间的 K 线

### 3. 数据查询

按时间范围或数量查询 K 线数据。

**API:**
```bash
# 获取最近 100 根 K 线
GET /api/candle/candles?symbol=XAUUSD&timeFrame=M5&count=100

# 获取指定时间范围
GET /api/candle/candles?symbol=XAUUSD&timeFrame=M5&startTime=2026-02-01T00:00:00Z&endTime=2026-02-09T00:00:00Z
```

### 4. 统计信息

查看数据库中的数据统计。

**API:**
```bash
GET /api/candle/stats
```

**返回示例:**
```json
{
  "totalRecords": 2000,
  "symbolTimeFrameCounts": {
    "XAUUSD_M5": 1000,
    "XAUUSD_H1": 500,
    "XAGUSD_M5": 500
  },
  "oldestDate": "2026-01-29T10:15:00Z",
  "newestDate": "2026-02-09T15:50:00Z",
  "tableName": "Candles"
}
```

## 数据模型

### CandleEntity (Azure Table Storage)

```csharp
PartitionKey: Symbol               // "XAUUSD"
RowKey: TimeFrame_DateTime         // "M5_20260209_1550"

Properties:
- Time: DateTime (UTC)
- Open, High, Low, Close: double
- Volume: long
- Spread: int
- IsComplete: bool                 // 是否已完成的K线
- Source: string                   // "OANDA"
```

**RowKey 设计优势:**
- 同一时间的 K 线使用相同 RowKey
- UpsertReplace 自动更新（无需先查询再更新）
- 支持高效的时间范围查询

## 时区处理

全链路 UTC 时间处理，确保时间一致性：

1. **OANDA 数据获取**: `DateTimeStyles.AdjustToUniversal`
2. **数据库查询**: `DateTime.SpecifyKind(entityTime, DateTimeKind.Utc)`
3. **时间比较**: `DateTime.UtcNow` vs 数据库UTC时间

## 配置

**appsettings.json:**
```json
{
  "AzureTableStorage": {
    "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=xxx;...",
    "CandleTableName": "Candles",
    "Enabled": true
  }
}
```

**开发环境（Azurite）:**
```json
{
  "AzureTableStorage": {
    "ConnectionString": "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=...;TableEndpoint=http://127.0.0.1:10012/devstoreaccount1;",
    "CandleTableName": "Candles",
    "Enabled": true
  }
}
```

## 使用场景

### 场景1: 系统首次运行

```bash
# 1. 初始化历史数据（1000根K线）
curl -X POST "http://localhost:5000/api/candle/initialize" \
  -H "Content-Type: application/json" \
  -d '{"symbols":["XAUUSD"],"timeFrames":["M5"],"count":1000}'

# 2. 查看统计
curl "http://localhost:5000/api/candle/stats"
```

### 场景2: 定时增量更新

```bash
# 每5分钟执行一次（建议使用定时任务）
curl -X POST "http://localhost:5000/api/candle/update?symbol=XAUUSD&timeFrame=M5"
```

### 场景3: 回测分析

```bash
# 获取指定时间段的完整数据
curl "http://localhost:5000/api/candle/candles?symbol=XAUUSD&timeFrame=M5&startTime=2026-02-01T00:00:00Z&endTime=2026-02-09T00:00:00Z"
```

## 性能优化

1. **批量操作**: SaveBatchAsync 使用批量提交（每批100条）
2. **分区策略**: 按 Symbol 分区，避免跨分区查询
3. **索引优化**: RowKey 包含时间信息，支持高效范围查询
4. **智能更新**: 仅获取缺失数据，避免重复API调用

## 成本分析

**Azure Table Storage 定价（按实际使用量）:**
- 存储: ~$0.045/GB/月
- 事务: $0.00036/10,000 次
- 出站数据: 前5GB免费

**估算（5品种 × 3周期 × 1000根K线）:**
- 存储: ~15,000 条记录 ≈ 2MB ≈ **$0.0001/月**
- 事务: 初始化 + 日常更新 ≈ 50,000 次 ≈ **$0.18/月**
- **总计: < $0.20/月** （相比 Cosmos DB 的 $30-50/月，节省 99%+）

## 故障排查

### 问题1: 时间显示不正确

**症状**: 数据库时间与当前时间相差数小时

**原因**: 时区处理不当（Local vs UTC）

**解决**:
- 确保 OANDA 数据解析使用 `AdjustToUniversal`
- 确保数据库查询返回时使用 `SpecifyKind(UTC)`
- 确保时间比较都使用 `DateTime.UtcNow`

### 问题2: 增量更新不工作

**症状**: 调用 update API 后数据未更新

**原因**:
1. 数据已是最新（时间差 < 周期）
2. OANDA API 未返回新数据

**诊断**:
```bash
# 查看详细日志
# 会显示: 最新时间、当前时间、时间差
```

### 问题3: 连接 Azurite 失败

**检查**:
1. Azurite 是否运行: `docker ps`
2. 端口是否正确: TableEndpoint 应为 `10012`
3. ConnectionString 格式是否正确

## 后续优化方向

1. **自动定时更新**: 实现后台服务，自动调用增量更新
2. **数据压缩**: 超过90天的数据可以压缩存储
3. **多数据源**: 支持从 TradeLocker 等其他数据源获取
4. **缓存层**: 添加内存缓存，减少数据库查询
5. **数据验证**: 检测并修复数据缺失或异常

## 相关文档

- [Azure Table Storage 文档](https://docs.microsoft.com/azure/storage/tables/)
- [Issue #6: 数据持久化](../GITHUB_ISSUES.md#issue-6)
- [Azurite 本地开发](https://docs.microsoft.com/azure/storage/common/storage-use-azurite)
