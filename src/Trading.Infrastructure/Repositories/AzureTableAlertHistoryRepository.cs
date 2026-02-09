using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using Trading.Infrastructure.Configuration;
using Trading.Infrastructure.Infrastructure;
using Trading.Infrastructure.Models;

namespace Trading.Infrastructure.Repositories;

/// <summary>
/// Azure Table Storage 告警历史仓储
/// </summary>
public class AzureTableAlertHistoryRepository : IAlertHistoryRepository
{
    private readonly TableClient _tableClient;
    private readonly ILogger<AzureTableAlertHistoryRepository> _logger;
    private const string PartitionKeyPrefix = "Alert";

    public AzureTableAlertHistoryRepository(
        AzureTableStorageContext context,
        AzureTableStorageSettings settings,
        ILogger<AzureTableAlertHistoryRepository> logger)
    {
        _logger = logger;
        _tableClient = context.GetTableClient(settings.AlertHistoryTableName);
    }

    public async Task<AlertHistory> AddAsync(AlertHistory alert)
    {
        try
        {
            if (string.IsNullOrEmpty(alert.Id))
                alert.Id = Guid.NewGuid().ToString();
            if (alert.AlertTime == default)
                alert.AlertTime = DateTime.UtcNow;

            // 使用日期作为 PartitionKey 以优化查询性能
            var partitionKey = $"{PartitionKeyPrefix}_{alert.AlertTime:yyyyMMdd}";
            var entity = new TableEntity(partitionKey, alert.Id)
            {
                { "Symbol", alert.Symbol },
                { "Type", alert.Type.ToString() },
                { "Message", alert.Message },
                { "Details", alert.Details },
                { "IsSent", alert.IsSent },
                { "AlertTime", alert.AlertTime }
            };

            await _tableClient.AddEntityAsync(entity);
            return alert;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存告警历史失败");
            throw;
        }
    }

    public async Task<AlertHistory?> GetByIdAsync(string id)
    {
        try
        {
            // 需要查询所有分区，因为我们不知道日期
            await foreach (var entity in _tableClient.QueryAsync<TableEntity>(filter: $"RowKey eq '{id}'", maxPerPage: 1))
            {
                return EntityToModel(entity);
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取告警历史失败: {Id}", id);
            return null;
        }
    }

    public async Task<(IEnumerable<AlertHistory> Items, int TotalCount)> GetAllAsync(
        int pageNumber = 1,
        int pageSize = 50,
        AlertHistoryType? type = null,
        string? symbol = null,
        DateTime? startTime = null,
        DateTime? endTime = null)
    {
        try
        {
            var results = new List<AlertHistory>();
            startTime ??= DateTime.UtcNow.AddDays(-30);
            endTime ??= DateTime.UtcNow;

            // 遍历日期范围内的所有分区
            for (var date = startTime.Value.Date; date <= endTime.Value.Date; date = date.AddDays(1))
            {
                var partitionKey = $"{PartitionKeyPrefix}_{date:yyyyMMdd}";
                var filter = $"PartitionKey eq '{partitionKey}'";

                if (!string.IsNullOrEmpty(symbol))
                    filter += $" and Symbol eq '{symbol}'";

                await foreach (var entity in _tableClient.QueryAsync<TableEntity>(filter: filter))
                {
                    var alert = EntityToModel(entity);
                    if (alert != null)
                    {
                        if (!type.HasValue || alert.Type == type.Value)
                            results.Add(alert);
                    }
                }
            }

            var totalCount = results.Count;
            var items = results
                .OrderByDescending(a => a.AlertTime)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize);

            return (items, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取所有告警历史失败");
            return (Enumerable.Empty<AlertHistory>(), 0);
        }
    }

    public async Task<IEnumerable<AlertHistory>> GetRecentAsync(int count = 100)
    {
        try
        {
            var results = new List<AlertHistory>();
            var today = DateTime.UtcNow.ToString("yyyyMMdd");
            var yesterday = DateTime.UtcNow.AddDays(-1).ToString("yyyyMMdd");

            // 查询最近2天的数据
            await foreach (var entity in _tableClient.QueryAsync<TableEntity>(
                filter: $"PartitionKey eq '{PartitionKeyPrefix}_{today}' or PartitionKey eq '{PartitionKeyPrefix}_{yesterday}'",
                maxPerPage: count))
            {
                var alert = EntityToModel(entity);
                if (alert != null)
                    results.Add(alert);
            }

            return results.OrderByDescending(a => a.AlertTime).Take(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取最近告警历史失败");
            return Enumerable.Empty<AlertHistory>();
        }
    }

    public async Task<IEnumerable<AlertHistory>> GetBySymbolAsync(string symbol, int limit = 100)
    {
        try
        {
            var results = new List<AlertHistory>();
            var startDate = DateTime.UtcNow.AddDays(-30);
            var endDate = DateTime.UtcNow;

            // 遍历最近30天的分区
            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                var partitionKey = $"{PartitionKeyPrefix}_{date:yyyyMMdd}";
                await foreach (var entity in _tableClient.QueryAsync<TableEntity>(
                    filter: $"PartitionKey eq '{partitionKey}' and Symbol eq '{symbol}'"))
                {
                    var alert = EntityToModel(entity);
                    if (alert != null)
                        results.Add(alert);
                }
            }

            return results.OrderByDescending(a => a.AlertTime).Take(limit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按品种获取告警历史失败: {Symbol}", symbol);
            return Enumerable.Empty<AlertHistory>();
        }
    }

    public async Task<IEnumerable<AlertHistory>> GetByTypeAsync(AlertHistoryType type, int limit = 100)
    {
        try
        {
            var results = new List<AlertHistory>();
            var startDate = DateTime.UtcNow.AddDays(-30);
            var endDate = DateTime.UtcNow;

            // 遍历最近30天的分区
            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                var partitionKey = $"{PartitionKeyPrefix}_{date:yyyyMMdd}";
                await foreach (var entity in _tableClient.QueryAsync<TableEntity>(
                    filter: $"PartitionKey eq '{partitionKey}' and Type eq '{type}'"))
                {
                    var alert = EntityToModel(entity);
                    if (alert != null)
                        results.Add(alert);
                }
            }

            return results.OrderByDescending(a => a.AlertTime).Take(limit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按类型获取告警历史失败: {Type}", type);
            return Enumerable.Empty<AlertHistory>();
        }
    }

    public async Task<int> DeleteOldRecordsAsync(DateTime beforeDate)
    {
        try
        {
            var count = 0;
            var startDate = beforeDate.AddDays(-90); // 假设最多查90天

            for (var date = startDate.Date; date < beforeDate.Date; date = date.AddDays(1))
            {
                var partitionKey = $"{PartitionKeyPrefix}_{date:yyyyMMdd}";
                await foreach (var entity in _tableClient.QueryAsync<TableEntity>(
                    filter: $"PartitionKey eq '{partitionKey}'"))
                {
                    await _tableClient.DeleteEntityAsync(entity.PartitionKey, entity.RowKey);
                    count++;
                }
            }

            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除旧记录失败");
            return 0;
        }
    }

    private AlertHistory? EntityToModel(TableEntity entity)
    {
        try
        {
            return new AlertHistory
            {
                Id = entity.RowKey,
                Symbol = entity.GetString("Symbol") ?? string.Empty,
                Type = Enum.TryParse<AlertHistoryType>(entity.GetString("Type"), out var type) ? type : AlertHistoryType.PriceAlert,
                Message = entity.GetString("Message") ?? string.Empty,
                Details = entity.GetString("Details") ?? string.Empty,
                IsSent = entity.GetBoolean("IsSent") ?? false,
                AlertTime = entity.GetDateTimeOffset("AlertTime")?.DateTime ?? DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "转换告警历史实体失败");
            return null;
        }
    }
}
