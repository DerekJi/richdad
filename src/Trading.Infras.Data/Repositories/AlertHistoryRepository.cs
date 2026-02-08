using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Logging;
using Trading.Infras.Data.Infrastructure;
using Trading.Infras.Data.Models;

namespace Trading.Infras.Data.Repositories;

/// <summary>
/// 告警历史仓储实现
/// </summary>
public class AlertHistoryRepository : IAlertHistoryRepository
{
    private readonly CosmosDbContext _context;
    private readonly ILogger<AlertHistoryRepository> _logger;

    public AlertHistoryRepository(
        CosmosDbContext context,
        ILogger<AlertHistoryRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<AlertHistory> AddAsync(AlertHistory alertHistory)
    {
        try
        {
            var response = await _context.EmaAlertsContainer.CreateItemAsync(
                alertHistory,
                new PartitionKey(alertHistory.Symbol));

            _logger.LogInformation("告警历史已保存: {Id}, 类型: {Type}, 品种: {Symbol}",
                response.Resource.Id, response.Resource.Type, response.Resource.Symbol);

            return response.Resource;
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
            var query = _context.EmaAlertsContainer
                .GetItemLinqQueryable<AlertHistory>()
                .Where(a => a.Id == id);

            var iterator = query.ToFeedIterator();
            var response = await iterator.ReadNextAsync();

            return response.FirstOrDefault();
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取告警历史失败: {Id}", id);
            throw;
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
            var query = _context.EmaAlertsContainer
                .GetItemLinqQueryable<AlertHistory>();

            // 应用筛选条件
            IQueryable<AlertHistory> filteredQuery = query;

            if (type.HasValue)
            {
                filteredQuery = filteredQuery.Where(a => a.Type == type.Value);
            }

            if (!string.IsNullOrEmpty(symbol))
            {
                filteredQuery = filteredQuery.Where(a => a.Symbol == symbol);
            }

            if (startTime.HasValue)
            {
                filteredQuery = filteredQuery.Where(a => a.AlertTime >= startTime.Value);
            }

            if (endTime.HasValue)
            {
                filteredQuery = filteredQuery.Where(a => a.AlertTime <= endTime.Value);
            }

            // 按时间倒序排列
            filteredQuery = filteredQuery.OrderByDescending(a => a.AlertTime);

            // 获取总数（注意：这可能会比较慢，实际生产中可能需要优化）
            var countIterator = filteredQuery.ToFeedIterator();
            var allItems = new List<AlertHistory>();
            while (countIterator.HasMoreResults)
            {
                var response = await countIterator.ReadNextAsync();
                allItems.AddRange(response);
            }
            var totalCount = allItems.Count;

            // 分页
            var pagedItems = allItems
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return (pagedItems, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询告警历史失败");
            throw;
        }
    }

    public async Task<IEnumerable<AlertHistory>> GetRecentAsync(int count = 100)
    {
        try
        {
            var query = _context.EmaAlertsContainer
                .GetItemLinqQueryable<AlertHistory>()
                .OrderByDescending(a => a.AlertTime)
                .Take(count);

            var iterator = query.ToFeedIterator();
            var results = new List<AlertHistory>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取最近告警历史失败");
            throw;
        }
    }

    public async Task<IEnumerable<AlertHistory>> GetBySymbolAsync(string symbol, int limit = 100)
    {
        try
        {
            var query = _context.EmaAlertsContainer
                .GetItemLinqQueryable<AlertHistory>()
                .Where(a => a.Symbol == symbol)
                .OrderByDescending(a => a.AlertTime)
                .Take(limit);

            var iterator = query.ToFeedIterator();
            var results = new List<AlertHistory>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取品种告警历史失败: {Symbol}", symbol);
            throw;
        }
    }

    public async Task<IEnumerable<AlertHistory>> GetByTypeAsync(AlertHistoryType type, int limit = 100)
    {
        try
        {
            var query = _context.EmaAlertsContainer
                .GetItemLinqQueryable<AlertHistory>()
                .Where(a => a.Type == type)
                .OrderByDescending(a => a.AlertTime)
                .Take(limit);

            var iterator = query.ToFeedIterator();
            var results = new List<AlertHistory>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取指定类型告警历史失败: {Type}", type);
            throw;
        }
    }

    public async Task<int> DeleteOldRecordsAsync(DateTime beforeDate)
    {
        try
        {
            var query = _context.EmaAlertsContainer
                .GetItemLinqQueryable<AlertHistory>()
                .Where(a => a.AlertTime < beforeDate);

            var iterator = query.ToFeedIterator();
            var deleteCount = 0;

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                foreach (var item in response)
                {
                    await _context.EmaAlertsContainer.DeleteItemAsync<AlertHistory>(
                        item.Id,
                        new PartitionKey(item.Symbol));
                    deleteCount++;
                }
            }

            _logger.LogInformation("删除了 {Count} 条旧告警历史记录", deleteCount);
            return deleteCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除旧告警历史失败");
            throw;
        }
    }
}
