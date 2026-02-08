## Issue 9: 实现回测与历史分析系统

### 标题
📊 Implement Backtesting and Historical Analysis System with AI Decision Audit

### 描述
实现完整的回测系统，验证 Al Brooks 形态识别和四级 AI 决策在历史数据上的表现，为 FTMO 考试提供策略验证。

### 背景
在进行真实交易之前，必须验证策略的有效性。回测系统需要：
- **模拟四级 AI 决策**：在历史数据上运行完整决策流程
- **跳过人工确认**：自动执行所有 AI 建议的交易
- **完整审计追踪**：记录每笔交易的 AI 推理过程
- **统计分析**：计算胜率、盈亏比、最大回撤等指标
- **FTMO 风控模拟**：验证是否满足 5% 日损和 10% 总损要求

### 实现功能

#### ✅ 1. 回测引擎核心

**新增服务：** `BacktestEngine`

```csharp
public class BacktestEngine
{
    private readonly TradingOrchestrationService _orchestration;
    private readonly MarketDataCacheService _dataService;
    private readonly IBacktestRepository _repository;
    private readonly ILogger<BacktestEngine> _logger;

    /// <summary>
    /// 运行回测
    /// </summary>
    public async Task<BacktestResult> RunBacktestAsync(BacktestConfig config)
    {
        _logger.LogInformation(
            "开始回测: {Symbol} from {StartDate} to {EndDate}",
            config.Symbol, config.StartDate, config.EndDate);

        var result = new BacktestResult
        {
            Config = config,
            StartTime = DateTime.UtcNow
        };

        // 1. 加载历史数据
        var candles = await LoadHistoricalDataAsync(
            config.Symbol, config.StartDate, config.EndDate);

        _logger.LogInformation("加载 {Count} 根 K 线数据", candles.Count);

        // 2. 初始化虚拟账户
        var account = new VirtualAccount
        {
            InitialBalance = config.InitialBalance,
            Balance = config.InitialBalance,
            Equity = config.InitialBalance,
            MaxDailyLossPercent = config.MaxDailyLossPercent,
            MaxTotalLossPercent = config.MaxTotalLossPercent
        };

        // 3. 按时间顺序模拟交易
        var currentDate = config.StartDate;
        var tradeNumber = 0;

        while (currentDate <= config.EndDate)
        {
            // 检查风控限制
            if (account.IsDailyLossLimitReached() || account.IsTotalLossLimitReached())
            {
                _logger.LogWarning(
                    "触发风控限制 @ {Date}, 日损: {DailyLoss}%, 总损: {TotalLoss}%",
                    currentDate, account.GetDailyLossPercent(), account.GetTotalLossPercent());

                // 如果是日损，重置到第二天
                if (account.IsDailyLossLimitReached())
                {
                    currentDate = currentDate.AddDays(1);
                    account.ResetDailyLoss();
                    continue;
                }
                else
                {
                    // 总损限制，终止回测
                    result.TerminationReason = "Max total loss reached";
                    break;
                }
            }

            // 4. 执行 AI 决策（回测模式）
            var decision = await ExecuteAIDecisionInBacktestModeAsync(
                config.Symbol, currentDate, candles);

            // 5. 如果 AI 决定开仓，执行虚拟交易
            if (decision.Action == "Execute")
            {
                tradeNumber++;

                var trade = new BacktestTrade
                {
                    TradeNumber = tradeNumber,
                    Symbol = config.Symbol,
                    Direction = decision.Direction,
                    EntryTime = currentDate,
                    EntryPrice = decision.EntryPrice,
                    StopLoss = decision.StopLoss,
                    TakeProfit = decision.TakeProfit,
                    LotSize = decision.LotSize,

                    // 保存 AI 决策上下文
                    L1_DailyBias = decision.Context.L1_DailyBias,
                    L2_Structure = decision.Context.L2_Structure,
                    L3_Signal = decision.Context.L3_Signal,
                    L4_Reasoning = decision.Reasoning,
                    L4_ThinkingProcess = decision.ThinkingProcess
                };

                // 6. 模拟交易执行和平仓
                await SimulateTradeExecutionAsync(trade, candles, account);

                result.Trades.Add(trade);

                _logger.LogInformation(
                    "交易 #{Number}: {Direction} @ {Entry}, PnL: {PnL} ({PnLPercent:F2}%)",
                    tradeNumber, trade.Direction, trade.EntryPrice,
                    trade.ProfitLoss, trade.ProfitLossPercent);
            }

            // 7. 前进到下一个时间点
            currentDate = GetNextAnalysisTime(currentDate, config.TimeFrame);
        }

        // 8. 计算回测统计
        result.EndTime = DateTime.UtcNow;
        result.FinalBalance = account.Balance;
        result.TotalReturn = (account.Balance - config.InitialBalance) / config.InitialBalance;
        result.TotalTrades = result.Trades.Count;
        result.WinningTrades = result.Trades.Count(t => t.ProfitLoss > 0);
        result.LosingTrades = result.Trades.Count(t => t.ProfitLoss < 0);
        result.WinRate = result.TotalTrades > 0
            ? (double)result.WinningTrades / result.TotalTrades
            : 0;
        result.AverageProfitLoss = result.Trades.Any()
            ? result.Trades.Average(t => t.ProfitLoss)
            : 0;
        result.MaxDrawdown = CalculateMaxDrawdown(result.Trades, config.InitialBalance);

        // 9. 保存回测结果
        await _repository.SaveBacktestResultAsync(result);

        _logger.LogInformation(
            "回测完成: {Trades} 笔交易, 胜率: {WinRate:P2}, 总收益: {Return:P2}",
            result.TotalTrades, result.WinRate, result.TotalReturn);

        return result;
    }

    /// <summary>
    /// 在回测模式下执行 AI 决策
    /// </summary>
    private async Task<FinalDecision> ExecuteAIDecisionInBacktestModeAsync(
        string symbol,
        DateTime analysisTime,
        List<Candle> allCandles)
    {
        // 获取到 analysisTime 为止的历史数据
        var historicalData = allCandles
            .Where(c => c.Time <= analysisTime)
            .ToList();

        // 模拟实时环境，只使用到当前时间的数据
        // 这里需要创建一个临时的数据上下文
        var context = new BacktestContext
        {
            CurrentTime = analysisTime,
            AvailableData = historicalData
        };

        // 执行四级 AI 决策
        var decision = await _orchestration.ExecuteTradingPipelineAsync(
            symbol, context);

        return decision;
    }

    /// <summary>
    /// 模拟交易执行和平仓
    /// </summary>
    private async Task SimulateTradeExecutionAsync(
        BacktestTrade trade,
        List<Candle> candles,
        VirtualAccount account)
    {
        // 查找入场后的 K 线数据
        var futureCandles = candles
            .Where(c => c.Time > trade.EntryTime)
            .OrderBy(c => c.Time)
            .ToList();

        foreach (var candle in futureCandles)
        {
            // 检查止损
            if (trade.Direction == "Buy" && candle.Low <= trade.StopLoss)
            {
                trade.ExitTime = candle.Time;
                trade.ExitPrice = trade.StopLoss;
                trade.ExitReason = "Stop Loss";
                break;
            }
            else if (trade.Direction == "Sell" && candle.High >= trade.StopLoss)
            {
                trade.ExitTime = candle.Time;
                trade.ExitPrice = trade.StopLoss;
                trade.ExitReason = "Stop Loss";
                break;
            }

            // 检查止盈
            if (trade.Direction == "Buy" && candle.High >= trade.TakeProfit)
            {
                trade.ExitTime = candle.Time;
                trade.ExitPrice = trade.TakeProfit;
                trade.ExitReason = "Take Profit";
                break;
            }
            else if (trade.Direction == "Sell" && candle.Low <= trade.TakeProfit)
            {
                trade.ExitTime = candle.Time;
                trade.ExitPrice = trade.TakeProfit;
                trade.ExitReason = "Take Profit";
                break;
            }

            // 可选：添加时间止损（如持仓超过 24 小时强制平仓）
            if ((candle.Time - trade.EntryTime).TotalHours > 24)
            {
                trade.ExitTime = candle.Time;
                trade.ExitPrice = candle.Close;
                trade.ExitReason = "Time Stop";
                break;
            }
        }

        // 如果遍历完所有数据还没平仓，按最后价格平仓
        if (trade.ExitTime == null)
        {
            var lastCandle = futureCandles.Last();
            trade.ExitTime = lastCandle.Time;
            trade.ExitPrice = lastCandle.Close;
            trade.ExitReason = "End of Data";
        }

        // 计算盈亏
        if (trade.Direction == "Buy")
        {
            trade.ProfitLoss = (trade.ExitPrice - trade.EntryPrice) * trade.LotSize * 100000;
        }
        else
        {
            trade.ProfitLoss = (trade.EntryPrice - trade.ExitPrice) * trade.LotSize * 100000;
        }

        trade.ProfitLossPercent = trade.ProfitLoss / account.Balance;

        // 更新账户
        account.Balance += trade.ProfitLoss;
        account.Equity = account.Balance;
        account.AddTradeToHistory(trade);
    }

    /// <summary>
    /// 计算最大回撤
    /// </summary>
    private double CalculateMaxDrawdown(List<BacktestTrade> trades, double initialBalance)
    {
        var equity = initialBalance;
        var peak = initialBalance;
        var maxDrawdown = 0.0;

        foreach (var trade in trades.OrderBy(t => t.ExitTime))
        {
            equity += trade.ProfitLoss;

            if (equity > peak)
                peak = equity;

            var drawdown = (peak - equity) / peak;
            if (drawdown > maxDrawdown)
                maxDrawdown = drawdown;
        }

        return maxDrawdown;
    }
}
```

#### ✅ 2. 虚拟账户管理

**VirtualAccount.cs:**

```csharp
public class VirtualAccount
{
    public double InitialBalance { get; set; }
    public double Balance { get; set; }
    public double Equity { get; set; }

    // FTMO 风控限制
    public double MaxDailyLossPercent { get; set; } = 5.0;
    public double MaxTotalLossPercent { get; set; } = 10.0;

    // 每日统计
    public DateTime CurrentDay { get; set; }
    public double DailyStartBalance { get; set; }
    public List<BacktestTrade> DailyTrades { get; set; } = new();

    // 历史记录
    public List<BacktestTrade> AllTrades { get; set; } = new();

    public bool IsDailyLossLimitReached()
    {
        var dailyLoss = DailyStartBalance - Balance;
        var dailyLossPercent = (dailyLoss / DailyStartBalance) * 100;
        return dailyLossPercent >= MaxDailyLossPercent;
    }

    public bool IsTotalLossLimitReached()
    {
        var totalLoss = InitialBalance - Balance;
        var totalLossPercent = (totalLoss / InitialBalance) * 100;
        return totalLossPercent >= MaxTotalLossPercent;
    }

    public double GetDailyLossPercent()
    {
        var dailyLoss = DailyStartBalance - Balance;
        return (dailyLoss / DailyStartBalance) * 100;
    }

    public double GetTotalLossPercent()
    {
        var totalLoss = InitialBalance - Balance;
        return (totalLoss / InitialBalance) * 100;
    }

    public void ResetDailyLoss()
    {
        CurrentDay = CurrentDay.AddDays(1);
        DailyStartBalance = Balance;
        DailyTrades.Clear();
    }

    public void AddTradeToHistory(BacktestTrade trade)
    {
        AllTrades.Add(trade);
        DailyTrades.Add(trade);
    }
}
```

#### ✅ 3. 数据模型

**BacktestConfig.cs:**

```csharp
public class BacktestConfig
{
    public string Symbol { get; set; } = "XAUUSD";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string TimeFrame { get; set; } = "M5";

    // 账户配置
    public double InitialBalance { get; set; } = 100000;
    public double MaxDailyLossPercent { get; set; } = 5.0;
    public double MaxTotalLossPercent { get; set; } = 10.0;

    // AI 配置
    public bool UseL4DeepSeekR1 { get; set; } = true;
    public int MinConfidenceScore { get; set; } = 75;
}

public class BacktestResult
{
    public BacktestConfig Config { get; set; } = new();
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    // 交易统计
    public List<BacktestTrade> Trades { get; set; } = new();
    public int TotalTrades { get; set; }
    public int WinningTrades { get; set; }
    public int LosingTrades { get; set; }
    public double WinRate { get; set; }
    public double AverageProfitLoss { get; set; }
    public double MaxDrawdown { get; set; }

    // 账户结果
    public double FinalBalance { get; set; }
    public double TotalReturn { get; set; }

    // 终止原因
    public string? TerminationReason { get; set; }

    // 性能指标
    public double SharpeRatio { get; set; }
    public double ProfitFactor { get; set; }
    public int MaxConsecutiveLosses { get; set; }
    public int MaxConsecutiveWins { get; set; }
}

public class BacktestTrade
{
    public int TradeNumber { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty; // Buy/Sell

    // 交易数据
    public DateTime? EntryTime { get; set; }
    public double EntryPrice { get; set; }
    public DateTime? ExitTime { get; set; }
    public double ExitPrice { get; set; }
    public string? ExitReason { get; set; }

    public double StopLoss { get; set; }
    public double TakeProfit { get; set; }
    public double LotSize { get; set; }

    // 盈亏
    public double ProfitLoss { get; set; }
    public double ProfitLossPercent { get; set; }

    // AI 决策上下文（审计追踪）
    public DailyBias? L1_DailyBias { get; set; }
    public StructureAnalysis? L2_Structure { get; set; }
    public SignalDetection? L3_Signal { get; set; }
    public string L4_Reasoning { get; set; } = string.Empty;
    public string L4_ThinkingProcess { get; set; } = string.Empty;
}
```

#### ✅ 4. 回测 API

**BacktestController.cs:**

```csharp
[ApiController]
[Route("api/[controller]")]
public class BacktestController : ControllerBase
{
    private readonly BacktestEngine _engine;
    private readonly IBacktestRepository _repository;

    /// <summary>
    /// 启动回测
    /// POST /api/backtest/run
    /// </summary>
    [HttpPost("run")]
    public async Task<ActionResult<BacktestResult>> RunBacktest(
        [FromBody] BacktestConfig config)
    {
        var result = await _engine.RunBacktestAsync(config);
        return Ok(result);
    }

    /// <summary>
    /// 获取回测历史
    /// GET /api/backtest/history
    /// </summary>
    [HttpGet("history")]
    public async Task<ActionResult<List<BacktestResult>>> GetBacktestHistory()
    {
        var history = await _repository.GetBacktestHistoryAsync();
        return Ok(history);
    }

    /// <summary>
    /// 获取特定回测详情
    /// GET /api/backtest/{id}
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<BacktestResult>> GetBacktestDetails(string id)
    {
        var result = await _repository.GetBacktestByIdAsync(id);
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    /// <summary>
    /// 获取交易详情（包含 AI 推理过程）
    /// GET /api/backtest/{id}/trades/{tradeNumber}
    /// </summary>
    [HttpGet("{id}/trades/{tradeNumber}")]
    public async Task<ActionResult<BacktestTrade>> GetTradeDetails(
        string id,
        int tradeNumber)
    {
        var trade = await _repository.GetTradeDetailsAsync(id, tradeNumber);
        if (trade == null)
            return NotFound();

        return Ok(trade);
    }

    /// <summary>
    /// 批量回测（多个时间段）
    /// POST /api/backtest/batch
    /// </summary>
    [HttpPost("batch")]
    public async Task<ActionResult<List<BacktestResult>>> RunBatchBacktest(
        [FromBody] List<BacktestConfig> configs)
    {
        var results = new List<BacktestResult>();

        foreach (var config in configs)
        {
            var result = await _engine.RunBacktestAsync(config);
            results.Add(result);
        }

        return Ok(results);
    }
}
```

#### ✅ 5. Web 可视化界面

**backtest.html:**

```html
<!DOCTYPE html>
<html>
<head>
    <title>Backtest Results</title>
    <script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
    <style>
        /* 样式省略 */
    </style>
</head>
<body>
    <div class="container">
        <h1>📊 Backtest Results</h1>

        <!-- 回测配置 -->
        <div class="config-panel">
            <h2>Run Backtest</h2>
            <form id="backtestForm">
                <label>Symbol:</label>
                <select name="symbol">
                    <option value="XAUUSD">XAUUSD</option>
                    <option value="EURUSD">EURUSD</option>
                </select>

                <label>Start Date:</label>
                <input type="date" name="startDate" required>

                <label>End Date:</label>
                <input type="date" name="endDate" required>

                <label>Initial Balance:</label>
                <input type="number" name="initialBalance" value="100000">

                <button type="submit">Run Backtest</button>
            </form>
        </div>

        <!-- 统计摘要 -->
        <div class="summary-panel">
            <h2>Summary</h2>
            <div class="stats-grid">
                <div class="stat-card">
                    <div class="stat-label">Total Trades</div>
                    <div class="stat-value" id="totalTrades">-</div>
                </div>
                <div class="stat-card">
                    <div class="stat-label">Win Rate</div>
                    <div class="stat-value" id="winRate">-</div>
                </div>
                <div class="stat-card">
                    <div class="stat-label">Total Return</div>
                    <div class="stat-value" id="totalReturn">-</div>
                </div>
                <div class="stat-card">
                    <div class="stat-label">Max Drawdown</div>
                    <div class="stat-value" id="maxDrawdown">-</div>
                </div>
            </div>
        </div>

        <!-- 权益曲线图 -->
        <div class="chart-panel">
            <h2>Equity Curve</h2>
            <canvas id="equityChart"></canvas>
        </div>

        <!-- 交易列表 -->
        <div class="trades-panel">
            <h2>Trades</h2>
            <table id="tradesTable">
                <thead>
                    <tr>
                        <th>#</th>
                        <th>Entry Time</th>
                        <th>Direction</th>
                        <th>Entry</th>
                        <th>Exit</th>
                        <th>P/L</th>
                        <th>Reason</th>
                        <th>Details</th>
                    </tr>
                </thead>
                <tbody></tbody>
            </table>
        </div>
    </div>

    <script>
        // JavaScript 实现省略
    </script>
</body>
</html>
```

### 验收标准

**功能完整性：**
- [ ] 成功加载历史数据并按时间顺序处理
- [ ] 四级 AI 决策在回测模式下正常工作
- [ ] 虚拟交易执行和平仓逻辑正确
- [ ] FTMO 风控限制正确触发

**统计准确性：**
- [ ] 胜率计算准确
- [ ] 盈亏计算准确
- [ ] 最大回撤计算准确
- [ ] 连续盈亏统计正确

**审计追踪：**
- [ ] 每笔交易保存完整 AI 推理过程
- [ ] 可查看 L1/L2/L3/L4 各级决策
- [ ] DeepSeek-R1 思维链完整保存

**性能：**
- [ ] 1 个月数据回测 < 5 分钟
- [ ] 并发回测支持
- [ ] 内存占用合理

### 相关文件

**新增文件：**
- `Trading.Backtest/Engine/BacktestEngine.cs`
- `Trading.Backtest/Models/VirtualAccount.cs`
- `Trading.Backtest/Models/BacktestConfig.cs`
- `Trading.Backtest/Models/BacktestResult.cs`
- `Trading.Backtest.Web/Controllers/BacktestController.cs`
- `Trading.Backtest.Web/wwwroot/backtest.html`

**文档：**
- `docs/BACKTEST_GUIDE.md` - 回测使用指南
- `docs/FTMO_RULES.md` - FTMO 规则说明

### 标签
`backtest`, `testing`, `analysis`, `ftmo`, `audit`

---

