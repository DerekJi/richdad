using Trading.Data.Models;
using Trading.Data.Interfaces;

namespace Trading.Backtest.Console.Services;

/// <summary>
/// 数据库服务
/// 职责：保存回测结果到Cosmos DB
/// </summary>
public class DatabaseService
{
    private readonly IBacktestRepository _repository;

    public DatabaseService(IBacktestRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// 保存回测结果到数据库
    /// </summary>
    public async Task<string> SaveResultAsync(BacktestResult result)
    {
        System.Console.WriteLine("\n正在保存到Cosmos DB...");

        var id = await _repository.SaveBacktestResultAsync(result);

        System.Console.WriteLine($"保存成功! ID: {id}");
        return id;
    }
}
