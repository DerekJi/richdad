using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Trading.AlertSystem.Data.Models;
using Trading.AlertSystem.Data.Infrastructure;

namespace Trading.AlertSystem.Data.Repositories;

/// <summary>
/// AI分析历史记录仓储实现
/// </summary>
public class CosmosAIAnalysisRepository : IAIAnalysisRepository
{
    private readonly Container _container;
    private readonly ILogger<CosmosAIAnalysisRepository> _logger;

    public CosmosAIAnalysisRepository(
        CosmosDbContext dbContext,
        ILogger<CosmosAIAnalysisRepository> logger)
    {
        _container = dbContext.GetContainer("AIAnalysisHistory");
        _logger = logger;
    }

    public async Task SaveAnalysisAsync(AIAnalysisHistory analysis)
    {
        try
        {
            await _container.CreateItemAsync(analysis, new PartitionKey(analysis.Symbol));
            _logger.LogDebug("AI分析记录已保存: {AnalysisType} {Symbol}", analysis.AnalysisType, analysis.Symbol);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存AI分析记录失败: {AnalysisType} {Symbol}", analysis.AnalysisType, analysis.Symbol);
            throw;
        }
    }

    public async Task<List<AIAnalysisHistory>> GetRecentAnalysesAsync(int count = 100)
    {
        try
        {
            var query = new QueryDefinition(
                "SELECT TOP @count * FROM c ORDER BY c.analysisTime DESC")
                .WithParameter("@count", count);

            var iterator = _container.GetItemQueryIterator<AIAnalysisHistory>(query);
            var results = new List<AIAnalysisHistory>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询最近AI分析记录失败");
            return new List<AIAnalysisHistory>();
        }
    }

    public async Task<List<AIAnalysisHistory>> GetAnalysesBySymbolAsync(
        string symbol,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        try
        {
            var queryText = "SELECT * FROM c WHERE c.symbol = @symbol";
            var query = new QueryDefinition(queryText).WithParameter("@symbol", symbol);

            if (startDate.HasValue)
            {
                queryText += " AND c.analysisTime >= @startDate";
                query = query.WithParameter("@startDate", startDate.Value);
            }

            if (endDate.HasValue)
            {
                queryText += " AND c.analysisTime <= @endDate";
                query = query.WithParameter("@endDate", endDate.Value);
            }

            queryText += " ORDER BY c.analysisTime DESC";
            query = new QueryDefinition(queryText);

            if (startDate.HasValue)
                query = query.WithParameter("@startDate", startDate.Value);
            if (endDate.HasValue)
                query = query.WithParameter("@endDate", endDate.Value);
            query = query.WithParameter("@symbol", symbol);

            var iterator = _container.GetItemQueryIterator<AIAnalysisHistory>(query);
            var results = new List<AIAnalysisHistory>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按品种查询AI分析记录失败: {Symbol}", symbol);
            return new List<AIAnalysisHistory>();
        }
    }

    public async Task<List<AIAnalysisHistory>> GetAnalysesByTypeAsync(
        string analysisType,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        try
        {
            var queryText = "SELECT * FROM c WHERE c.analysisType = @analysisType";
            var query = new QueryDefinition(queryText);

            if (startDate.HasValue)
            {
                queryText += " AND c.analysisTime >= @startDate";
            }

            if (endDate.HasValue)
            {
                queryText += " AND c.analysisTime <= @endDate";
            }

            queryText += " ORDER BY c.analysisTime DESC";

            query = new QueryDefinition(queryText)
                .WithParameter("@analysisType", analysisType);

            if (startDate.HasValue)
                query = query.WithParameter("@startDate", startDate.Value);
            if (endDate.HasValue)
                query = query.WithParameter("@endDate", endDate.Value);

            var iterator = _container.GetItemQueryIterator<AIAnalysisHistory>(query);
            var results = new List<AIAnalysisHistory>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按类型查询AI分析记录失败: {AnalysisType}", analysisType);
            return new List<AIAnalysisHistory>();
        }
    }

    public async Task<AIAnalysisStatistics> GetStatisticsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        try
        {
            var queryText = "SELECT * FROM c WHERE 1=1";
            var query = new QueryDefinition(queryText);

            if (startDate.HasValue)
            {
                queryText += " AND c.analysisTime >= @startDate";
                query = query.WithParameter("@startDate", startDate.Value);
            }

            if (endDate.HasValue)
            {
                queryText += " AND c.analysisTime <= @endDate";
                query = query.WithParameter("@endDate", endDate.Value);
            }

            query = new QueryDefinition(queryText);
            if (startDate.HasValue)
                query = query.WithParameter("@startDate", startDate.Value);
            if (endDate.HasValue)
                query = query.WithParameter("@endDate", endDate.Value);

            var iterator = _container.GetItemQueryIterator<AIAnalysisHistory>(query);
            var allAnalyses = new List<AIAnalysisHistory>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                allAnalyses.AddRange(response);
            }

            return new AIAnalysisStatistics
            {
                TotalAnalyses = allAnalyses.Count,
                SuccessfulAnalyses = allAnalyses.Count(a => a.IsSuccess),
                FailedAnalyses = allAnalyses.Count(a => !a.IsSuccess),
                CachedAnalyses = allAnalyses.Count(a => a.FromCache),
                TotalEstimatedTokens = allAnalyses.Sum(a => a.EstimatedTokens),
                AverageResponseTimeMs = allAnalyses.Any() ? allAnalyses.Average(a => a.ResponseTimeMs) : 0,
                AnalysesByType = allAnalyses.GroupBy(a => a.AnalysisType)
                    .ToDictionary(g => g.Key, g => g.Count()),
                AnalysesBySymbol = allAnalyses.GroupBy(a => a.Symbol)
                    .ToDictionary(g => g.Key, g => g.Count())
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取AI分析统计信息失败");
            return new AIAnalysisStatistics();
        }
    }
}
