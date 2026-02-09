## Issue 4: 重构基础设施项目架构

### 标题
🏗️ Refactor Infrastructure Projects and Add Unified Order Execution Interface

### 描述
重构现有代码架构，统一命名规范，添加订单执行抽象层，为AI Agent集成做准备。

### 背景
当前系统的基础设施项目命名不够清晰，且缺少统一的订单执行接口：
- 项目命名：`Trading.AlertSystem.*` 容易与业务逻辑混淆
- 订单接口：`IOandaService` 和 `ITradeLockerService` 接口不统一
- 缺少抽象：难以在不同平台间切换

通过此次重构，系统将：
- **清晰的命名**：基础设施项目统一使用 `Trading.Infras.*` 前缀
- **统一接口**：创建 `IOrderExecutionService` 抽象层
- **易于扩展**：未来添加新交易平台更简单
- **为AI Agent做准备**：提供清晰的API供Agent调用

### 实现功能

#### ✅ 1. 项目重命名

**重命名映射：**
```
Trading.AlertSystem.Data       → Trading.Infrastructure
Trading.AlertSystem.Service    → Trading.Services
Trading.AlertSystem.Web        → Trading.Infras.Web
Trading.AlertSystem.Mobile     → Trading.Infras.Mobile (如果存在)
```

**保持不变的项目：**
- `Trading.Core` - 核心领域逻辑
- `Trading.Models` - 数据模型
- `Trading.AI` - AI分析服务
- `Trading.Backtest.*` - 回测相关

**需要更新：**
- 所有项目引用（.csproj）
- 命名空间（namespace）
- 解决方案文件（.sln）
- 文档中的引用

#### ✅ 2. 添加统一订单执行接口

**新增接口：** `Trading.Core/Trading/IOrderExecutionService.cs`

```csharp
public interface IOrderExecutionService
{
    /// <summary>
    /// 获取当前使用的交易平台名称
    /// </summary>
    string PlatformName { get; }

    /// <summary>
    /// 下市价单
    /// </summary>
    Task<OrderResult> PlaceMarketOrder(
        string symbol,
        decimal lots,
        OrderDirection direction,
        decimal? stopLoss = null,
        decimal? takeProfit = null,
        string? comment = null);

    /// <summary>
    /// 下限价单
    /// </summary>
    Task<OrderResult> PlaceLimitOrder(
        string symbol,
        decimal lots,
        OrderDirection direction,
        decimal limitPrice,
        decimal? stopLoss = null,
        decimal? takeProfit = null,
        string? comment = null);

    /// <summary>
    /// 获取订单状态
    /// </summary>
    Task<OrderStatus> GetOrderStatus(string orderId);

    /// <summary>
    /// 修改止损止盈
    /// </summary>
    Task<bool> ModifyOrder(
        string orderId,
        decimal? newStopLoss = null,
        decimal? newTakeProfit = null);

    /// <summary>
    /// 平仓
    /// </summary>
    Task<bool> CloseOrder(string orderId, decimal? lots = null);

    /// <summary>
    /// 获取当前持仓
    /// </summary>
    Task<List<Position>> GetOpenPositions(string? symbol = null);
}

public class OrderResult
{
    public bool Success { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public decimal ExecutedPrice { get; set; }
    public decimal ExecutedLots { get; set; }
    public DateTime ExecutedTime { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
}

public enum OrderDirection { Buy, Sell }

public class OrderStatus
{
    public string OrderId { get; set; } = string.Empty;
    public OrderState State { get; set; }
    public decimal FilledLots { get; set; }
    public decimal RemainingLots { get; set; }
    public decimal? AveragePrice { get; set; }
}

public enum OrderState
{
    Pending,
    PartiallyFilled,
    Filled,
    Cancelled,
    Rejected
}

public class Position
{
    public string PositionId { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public OrderDirection Direction { get; set; }
    public decimal Lots { get; set; }
    public decimal OpenPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal? StopLoss { get; set; }
    public decimal? TakeProfit { get; set; }
    public decimal ProfitLoss { get; set; }
    public DateTime OpenTime { get; set; }
    public string? Comment { get; set; }
}
```

#### ✅ 3. 实现平台适配器

**新增适配器：** `Trading.Services/Adapters/`

**OandaOrderAdapter.cs:**
```csharp
public class OandaOrderAdapter : IOrderExecutionService
{
    private readonly IOandaService _oandaService;
    private readonly ILogger<OandaOrderAdapter> _logger;

    public string PlatformName => "Oanda";

    public OandaOrderAdapter(
        IOandaService oandaService,
        ILogger<OandaOrderAdapter> logger)
    {
        _oandaService = oandaService;
        _logger = logger;
    }

    public async Task<OrderResult> PlaceMarketOrder(
        string symbol,
        decimal lots,
        OrderDirection direction,
        decimal? stopLoss = null,
        decimal? takeProfit = null,
        string? comment = null)
    {
        try
        {
            // 转换参数格式
            var oandaSymbol = ConvertToOandaSymbol(symbol);
            var units = ConvertLotsToUnits(lots, symbol);

            // 调用Oanda API
            var result = await _oandaService.PlaceMarketOrder(
                oandaSymbol,
                units,
                direction == OrderDirection.Buy ? "buy" : "sell",
                stopLoss,
                takeProfit);

            // 转换返回格式
            return new OrderResult
            {
                Success = result.Success,
                OrderId = result.OrderId,
                ExecutedPrice = result.Price,
                ExecutedLots = lots,
                ExecutedTime = result.Time,
                Message = result.Message
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Oanda下单失败: {Symbol} {Lots} {Direction}",
                symbol, lots, direction);
            return new OrderResult
            {
                Success = false,
                Message = ex.Message,
                ErrorCode = "EXECUTION_ERROR"
            };
        }
    }

    // 其他方法实现...

    private string ConvertToOandaSymbol(string symbol)
    {
        // XAUUSD -> XAU_USD
        return symbol.Contains("_") ? symbol :
            symbol.Insert(symbol.Length - 3, "_");
    }

    private int ConvertLotsToUnits(decimal lots, string symbol)
    {
        // Oanda使用单位制，1手 = 不同的单位数
        if (symbol.StartsWith("XAU")) return (int)lots; // 黄金 1手=1单位
        return (int)(lots * 100000); // 外汇 1手=100000单位
    }
}
```

**TradeLockerOrderAdapter.cs:**
```csharp
public class TradeLockerOrderAdapter : IOrderExecutionService
{
    private readonly ITradeLockerService _tradeLockerService;
    private readonly ILogger<TradeLockerOrderAdapter> _logger;

    public string PlatformName => "TradeLocker";

    // 类似实现...
}
```

#### ✅ 4. 服务注册配置

**更新：** `Trading.Infras.Web/Program.cs`

```csharp
// 根据配置选择订单执行平台
var orderPlatform = builder.Configuration["OrderExecution:Platform"] ?? "Oanda";

if (orderPlatform.Equals("Oanda", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddScoped<IOrderExecutionService, OandaOrderAdapter>();
    _logger.LogInformation("✅ 使用 Oanda 作为订单执行平台");
}
else if (orderPlatform.Equals("TradeLocker", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddScoped<IOrderExecutionService, TradeLockerOrderAdapter>();
    _logger.LogInformation("✅ 使用 TradeLocker 作为订单执行平台");
}
else
{
    _logger.LogWarning("⚠️ 未知的订单执行平台: {Platform}，使用模拟模式", orderPlatform);
    builder.Services.AddScoped<IOrderExecutionService, MockOrderExecutionService>();
}
```

**新增配置：** `appsettings.json`

```json
{
  "OrderExecution": {
    "Platform": "Oanda",  // Oanda, TradeLocker, Mock
    "TimeoutSeconds": 30,
    "MaxRetries": 3,
    "EnableLogging": true
  }
}
```

### 重构步骤

#### 阶段1: 项目重命名（2-3小时）

1. **重命名项目文件夹和文件**
   ```bash
   git mv src/Trading.AlertSystem.Data src/Trading.Infrastructure
   git mv src/Trading.AlertSystem.Service src/Trading.Services
   git mv src/Trading.AlertSystem.Web src/Trading.Web
   ```

2. **更新项目文件（.csproj）**
   - RootNamespace
   - AssemblyName
   - 项目引用路径

3. **更新解决方案文件（.sln）**
   - 项目路径
   - 项目GUID

4. **全局替换命名空间**
   ```bash
   # 查找所有需要替换的文件
   grep -r "Trading.AlertSystem" src/ --include="*.cs"

   # 批量替换（需要小心测试）
   Trading.AlertSystem.Data → Trading.Infrastructure
   Trading.AlertSystem.Service → Trading.Services
   Trading.AlertSystem.Web → Trading.Infras.Web
   ```

5. **验证编译**
   ```bash
   dotnet clean
   dotnet restore
   dotnet build
   ```

#### 阶段2: 添加订单执行接口（3-4小时）

1. **创建接口定义**
   - `Trading.Core/Trading/IOrderExecutionService.cs`
   - 相关模型类

2. **实现适配器**
   - `OandaOrderAdapter.cs`
   - `TradeLockerOrderAdapter.cs`
   - `MockOrderExecutionService.cs`（用于测试）

3. **更新服务注册**
   - `Program.cs`
   - 配置文件

4. **编写单元测试**
   - 测试适配器转换逻辑
   - 测试错误处理

#### 阶段3: 文档更新（1-2小时）

1. **更新所有文档**
   - README.md
   - QUICKSTART.md
   - docs/*.md

2. **更新配置示例**
   - appsettings.json
   - appsettings.Development.json

3. **更新 GitHub Issues**
   - 已关闭的 Issues 中的引用

### 验收标准

**重命名部分：**
- [ ] 所有项目成功重命名
- [ ] 项目引用路径正确
- [ ] 命名空间全部更新
- [ ] 解决方案编译通过
- [ ] 所有测试通过
- [ ] 文档已更新

**订单执行接口：**
- [ ] `IOrderExecutionService` 接口定义完整
- [ ] Oanda适配器实现并测试通过
- [ ] TradeLocker适配器实现并测试通过
- [ ] 配置切换功能正常
- [ ] 错误处理完善
- [ ] 日志记录清晰
- [ ] 单元测试覆盖率 > 80%

### 技术债务清理

**顺便优化：**
- [ ] 移除未使用的依赖
- [ ] 统一日志格式
- [ ] 统一异常处理模式
- [ ] 优化配置验证

### 风险评估

**中等风险：**
- ⚠️ 大量文件重命名可能导致 Git 历史混乱
  - **缓解**：使用 `git mv` 保留历史
  - **缓解**：分多个小 commit

- ⚠️ 命名空间替换可能有遗漏
  - **缓解**：使用 IDE 的全局替换功能
  - **缓解**：编译后运行完整测试套件

**低风险：**
- 新增适配器不影响现有功能
- 可以先部署 Mock 实现进行测试

### 相关文件

**需要修改的主要文件：**
- 所有 `*.csproj` 文件
- `TradingSystem.sln`
- 所有 `.cs` 文件的命名空间
- `Program.cs`
- `appsettings.json`
- 所有 `docs/*.md` 文件

**新增文件：**
- `Trading.Core/Trading/IOrderExecutionService.cs`
- `Trading.Services/Adapters/OandaOrderAdapter.cs`
- `Trading.Services/Adapters/TradeLockerOrderAdapter.cs`
- `Trading.Services/Adapters/MockOrderExecutionService.cs`

### 标签
`refactoring`, `architecture`, `breaking-change`, `enhancement`

---

