using Microsoft.Extensions.Logging;
using Trading.Core.Trading;
using Trading.Models;

namespace Trading.Services.Adapters;

/// <summary>
/// 模拟订单执行服务（用于测试和演示）
/// </summary>
public class MockOrderExecutionService : IOrderExecutionService
{
    private readonly ILogger<MockOrderExecutionService> _logger;
    private readonly Dictionary<string, Position> _positions = new();
    private int _orderCounter = 1000;

    public string PlatformName => "Mock (Demo)";

    public MockOrderExecutionService(ILogger<MockOrderExecutionService> logger)
    {
        _logger = logger;
    }

    public Task<OrderResult> PlaceMarketOrderAsync(
        string symbol,
        decimal lots,
        OrderDirection direction,
        decimal? stopLoss = null,
        decimal? takeProfit = null,
        string? comment = null)
    {
        try
        {
            var orderId = $"MOCK_{_orderCounter++}";
            var executedPrice = GenerateMockPrice(symbol);
            var executedTime = DateTime.UtcNow;

            // 创建模拟持仓
            var position = new Position
            {
                PositionId = orderId,
                Symbol = symbol,
                Direction = direction,
                Lots = lots,
                OpenPrice = executedPrice,
                CurrentPrice = executedPrice,
                StopLoss = stopLoss,
                TakeProfit = takeProfit,
                ProfitLoss = 0,
                OpenTime = executedTime,
                Comment = comment
            };

            _positions[orderId] = position;

            _logger.LogInformation("模拟下单成功: {OrderId} {Symbol} {Lots} {Direction} @{Price}",
                orderId, symbol, lots, direction, executedPrice);

            return Task.FromResult(new OrderResult
            {
                Success = true,
                OrderId = orderId,
                ExecutedPrice = executedPrice,
                ExecutedLots = lots,
                ExecutedTime = executedTime,
                Message = $"模拟订单已创建（仅供测试）"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "模拟下单失败: {Symbol} {Lots} {Direction}", symbol, lots, direction);
            return Task.FromResult(new OrderResult
            {
                Success = false,
                Message = $"模拟下单失败: {ex.Message}",
                ErrorCode = "MOCK_ORDER_ERROR"
            });
        }
    }

    public Task<OrderResult> PlaceLimitOrderAsync(
        string symbol,
        decimal lots,
        OrderDirection direction,
        decimal limitPrice,
        decimal? stopLoss = null,
        decimal? takeProfit = null,
        string? comment = null)
    {
        var orderId = $"MOCK_{_orderCounter++}";
        _logger.LogInformation("模拟限价单: {OrderId} {Symbol} {Lots} {Direction} @{LimitPrice}",
            orderId, symbol, lots, direction, limitPrice);

        return Task.FromResult(new OrderResult
        {
            Success = true,
            OrderId = orderId,
            ExecutedPrice = limitPrice,
            ExecutedLots = lots,
            ExecutedTime = DateTime.UtcNow,
            Message = "模拟限价单已创建（仅供测试）"
        });
    }

    public Task<OrderStatus> GetOrderStatusAsync(string orderId)
    {
        if (!_positions.TryGetValue(orderId, out var position))
        {
            throw new KeyNotFoundException($"订单 {orderId} 不存在");
        }

        var status = new OrderStatus
        {
            OrderId = position.PositionId,
            Symbol = position.Symbol,
            Direction = position.Direction,
            Lots = position.Lots,
            OpenPrice = position.OpenPrice,
            CurrentPrice = position.CurrentPrice,
            StopLoss = position.StopLoss,
            TakeProfit = position.TakeProfit,
            ProfitLoss = position.ProfitLoss,
            OpenTime = position.OpenTime,
            State = OrderState.Open,
            Comment = position.Comment
        };

        return Task.FromResult(status);
    }

    public Task<bool> ModifyOrderAsync(string orderId, decimal? newStopLoss = null, decimal? newTakeProfit = null)
    {
        if (!_positions.TryGetValue(orderId, out var position))
        {
            _logger.LogWarning("修改订单失败: 订单 {OrderId} 不存在", orderId);
            return Task.FromResult(false);
        }

        if (newStopLoss.HasValue)
            position.StopLoss = newStopLoss.Value;

        if (newTakeProfit.HasValue)
            position.TakeProfit = newTakeProfit.Value;

        _logger.LogInformation("模拟修改订单成功: {OrderId} SL:{StopLoss} TP:{TakeProfit}",
            orderId, position.StopLoss, position.TakeProfit);

        return Task.FromResult(true);
    }

    public Task<bool> CloseOrderAsync(string orderId, decimal? lots = null)
    {
        if (!_positions.Remove(orderId))
        {
            _logger.LogWarning("平仓失败: 订单 {OrderId} 不存在", orderId);
            return Task.FromResult(false);
        }

        _logger.LogInformation("模拟平仓成功: {OrderId}", orderId);
        return Task.FromResult(true);
    }

    public Task<List<Position>> GetOpenPositionsAsync(string? symbol = null)
    {
        var positions = _positions.Values.ToList();

        if (!string.IsNullOrEmpty(symbol))
        {
            positions = positions.Where(p => p.Symbol == symbol).ToList();
        }

        return Task.FromResult(positions);
    }

    /// <summary>
    /// 生成模拟价格（基于品种）
    /// </summary>
    private decimal GenerateMockPrice(string symbol)
    {
        return symbol.ToUpperInvariant() switch
        {
            "XAU_USD" or "XAUUSD" => 2650m + (decimal)(Random.Shared.NextDouble() * 10 - 5),
            "XAG_USD" or "XAGUSD" => 30m + (decimal)(Random.Shared.NextDouble() * 2 - 1),
            "EUR_USD" or "EURUSD" => 1.08m + (decimal)(Random.Shared.NextDouble() * 0.01),
            "GBP_USD" or "GBPUSD" => 1.28m + (decimal)(Random.Shared.NextDouble() * 0.01),
            "USD_JPY" or "USDJPY" => 148m + (decimal)(Random.Shared.NextDouble() * 2),
            _ => 100m
        };
    }
}
