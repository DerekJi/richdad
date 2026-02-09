using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using Trading.Infrastructure.Configuration;
using Trading.Infrastructure.Models;

namespace Trading.Infrastructure.Repositories;

/// <summary>
/// 预处理数据仓储接口
/// </summary>
public interface IProcessedDataRepository
{
    Task SaveBatchAsync(List<ProcessedDataEntity> entities);
    Task<List<ProcessedDataEntity>> GetRangeAsync(string symbol, string timeFrame, DateTime startTime, DateTime endTime);
    Task<ProcessedDataEntity?> GetByTimeAsync(string symbol, string timeFrame, DateTime time);
    Task<int> GetCountAsync(string symbol, string timeFrame);
    Task DeleteRangeAsync(string symbol, string timeFrame, DateTime startTime, DateTime endTime);
}

/// <summary>
/// 预处理数据仓储实现
/// </summary>
public class ProcessedDataRepository : IProcessedDataRepository
{
    private readonly TableClient _tableClient;
    private readonly ILogger<ProcessedDataRepository> _logger;

    public ProcessedDataRepository(
        AzureTableStorageSettings settings,
        ILogger<ProcessedDataRepository> logger)
    {
        _logger = logger;

        var serviceClient = new TableServiceClient(settings.ConnectionString);
        _tableClient = serviceClient.GetTableClient(settings.ProcessedDataTableName ?? "ProcessedData");

        // 确保表存在
        _tableClient.CreateIfNotExists();
    }

    /// <summary>
    /// 批量保存预处理数据
    /// </summary>
    public async Task SaveBatchAsync(List<ProcessedDataEntity> entities)
    {
        if (!entities.Any())
        {
            _logger.LogWarning("没有数据需要保存");
            return;
        }

        try
        {
            // 按 PartitionKey 分组
            var groups = entities.GroupBy(e => e.PartitionKey);

            foreach (var group in groups)
            {
                // Azure Table Storage 批量操作限制：100条/批次，且必须同一个 PartitionKey
                var batches = group.Chunk(100);

                foreach (var batch in batches)
                {
                    var batchOperations = new List<TableTransactionAction>();

                    foreach (var entity in batch)
                    {
                        batchOperations.Add(new TableTransactionAction(
                            TableTransactionActionType.UpsertReplace,
                            entity));
                    }

                    await _tableClient.SubmitTransactionAsync(batchOperations);
                }
            }

            _logger.LogInformation("成功保存 {Count} 条预处理数据", entities.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存预处理数据失败");
            throw;
        }
    }

    /// <summary>
    /// 按时间范围查询
    /// </summary>
    public async Task<List<ProcessedDataEntity>> GetRangeAsync(
        string symbol,
        string timeFrame,
        DateTime startTime,
        DateTime endTime)
    {
        try
        {
            var partitionKey = $"{symbol}_{timeFrame}";
            var startKey = startTime.ToString("yyyyMMdd_HHmm");
            var endKey = endTime.ToString("yyyyMMdd_HHmm");

            var filter = $"PartitionKey eq '{partitionKey}' and " +
                         $"RowKey ge '{startKey}' and RowKey le '{endKey}'";

            var results = new List<ProcessedDataEntity>();
            await foreach (var entity in _tableClient.QueryAsync<ProcessedDataEntity>(filter))
            {
                results.Add(entity);
            }

            _logger.LogDebug("查询到 {Count} 条预处理数据 ({Symbol} {TimeFrame})",
                results.Count, symbol, timeFrame);

            return results.OrderBy(e => e.Time).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询预处理数据失败 ({Symbol} {TimeFrame})",
                symbol, timeFrame);
            return new List<ProcessedDataEntity>();
        }
    }

    /// <summary>
    /// 按时间查询单条记录
    /// </summary>
    public async Task<ProcessedDataEntity?> GetByTimeAsync(string symbol, string timeFrame, DateTime time)
    {
        try
        {
            var partitionKey = $"{symbol}_{timeFrame}";
            var rowKey = time.ToString("yyyyMMdd_HHmm");

            var response = await _tableClient.GetEntityAsync<ProcessedDataEntity>(partitionKey, rowKey);
            return response.Value;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询预处理数据失败 ({Symbol} {TimeFrame} {Time})",
                symbol, timeFrame, time);
            return null;
        }
    }

    /// <summary>
    /// 统计记录数
    /// </summary>
    public async Task<int> GetCountAsync(string symbol, string timeFrame)
    {
        try
        {
            var partitionKey = $"{symbol}_{timeFrame}";
            var filter = $"PartitionKey eq '{partitionKey}'";

            var count = 0;
            await foreach (var _ in _tableClient.QueryAsync<ProcessedDataEntity>(
                filter: filter,
                select: new[] { "RowKey" }))
            {
                count++;
            }

            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "统计预处理数据失败 ({Symbol} {TimeFrame})",
                symbol, timeFrame);
            return 0;
        }
    }

    /// <summary>
    /// 删除指定时间范围的数据
    /// </summary>
    public async Task DeleteRangeAsync(
        string symbol,
        string timeFrame,
        DateTime startTime,
        DateTime endTime)
    {
        try
        {
            var entities = await GetRangeAsync(symbol, timeFrame, startTime, endTime);

            if (!entities.Any())
            {
                _logger.LogInformation("没有数据需要删除");
                return;
            }

            // 按 PartitionKey 分组
            var groups = entities.GroupBy(e => e.PartitionKey);

            foreach (var group in groups)
            {
                var batches = group.Chunk(100);

                foreach (var batch in batches)
                {
                    var batchOperations = new List<TableTransactionAction>();

                    foreach (var entity in batch)
                    {
                        batchOperations.Add(new TableTransactionAction(
                            TableTransactionActionType.Delete,
                            entity));
                    }

                    await _tableClient.SubmitTransactionAsync(batchOperations);
                }
            }

            _logger.LogInformation("成功删除 {Count} 条预处理数据", entities.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除预处理数据失败");
            throw;
        }
    }
}
