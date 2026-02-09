using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Trading.Infrastructure.Configuration;
using Trading.Infrastructure.Models;
using Trading.Models;

namespace Trading.Infrastructure.Repositories;

/// <summary>
/// K线数据仓储实现 - 基于 Azure Table Storage
/// </summary>
public class CandleRepository : ICandleRepository
{
    private readonly TableClient _tableClient;
    private readonly ILogger<CandleRepository> _logger;
    private readonly string _tableName;

    public CandleRepository(
        IOptions<AzureTableStorageSettings> settings,
        ILogger<CandleRepository> logger)
    {
        _logger = logger;
        _tableName = settings.Value.CandleTableName ?? "Candles";

        try
        {
            var serviceClient = new TableServiceClient(settings.Value.ConnectionString);
            _tableClient = serviceClient.GetTableClient(_tableName);
            _tableClient.CreateIfNotExists();

            _logger.LogInformation("CandleRepository 初始化成功，表名: {TableName}", _tableName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CandleRepository 初始化失败");
            throw;
        }
    }

    public async Task<List<Candle>> GetRangeAsync(
        string symbol,
        string timeFrame,
        DateTime startTime,
        DateTime endTime)
    {
        try
        {
            var startRowKey = $"{timeFrame}_{startTime:yyyyMMdd_HHmm}";
            var endRowKey = $"{timeFrame}_{endTime:yyyyMMdd_HHmm}";

            var filter = $"PartitionKey eq '{symbol}' and RowKey ge '{startRowKey}' and RowKey le '{endRowKey}'";

            var results = new List<Candle>();
            await foreach (var entity in _tableClient.QueryAsync<CandleEntity>(filter))
            {
                results.Add(entity.ToCandle());
            }

            _logger.LogDebug(
                "从缓存获取 {Count} 根 K 线 ({Symbol} {TimeFrame}, {Start} - {End})",
                results.Count, symbol, timeFrame, startTime, endTime);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "查询 K 线数据失败 ({Symbol} {TimeFrame}, {Start} - {End})",
                symbol, timeFrame, startTime, endTime);
            throw;
        }
    }

    public async Task SaveBatchAsync(
        string symbol,
        string timeFrame,
        List<Candle> candles,
        string source = "OANDA")
    {
        if (!candles.Any())
        {
            return;
        }

        try
        {
            // Azure Table Storage 批量操作限制：
            // 1. 同一批次最多 100 条
            // 2. 必须在同一个 PartitionKey 下
            var batches = candles
                .Select(c => CandleEntity.FromCandle(symbol, timeFrame, c, source))
                .Chunk(100);

            int totalSaved = 0;
            foreach (var batch in batches)
            {
                // 使用 UpsertReplace 确保数据可以被更新
                var batchOperation = batch.Select(entity =>
                    new TableTransactionAction(TableTransactionActionType.UpsertReplace, entity));

                await _tableClient.SubmitTransactionAsync(batchOperation);
                totalSaved += batch.Length;
            }

            _logger.LogInformation(
                "成功保存 {Count} 根 K 线到数据库 ({Symbol} {TimeFrame})",
                totalSaved, symbol, timeFrame);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "批量保存 K 线数据失败 ({Symbol} {TimeFrame}, {Count} 条)",
                symbol, timeFrame, candles.Count);
            throw;
        }
    }

    public async Task<DateTime?> GetLatestTimeAsync(string symbol, string timeFrame)
    {
        try
        {
            var filter = $"PartitionKey eq '{symbol}' and RowKey ge '{timeFrame}_'";

            // 按 RowKey 降序排列，获取最新的一条
            DateTime? latestTime = null;
            await foreach (var entity in _tableClient.QueryAsync<CandleEntity>(
                filter: filter,
                maxPerPage: 1000))
            {
                // 确保时间是UTC格式
                var entityTime = entity.Time.Kind == DateTimeKind.Utc
                    ? entity.Time
                    : DateTime.SpecifyKind(entity.Time, DateTimeKind.Utc);

                if (latestTime == null || entityTime > latestTime)
                {
                    latestTime = entityTime;
                }
            }

            return latestTime;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取最新时间失败 ({Symbol} {TimeFrame})", symbol, timeFrame);
            return null;
        }
    }

    public async Task<DateTime?> GetEarliestTimeAsync(string symbol, string timeFrame)
    {
        try
        {
            var filter = $"PartitionKey eq '{symbol}' and RowKey ge '{timeFrame}_'";

            DateTime? earliestTime = null;
            await foreach (var entity in _tableClient.QueryAsync<CandleEntity>(
                filter: filter,
                maxPerPage: 1000))
            {
                // 确保时间是UTC格式
                var entityTime = entity.Time.Kind == DateTimeKind.Utc
                    ? entity.Time
                    : DateTime.SpecifyKind(entity.Time, DateTimeKind.Utc);

                if (earliestTime == null || entityTime < earliestTime)
                {
                    earliestTime = entityTime;
                }
            }

            return earliestTime;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取最早时间失败 ({Symbol} {TimeFrame})", symbol, timeFrame);
            return null;
        }
    }

    public async Task<int> GetCountAsync(string symbol, string timeFrame)
    {
        try
        {
            var filter = $"PartitionKey eq '{symbol}' and RowKey ge '{timeFrame}_' and RowKey lt '{timeFrame}a'";

            int count = 0;
            await foreach (var entity in _tableClient.QueryAsync<CandleEntity>(
                filter: filter,
                maxPerPage: 1000))
            {
                count++;
            }

            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取记录数失败 ({Symbol} {TimeFrame})", symbol, timeFrame);
            return 0;
        }
    }

    public async Task<Dictionary<string, object>> GetStatisticsAsync()
    {
        var stats = new Dictionary<string, object>();

        try
        {
            // 统计各品种和时间周期的数据量
            var symbolTimeFrameCounts = new Dictionary<string, int>();
            DateTime? oldestDate = null;
            DateTime? newestDate = null;

            await foreach (var entity in _tableClient.QueryAsync<CandleEntity>())
            {
                var key = $"{entity.Symbol}_{entity.TimeFrame}";
                symbolTimeFrameCounts[key] = symbolTimeFrameCounts.GetValueOrDefault(key, 0) + 1;

                if (oldestDate == null || entity.Time < oldestDate)
                {
                    oldestDate = entity.Time;
                }
                if (newestDate == null || entity.Time > newestDate)
                {
                    newestDate = entity.Time;
                }
            }

            stats["TotalRecords"] = symbolTimeFrameCounts.Values.Sum();
            stats["SymbolTimeFrameCounts"] = symbolTimeFrameCounts;
            stats["OldestDate"] = oldestDate as object ?? "N/A";
            stats["NewestDate"] = newestDate as object ?? "N/A";
            stats["TableName"] = _tableName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取统计信息失败");
            stats["Error"] = ex.Message;
        }

        return stats;
    }

    public async Task DeleteRangeAsync(
        string symbol,
        string timeFrame,
        DateTime startTime,
        DateTime endTime)
    {
        try
        {
            var startRowKey = $"{timeFrame}_{startTime:yyyyMMdd_HHmm}";
            var endRowKey = $"{timeFrame}_{endTime:yyyyMMdd_HHmm}";

            var filter = $"PartitionKey eq '{symbol}' and RowKey ge '{startRowKey}' and RowKey le '{endRowKey}'";

            var entitiesToDelete = new List<CandleEntity>();
            await foreach (var entity in _tableClient.QueryAsync<CandleEntity>(filter))
            {
                entitiesToDelete.Add(entity);
            }

            // 批量删除
            var batches = entitiesToDelete.Chunk(100);
            int totalDeleted = 0;

            foreach (var batch in batches)
            {
                var batchOperation = batch.Select(entity =>
                    new TableTransactionAction(TableTransactionActionType.Delete, entity));

                await _tableClient.SubmitTransactionAsync(batchOperation);
                totalDeleted += batch.Length;
            }

            _logger.LogInformation(
                "删除 {Count} 根 K 线 ({Symbol} {TimeFrame}, {Start} - {End})",
                totalDeleted, symbol, timeFrame, startTime, endTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "删除数据失败 ({Symbol} {TimeFrame}, {Start} - {End})",
                symbol, timeFrame, startTime, endTime);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(string symbol, string timeFrame, DateTime time)
    {
        try
        {
            var rowKey = $"{timeFrame}_{time:yyyyMMdd_HHmm}";
            var filter = $"PartitionKey eq '{symbol}' and RowKey eq '{rowKey}'";

            await foreach (var _ in _tableClient.QueryAsync<CandleEntity>(filter, maxPerPage: 1))
            {
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查数据是否存在失败 ({Symbol} {TimeFrame}, {Time})", symbol, timeFrame, time);
            return false;
        }
    }
}
